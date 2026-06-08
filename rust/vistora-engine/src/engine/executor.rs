use std::time::Instant;

use datafusion::{
    arrow::{
        array::{Array, StringArray},
        record_batch::RecordBatch,
    },
    prelude::SessionContext,
};
use sqlparser::{
    ast::{Query, Select, SetExpr, Statement},
    dialect::GenericDialect,
    parser::Parser,
};
use tokio::time::{Duration, timeout};

use crate::{
    engine::{
        backend::{BackendRegistry, backend_for},
        federated,
        providers::{self, DatabaseProviderPool},
        types::{DEFAULT_LIMIT, DEFAULT_TIMEOUT_MS, DataSourceKind, MAX_LIMIT},
    },
    error::EngineError,
    models::{
        ColumnInfo, DataSourceConnection, ExplainPlanInfo, ExplainResult, FederatedQueryRequest,
        QueryRequest, QueryResult, TableInfo,
    },
};

pub async fn test_connection(
    registry: &BackendRegistry,
    source: &DataSourceConnection,
) -> Result<(), EngineError> {
    let backend = backend_for(source)?;
    backend.test_connection(registry, source).await
}

pub async fn list_tables(
    registry: &BackendRegistry,
    source: &DataSourceConnection,
) -> Result<Vec<TableInfo>, EngineError> {
    let backend = backend_for(source)?;
    backend.list_tables(registry, source).await
}

pub async fn list_columns(
    registry: &BackendRegistry,
    source: &DataSourceConnection,
    schema: Option<String>,
    table: String,
) -> Result<Vec<ColumnInfo>, EngineError> {
    let backend = backend_for(source)?;
    backend.list_columns(registry, source, schema, table).await
}

pub async fn query(
    registry: &BackendRegistry,
    request: QueryRequest,
) -> Result<QueryResult, EngineError> {
    validate_readonly_sql(&request.sql)?;

    let sql = limited_sql(&request.sql, request.limit);
    let timeout_duration = Duration::from_millis(request.timeout_ms.unwrap_or(DEFAULT_TIMEOUT_MS));
    let started = Instant::now();

    let backend = backend_for(&request.data_source)?;
    backend
        .query(
            registry,
            &request.data_source,
            &sql,
            timeout_duration,
            started,
        )
        .await
}

pub async fn execute_federated_query(
    registry: &BackendRegistry,
    request: FederatedQueryRequest,
) -> Result<QueryResult, EngineError> {
    validate_readonly_sql(&request.sql)?;

    let sql = limited_sql(&request.sql, request.limit);
    let timeout_duration = Duration::from_millis(request.timeout_ms.unwrap_or(DEFAULT_TIMEOUT_MS));
    let started = Instant::now();

    federated::execute_federated_query(
        registry,
        &request.data_sources,
        &sql,
        timeout_duration,
        started,
    )
    .await
}

pub async fn explain_query(
    registry: &BackendRegistry,
    request: QueryRequest,
) -> Result<ExplainResult, EngineError> {
    validate_readonly_sql(&request.sql)?;

    let sql = limited_sql(&request.sql, request.limit);
    let timeout_duration = Duration::from_millis(request.timeout_ms.unwrap_or(DEFAULT_TIMEOUT_MS));
    let started = Instant::now();
    let ctx = source_context(registry, &request.data_source).await?;

    explain_context(&ctx, &sql, timeout_duration, started).await
}

pub async fn explain_federated_query(
    registry: &BackendRegistry,
    request: FederatedQueryRequest,
) -> Result<ExplainResult, EngineError> {
    validate_readonly_sql(&request.sql)?;

    let sql = limited_sql(&request.sql, request.limit);
    let timeout_duration = Duration::from_millis(request.timeout_ms.unwrap_or(DEFAULT_TIMEOUT_MS));
    let started = Instant::now();
    let ctx = federated::build_federated_context(registry, &request.data_sources).await?;

    explain_context(&ctx, &sql, timeout_duration, started).await
}

async fn source_context(
    registry: &BackendRegistry,
    source: &DataSourceConnection,
) -> Result<SessionContext, EngineError> {
    match source.kind()? {
        DataSourceKind::Postgres => {
            let pool = DatabaseProviderPool::Postgres(registry.postgres_pool(source).await?);
            providers::build_context(&pool, source).await
        }
        DataSourceKind::MySql => {
            let pool = DatabaseProviderPool::MySql(registry.mysql_pool(source).await?);
            providers::build_context(&pool, source).await
        }
        DataSourceKind::ClickHouse => {
            let pool = registry.clickhouse_pool(source).await?;
            providers::clickhouse::build_context(&pool, source).await
        }
        DataSourceKind::MongoDb => {
            let pool = registry.mongodb_pool(source).await?;
            providers::mongodb::build_context(&pool, source).await
        }
        DataSourceKind::Sqlite => {
            let pool = registry.sqlite_pool(source).await?;
            providers::sqlite::build_context(&pool, source).await
        }
        DataSourceKind::Csv
        | DataSourceKind::Json
        | DataSourceKind::Excel
        | DataSourceKind::Parquet
        | DataSourceKind::Files => {
            let ctx = registry.file_context(source).await?;
            Ok(ctx.as_ref().clone())
        }
    }
}

