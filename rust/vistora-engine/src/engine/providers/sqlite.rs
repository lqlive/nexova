use std::{sync::Arc, time::Instant};

use datafusion::sql::TableReference;
use datafusion::{datasource::TableProvider, prelude::SessionContext};
use datafusion_table_providers::{
    sql::db_connection_pool::{
        DbConnectionPool, Mode,
        dbconnection::{get_schema, get_tables},
        sqlitepool::{SqliteConnectionPool, SqliteConnectionPoolFactory},
    },
    sqlite::SqliteTableFactory,
};
use tokio::time::{Duration, timeout};

use crate::{
    engine::files::{batches_to_rows, columns_from_fields},
    error::EngineError,
    models::{ColumnInfo, DataSourceConnection, QueryResult, TableInfo},
};

const DEFAULT_SCHEMA: &str = "main";
const DEFAULT_BUSY_TIMEOUT_MS: u64 = 5_000;

pub async fn create_pool(
    source: &DataSourceConnection,
) -> Result<Arc<SqliteConnectionPool>, EngineError> {
    let path = sqlite_path(source)?;
    let pool = SqliteConnectionPoolFactory::new(
        path,
        Mode::File,
        std::time::Duration::from_millis(DEFAULT_BUSY_TIMEOUT_MS),
    )
    .build()
    .await
    .map_err(|error| EngineError::InvalidConnection(format!("sqlite pool: {error}")))?;

    Ok(Arc::new(pool))
}

pub async fn test_connection(_pool: &Arc<SqliteConnectionPool>) -> Result<(), EngineError> {
    // Pool construction opens and configures the SQLite database file.
    Ok(())
}

pub async fn list_tables(pool: &Arc<SqliteConnectionPool>) -> Result<Vec<TableInfo>, EngineError> {
    let tables = get_tables(
        pool.connect().await.map_err(provider_error)?,
        DEFAULT_SCHEMA,
    )
    .await
    .map_err(provider_error)?;

    Ok(tables
        .into_iter()
        .map(|table| TableInfo {
            schema: Some(DEFAULT_SCHEMA.to_string()),
            name: table,
            table_type: "BASE TABLE".to_string(),
        })
        .collect())
}

pub async fn list_columns(
    pool: &Arc<SqliteConnectionPool>,
    _source: &DataSourceConnection,
    schema: Option<String>,
    table: String,
) -> Result<Vec<ColumnInfo>, EngineError> {
    let schema = schema
        .as_deref()
        .map(str::trim)
        .filter(|schema| !schema.is_empty())
        .unwrap_or(DEFAULT_SCHEMA);
    let table_reference = TableReference::partial(schema, table.as_str());
    let schema = get_schema(
        pool.connect().await.map_err(provider_error)?,
        &table_reference,
    )
    .await
    .map_err(provider_error)?;

    Ok(columns_from_fields(schema.fields()))
}

pub async fn query(
    pool: &Arc<SqliteConnectionPool>,
    source: &DataSourceConnection,
    sql: &str,
    timeout_duration: Duration,
    started: Instant,
) -> Result<QueryResult, EngineError> {
    let ctx = build_context(pool, source).await?;
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

pub async fn register_table(
    ctx: &SessionContext,
    pool: Arc<SqliteConnectionPool>,
    table: &str,
    alias: &str,
) -> Result<(), EngineError> {
    let provider = table_provider(pool, table).await?;
    ctx.register_table(alias, provider)?;
    Ok(())
}

pub async fn build_context(
    pool: &Arc<SqliteConnectionPool>,
    _source: &DataSourceConnection,
) -> Result<SessionContext, EngineError> {
    let ctx = SessionContext::new();
    let tables = get_tables(
        pool.connect().await.map_err(provider_error)?,
        DEFAULT_SCHEMA,
    )
    .await
    .map_err(provider_error)?;

    for table in tables {
        register_table(&ctx, pool.clone(), table.as_str(), table.as_str()).await?;
    }

    Ok(ctx)
}

async fn table_provider(
    pool: Arc<SqliteConnectionPool>,
    table: &str,
) -> Result<Arc<dyn TableProvider + 'static>, EngineError> {
    let factory = SqliteTableFactory::new(pool);
    factory
        .table_provider(TableReference::bare(table))
        .await
        .map_err(|error| EngineError::provider_error("sqlite table provider", error))
}

fn sqlite_path(source: &DataSourceConnection) -> Result<&str, EngineError> {
    source
        .path
        .as_deref()
        .or(source.connection_string.as_deref())
        .map(str::trim)
        .filter(|value| !value.is_empty())
        .ok_or_else(|| EngineError::InvalidConnection("missing sqlite path".to_string()))
}

fn provider_error(error: impl std::fmt::Display) -> EngineError {
    EngineError::provider_error("sqlite provider", error)
}
