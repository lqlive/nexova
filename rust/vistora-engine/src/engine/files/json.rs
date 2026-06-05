use std::path::Path;

use datafusion::prelude::{JsonReadOptions, SessionContext};

use crate::error::EngineError;

pub async fn register(ctx: &SessionContext, table: &str, path: &Path) -> Result<(), EngineError> {
    let path = path.to_string_lossy().into_owned();
    ctx.register_json(table, path.as_str(), JsonReadOptions::default())
        .await?;
    Ok(())
}