async fn explain_context(
    ctx: &SessionContext,
    sql: &str,
    timeout_duration: Duration,
    started: Instant,
) -> Result<ExplainResult, EngineError> {
    let explain_sql = format!("EXPLAIN VERBOSE {}", sql.trim());
    let df = ctx.sql(&explain_sql).await?;
    let batches = timeout(timeout_duration, df.collect())
        .await
        .map_err(|_| EngineError::Timeout)??;
    let plans = explain_plans_from_batches(&batches)?;
    let logical_plan = find_plan(&plans, "logical_plan");
    let physical_plan = find_plan(&plans, "physical_plan");

    Ok(ExplainResult {
        logical_plan,
        physical_plan,
        plans,
        duration_ms: started.elapsed().as_millis() as u64,
    })
}

fn explain_plans_from_batches(
    batches: &[RecordBatch],
) -> Result<Vec<ExplainPlanInfo>, EngineError> {
    let mut plans = Vec::new();

    for batch in batches {
        if batch.num_columns() < 2 {
            continue;
        }

        let plan_types = batch
            .column(0)
            .as_any()
            .downcast_ref::<StringArray>()
            .ok_or_else(|| {
                EngineError::QueryExecution(
                    "unexpected EXPLAIN output: plan_type column is not utf8".to_string(),
                )
            })?;
        let plan_values = batch
            .column(1)
            .as_any()
            .downcast_ref::<StringArray>()
            .ok_or_else(|| {
                EngineError::QueryExecution(
                    "unexpected EXPLAIN output: plan column is not utf8".to_string(),
                )
            })?;

        for row in 0..batch.num_rows() {
            if plan_types.is_null(row) || plan_values.is_null(row) {
                continue;
            }

            plans.push(ExplainPlanInfo {
                plan_type: plan_types.value(row).to_string(),
                plan: plan_values.value(row).to_string(),
            });
        }
    }

    Ok(plans)
}

fn find_plan(plans: &[ExplainPlanInfo], plan_type: &str) -> Option<String> {
    plans
        .iter()
        .find(|plan| plan.plan_type == plan_type)
        .map(|plan| plan.plan.clone())
}

fn validate_readonly_sql(sql: &str) -> Result<(), EngineError> {
    let normalized = normalize_sql(sql);

    if normalized.is_empty() {
        return Err(EngineError::SqlSyntax("SQL query is empty".to_string()));
    }

    let dialect = GenericDialect;
    let statements = Parser::parse_sql(&dialect, normalized)
        .map_err(|error| EngineError::SqlSyntax(error.to_string()))?;

    if statements.len() != 1 {
        return Err(EngineError::ReadOnlyViolation(
            "exactly one SQL statement is allowed".to_string(),
        ));
    }

    match statements.first() {
        Some(Statement::Query(query)) if is_readonly_query(query) => Ok(()),
        _ => Err(EngineError::ReadOnlyViolation(
            "only read-only SELECT queries are allowed".to_string(),
        )),
    }
}

/// Strips leading/trailing whitespace and any trailing statement terminator(s).
///
/// A single trailing `;` is a common, valid way to end a query, so it is
/// tolerated here. Terminators that separate multiple statements remain in the
/// string, so multi-statement input is still rejected by the parser's
/// statement-count check.
fn normalize_sql(sql: &str) -> &str {
    sql.trim()
        .trim_end_matches(|character: char| character == ';' || character.is_whitespace())
}

fn is_readonly_query(query: &Query) -> bool {
    if !query.locks.is_empty() {
        return false;
    }

    if let Some(with) = query.with.as_ref() {
        if !with
            .cte_tables
            .iter()
            .all(|cte| is_readonly_query(&cte.query))
        {
            return false;
        }
    }

    is_readonly_set_expr(&query.body)
}

fn is_readonly_set_expr(set_expr: &SetExpr) -> bool {
    match set_expr {
        SetExpr::Select(select) => is_readonly_select(select),
        SetExpr::Query(query) => is_readonly_query(query),
        SetExpr::SetOperation { left, right, .. } => {
            is_readonly_set_expr(left) && is_readonly_set_expr(right)
        }
        SetExpr::Values(_)
        | SetExpr::Insert(_)
        | SetExpr::Update(_)
        | SetExpr::Delete(_)
        | SetExpr::Merge(_)
        | SetExpr::Table(_) => false,
    }
}

