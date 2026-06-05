mod registry;

use std::time::Instant;

use async_trait::async_trait;
pub use registry::BackendRegistry;
use tokio::time::Duration;

use crate::{
    engine::{
        files,
        providers::{self, DatabaseProviderPool},
        types::DataSourceKind,
    },
    error::EngineError,
    models::{ColumnInfo, DataSourceConnection, QueryResult, TableInfo},
};

#[async_trait]
pub trait QueryBackend: Send + Sync {
    async fn test_connection(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
    ) -> Result<(), EngineError>;

    async fn list_tables(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
    ) -> Result<Vec<TableInfo>, EngineError>;

    async fn list_columns(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
        schema: Option<String>,
        table: String,
    ) -> Result<Vec<ColumnInfo>, EngineError>;

    async fn query(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
        sql: &str,
        timeout_duration: Duration,
        started: Instant,
    ) -> Result<QueryResult, EngineError>;
}

pub struct DataFusionPostgresBackend;

pub struct DataFusionMySqlBackend;

pub struct DataFusionClickHouseBackend;

pub struct DataFusionMongoDbBackend;

pub struct DataFusionSqliteBackend;

pub struct DataFusionFileBackend;

pub fn backend_for(source: &DataSourceConnection) -> Result<Box<dyn QueryBackend>, EngineError> {
    Ok(match source.kind()? {
        DataSourceKind::Postgres => Box::new(DataFusionPostgresBackend) as Box<dyn QueryBackend>,
        DataSourceKind::MySql => Box::new(DataFusionMySqlBackend) as Box<dyn QueryBackend>,
        DataSourceKind::ClickHouse => {
            Box::new(DataFusionClickHouseBackend) as Box<dyn QueryBackend>
        }
        DataSourceKind::MongoDb => Box::new(DataFusionMongoDbBackend) as Box<dyn QueryBackend>,
        DataSourceKind::Sqlite => Box::new(DataFusionSqliteBackend) as Box<dyn QueryBackend>,
        DataSourceKind::Csv
        | DataSourceKind::Json
        | DataSourceKind::Excel
        | DataSourceKind::Parquet
        | DataSourceKind::Files => Box::new(DataFusionFileBackend) as Box<dyn QueryBackend>,
    })
}

#[async_trait]
impl QueryBackend for DataFusionPostgresBackend {
    async fn test_connection(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
    ) -> Result<(), EngineError> {
        let pool = DatabaseProviderPool::Postgres(registry.postgres_pool(source).await?);
        providers::test_connection(&pool).await
    }

    async fn list_tables(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
    ) -> Result<Vec<TableInfo>, EngineError> {
        let pool = DatabaseProviderPool::Postgres(registry.postgres_pool(source).await?);
        providers::list_tables(&pool).await
    }

    async fn list_columns(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
        schema: Option<String>,
        table: String,
    ) -> Result<Vec<ColumnInfo>, EngineError> {
        let pool = DatabaseProviderPool::Postgres(registry.postgres_pool(source).await?);
        providers::list_columns(&pool, source, schema, table).await
    }

    async fn query(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
        sql: &str,
        timeout_duration: Duration,
        started: Instant,
    ) -> Result<QueryResult, EngineError> {
        let pool = DatabaseProviderPool::Postgres(registry.postgres_pool(source).await?);
        providers::query(&pool, source, sql, timeout_duration, started).await
    }
}

#[async_trait]
impl QueryBackend for DataFusionMySqlBackend {
    async fn test_connection(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
    ) -> Result<(), EngineError> {
        let pool = DatabaseProviderPool::MySql(registry.mysql_pool(source).await?);
        providers::test_connection(&pool).await
    }

    async fn list_tables(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
    ) -> Result<Vec<TableInfo>, EngineError> {
        let pool = DatabaseProviderPool::MySql(registry.mysql_pool(source).await?);
        providers::list_tables(&pool).await
    }

    async fn list_columns(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
        schema: Option<String>,
        table: String,
    ) -> Result<Vec<ColumnInfo>, EngineError> {
        let pool = DatabaseProviderPool::MySql(registry.mysql_pool(source).await?);
        providers::list_columns(&pool, source, schema, table).await
    }

    async fn query(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
        sql: &str,
        timeout_duration: Duration,
        started: Instant,
    ) -> Result<QueryResult, EngineError> {
        let pool = DatabaseProviderPool::MySql(registry.mysql_pool(source).await?);
        providers::query(&pool, source, sql, timeout_duration, started).await
    }
}

