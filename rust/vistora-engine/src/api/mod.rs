pub mod health;
pub mod query;
pub mod schema;

use axum::{
    Router,
    routing::{get, post},
};

use crate::engine::backend::BackendRegistry;

pub fn router(registry: BackendRegistry) -> Router {
    Router::new()
        .route("/health", get(health::health))
        .route("/test-connection", post(schema::test_connection))
        .route("/schema/tables", post(schema::list_tables))
        .route("/schema/columns", post(schema::list_columns))
        .route("/query", post(query::query))
        .route("/query/explain", post(query::explain_query))
        .route("/query/federated", post(query::federated_query))
        .route(
            "/query/federated/explain",
            post(query::explain_federated_query),
        )
        .with_state(registry)
}
