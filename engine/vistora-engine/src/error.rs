use axum::{
    Json,
    http::StatusCode,
    response::{IntoResponse, Response},
};
use datafusion::{common::SchemaError, error::DataFusionError};
use serde_json::json;
use thiserror::Error;

#[derive(Debug, Error)]
pub enum EngineError {
    #[error("unsupported data source type")]
    UnsupportedDataSource,

    #[error("invalid connection: {0}")]
    InvalidConnection(String),

    #[error("sql syntax error: {0}")]
    SqlSyntax(String),

    #[error("read-only violation: {0}")]
    ReadOnlyViolation(String),

    #[error("table not found: {0}")]
    TableNotFound(String),

    #[error("column not found: {0}")]
    ColumnNotFound(String),

    #[error("query execution failed: {0}")]
    QueryExecution(String),

    #[error("invalid sql: {0}")]
    InvalidSql(String),

    #[error("query timed out")]
    Timeout,

    #[error("failed to read file source: {0}")]
    FileSource(String),
}

impl From<DataFusionError> for EngineError {
    fn from(value: DataFusionError) -> Self {
        classify_datafusion_error(value)
    }
}

impl IntoResponse for EngineError {
    fn into_response(self) -> Response {
        let status = match self {
            EngineError::SqlSyntax(_)
            | EngineError::ReadOnlyViolation(_)
            | EngineError::TableNotFound(_)
            | EngineError::ColumnNotFound(_)
            | EngineError::InvalidSql(_) => StatusCode::BAD_REQUEST,
            EngineError::InvalidConnection(_) => StatusCode::BAD_REQUEST,
            EngineError::UnsupportedDataSource => StatusCode::BAD_REQUEST,
            EngineError::Timeout => StatusCode::REQUEST_TIMEOUT,
            EngineError::FileSource(_) => StatusCode::BAD_REQUEST,
            EngineError::QueryExecution(_) => StatusCode::BAD_REQUEST,
        };

        let code = self.code();
        let category = self.category();
        let message = self.user_message();
        let detail = self.detail();

        let body = Json(json!({
            "error": message,
            "code": code,
            "category": category,
            "message": message,
            "detail": detail
        }));
        (status, body).into_response()
    }
}

impl EngineError {
    pub fn provider_error(context: &str, error: impl std::fmt::Display) -> Self {
        classify_provider_message(format!("{context}: {error}"))
    }

    fn code(&self) -> &'static str {
        match self {
            EngineError::UnsupportedDataSource => "unsupported_data_source",
            EngineError::InvalidConnection(_) => "connection_failed",
            EngineError::SqlSyntax(_) | EngineError::InvalidSql(_) => "sql_syntax_error",
            EngineError::ReadOnlyViolation(_) => "read_only_violation",
            EngineError::TableNotFound(_) => "table_not_found",
            EngineError::ColumnNotFound(_) => "column_not_found",
            EngineError::QueryExecution(_) => "query_execution_failed",
            EngineError::Timeout => "query_timeout",
            EngineError::FileSource(_) => "file_source_error",
        }
    }

    fn category(&self) -> &'static str {
        match self {
            EngineError::UnsupportedDataSource => "data_source",
            EngineError::InvalidConnection(_) => "connection_failed",
            EngineError::SqlSyntax(_) | EngineError::InvalidSql(_) => "sql_syntax_error",
            EngineError::ReadOnlyViolation(_) => "permission_or_read_only",
            EngineError::TableNotFound(_) => "table_not_found",
            EngineError::ColumnNotFound(_) => "column_not_found",
            EngineError::QueryExecution(_) => "query_execution",
            EngineError::Timeout => "timeout",
            EngineError::FileSource(_) => "file_source",
        }
    }

    fn user_message(&self) -> &'static str {
        match self {
            EngineError::UnsupportedDataSource => "Unsupported data source type",
            EngineError::InvalidConnection(_) => "Connection failed",
            EngineError::SqlSyntax(_) | EngineError::InvalidSql(_) => "SQL syntax error",
            EngineError::ReadOnlyViolation(_) => "Permission or read-only restriction",
            EngineError::TableNotFound(_) => "Table not found",
            EngineError::ColumnNotFound(_) => "Column not found",
            EngineError::QueryExecution(_) => "Query execution failed",
            EngineError::Timeout => "Query timed out",
            EngineError::FileSource(_) => "File source error",
        }
    }

    fn detail(&self) -> String {
        match self {
            EngineError::UnsupportedDataSource => "unsupported data source type".to_string(),
            EngineError::InvalidConnection(detail)
            | EngineError::SqlSyntax(detail)
            | EngineError::ReadOnlyViolation(detail)
            | EngineError::TableNotFound(detail)
            | EngineError::ColumnNotFound(detail)
            | EngineError::QueryExecution(detail)
            | EngineError::InvalidSql(detail)
            | EngineError::FileSource(detail) => detail.clone(),
            EngineError::Timeout => "query timed out".to_string(),
        }
    }
}

