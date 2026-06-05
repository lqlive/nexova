use std::collections::HashSet;

use datafusion::prelude::SessionContext;

use crate::{
    engine::{backend::BackendRegistry, files},
    error::EngineError,
    models::DataSourceConnection,
};

pub async fn register_source(
    ctx: &SessionContext,
    registry: &BackendRegistry,
    source: &DataSourceConnection,
    registered_tables: &mut HashSet<String>,
) -> Result<(), EngineError> {
    let source_ctx = registry.file_context(source).await?;
    files::register_cached_federated_source(ctx, &source_ctx, source, registered_tables).await
}