fn is_readonly_select(select: &Select) -> bool {
    if select.into.is_some() {
        return false;
    }

    true
}

fn limited_sql(sql: &str, limit: Option<u32>) -> String {
    let limit = limit.unwrap_or(DEFAULT_LIMIT).min(MAX_LIMIT);
    let trimmed = normalize_sql(sql);

    if has_top_level_limit(trimmed) {
        trimmed.to_string()
    } else {
        format!("{trimmed} LIMIT {limit}")
    }
}

fn has_top_level_limit(sql: &str) -> bool {
    let dialect = GenericDialect;
    let Ok(statements) = Parser::parse_sql(&dialect, sql) else {
        return false;
    };

    match statements.as_slice() {
        [Statement::Query(query)] => query.limit_clause.is_some() || query.fetch.is_some(),
        _ => false,
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn validate_readonly_sql_allows_select() {
        assert!(validate_readonly_sql("select * from users").is_ok());
        assert!(validate_readonly_sql("  SELECT id from users").is_ok());
    }

    #[test]
    fn validate_readonly_sql_allows_with_queries() {
        assert!(
            validate_readonly_sql(
                "with active_users as (select * from users) select * from active_users"
            )
            .is_ok()
        );
    }

    #[test]
    fn validate_readonly_sql_allows_set_operations() {
        assert!(
            validate_readonly_sql("select id from users union all select id from admins").is_ok()
        );
    }

    #[test]
    fn validate_readonly_sql_allows_semicolon_inside_string_literals() {
        assert!(validate_readonly_sql("select ';' as separator from users").is_ok());
    }

    #[test]
    fn validate_readonly_sql_rejects_mutations() {
        assert!(matches!(
            validate_readonly_sql("delete from users"),
            Err(EngineError::ReadOnlyViolation(_))
        ));

        assert!(matches!(
            validate_readonly_sql("insert into users values (1)"),
            Err(EngineError::ReadOnlyViolation(_))
        ));
    }

    #[test]
    fn validate_readonly_sql_rejects_select_into() {
        assert!(matches!(
            validate_readonly_sql("select * into copied_users from users"),
            Err(EngineError::ReadOnlyViolation(_))
        ));
    }

    #[test]
    fn validate_readonly_sql_rejects_values_statement() {
        assert!(matches!(
            validate_readonly_sql("values (1), (2)"),
            Err(EngineError::ReadOnlyViolation(_))
        ));
    }

    #[test]
    fn validate_readonly_sql_rejects_for_update() {
        assert!(matches!(
            validate_readonly_sql("select * from users for update"),
            Err(EngineError::ReadOnlyViolation(_))
        ));
    }

    #[test]
    fn validate_readonly_sql_rejects_multiple_statements() {
        assert!(matches!(
            validate_readonly_sql("select * from users; drop table users"),
            Err(EngineError::ReadOnlyViolation(_))
        ));
    }

    #[test]
    fn validate_readonly_sql_allows_trailing_statement_terminator() {
        assert!(validate_readonly_sql("select * from users;").is_ok());
        assert!(validate_readonly_sql("select * from users ;\n").is_ok());
    }

    #[test]
    fn validate_readonly_sql_rejects_multiple_statements_with_trailing_terminator() {
        assert!(matches!(
            validate_readonly_sql("select * from users; drop table users;"),
            Err(EngineError::ReadOnlyViolation(_))
        ));
    }

    #[test]
    fn validate_readonly_sql_rejects_invalid_sql() {
        assert!(matches!(
            validate_readonly_sql("select from"),
            Err(EngineError::SqlSyntax(_))
        ));
    }

    #[test]
    fn limited_sql_applies_default_limit() {
        assert_eq!(
            limited_sql("select * from users", None),
            "select * from users LIMIT 1000"
        );
    }

    #[test]
    fn limited_sql_trims_input() {
        assert_eq!(
            limited_sql("  select * from users  ", Some(25)),
            "select * from users LIMIT 25"
        );
    }

    #[test]
    fn limited_sql_strips_trailing_terminator() {
        assert_eq!(
            limited_sql("select * from users;", Some(25)),
            "select * from users LIMIT 25"
        );
    }

    #[test]
    fn limited_sql_caps_max_limit() {
        assert_eq!(
            limited_sql("select * from users", Some(10_000)),
            "select * from users LIMIT 5000"
        );
    }

    #[test]
    fn limited_sql_preserves_order_by_at_top_level() {
        assert_eq!(
            limited_sql("select * from users order by created_at", Some(100)),
            "select * from users order by created_at LIMIT 100"
        );
    }

    #[test]
    fn limited_sql_keeps_existing_top_level_limit() {
        assert_eq!(
            limited_sql(
                "select * from users order by created_at limit 10",
                Some(100)
            ),
            "select * from users order by created_at limit 10"
        );
    }
}
