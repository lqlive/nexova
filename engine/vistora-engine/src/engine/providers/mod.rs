pub mod clickhouse;
pub mod mongodb;
mod mysql;
mod postgres;
pub mod sqlite;

use std::{sync::Arc, time::Instant};

use datafusion::prelude::SessionContext;
use datafusion_table_providers::sql::db_connection_pool::{
    mysqlpool::MySQLConnectionPool, postgrespool::PostgresConnectionPool,
};
use tokio::time::{Duration, timeout};

use crate::{
    engine::files::{batches_to_rows, columns_from_fields},
    error::EngineError,
    models::{ColumnInfo, DataSourceConnection, QueryResult, TableInfo},
};

pub enum DatabaseProviderPool {
    Postgres(Arc<PostgresConnectionPool>),
    MySql(Arc<MySQLConnectionPool>),
}

pub async fn test_connection(_pool: &DatabaseProviderPool) -> Result<(), EngineError> {
    // Pool construction already validates connectivity.
    Ok(())
}

pub async fn list_tables(pool: &DatabaseProviderPool) -> Result<Vec<TableInfo>, EngineError> {
    match pool {
        DatabaseProviderPool::Postgres(pool) => postgres::list_tables(pool).await,
        DatabaseProviderPool::MySql(pool) => mysql::list_tables(pool).await,
    }
}

pub async fn list_columns(
    pool: &DatabaseProviderPool,
    source: &DataSourceConnection,
    schema: Option<String>,
    table: String,
) -> Result<Vec<ColumnInfo>, EngineError> {
    match pool {
        DatabaseProviderPool::Postgres(pool) => {
            postgres::list_columns(pool, source, schema, table).await
        }
        DatabaseProviderPool::MySql(pool) => mysql::list_columns(pool, source, schema, table).await,
    }
}

pub async fn query(
    pool: &DatabaseProviderPool,
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

pub async fn build_context(
    pool: &DatabaseProviderPool,
    source: &DataSourceConnection,
) -> Result<SessionContext, EngineError> {
    match pool {
        DatabaseProviderPool::Postgres(pool) => postgres::build_context(pool.clone(), source).await,
        DatabaseProviderPool::MySql(pool) => mysql::build_context(pool.clone(), source).await,
    }
}

pub async fn register_table(
    ctx: &SessionContext,
    pool: &DatabaseProviderPool,
    source: &DataSourceConnection,
    table: &str,
    alias: &str,
) -> Result<(), EngineError> {
    match pool {
        DatabaseProviderPool::Postgres(pool) => {
            postgres::register_table(ctx, pool.clone(), source, table, alias).await
        }
        DatabaseProviderPool::MySql(pool) => {
            mysql::register_table(ctx, pool.clone(), source, table, alias).await
        }
    }
}

pub(crate) fn provider_error(error: impl std::fmt::Display) -> EngineError {
    EngineError::provider_error("database provider", error)
}
