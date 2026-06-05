mod databases;
mod files;

use std::{collections::HashSet, time::Instant};

use datafusion::prelude::SessionContext;
use datafusion_federation::default_session_state;
use tokio::time::{Duration, timeout};

use crate::{
    engine::{
        backend::BackendRegistry,
        files::{batches_to_rows, columns_from_fields},
        types::DataSourceKind,
    },
    error::EngineError,
    models::{DataSourceConnection, QueryResult},
};

pub async fn execute_federated_query(
    registry: &BackendRegistry,
    sources: &[DataSourceConnection],
    sql: &str,
    timeout_duration: Duration,
    started: Instant,
) -> Result<QueryResult, EngineError> {
    let ctx = build_federated_context(registry, sources).await?;
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

pub async fn build_federated_context(
    registry: &BackendRegistry,
    sources: &[DataSourceConnection],
) -> Result<SessionContext, EngineError> {
    if sources.is_empty() {
        return Err(EngineError::InvalidConnection(
            "at least one data source is required".to_string(),
        ));
    }

    let ctx = SessionContext::new_with_state(default_session_state());
    let mut registered_tables = HashSet::new();

    for source in sources {
        match source.kind()? {
            DataSourceKind::Csv
            | DataSourceKind::Json
            | DataSourceKind::Excel
            | DataSourceKind::Parquet
            | DataSourceKind::Files => {
                files::register_source(&ctx, registry, source, &mut registered_tables).await?;
            }
            DataSourceKind::Postgres
            | DataSourceKind::MySql
            | DataSourceKind::ClickHouse
            | DataSourceKind::MongoDb
            | DataSourceKind::Sqlite => {
                databases::register_source(&ctx, registry, source, &mut registered_tables).await?;
            }
        }
    }

    Ok(ctx)
}
