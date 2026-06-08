//! File based data sources (CSV / JSON / Excel / Parquet).
//!
//! CSV, JSON, and Parquet are registered directly into a DataFusion
//! [`SessionContext`]. Excel has no native reader in DataFusion, so it is read
//! with `calamine` into an Arrow [`RecordBatch`] and registered as an in-memory
//! table. After registration, all file kinds share the same SQL execution path.

mod csv;
mod excel;
mod json;
mod parquet;
mod remote;

use std::{
    collections::HashSet,
    fs,
    path::{Path, PathBuf},
    time::Instant,
};

use datafusion::arrow::datatypes::{DataType, Fields};
use datafusion::arrow::json::ArrayWriter;
use datafusion::arrow::record_batch::RecordBatch;
use datafusion::prelude::SessionContext;
use serde_json::{Map, Value};
use tokio::time::{Duration, timeout};

use crate::{
    engine::types::DataSourceKind,
    error::EngineError,
    models::{ColumnInfo, DataSourceConnection, QueryResult, TableInfo},
};

pub async fn test_connection(
    ctx: &SessionContext,
    source: &DataSourceConnection,
) -> Result<(), EngineError> {
    let tables = discover(source).await?;
    for table in tables {
        ctx.sql(&format!("SELECT * FROM {} LIMIT 0", table.name))
            .await?
            .collect()
            .await?;
    }
    Ok(())
}

pub async fn list_tables(source: &DataSourceConnection) -> Result<Vec<TableInfo>, EngineError> {
    Ok(discover(source)
        .await?
        .into_iter()
        .map(|table| TableInfo {
            schema: None,
            name: table.name,
            table_type: table_type(table.kind).to_string(),
        })
        .collect())
}

pub async fn list_columns(
    ctx: &SessionContext,
    source: &DataSourceConnection,
    table: String,
) -> Result<Vec<ColumnInfo>, EngineError> {
    let table = resolve_table_name(source, table).await?;
    let df = ctx.table(table.as_str()).await?;
    Ok(columns_from_fields(df.schema().fields()))
}

pub async fn query(
    ctx: &SessionContext,
    _source: &DataSourceConnection,
    sql: &str,
    timeout_duration: Duration,
    started: Instant,
) -> Result<QueryResult, EngineError> {
    let df = ctx.sql(sql).await?;
    let columns = columns_from_fields(df.schema().fields());

    let batches = timeout(timeout_duration, df.collect())
        .await
        .map_err(|_| EngineError::Timeout)??;

    let rows = batches_to_rows(&columns, &batches)?;

    Ok(QueryResult {
        row_count: rows.len() as u64,
        columns,
        rows,
        duration_ms: started.elapsed().as_millis() as u64,
    })
}

pub async fn execute_federated_query(
    sources: &[DataSourceConnection],
    sql: &str,
    timeout_duration: Duration,
    started: Instant,
) -> Result<QueryResult, EngineError> {
    let ctx = build_federated_context(sources).await?;
    let df = ctx.sql(sql).await?;
    let columns = columns_from_fields(df.schema().fields());

    let batches = timeout(timeout_duration, df.collect())
        .await
        .map_err(|_| EngineError::Timeout)??;

    let rows = batches_to_rows(&columns, &batches)?;

    Ok(QueryResult {
        row_count: rows.len() as u64,
        columns,
        rows,
        duration_ms: started.elapsed().as_millis() as u64,
    })
}

/// Fingerprint of the files backing a source, used to invalidate cached
/// contexts when files are added, removed, or modified in the source directory.
pub async fn path_signature(source: &DataSourceConnection) -> Result<String, EngineError> {
    if remote::is_s3(source) {
        return remote::signature(source).await;
    }
    local_signature(source)
}