#[async_trait]
impl QueryBackend for DataFusionClickHouseBackend {
    async fn test_connection(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
    ) -> Result<(), EngineError> {
        let pool = registry.clickhouse_pool(source).await?;
        providers::clickhouse::test_connection(&pool).await
    }

    async fn list_tables(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
    ) -> Result<Vec<TableInfo>, EngineError> {
        let pool = registry.clickhouse_pool(source).await?;
        providers::clickhouse::list_tables(&pool, source).await
    }

    async fn list_columns(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
        schema: Option<String>,
        table: String,
    ) -> Result<Vec<ColumnInfo>, EngineError> {
        let pool = registry.clickhouse_pool(source).await?;
        providers::clickhouse::list_columns(&pool, source, schema, table).await
    }

    async fn query(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
        sql: &str,
        timeout_duration: Duration,
        started: Instant,
    ) -> Result<QueryResult, EngineError> {
        let pool = registry.clickhouse_pool(source).await?;
        providers::clickhouse::query(&pool, source, sql, timeout_duration, started).await
    }
}

#[async_trait]
impl QueryBackend for DataFusionMongoDbBackend {
    async fn test_connection(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
    ) -> Result<(), EngineError> {
        let pool = registry.mongodb_pool(source).await?;
        providers::mongodb::test_connection(&pool).await
    }

    async fn list_tables(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
    ) -> Result<Vec<TableInfo>, EngineError> {
        let pool = registry.mongodb_pool(source).await?;
        providers::mongodb::list_tables(&pool).await
    }

    async fn list_columns(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
        schema: Option<String>,
        table: String,
    ) -> Result<Vec<ColumnInfo>, EngineError> {
        let pool = registry.mongodb_pool(source).await?;
        providers::mongodb::list_columns(&pool, source, schema, table).await
    }

    async fn query(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
        sql: &str,
        timeout_duration: Duration,
        started: Instant,
    ) -> Result<QueryResult, EngineError> {
        let pool = registry.mongodb_pool(source).await?;
        providers::mongodb::query(&pool, source, sql, timeout_duration, started).await
    }
}

#[async_trait]
impl QueryBackend for DataFusionSqliteBackend {
    async fn test_connection(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
    ) -> Result<(), EngineError> {
        let pool = registry.sqlite_pool(source).await?;
        providers::sqlite::test_connection(&pool).await
    }

    async fn list_tables(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
    ) -> Result<Vec<TableInfo>, EngineError> {
        let pool = registry.sqlite_pool(source).await?;
        providers::sqlite::list_tables(&pool).await
    }

    async fn list_columns(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
        schema: Option<String>,
        table: String,
    ) -> Result<Vec<ColumnInfo>, EngineError> {
        let pool = registry.sqlite_pool(source).await?;
        providers::sqlite::list_columns(&pool, source, schema, table).await
    }

    async fn query(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
        sql: &str,
        timeout_duration: Duration,
        started: Instant,
    ) -> Result<QueryResult, EngineError> {
        let pool = registry.sqlite_pool(source).await?;
        providers::sqlite::query(&pool, source, sql, timeout_duration, started).await
    }
}

#[async_trait]
impl QueryBackend for DataFusionFileBackend {
    async fn test_connection(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
    ) -> Result<(), EngineError> {
        let ctx = registry.file_context(source).await?;
        files::test_connection(ctx.as_ref(), source).await
    }

    async fn list_tables(
        &self,
        _registry: &BackendRegistry,
        source: &DataSourceConnection,
    ) -> Result<Vec<TableInfo>, EngineError> {
        files::list_tables(source).await
    }

    async fn list_columns(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
        _schema: Option<String>,
        table: String,
    ) -> Result<Vec<ColumnInfo>, EngineError> {
        let ctx = registry.file_context(source).await?;
        files::list_columns(ctx.as_ref(), source, table).await
    }

    async fn query(
        &self,
        registry: &BackendRegistry,
        source: &DataSourceConnection,
        sql: &str,
        timeout_duration: Duration,
        started: Instant,
    ) -> Result<QueryResult, EngineError> {
        let ctx = registry.file_context(source).await?;
        files::query(ctx.as_ref(), source, sql, timeout_duration, started).await
    }
}
