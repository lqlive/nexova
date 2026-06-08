use std::{collections::HashMap, sync::Arc};

use datafusion::prelude::SessionContext;
use datafusion_table_providers::{
    mongodb::connection_pool::MongoDBConnectionPool,
    sql::db_connection_pool::{
        clickhousepool::ClickHouseConnectionPool, mysqlpool::MySQLConnectionPool,
        postgrespool::PostgresConnectionPool, sqlitepool::SqliteConnectionPool,
    },
    util::secrets::to_secret_map,
};
use tokio::sync::RwLock;

use crate::{
    engine::{files, providers},
    error::EngineError,
    models::DataSourceConnection,
};

#[derive(Clone, Default)]
pub struct BackendRegistry {
    postgres_pools: Arc<RwLock<HashMap<String, Arc<PostgresConnectionPool>>>>,
    mysql_pools: Arc<RwLock<HashMap<String, Arc<MySQLConnectionPool>>>>,
    clickhouse_pools: Arc<RwLock<HashMap<String, Arc<ClickHouseConnectionPool>>>>,
    mongodb_pools: Arc<RwLock<HashMap<String, Arc<MongoDBConnectionPool>>>>,
    sqlite_pools: Arc<RwLock<HashMap<String, Arc<SqliteConnectionPool>>>>,
    file_contexts: Arc<RwLock<HashMap<String, (String, Arc<SessionContext>)>>>,
}

impl BackendRegistry {
    pub fn new() -> Self {
        Self::default()
    }

    pub async fn postgres_pool(
        &self,
        source: &DataSourceConnection,
    ) -> Result<Arc<PostgresConnectionPool>, EngineError> {
        let cache_key = postgres_cache_key(source)?;

        if let Some(pool) = self.postgres_pools.read().await.get(&cache_key) {
            return Ok(pool.clone());
        }

        let pool = Arc::new(
            PostgresConnectionPool::new(to_secret_map(HashMap::from([
                (
                    "host".to_string(),
                    required(source.host.as_deref(), "host")?.to_string(),
                ),
                (
                    "user".to_string(),
                    required(source.username.as_deref(), "username")?.to_string(),
                ),
                (
                    "db".to_string(),
                    required(source.database.as_deref(), "database")?.to_string(),
                ),
                (
                    "pass".to_string(),
                    source.password.as_deref().unwrap_or_default().to_string(),
                ),
                (
                    "port".to_string(),
                    source
                        .port
                        .ok_or_else(|| EngineError::InvalidConnection("missing port".to_string()))?
                        .to_string(),
                ),
                ("sslmode".to_string(), "disable".to_string()),
            ])))
            .await
            .map_err(|error| EngineError::InvalidConnection(format!("postgres pool: {error}")))?,
        );

        self.postgres_pools
            .write()
            .await
            .insert(cache_key, pool.clone());

        Ok(pool)
    }

    pub async fn mysql_pool(
        &self,
        source: &DataSourceConnection,
    ) -> Result<Arc<MySQLConnectionPool>, EngineError> {
        let cache_key = mysql_cache_key(source)?;

        if let Some(pool) = self.mysql_pools.read().await.get(&cache_key) {
            return Ok(pool.clone());
        }

        let pool = Arc::new(
            MySQLConnectionPool::new(to_secret_map(HashMap::from([
                ("connection_string".to_string(), source.connection_string()?),
                ("sslmode".to_string(), "disabled".to_string()),
            ])))
            .await
            .map_err(|error| EngineError::InvalidConnection(format!("mysql pool: {error}")))?,
        );

        self.mysql_pools
            .write()
            .await
            .insert(cache_key, pool.clone());

        Ok(pool)
    }

    pub async fn clickhouse_pool(
        &self,
        source: &DataSourceConnection,
    ) -> Result<Arc<ClickHouseConnectionPool>, EngineError> {
        let cache_key = clickhouse_cache_key(source)?;

        if let Some(pool) = self.clickhouse_pools.read().await.get(&cache_key) {
            return Ok(pool.clone());
        }

        let pool = Arc::new(
            ClickHouseConnectionPool::new(to_secret_map(HashMap::from([
                ("url".to_string(), source.connection_string()?),
                (
                    "database".to_string(),
                    required(source.database.as_deref(), "database")?.to_string(),
                ),
                (
                    "user".to_string(),
                    required(source.username.as_deref(), "username")?.to_string(),
                ),
                (
                    "password".to_string(),
                    source.password.as_deref().unwrap_or_default().to_string(),
                ),
            ])))
            .await
            .map_err(|error| EngineError::InvalidConnection(format!("clickhouse pool: {error}")))?,
        );

        self.clickhouse_pools
            .write()
            .await
            .insert(cache_key, pool.clone());

        Ok(pool)
    }