fn local_signature(source: &DataSourceConnection) -> Result<String, EngineError> {
    let root = PathBuf::from(source.require_path()?);
    let metadata = fs::metadata(&root)
        .map_err(|error| EngineError::FileSource(format!("read path metadata: {error}")))?;

    let mut parts: Vec<String> = Vec::new();
    if metadata.is_dir() {
        let mut entries = fs::read_dir(&root)
            .map_err(|error| EngineError::FileSource(format!("read directory: {error}")))?
            .collect::<Result<Vec<_>, _>>()
            .map_err(|error| EngineError::FileSource(format!("read directory entry: {error}")))?;
        entries.sort_by_key(|entry| entry.path());

        for entry in entries {
            let path = entry.path();
            if path.is_file() && kind_from_extension(&path).is_some() {
                parts.push(file_fingerprint(&path));
            }
        }
    } else if metadata.is_file() {
        parts.push(file_fingerprint(&root));
    }

    Ok(parts.join("|"))
}

fn file_fingerprint(path: &Path) -> String {
    let metadata = fs::metadata(path).ok();
    let len = metadata.as_ref().map(|meta| meta.len()).unwrap_or_default();
    let modified = metadata
        .as_ref()
        .and_then(|meta| meta.modified().ok())
        .and_then(|time| time.duration_since(std::time::UNIX_EPOCH).ok())
        .map(|elapsed| elapsed.as_nanos())
        .unwrap_or_default();

    format!("{}:{len}:{modified}", path.display())
}

/// Build a DataFusion context with the file registered under the logical table name.
pub async fn build_context(source: &DataSourceConnection) -> Result<SessionContext, EngineError> {
    let ctx = SessionContext::new();

    for table in discover(source).await? {
        register_table(&ctx, &table, source).await?;
    }

    Ok(ctx)
}

pub async fn build_federated_context(
    sources: &[DataSourceConnection],
) -> Result<SessionContext, EngineError> {
    if sources.is_empty() {
        return Err(EngineError::InvalidConnection(
            "at least one data source is required".to_string(),
        ));
    }

    let ctx = SessionContext::new();
    let mut registered_tables = HashSet::new();

    for source in sources {
        ensure_file_source(source)?;
        for table in discover(source).await? {
            if !registered_tables.insert(table.name.clone()) {
                return Err(EngineError::FileSource(format!(
                    "duplicate table name '{}' across data sources",
                    table.name
                )));
            }

            register_table(&ctx, &table, source).await?;
        }
    }

    Ok(ctx)
}

pub async fn register_federated_source(
    ctx: &SessionContext,
    source: &DataSourceConnection,
    registered_tables: &mut HashSet<String>,
) -> Result<(), EngineError> {
    ensure_file_source(source)?;
    let mut tables = discover(source).await?;
    let alias = explicit_alias(source);

    if alias.is_some() && tables.len() > 1 {
        return Err(EngineError::InvalidConnection(
            "file source alias can only be used with a single discovered table".to_string(),
        ));
    }

    for table in &mut tables {
        if let Some(alias) = alias.clone() {
            table.name = alias;
        }

        if !registered_tables.insert(table.name.clone()) {
            return Err(EngineError::FileSource(format!(
                "duplicate federated table name '{}'",
                table.name
            )));
        }

        register_table(ctx, table, source).await?;
    }

    Ok(())
}

pub async fn register_cached_federated_source(
    ctx: &SessionContext,
    source_ctx: &SessionContext,
    source: &DataSourceConnection,
    registered_tables: &mut HashSet<String>,
) -> Result<(), EngineError> {
    ensure_file_source(source)?;
    ensure_remote_store(ctx, source)?;
    let mut tables = discover(source).await?;
    let alias = explicit_alias(source);

    if alias.is_some() && tables.len() > 1 {
        return Err(EngineError::InvalidConnection(
            "file source alias can only be used with a single discovered table".to_string(),
        ));
    }

    for table in &mut tables {
        let source_table_name = table.name.clone();
        if let Some(alias) = alias.clone() {
            table.name = alias;
        }

        if !registered_tables.insert(table.name.clone()) {
            return Err(EngineError::FileSource(format!(
                "duplicate federated table name '{}'",
                table.name
            )));
        }

        let provider = source_ctx
            .table_provider(source_table_name.as_str())
            .await?;
        ctx.register_table(table.name.as_str(), provider)?;
    }

    Ok(())
}

