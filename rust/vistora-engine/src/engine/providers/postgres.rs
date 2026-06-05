use std::sync::Arc;

use datafusion::{prelude::SessionContext, sql::TableReference};
use datafusion_table_providers::{
    common::DatabaseCatalogProvider,
    postgres::PostgresTableFactory,
    sql::db_connection_pool::{
        DbConnectionPool,
        dbconnection::{get_schema, get_schemas, get_tables},
        postgrespool::PostgresConnectionPool,
    },
};

use crate::{
    engine::{files::columns_from_fields, providers::provider_error},
    error::EngineError,
    models::{ColumnInfo, DataSourceConnection, TableInfo},
};

pub async fn list_tables(
    pool: &Arc<PostgresConnectionPool>,
) -> Result<Vec<TableInfo>, EngineError> {
    let schemas = get_schemas(pool.connect().await.map_err(provider_error)?)
        .await
        .map_err(provider_error)?;
    let mut tables = Vec::new();

    for schema in schemas {
        for table in get_tables(
            pool.connect().await.map_err(provider_error)?,
            schema.as_str(),
        )
        .await
        .map_err(provider_error)?
        {
            tables.push(TableInfo {
                schema: Some(schema.clone()),
                name: table,
                table_type: "BASE TABLE".to_string(),
            });
        }
    }

    Ok(tables)
}

pub async fn list_columns(
    pool: &Arc<PostgresConnectionPool>,
    source: &DataSourceConnection,
    schema: Option<String>,
    table: String,
) -> Result<Vec<ColumnInfo>, EngineError> {
    let schema = default_schema(source, schema)?;
    let table_reference = TableReference::partial(schema, table.as_str());
    let schema = get_schema(
        pool.connect().await.map_err(provider_error)?,
        &table_reference,
    )
    .await
    .map_err(provider_error)?;

    Ok(columns_from_fields(schema.fields()))
}

pub async fn build_context(
    pool: Arc<PostgresConnectionPool>,
    source: &DataSourceConnection,
) -> Result<SessionContext, EngineError> {
    let ctx = SessionContext::new();
    let catalog = DatabaseCatalogProvider::try_new(pool.clone())
        .await
        .map_err(provider_error)?;
    ctx.register_catalog("postgres", Arc::new(catalog));
    register_default_schema(&ctx, pool, source).await?;
    Ok(ctx)
}

pub async fn register_table(
    ctx: &SessionContext,
    pool: Arc<PostgresConnectionPool>,
    source: &DataSourceConnection,
    table: &str,
    alias: &str,
) -> Result<(), EngineError> {
    let factory = PostgresTableFactory::new(pool);
    let schema = default_schema(source, source.schema.clone())?;
    let provider = factory
        .table_provider(TableReference::partial(schema.as_str(), table))
        .await
        .map_err(provider_error)?;
    ctx.register_table(alias, provider)?;
    Ok(())
}

async fn register_default_schema(
    ctx: &SessionContext,
    pool: Arc<PostgresConnectionPool>,
    source: &DataSourceConnection,
) -> Result<(), EngineError> {
    let schema = default_schema(source, source.schema.clone())?;
    let factory = PostgresTableFactory::new(pool.clone());
    let tables = get_tables(
        pool.connect().await.map_err(provider_error)?,
        schema.as_str(),
    )
    .await
    .map_err(provider_error)?;

    for table in tables {
        let provider = factory
            .table_provider(TableReference::partial(schema.as_str(), table.as_str()))
            .await
            .map_err(provider_error)?;
        ctx.register_table(table.as_str(), provider)?;
    }

    Ok(())
}

fn default_schema(
    _source: &DataSourceConnection,
    schema: Option<String>,
) -> Result<String, EngineError> {
    Ok(schema
        .as_deref()
        .map(str::trim)
        .filter(|schema| !schema.is_empty())
        .unwrap_or("public")
        .to_string())
}
