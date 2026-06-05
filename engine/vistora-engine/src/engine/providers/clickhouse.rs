use std::{sync::Arc, time::Instant};

use datafusion::sql::TableReference;
use datafusion::{datasource::TableProvider, prelude::SessionContext};
use datafusion_table_providers::{
    clickhouse::ClickHouseTableFactory,
    sql::db_connection_pool::{
        DbConnectionPool,
        clickhousepool::ClickHouseConnectionPool,
        dbconnection::{get_schema, get_tables},
    },
};
use tokio::time::{Duration, timeout};

use crate::{
    engine::files::{batches_to_rows, columns_from_fields},
    error::EngineError,
    models::{ColumnInfo, DataSourceConnection, QueryResult, TableInfo},
};

pub async fn test_connection(_pool: &Arc<ClickHouseConnectionPool>) -> Result<(), EngineError> {
    // Pool construction already runs SELECT 1 through the provider client.
    Ok(())
}

pub async fn list_tables(
    pool: &Arc<ClickHouseConnectionPool>,
    source: &DataSourceConnection,
) -> Result<Vec<TableInfo>, EngineError> {
    let database = database_name(source)?;
    let tables = get_tables(pool.connect().await.map_err(provider_error)?, database)
        .await
        .map_err(provider_error)?;

    Ok(tables
        .into_iter()
        .map(|table| TableInfo {
            schema: Some(database.to_string()),
            name: table,
            table_type: "BASE TABLE".to_string(),
        })
        .collect())
}

pub async fn list_columns(
    pool: &Arc<ClickHouseConnectionPool>,
    source: &DataSourceConnection,
    schema: Option<String>,
    table: String,
) -> Result<Vec<ColumnInfo>, EngineError> {
    let schema = schema
        .as_deref()
        .map(str::trim)
        .filter(|schema| !schema.is_empty())
        .unwrap_or(database_name(source)?);
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
    pool: &Arc<ClickHouseConnectionPool>,
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
    pool: Arc<ClickHouseConnectionPool>,
    source: &DataSourceConnection,
    table: &str,
    alias: &str,
) -> Result<(), EngineError> {
    let provider = table_provider(pool, source, table).await?;
    ctx.register_table(alias, provider)?;
    Ok(())
}

pub async fn build_context(
    pool: &Arc<ClickHouseConnectionPool>,
    source: &DataSourceConnection,
) -> Result<SessionContext, EngineError> {
    let ctx = SessionContext::new();
    let database = database_name(source)?;
    let tables = get_tables(pool.connect().await.map_err(provider_error)?, database)
        .await
        .map_err(provider_error)?;

    for table in tables {
        register_table(&ctx, pool.clone(), source, table.as_str(), table.as_str()).await?;
    }

    Ok(ctx)
}

async fn table_provider(
    pool: Arc<ClickHouseConnectionPool>,
    source: &DataSourceConnection,
    table: &str,
) -> Result<Arc<dyn TableProvider + 'static>, EngineError> {
    let factory = ClickHouseTableFactory::new(pool);
    let database = database_name(source)?;

    factory
        .table_provider(TableReference::partial(database, table), None)
        .await
        .map_err(|error| EngineError::provider_error("clickhouse table provider", error))
}

fn database_name(source: &DataSourceConnection) -> Result<&str, EngineError> {
    source
        .schema
        .as_deref()
        .map(str::trim)
        .filter(|value| !value.is_empty())
        .or_else(|| {
            source
                .database
                .as_deref()
                .map(str::trim)
                .filter(|value| !value.is_empty())
        })
        .ok_or_else(|| EngineError::InvalidConnection("missing database".to_string()))
}

fn provider_error(error: impl std::fmt::Display) -> EngineError {
    EngineError::provider_error("clickhouse provider", error)
}
