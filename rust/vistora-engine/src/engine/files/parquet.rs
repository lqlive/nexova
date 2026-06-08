use datafusion::prelude::SessionContext;

use crate::error::EngineError;

/// Register a Parquet source. `target` is a local filesystem path or an
/// `s3://bucket/key` URL whose object store has already been registered.
pub async fn register(ctx: &SessionContext, table: &str, target: &str) -> Result<(), EngineError> {
    ctx.register_parquet(table, target, Default::default())
        .await?;
    Ok(())
}
