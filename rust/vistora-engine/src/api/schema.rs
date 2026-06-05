use axum::{Json, extract::State};
use serde_json::{Value, json};

use crate::{
    engine::{backend::BackendRegistry, executor},
    error::EngineError,
    models::{ColumnInfo, DataSourceRequest, SchemaColumnsRequest, TableInfo},
};

pub async fn test_connection(
    State(registry): State<BackendRegistry>,
    Json(request): Json<DataSourceRequest>,
) -> Result<Json<Value>, EngineError> {
    executor::test_connection(&registry, &request.data_source).await?;
    Ok(Json(json!({ "status": "connected" })))
}

pub async fn list_tables(
    State(registry): State<BackendRegistry>,
    Json(request): Json<DataSourceRequest>,
) -> Result<Json<Vec<TableInfo>>, EngineError> {
    Ok(Json(
        executor::list_tables(&registry, &request.data_source).await?,
    ))
}

pub async fn list_columns(
    State(registry): State<BackendRegistry>,
    Json(request): Json<SchemaColumnsRequest>,
) -> Result<Json<Vec<ColumnInfo>>, EngineError> {
    Ok(Json(
        executor::list_columns(
            &registry,
            &request.data_source,
            request.schema,
            request.table,
        )
        .await?,
    ))
}
