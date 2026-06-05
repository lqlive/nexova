use std::{collections::HashMap, sync::Arc, time::Instant};

use datafusion::sql::TableReference;
use datafusion::{datasource::TableProvider, prelude::SessionContext};
use datafusion_table_providers::{
    mongodb::{MongoDBTableFactory, connection_pool::MongoDBConnectionPool},
    util::secrets::to_secret_map,
};
use tokio::time::{Duration, timeout};

use crate::{
    engine::files::{batches_to_rows, columns_from_fields},
    error::EngineError,
    models::{ColumnInfo, DataSourceConnection, QueryResult, TableInfo},
};

pub async fn create_pool(
    source: &DataSourceConnection,
) -> Result<Arc<MongoDBConnectionPool>, EngineError> {
    let mut params = HashMap::new();

    if source
        .connection_string
        .as_deref()
        .map(str::trim)
        .is_some_and(|value| !value.is_empty())
    {
        params.insert("connection_string".to_string(), source.connection_string()?);
    } else {
        params.insert(
            "host".to_string(),
            required(source.host.as_deref(), "host")?.to_string(),
        );
        params.insert(
            "port".to_string(),
            source
                .port
                .ok_or_else(|| EngineError::InvalidConnection("missing port".to_string()))?
                .to_string(),
        );
        params.insert(
            "db".to_string(),
            required(source.database.as_deref(), "database")?.to_string(),
        );
        params.insert(
            "user".to_string(),
            required(source.username.as_deref(), "username")?.to_string(),
        );
        params.insert(
            "pass".to_string(),
            source.password.as_deref().unwrap_or_default().to_string(),
        );
        params.insert("sslmode".to_string(), "disabled".to_string());
    }

    Ok(Arc::new(
        MongoDBConnectionPool::new(to_secret_map(params))
            .await
            .map_err(|error| EngineError::InvalidConnection(format!("mongodb pool: {error}")))?,
    ))
}

pub async fn test_connection(_pool: &Arc<MongoDBConnectionPool>) -> Result<(), EngineError> {
    // Pool construction already pings MongoDB.
    Ok(())
}

pub async fn list_tables(pool: &Arc<MongoDBConnectionPool>) -> Result<Vec<TableInfo>, EngineError> {
    let connection = pool
        .connect()
        .await
        .map_err(|error| EngineError::provider_error("mongodb provider", error))?;
    let tables = connection
        .tables()
        .await
        .map_err(|error| EngineError::provider_error("mongodb provider", error))?;

    Ok(tables
        .into_iter()
        .map(|table| TableInfo {
            schema: None,
            name: table,
            table_type: "COLLECTION".to_string(),
        })
        .collect())
}

pub async fn list_columns(
    pool: &Arc<MongoDBConnectionPool>,
    _source: &DataSourceConnection,
    _schema: Option<String>,
    table: String,
) -> Result<Vec<ColumnInfo>, EngineError> {
    let connection = pool
        .connect()
        .await
        .map_err(|error| EngineError::provider_error("mongodb provider", error))?;
    let schema = connection
        .get_schema(&TableReference::bare(table.as_str()))
        .await
        .map_err(|error| EngineError::provider_error("mongodb provider", error))?;

    Ok(columns_from_fields(schema.fields()))
}

pub async fn query(
    pool: &Arc<MongoDBConnectionPool>,
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
    pool: Arc<MongoDBConnectionPool>,
    table: &str,
    alias: &str,
) -> Result<(), EngineError> {
    let provider = table_provider(pool, table).await?;
    ctx.register_table(alias, provider)?;
    Ok(())
}

pub async fn build_context(
    pool: &Arc<MongoDBConnectionPool>,
    _source: &DataSourceConnection,
) -> Result<SessionContext, EngineError> {
    let ctx = SessionContext::new();
    let connection = pool
        .connect()
        .await
        .map_err(|error| EngineError::provider_error("mongodb provider", error))?;
    let tables = connection
        .tables()
        .await
        .map_err(|error| EngineError::provider_error("mongodb provider", error))?;

    for table in tables {
        register_table(&ctx, pool.clone(), table.as_str(), table.as_str()).await?;
    }

    Ok(ctx)
}

async fn table_provider(
    pool: Arc<MongoDBConnectionPool>,
    table: &str,
) -> Result<Arc<dyn TableProvider + 'static>, EngineError> {
    let factory = MongoDBTableFactory::new(pool);
    factory
        .table_provider(TableReference::bare(table))
        .await
        .map_err(|error| EngineError::provider_error("mongodb table provider", error))
}

fn required<'a>(value: Option<&'a str>, field: &str) -> Result<&'a str, EngineError> {
    value
        .map(str::trim)
        .filter(|value| !value.is_empty())
        .ok_or_else(|| EngineError::InvalidConnection(format!("missing {field}")))
}
