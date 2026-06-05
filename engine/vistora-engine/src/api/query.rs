use axum::{Json, extract::State};

use crate::{
    engine::{backend::BackendRegistry, executor},
    error::EngineError,
    models::{ExplainResult, FederatedQueryRequest, QueryRequest, QueryResult},
};

pub async fn query(
    State(registry): State<BackendRegistry>,
    Json(request): Json<QueryRequest>,
) -> Result<Json<QueryResult>, EngineError> {
    Ok(Json(executor::query(&registry, request).await?))
}

pub async fn federated_query(
    State(registry): State<BackendRegistry>,
    Json(request): Json<FederatedQueryRequest>,
) -> Result<Json<QueryResult>, EngineError> {
    Ok(Json(
        executor::execute_federated_query(&registry, request).await?,
    ))
}

pub async fn explain_query(
    State(registry): State<BackendRegistry>,
    Json(request): Json<QueryRequest>,
) -> Result<Json<ExplainResult>, EngineError> {
    Ok(Json(executor::explain_query(&registry, request).await?))
}

pub async fn explain_federated_query(
    State(registry): State<BackendRegistry>,
    Json(request): Json<FederatedQueryRequest>,
) -> Result<Json<ExplainResult>, EngineError> {
    Ok(Json(
        executor::explain_federated_query(&registry, request).await?,
    ))
}