fn ensure_file_source(source: &DataSourceConnection) -> Result<(), EngineError> {
    match source.kind()? {
        DataSourceKind::Csv
        | DataSourceKind::Json
        | DataSourceKind::Excel
        | DataSourceKind::Parquet
        | DataSourceKind::Files => Ok(()),
        DataSourceKind::Postgres
        | DataSourceKind::MySql
        | DataSourceKind::ClickHouse
        | DataSourceKind::MongoDb
        | DataSourceKind::Sqlite => Err(EngineError::UnsupportedDataSource),
    }
}

async fn register_table(
    ctx: &SessionContext,
    table: &FileTable,
    source: &DataSourceConnection,
) -> Result<(), EngineError> {
    let name = table.name.as_str();
    match &table.location {
        Location::Local(path) => match table.kind {
            DataSourceKind::Csv => csv::register_local(ctx, name, path, source).await,
            DataSourceKind::Json => json::register(ctx, name, &path_string(path)).await,
            DataSourceKind::Excel => excel::register_local(ctx, name, path, source),
            DataSourceKind::Parquet => parquet::register(ctx, name, &path_string(path)).await,
            DataSourceKind::Postgres
            | DataSourceKind::MySql
            | DataSourceKind::ClickHouse
            | DataSourceKind::MongoDb
            | DataSourceKind::Sqlite
            | DataSourceKind::Files => Err(EngineError::UnsupportedDataSource),
        },
        Location::Remote(object) => {
            ctx.runtime_env()
                .register_object_store(&object.base_url, object.store.clone());
            match table.kind {
                DataSourceKind::Csv => {
                    let bytes = remote::read_object(&object.store, &object.object_path).await?;
                    csv::register_remote(ctx, name, &object.url, &bytes, source).await
                }
                DataSourceKind::Json => json::register(ctx, name, &object.url).await,
                DataSourceKind::Parquet => parquet::register(ctx, name, &object.url).await,
                DataSourceKind::Excel => {
                    let bytes = remote::read_object(&object.store, &object.object_path).await?;
                    excel::register_bytes(ctx, name, bytes, source)
                }
                DataSourceKind::Postgres
                | DataSourceKind::MySql
                | DataSourceKind::ClickHouse
                | DataSourceKind::MongoDb
                | DataSourceKind::Sqlite
                | DataSourceKind::Files => Err(EngineError::UnsupportedDataSource),
            }
        }
    }
}

/// Register the object store for an S3-backed source onto the given context.
/// No-op for local filesystem sources.
fn ensure_remote_store(
    ctx: &SessionContext,
    source: &DataSourceConnection,
) -> Result<(), EngineError> {
    if remote::is_s3(source) {
        let (store, base_url) = remote::store_for(source)?;
        ctx.runtime_env().register_object_store(&base_url, store);
    }
    Ok(())
}

fn path_string(path: &Path) -> String {
    path.to_string_lossy().into_owned()
}

#[derive(Clone)]
struct FileTable {
    name: String,
    kind: DataSourceKind,
    location: Location,
}

#[derive(Clone)]
enum Location {
    Local(PathBuf),
    Remote(remote::RemoteObject),
}

/// Dispatch discovery to the local filesystem or S3 backend.
async fn discover(source: &DataSourceConnection) -> Result<Vec<FileTable>, EngineError> {
    if remote::is_s3(source) {
        remote::discover(source, source.kind()?).await
    } else {
        discover_local(source)
    }
}

