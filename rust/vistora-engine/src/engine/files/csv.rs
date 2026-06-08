use std::{
    fs::File,
    io::{BufRead, BufReader, Cursor},
    path::Path,
};

use datafusion::arrow::datatypes::{DataType, Field, Schema, TimeUnit};
use datafusion::prelude::{CsvReadOptions, SessionContext};

use crate::{
    error::EngineError,
    models::{ColumnSchemaOverride, DataSourceConnection},
};

const INFER_MAX_ROWS: usize = 1_000;

pub async fn register_local(
    ctx: &SessionContext,
    table: &str,
    path: &Path,
    source: &DataSourceConnection,
) -> Result<(), EngineError> {
    let schema = if let Some(schema_override) = source.schema_override.as_ref() {
        schema_from_override(schema_override)?
    } else {
        let file = File::open(path).map_err(|error| {
            EngineError::FileSource(format!("open csv for schema inference: {error}"))
        })?;
        infer_schema(BufReader::new(file), source)?
    };

    register_csv(ctx, table, &path_string(path), schema, source).await
}

pub async fn register_remote(
    ctx: &SessionContext,
    table: &str,
    url: &str,
    bytes: &[u8],
    source: &DataSourceConnection,
) -> Result<(), EngineError> {
    let schema = if let Some(schema_override) = source.schema_override.as_ref() {
        schema_from_override(schema_override)?
    } else {
        infer_schema(BufReader::new(Cursor::new(bytes)), source)?
    };

    register_csv(ctx, table, url, schema, source).await
}

async fn register_csv(
    ctx: &SessionContext,
    table: &str,
    target: &str,
    schema: Schema,
    source: &DataSourceConnection,
) -> Result<(), EngineError> {
    let mut options = CsvReadOptions::new().has_header(source.has_header.unwrap_or(true));

    if let Some(delimiter) = source
        .delimiter
        .as_deref()
        .and_then(|value| value.as_bytes().first().copied())
    {
        options = options.delimiter(delimiter);
    }

    options = options.schema(&schema);

    ctx.register_csv(table, target, options).await?;
    Ok(())
}

fn schema_from_override(columns: &[ColumnSchemaOverride]) -> Result<Schema, EngineError> {
    if columns.is_empty() {
        return Err(EngineError::InvalidConnection(
            "schemaOverride must contain at least one column".to_string(),
        ));
    }

    let fields = columns
        .iter()
        .map(|column| {
            Ok(Field::new(
                column.name.as_str(),
                data_type_from_override(column)?,
                column.nullable.unwrap_or(true),
            ))
        })
        .collect::<Result<Vec<_>, EngineError>>()?;

    Ok(Schema::new(fields))
}

fn data_type_from_override(column: &ColumnSchemaOverride) -> Result<DataType, EngineError> {
    let column_type = column.column_type.trim().to_ascii_lowercase();
    match column_type.as_str() {
        "string" | "str" | "utf8" | "text" => Ok(DataType::Utf8),
        "bool" | "boolean" => Ok(DataType::Boolean),
        "int" | "integer" | "int64" | "long" => Ok(DataType::Int64),
        "float" | "float64" | "double" => Ok(DataType::Float64),
        "date" | "date32" => Ok(DataType::Date32),
        "timestamp" | "datetime" => Ok(DataType::Timestamp(TimeUnit::Nanosecond, None)),
        "decimal" | "decimal128" => Ok(DataType::Decimal128(
            column.precision.unwrap_or(18),
            column.scale.unwrap_or(2),
        )),
        _ => Err(EngineError::InvalidConnection(format!(
            "unsupported schemaOverride type '{}' for column '{}'",
            column.column_type, column.name
        ))),
    }
}

fn infer_schema(
    reader: impl BufRead,
    source: &DataSourceConnection,
) -> Result<Schema, EngineError> {
    let delimiter = delimiter(source);
    let has_header = source.has_header.unwrap_or(true);
    let mut lines = reader.lines();

    let Some(first_line) = lines.next() else {
        return Err(EngineError::FileSource("csv file is empty".to_string()));
    };
    let first_line =
        first_line.map_err(|error| EngineError::FileSource(format!("read csv header: {error}")))?;
    let first_values = split_csv_line(&first_line, delimiter);
    let headers = if has_header {
        first_values
    } else {
        (1..=first_values.len())
            .map(|index| format!("column_{index}"))
            .collect()
    };

    let mut columns = vec![ColumnInference::default(); headers.len()];
    if !has_header {
        apply_row(&mut columns, &split_csv_line(&first_line, delimiter));
    }

    for line in lines.take(INFER_MAX_ROWS) {
        let line =
            line.map_err(|error| EngineError::FileSource(format!("read csv row: {error}")))?;
        apply_row(&mut columns, &split_csv_line(&line, delimiter));
    }

    let fields = headers
        .into_iter()
        .enumerate()
        .map(|(index, name)| {
            let inference = columns.get(index).cloned().unwrap_or_default();
            Field::new(name, inference.data_type(), inference.nullable)
        })
        .collect::<Vec<_>>();

    Ok(Schema::new(fields))
}

#[derive(Debug, Clone)]
struct ColumnInference {
    nullable: bool,
    has_value: bool,
    bool_candidate: bool,
    int_candidate: bool,
    decimal_candidate: bool,
    float_candidate: bool,
    date_candidate: bool,
    timestamp_candidate: bool,
    max_precision: u8,
    max_scale: i8,
}

