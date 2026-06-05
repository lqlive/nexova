use std::collections::HashSet;

use datafusion::prelude::SessionContext;

use crate::{
    engine::{
        backend::BackendRegistry,
        providers::{self, DatabaseProviderPool},
        types::DataSourceKind,
    },
    error::EngineError,
    models::DataSourceConnection,
};

pub async fn register_source(
    ctx: &SessionContext,
    registry: &BackendRegistry,
    source: &DataSourceConnection,
    registered_tables: &mut HashSet<String>,
) -> Result<(), EngineError> {
    let table = required(source.table.as_deref(), "table")?;
    let alias = source
        .alias
        .as_deref()
        .map(str::trim)
        .filter(|alias| !alias.is_empty())
        .unwrap_or(table)
        .to_string();

    if !registered_tables.insert(alias.clone()) {
        return Err(EngineError::FileSource(format!(
            "duplicate federated table name '{alias}'"
        )));
    }

    match source.kind()? {
        DataSourceKind::Postgres => {
            register_postgres_table(ctx, registry, source, table, &alias).await
        }
        DataSourceKind::MySql => register_mysql_table(ctx, registry, source, table, &alias).await,
        DataSourceKind::ClickHouse => {
            register_clickhouse_table(ctx, registry, source, table, &alias).await
        }
        DataSourceKind::MongoDb => {
            register_mongodb_table(ctx, registry, source, table, &alias).await
        }
        DataSourceKind::Sqlite => register_sqlite_table(ctx, registry, source, table, &alias).await,
        DataSourceKind::Csv
        | DataSourceKind::Json
        | DataSourceKind::Excel
        | DataSourceKind::Parquet
        | DataSourceKind::Files => Err(EngineError::UnsupportedDataSource),
    }
}

async fn register_postgres_table(
    ctx: &SessionContext,
    registry: &BackendRegistry,
    source: &DataSourceConnection,
    table: &str,
    alias: &str,
) -> Result<(), EngineError> {
    let database_pool = DatabaseProviderPool::Postgres(registry.postgres_pool(source).await?);
    providers::register_table(ctx, &database_pool, source, table, alias).await
}

async fn register_mysql_table(
    ctx: &SessionContext,
    registry: &BackendRegistry,
    source: &DataSourceConnection,
    table: &str,
    alias: &str,
) -> Result<(), EngineError> {
    let database_pool = DatabaseProviderPool::MySql(registry.mysql_pool(source).await?);
    providers::register_table(ctx, &database_pool, source, table, alias).await
}

async fn register_clickhouse_table(
    ctx: &SessionContext,
    registry: &BackendRegistry,
    source: &DataSourceConnection,
    table: &str,
    alias: &str,
) -> Result<(), EngineError> {
    let pool = registry.clickhouse_pool(source).await?;
    providers::clickhouse::register_table(ctx, pool, source, table, alias).await
}

async fn register_mongodb_table(
    ctx: &SessionContext,
    registry: &BackendRegistry,
    source: &DataSourceConnection,
    table: &str,
    alias: &str,
) -> Result<(), EngineError> {
    let pool = registry.mongodb_pool(source).await?;
    providers::mongodb::register_table(ctx, pool, table, alias).await
}

async fn register_sqlite_table(
    ctx: &SessionContext,
    registry: &BackendRegistry,
    source: &DataSourceConnection,
    table: &str,
    alias: &str,
) -> Result<(), EngineError> {
    let pool = registry.sqlite_pool(source).await?;
    providers::sqlite::register_table(ctx, pool, table, alias).await
}

fn required<'a>(value: Option<&'a str>, field: &str) -> Result<&'a str, EngineError> {
    value
        .map(str::trim)
        .filter(|value| !value.is_empty())
        .ok_or_else(|| EngineError::InvalidConnection(format!("missing {field}")))
}
