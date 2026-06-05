use std::path::Path;
use std::sync::Arc;

use calamine::{Data, Reader, open_workbook_auto};
use datafusion::arrow::array::{ArrayRef, Float64Array, StringArray};
use datafusion::arrow::datatypes::{DataType, Field, Schema};
use datafusion::arrow::record_batch::RecordBatch;
use datafusion::prelude::SessionContext;

use crate::{error::EngineError, models::DataSourceConnection};

pub fn register(
    ctx: &SessionContext,
    table: &str,
    path: &Path,
    source: &DataSourceConnection,
) -> Result<(), EngineError> {
    let batch = read_excel(path, source)?;
    ctx.register_batch(table, batch)?;
    Ok(())
}

/// Read the first (or named) sheet of an Excel workbook into an Arrow record batch.
///
/// Each column is inferred as `Float64` when every non-empty cell is numeric,
/// otherwise it falls back to `Utf8`.
fn read_excel(path: &Path, source: &DataSourceConnection) -> Result<RecordBatch, EngineError> {
    let mut workbook = open_workbook_auto(path)
        .map_err(|error| EngineError::FileSource(format!("open excel: {error}")))?;

    let sheet_name = match &source.sheet {
        Some(sheet) => sheet.clone(),
        None => workbook
            .sheet_names()
            .first()
            .cloned()
            .ok_or_else(|| EngineError::FileSource("workbook has no sheets".to_string()))?,
    };

    let range = workbook
        .worksheet_range(&sheet_name)
        .map_err(|error| EngineError::FileSource(format!("read sheet: {error}")))?;

    let width = range.width();
    if width == 0 {
        return Err(EngineError::FileSource("sheet is empty".to_string()));
    }

    let has_header = source.has_header.unwrap_or(true);
    let mut row_iter = range.rows();

    let headers: Vec<String> = if has_header {
        match row_iter.next() {
            Some(first) => (0..width)
                .map(|index| {
                    first
                        .get(index)
                        .map(cell_to_string)
                        .filter(|value| !value.trim().is_empty())
                        .unwrap_or_else(|| format!("column_{}", index + 1))
                })
                .collect(),
            None => default_headers(width),
        }
    } else {
        default_headers(width)
    };

    let data: Vec<&[Data]> = row_iter.collect();

    let mut arrays: Vec<ArrayRef> = Vec::with_capacity(width);
    let mut fields: Vec<Field> = Vec::with_capacity(width);

    for column in 0..width {
        let cells: Vec<Option<&Data>> = data.iter().map(|row| row.get(column)).collect();

        let numeric = cells
            .iter()
            .all(|cell| cell.is_none_or(|data| cell_is_blank(data) || cell_to_f64(data).is_some()))
            && cells
                .iter()
                .any(|cell| cell.is_some_and(|data| cell_to_f64(data).is_some()));

        if numeric {
            let values: Vec<Option<f64>> = cells
                .iter()
                .map(|cell| cell.and_then(|data| cell_to_f64(data)))
                .collect();
            arrays.push(Arc::new(Float64Array::from(values)) as ArrayRef);
            fields.push(Field::new(
                headers[column].as_str(),
                DataType::Float64,
                true,
            ));
        } else {
            let values: Vec<Option<String>> = cells
                .iter()
                .map(|cell| match *cell {
                    Some(data) if !cell_is_blank(data) => Some(cell_to_string(data)),
                    _ => None,
                })
                .collect();
            arrays.push(Arc::new(StringArray::from(values)) as ArrayRef);
            fields.push(Field::new(headers[column].as_str(), DataType::Utf8, true));
        }
    }

    let schema = Arc::new(Schema::new(fields));
    RecordBatch::try_new(schema, arrays)
        .map_err(|error| EngineError::FileSource(format!("build batch: {error}")))
}

fn default_headers(width: usize) -> Vec<String> {
    (0..width)
        .map(|index| format!("column_{}", index + 1))
        .collect()
}

fn cell_is_blank(cell: &Data) -> bool {
    matches!(cell, Data::Empty) || matches!(cell, Data::String(value) if value.trim().is_empty())
}

fn cell_to_f64(cell: &Data) -> Option<f64> {
    match cell {
        Data::Float(value) => Some(*value),
        Data::Int(value) => Some(*value as f64),
        Data::Bool(value) => Some(if *value { 1.0 } else { 0.0 }),
        Data::String(value) => value.trim().parse::<f64>().ok(),
        _ => None,
    }
}

fn cell_to_string(cell: &Data) -> String {
    match cell {
        Data::Empty => String::new(),
        Data::String(value) => value.clone(),
        Data::Float(value) => value.to_string(),
        Data::Int(value) => value.to_string(),
        Data::Bool(value) => value.to_string(),
        Data::DateTimeIso(value) => value.clone(),
        Data::DurationIso(value) => value.clone(),
        other => other.to_string(),
    }
}