    pub async fn mongodb_pool(
        &self,
        source: &DataSourceConnection,
    ) -> Result<Arc<MongoDBConnectionPool>, EngineError> {
        let cache_key = mongodb_cache_key(source)?;

        if let Some(pool) = self.mongodb_pools.read().await.get(&cache_key) {
            return Ok(pool.clone());
        }

        let pool = providers::mongodb::create_pool(source).await?;
        self.mongodb_pools
            .write()
            .await
            .insert(cache_key, pool.clone());

        Ok(pool)
    }

    pub async fn sqlite_pool(
        &self,
        source: &DataSourceConnection,
    ) -> Result<Arc<SqliteConnectionPool>, EngineError> {
        let cache_key = sqlite_cache_key(source)?;

        if let Some(pool) = self.sqlite_pools.read().await.get(&cache_key) {
            return Ok(pool.clone());
        }

        let pool = providers::sqlite::create_pool(source).await?;
        self.sqlite_pools
            .write()
            .await
            .insert(cache_key, pool.clone());

        Ok(pool)
    }

    pub async fn file_context(
        &self,
        source: &DataSourceConnection,
    ) -> Result<Arc<SessionContext>, EngineError> {
        let cache_key = file_cache_key(source)?;
        let signature = files::path_signature(source)?;

        if let Some((cached_signature, ctx)) = self.file_contexts.read().await.get(&cache_key) {
            if *cached_signature == signature {
                return Ok(ctx.clone());
            }
        }

        let ctx = Arc::new(files::build_context(source).await?);
        self.file_contexts
            .write()
            .await
            .insert(cache_key, (signature, ctx.clone()));

        Ok(ctx)
    }
}

fn postgres_cache_key(source: &DataSourceConnection) -> Result<String, EngineError> {
    Ok(format!(
        "host={};port={};database={};user={}",
        required(source.host.as_deref(), "host")?,
        source
            .port
            .ok_or_else(|| EngineError::InvalidConnection("missing port".to_string()))?,
        required(source.database.as_deref(), "database")?,
        required(source.username.as_deref(), "username")?
    ))
}

fn mysql_cache_key(source: &DataSourceConnection) -> Result<String, EngineError> {
    Ok(format!(
        "connection_string={};database={};user={}",
        source.connection_string()?,
        required(source.database.as_deref(), "database")?,
        required(source.username.as_deref(), "username")?
    ))
}

fn clickhouse_cache_key(source: &DataSourceConnection) -> Result<String, EngineError> {
    Ok(format!(
        "url={};database={};user={}",
        source.connection_string()?,
        required(source.database.as_deref(), "database")?,
        required(source.username.as_deref(), "username")?
    ))
}

fn mongodb_cache_key(source: &DataSourceConnection) -> Result<String, EngineError> {
    Ok(format!("connection_string={}", source.connection_string()?))
}

fn sqlite_cache_key(source: &DataSourceConnection) -> Result<String, EngineError> {
    let path = source
        .path
        .as_deref()
        .or(source.connection_string.as_deref())
        .map(str::trim)
        .filter(|value| !value.is_empty())
        .ok_or_else(|| EngineError::InvalidConnection("missing sqlite path".to_string()))?;

    Ok(format!("path={path}"))
}

fn file_cache_key(source: &DataSourceConnection) -> Result<String, EngineError> {
    Ok(format!(
        "type={};path={};table={};has_header={:?};delimiter={};sheet={};schema_override={:?}",
        source.source_type.to_ascii_lowercase(),
        source.require_path()?,
        source.table.as_deref().unwrap_or_default(),
        source.has_header,
        source.delimiter.as_deref().unwrap_or_default(),
        source.sheet.as_deref().unwrap_or_default(),
        source.schema_override
    ))
}

fn required<'a>(value: Option<&'a str>, field: &str) -> Result<&'a str, EngineError> {
    value
        .map(str::trim)
        .filter(|value| !value.is_empty())
        .ok_or_else(|| EngineError::InvalidConnection(format!("missing {field}")))
}