fn discover_local(source: &DataSourceConnection) -> Result<Vec<FileTable>, EngineError> {
    let requested_kind = source.kind()?;
    let root = PathBuf::from(source.require_path()?);
    let metadata = fs::metadata(&root)
        .map_err(|error| EngineError::FileSource(format!("read path metadata: {error}")))?;

    let tables = if metadata.is_dir() {
        discover_directory_tables(&root, requested_kind)?
    } else if metadata.is_file() {
        vec![discover_file_table(
            &root,
            requested_kind,
            explicit_table_name(source),
        )?]
    } else {
        return Err(EngineError::FileSource(
            "path is neither a file nor a directory".to_string(),
        ));
    };

    if tables.is_empty() {
        return Err(EngineError::FileSource(
            "no supported files found for this data source".to_string(),
        ));
    }

    ensure_unique_table_names(&tables)?;
    Ok(tables)
}

fn discover_directory_tables(
    root: &Path,
    requested_kind: DataSourceKind,
) -> Result<Vec<FileTable>, EngineError> {
    let mut entries = fs::read_dir(root)
        .map_err(|error| EngineError::FileSource(format!("read directory: {error}")))?
        .collect::<Result<Vec<_>, _>>()
        .map_err(|error| EngineError::FileSource(format!("read directory entry: {error}")))?;

    entries.sort_by_key(|entry| entry.path());

    let mut tables = Vec::new();
    for entry in entries {
        let path = entry.path();
        if !path.is_file() {
            continue;
        }

        let Some(kind) = kind_from_extension(&path) else {
            continue;
        };

        if kind_matches(requested_kind, kind) {
            tables.push(FileTable {
                name: table_name_from_path(&path),
                kind,
                location: Location::Local(path),
            });
        }
    }

    Ok(tables)
}

fn discover_file_table(
    path: &Path,
    requested_kind: DataSourceKind,
    explicit_name: Option<String>,
) -> Result<FileTable, EngineError> {
    let kind = match requested_kind {
        DataSourceKind::Files => kind_from_extension(path)
            .ok_or_else(|| EngineError::FileSource("unsupported file extension".to_string()))?,
        DataSourceKind::Csv
        | DataSourceKind::Json
        | DataSourceKind::Excel
        | DataSourceKind::Parquet => {
            let actual = kind_from_extension(path)
                .ok_or_else(|| EngineError::FileSource("unsupported file extension".to_string()))?;
            if actual != requested_kind {
                return Err(EngineError::FileSource(format!(
                    "file extension does not match source type: expected {}, found {}",
                    table_type(requested_kind),
                    table_type(actual)
                )));
            }
            requested_kind
        }
        DataSourceKind::Postgres
        | DataSourceKind::MySql
        | DataSourceKind::ClickHouse
        | DataSourceKind::MongoDb
        | DataSourceKind::Sqlite => {
            return Err(EngineError::UnsupportedDataSource);
        }
    };

    Ok(FileTable {
        name: explicit_name.unwrap_or_else(|| table_name_from_path(path)),
        kind,
        location: Location::Local(path.to_path_buf()),
    })
}

async fn resolve_table_name(
    source: &DataSourceConnection,
    requested_table: String,
) -> Result<String, EngineError> {
    let requested_table = requested_table.trim();
    let tables = discover(source).await?;

    if requested_table.is_empty() {
        return Ok(tables
            .first()
            .map(|table| table.name.clone())
            .expect("discover returns at least one table"));
    }

    if tables.iter().any(|table| table.name == requested_table) {
        return Ok(requested_table.to_string());
    }

    Err(EngineError::InvalidConnection(format!(
        "unknown file table '{requested_table}'"
    )))
}

fn explicit_table_name(source: &DataSourceConnection) -> Option<String> {
    source
        .table
        .as_deref()
        .map(str::trim)
        .filter(|name| !name.is_empty())
        .map(sanitize_table_name)
}

fn explicit_alias(source: &DataSourceConnection) -> Option<String> {
    source
        .alias
        .as_deref()
        .map(str::trim)
        .filter(|name| !name.is_empty())
        .map(sanitize_table_name)
}

fn kind_matches(requested: DataSourceKind, actual: DataSourceKind) -> bool {
    requested == DataSourceKind::Files || requested == actual
}