impl Default for ColumnInference {
    fn default() -> Self {
        Self {
            nullable: false,
            has_value: false,
            bool_candidate: true,
            int_candidate: true,
            decimal_candidate: true,
            float_candidate: true,
            date_candidate: true,
            timestamp_candidate: true,
            max_precision: 1,
            max_scale: 0,
        }
    }
}

impl ColumnInference {
    fn observe(&mut self, value: &str) {
        let value = value.trim();
        if value.is_empty() {
            self.nullable = true;
            return;
        }

        self.has_value = true;
        self.bool_candidate &= is_bool(value);
        self.int_candidate &= is_int(value);
        self.float_candidate &= value.parse::<f64>().is_ok();
        self.date_candidate &= is_date(value);
        self.timestamp_candidate &= is_timestamp(value);

        if let Some((precision, scale)) = decimal_shape(value) {
            self.max_precision = self.max_precision.max(precision);
            self.max_scale = self.max_scale.max(scale);
        } else {
            self.decimal_candidate = false;
        }
    }

    fn data_type(&self) -> DataType {
        if !self.has_value {
            return DataType::Utf8;
        }

        if self.bool_candidate {
            DataType::Boolean
        } else if self.int_candidate {
            DataType::Int64
        } else if self.decimal_candidate && self.max_scale > 0 && self.max_precision <= 38 {
            DataType::Decimal128(self.max_precision, self.max_scale)
        } else if self.float_candidate {
            DataType::Float64
        } else if self.timestamp_candidate {
            DataType::Timestamp(TimeUnit::Nanosecond, None)
        } else if self.date_candidate {
            DataType::Date32
        } else {
            DataType::Utf8
        }
    }
}

fn apply_row(columns: &mut [ColumnInference], values: &[String]) {
    for (index, column) in columns.iter_mut().enumerate() {
        column.observe(values.get(index).map(String::as_str).unwrap_or_default());
    }
}

fn delimiter(source: &DataSourceConnection) -> u8 {
    source
        .delimiter
        .as_deref()
        .and_then(|value| value.as_bytes().first().copied())
        .unwrap_or(b',')
}

fn split_csv_line(line: &str, delimiter: u8) -> Vec<String> {
    let delimiter = delimiter as char;
    let mut values = Vec::new();
    let mut current = String::new();
    let mut chars = line.chars().peekable();
    let mut in_quotes = false;

    while let Some(ch) = chars.next() {
        match ch {
            '"' if in_quotes && chars.peek() == Some(&'"') => {
                current.push('"');
                chars.next();
            }
            '"' => in_quotes = !in_quotes,
            value if value == delimiter && !in_quotes => {
                values.push(current.trim().to_string());
                current.clear();
            }
            value => current.push(value),
        }
    }

    values.push(current.trim().to_string());
    values
}

fn is_bool(value: &str) -> bool {
    matches!(value.to_ascii_lowercase().as_str(), "true" | "false")
}

fn is_int(value: &str) -> bool {
    value.parse::<i64>().is_ok()
}

fn is_date(value: &str) -> bool {
    let bytes = value.as_bytes();
    bytes.len() == 10
        && bytes[4] == b'-'
        && bytes[7] == b'-'
        && bytes[..4].iter().all(u8::is_ascii_digit)
        && bytes[5..7].iter().all(u8::is_ascii_digit)
        && bytes[8..10].iter().all(u8::is_ascii_digit)
}

fn is_timestamp(value: &str) -> bool {
    if value.len() < 19 {
        return false;
    }

    let separator = value.as_bytes()[10];
    is_date(&value[..10]) && matches!(separator, b'T' | b' ') && is_time_prefix(&value[11..])
}

fn is_time_prefix(value: &str) -> bool {
    let bytes = value.as_bytes();
    bytes.len() >= 8
        && bytes[2] == b':'
        && bytes[5] == b':'
        && bytes[..2].iter().all(u8::is_ascii_digit)
        && bytes[3..5].iter().all(u8::is_ascii_digit)
        && bytes[6..8].iter().all(u8::is_ascii_digit)
}

fn decimal_shape(value: &str) -> Option<(u8, i8)> {
    let unsigned = value.strip_prefix(['+', '-']).unwrap_or(value);
    if unsigned.is_empty() || unsigned.contains(['e', 'E']) {
        return None;
    }

    let mut parts = unsigned.split('.');
    let whole = parts.next().unwrap_or_default();
    let fraction = parts.next();
    if parts.next().is_some() {
        return None;
    }

    if whole.is_empty() && fraction.is_none_or(str::is_empty) {
        return None;
    }

    if !whole.chars().all(|ch| ch.is_ascii_digit())
        || !fraction
            .unwrap_or_default()
            .chars()
            .all(|ch| ch.is_ascii_digit())
    {
        return None;
    }

    let precision = whole.len() + fraction.unwrap_or_default().len();
    if precision == 0 || precision > u8::MAX as usize {
        return None;
    }

    let scale = fraction.unwrap_or_default().len();
    if scale > i8::MAX as usize {
        return None;
    }

    Some((precision as u8, scale as i8))
}

fn path_string(path: &Path) -> String {
    path.to_string_lossy().into_owned()
}