fn classify_datafusion_error(error: DataFusionError) -> EngineError {
    match error {
        DataFusionError::SQL(parser_error, _) => EngineError::SqlSyntax(parser_error.to_string()),
        DataFusionError::SchemaError(schema_error, _) => classify_schema_error(*schema_error),
        DataFusionError::Plan(message) | DataFusionError::Execution(message) => {
            classify_query_message(message)
        }
        DataFusionError::IoError(error) => EngineError::FileSource(error.to_string()),
        DataFusionError::External(error) => classify_provider_message(error.to_string()),
        DataFusionError::Context(context, source) => {
            let classified = classify_datafusion_error(*source);
            merge_context(classified, context)
        }
        DataFusionError::Diagnostic(_, source) => classify_datafusion_error(*source),
        DataFusionError::Collection(errors) => errors
            .into_iter()
            .next()
            .map(classify_datafusion_error)
            .unwrap_or_else(|| EngineError::QueryExecution("unknown DataFusion error".to_string())),
        DataFusionError::Shared(error) => classify_query_message(error.to_string()),
        other => classify_query_message(other.to_string()),
    }
}

fn classify_schema_error(error: SchemaError) -> EngineError {
    match error {
        SchemaError::FieldNotFound { field, .. } => {
            EngineError::ColumnNotFound(field.quoted_flat_name())
        }
        other => classify_query_message(other.to_string()),
    }
}

fn classify_query_message(message: String) -> EngineError {
    let lower = message.to_ascii_lowercase();

    if contains_any(
        &lower,
        &[
            "table not found",
            "no table named",
            "failed to resolve table",
        ],
    ) {
        EngineError::TableNotFound(message)
    } else if contains_any(
        &lower,
        &[
            "field not found",
            "no field named",
            "column not found",
            "no column named",
        ],
    ) {
        EngineError::ColumnNotFound(message)
    } else if contains_any(&lower, &["syntax error", "parser error", "sql parser"]) {
        EngineError::SqlSyntax(message)
    } else if contains_any(
        &lower,
        &[
            "permission denied",
            "access denied",
            "not authorized",
            "read-only",
            "readonly",
        ],
    ) {
        EngineError::ReadOnlyViolation(message)
    } else {
        EngineError::QueryExecution(message)
    }
}

fn classify_provider_message(message: String) -> EngineError {
    let lower = message.to_ascii_lowercase();

    if contains_any(
        &lower,
        &[
            "connect",
            "connection",
            "pool",
            "authentication",
            "password",
            "login",
            "tls",
        ],
    ) {
        EngineError::InvalidConnection(message)
    } else {
        classify_query_message(message)
    }
}

fn merge_context(error: EngineError, context: String) -> EngineError {
    let detail = format!("{context}: {}", error.detail());
    match error {
        EngineError::UnsupportedDataSource => EngineError::UnsupportedDataSource,
        EngineError::InvalidConnection(_) => EngineError::InvalidConnection(detail),
        EngineError::SqlSyntax(_) => EngineError::SqlSyntax(detail),
        EngineError::ReadOnlyViolation(_) => EngineError::ReadOnlyViolation(detail),
        EngineError::TableNotFound(_) => EngineError::TableNotFound(detail),
        EngineError::ColumnNotFound(_) => EngineError::ColumnNotFound(detail),
        EngineError::QueryExecution(_) => EngineError::QueryExecution(detail),
        EngineError::InvalidSql(_) => EngineError::InvalidSql(detail),
        EngineError::Timeout => EngineError::Timeout,
        EngineError::FileSource(_) => EngineError::FileSource(detail),
    }
}

fn contains_any(value: &str, needles: &[&str]) -> bool {
    needles.iter().any(|needle| value.contains(needle))
}