fn kind_from_extension(path: &Path) -> Option<DataSourceKind> {
    match path
        .extension()?
        .to_string_lossy()
        .to_ascii_lowercase()
        .as_str()
    {
        "csv" => Some(DataSourceKind::Csv),
        "json" | "jsonl" | "ndjson" => Some(DataSourceKind::Json),
        "xlsx" | "xls" => Some(DataSourceKind::Excel),
        "parquet" | "pq" => Some(DataSourceKind::Parquet),
        _ => None,
    }
}

fn table_type(kind: DataSourceKind) -> &'static str {
    match kind {
        DataSourceKind::Csv => "CSV",
        DataSourceKind::Json => "JSON",
        DataSourceKind::Excel => "EXCEL",
        DataSourceKind::Parquet => "PARQUET",
        DataSourceKind::Files => "FILE",
        DataSourceKind::Postgres => "POSTGRES",
        DataSourceKind::MySql => "MYSQL",
        DataSourceKind::ClickHouse => "CLICKHOUSE",
        DataSourceKind::MongoDb => "MONGODB",
        DataSourceKind::Sqlite => "SQLITE",
    }
}

fn table_name_from_path(path: &Path) -> String {
    let raw = path
        .file_stem()
        .and_then(|stem| stem.to_str())
        .unwrap_or("data");
    sanitize_table_name(raw)
}

fn sanitize_table_name(raw: &str) -> String {
    let mut name = String::new();
    for character in raw.chars() {
        if character.is_ascii_alphanumeric() || character == '_' {
            name.push(character);
        } else {
            name.push('_');
        }
    }

    let name = name.trim_matches('_').to_string();
    if name.is_empty() {
        return "data".to_string();
    }

    if name
        .chars()
        .next()
        .is_some_and(|character| character.is_ascii_digit())
    {
        format!("t_{name}")
    } else {
        name
    }
}

fn ensure_unique_table_names(tables: &[FileTable]) -> Result<(), EngineError> {
    let mut names = HashSet::new();
    for table in tables {
        if !names.insert(table.name.clone()) {
            return Err(EngineError::FileSource(format!(
                "duplicate table name '{}' after sanitizing file names",
                table.name
            )));
        }
    }
    Ok(())
}

pub(crate) fn columns_from_fields(fields: &Fields) -> Vec<ColumnInfo> {
    fields
        .iter()
        .map(|field| ColumnInfo {
            name: field.name().clone(),
            column_type: field.data_type().to_string(),
            nullable: field.is_nullable(),
            precision: decimal_precision(field.data_type()),
            scale: decimal_scale(field.data_type()),
        })
        .collect()
}

fn decimal_precision(data_type: &DataType) -> Option<u8> {
    match data_type {
        DataType::Decimal128(precision, _) | DataType::Decimal256(precision, _) => Some(*precision),
        _ => None,
    }
}

fn decimal_scale(data_type: &DataType) -> Option<i8> {
    match data_type {
        DataType::Decimal128(_, scale) | DataType::Decimal256(_, scale) => Some(*scale),
        _ => None,
    }
}

pub(crate) fn batches_to_rows(
    columns: &[ColumnInfo],
    batches: &[RecordBatch],
) -> Result<Vec<Vec<Value>>, EngineError> {
    if batches.is_empty() {
        return Ok(Vec::new());
    }

    let mut buffer = Vec::new();
    {
        let mut writer = ArrayWriter::new(&mut buffer);
        for batch in batches {
            writer
                .write(batch)
                .map_err(|error| EngineError::FileSource(format!("encode rows: {error}")))?;
        }
        writer
            .finish()
            .map_err(|error| EngineError::FileSource(format!("encode rows: {error}")))?;
    }

    if buffer.is_empty() {
        return Ok(Vec::new());
    }

    let objects: Vec<Map<String, Value>> = serde_json::from_slice(&buffer)
        .map_err(|error| EngineError::FileSource(format!("decode rows: {error}")))?;

    Ok(objects
        .into_iter()
        .map(|object| {
            columns
                .iter()
                .map(|column| object.get(&column.name).cloned().unwrap_or(Value::Null))
                .collect()
        })
        .collect())
}
