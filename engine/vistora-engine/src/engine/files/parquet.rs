use std::path::Path;

use datafusion::prelude::SessionContext;

use crate::error::EngineError;

pub async fn register(ctx: &SessionContext, table: &str, path: &Path) -> Result<(), EngineError> {
    let path = path.to_string_lossy().into_owned();
    ctx.register_parquet(table, path.as_str(), Default::default())
        .await?;
    Ok(())
}
