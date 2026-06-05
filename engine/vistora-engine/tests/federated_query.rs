use std::{env, path::Path, sync::Arc};

use datafusion::arrow::{
    array::{Float64Array, Int32Array},
    datatypes::{DataType, Field, Schema},
    record_batch::RecordBatch,
};
use datafusion_table_providers::{
    sql::db_connection_pool::clickhousepool::ClickHouseConnectionPool, util::secrets::to_secret_map,
};
use mysql_async::prelude::Queryable;
use parquet::arrow::ArrowWriter;
use serde_json::Value;
use tempfile::TempDir;
use tokio_postgres::NoTls;
use vistora_engine::{
    engine::{backend::BackendRegistry, executor},
    models::{DataSourceConnection, FederatedQueryRequest},
};

fn file_source(source_type: &str, path: &Path) -> DataSourceConnection {
    DataSourceConnection {
        source_type: source_type.to_string(),
        connection_string: None,
        host: None,
        port: None,
        database: None,
        username: None,
        password: None,
        schema: None,
        path: Some(path.to_string_lossy().into_owned()),
        table: None,
        alias: None,
        has_header: None,
        delimiter: None,
        sheet: None,
        schema_override: None,
    }
}

fn db_source_from_url(
    source_type: &str,
    url: &str,
    table: &str,
    alias: &str,
) -> DataSourceConnection {
    let default_port = match source_type {
        "postgres" => 5432,
        "clickhouse" => 8123,
        _ => 3306,
    };
    let rest = url.split_once("://").map(|(_, rest)| rest).unwrap_or(url);
    let (authority, database_with_query) = rest.rsplit_once('/').expect("database in url");
    let database = database_with_query
        .split(['?', '#'])
        .next()
        .expect("database name");
    let (auth, host_port) = authority.rsplit_once('@').expect("credentials in url");
    let (username, password) = auth.split_once(':').unwrap_or((auth, ""));
    let (host, port) = host_port
        .rsplit_once(':')
        .map(|(host, port)| (host, port.parse::<u16>().unwrap_or(default_port)))
        .unwrap_or((host_port, default_port));

    DataSourceConnection {
        source_type: source_type.to_string(),
        connection_string: Some(url.to_string()).filter(|_| source_type == "mongodb"),
        host: Some(host.to_string()),
        port: Some(port),
        database: Some(database.to_string()),
        username: Some(username.to_string()),
        password: Some(password.to_string()),
        schema: None,
        path: None,
        table: Some(table.to_string()),
        alias: Some(alias.to_string()),
        has_header: None,
        delimiter: None,
        sheet: None,
        schema_override: None,
    }
}

async fn clickhouse_pool(source: &DataSourceConnection) -> ClickHouseConnectionPool {
    ClickHouseConnectionPool::new(to_secret_map(std::collections::HashMap::from([
        (
            "url".to_string(),
            format!(
                "http://{}:{}",
                source.host.as_deref().expect("host"),
                source.port.expect("port")
            ),
        ),
        (
            "database".to_string(),
            source.database.as_deref().expect("database").to_string(),
        ),
        (
            "user".to_string(),
            source.username.as_deref().expect("username").to_string(),
        ),
        (
            "password".to_string(),
            source.password.as_deref().unwrap_or_default().to_string(),
        ),
    ])))
    .await
    .unwrap()
}

async fn postgres_client(url: &str) -> tokio_postgres::Client {
    let (client, connection) = tokio_postgres::connect(url, NoTls).await.unwrap();
    tokio::spawn(async move {
        if let Err(error) = connection.await {
            eprintln!("postgres test connection error: {error}");
        }
    });
    client
}

fn federated_request(sources: Vec<DataSourceConnection>, sql: &str) -> FederatedQueryRequest {
    FederatedQueryRequest {
        data_sources: sources,
        sql: sql.to_string(),
        limit: Some(100),
        timeout_ms: Some(30_000),
    }
}

async fn execute_federated_query(
    request: FederatedQueryRequest,
) -> Result<vistora_engine::models::QueryResult, vistora_engine::error::EngineError> {
    let registry = BackendRegistry::new();
    executor::execute_federated_query(&registry, request).await
}

fn write_csv(path: &Path) {
    std::fs::write(path, "customer_id,amount\n1,10.0\n2,20.0\n1,5.0\n").unwrap();
}

fn write_regions_csv(path: &Path) {
    std::fs::write(
        path,
        "region,label\nnorth,North Region\nsouth,South Region\n",
    )
    .unwrap();
}

fn write_sales_csv(path: &Path) {
    std::fs::write(path, "region,amount\nnorth,10.0\nsouth,20.0\nnorth,5.0\n").unwrap();
}

fn write_parquet(path: &Path) {
    let schema = Arc::new(Schema::new(vec![
        Field::new("customer_id", DataType::Int32, false),
        Field::new("amount", DataType::Float64, false),
    ]));
    let batch = RecordBatch::try_new(
        schema.clone(),
        vec![
            Arc::new(Int32Array::from(vec![1, 2, 1])) as _,
            Arc::new(Float64Array::from(vec![10.0, 20.0, 5.0])) as _,
        ],
    )
    .unwrap();

    let file = std::fs::File::create(path).unwrap();
    let mut writer = ArrowWriter::try_new(file, schema, None).unwrap();
    writer.write(&batch).unwrap();
    writer.close().unwrap();
}

fn as_f64(value: &Value) -> f64 {
    value.as_f64().unwrap_or_else(|| {
        value
            .as_i64()
            .map(|value| value as f64)
            .expect("numeric value")
    })
}

#[tokio::test]
async fn federated_query_can_join_multiple_file_sources() {
    let temp = TempDir::new().unwrap();
    let sales_path = temp.path().join("sales.csv");
    let regions_path = temp.path().join("regions.csv");
    write_sales_csv(&sales_path);
    write_regions_csv(&regions_path);

    let result = execute_federated_query(federated_request(
        vec![
            file_source("csv", &sales_path),
            file_source("csv", &regions_path),
        ],
        r#"
        select r.label, sum(s.amount) as total
        from sales s
        join regions r on s.region = r.region
        group by r.label
        order by r.label
        "#,
    ))
    .await
    .unwrap();

    assert_eq!(result.row_count, 2);
    assert_eq!(result.rows[0][0], Value::String("North Region".to_string()));
    assert_eq!(as_f64(&result.rows[0][1]), 15.0);
}

#[tokio::test]
async fn federated_query_rejects_duplicate_aliases() {
    let temp = TempDir::new().unwrap();
    let first = temp.path().join("first.csv");
    let second = temp.path().join("second.csv");
    write_sales_csv(&first);
    write_sales_csv(&second);

    let mut first_source = file_source("csv", &first);
    first_source.alias = Some("sales".to_string());
    let mut second_source = file_source("csv", &second);
    second_source.alias = Some("sales".to_string());

    let error = execute_federated_query(federated_request(
        vec![first_source, second_source],
        "select * from sales",
    ))
    .await
    .unwrap_err();

    assert!(
        error
            .to_string()
            .contains("duplicate federated table name 'sales'")
    );
}

#[tokio::test]
async fn postgres_table_can_be_registered_for_federated_query() {
    let Ok(url) = env::var("VISTORA_TEST_POSTGRES_URL") else {
        return;
    };
    let client = postgres_client(&url).await;
    client
        .batch_execute(
            r#"
            drop table if exists vistora_federated_pg_customers;
            create table vistora_federated_pg_customers (id integer primary key, label text not null);
            insert into vistora_federated_pg_customers (id, label) values (1, 'North'), (2, 'South');
            "#,
        )
        .await
        .unwrap();

    let source = db_source_from_url(
        "postgres",
        &url,
        "vistora_federated_pg_customers",
        "pg_customers",
    );
    let result = execute_federated_query(federated_request(
        vec![source],
        "select label from pg_customers order by id",
    ))
    .await
    .unwrap();

    assert_eq!(result.row_count, 2);
    assert_eq!(result.rows[0][0], Value::String("North".to_string()));
}

#[tokio::test]
async fn mysql_table_can_be_registered_for_federated_query() {
    let Ok(url) = env::var("VISTORA_TEST_MYSQL_URL") else {
        return;
    };
    let pool = mysql_async::Pool::new(url.as_str());
    let mut conn = pool.get_conn().await.unwrap();
    conn.query_drop("drop table if exists vistora_federated_mysql_customers")
        .await
        .unwrap();
    conn.query_drop(
        "create table vistora_federated_mysql_customers (id integer primary key, label varchar(64) not null)",
    )
        .await
        .unwrap();
    conn.query_drop(
        "insert into vistora_federated_mysql_customers (id, label) values (1, 'North'), (2, 'South')",
    )
        .await
        .unwrap();
    drop(conn);
    pool.disconnect().await.unwrap();

    let source = db_source_from_url(
        "mysql",
        &url,
        "vistora_federated_mysql_customers",
        "mysql_customers",
    );
    let result = execute_federated_query(federated_request(
        vec![source],
        "select label from mysql_customers order by id",
    ))
    .await
    .unwrap();

    assert_eq!(result.row_count, 2);
    assert_eq!(result.rows[0][0], Value::String("North".to_string()));
}

#[tokio::test]
async fn csv_can_join_with_postgres_table() {
    let Ok(url) = env::var("VISTORA_TEST_POSTGRES_URL") else {
        return;
    };
    let client = postgres_client(&url).await;
    client
        .batch_execute(
            r#"
            drop table if exists vistora_federated_pg_regions;
            create table vistora_federated_pg_regions (id integer primary key, label text not null);
            insert into vistora_federated_pg_regions (id, label) values (1, 'North'), (2, 'South');
            "#,
        )
        .await
        .unwrap();

    let temp = TempDir::new().unwrap();
    let csv_path = temp.path().join("orders.csv");
    write_csv(&csv_path);

    let db_source = db_source_from_url(
        "postgres",
        &url,
        "vistora_federated_pg_regions",
        "pg_regions",
    );
    let result = execute_federated_query(federated_request(
        vec![file_source("csv", &csv_path), db_source],
        r#"
        select r.label, sum(o.amount) as total
        from orders o
        join pg_regions r on o.customer_id = r.id
        group by r.label
        order by r.label
        "#,
    ))
    .await
    .unwrap();

    assert_eq!(result.row_count, 2);
    assert_eq!(result.rows[0][0], Value::String("North".to_string()));
    assert_eq!(as_f64(&result.rows[0][1]), 15.0);
}

#[tokio::test]
async fn parquet_can_join_with_mysql_table() {
    let Ok(url) = env::var("VISTORA_TEST_MYSQL_URL") else {
        return;
    };
    let pool = mysql_async::Pool::new(url.as_str());
    let mut conn = pool.get_conn().await.unwrap();
    conn.query_drop("drop table if exists vistora_federated_mysql_regions")
        .await
        .unwrap();
    conn.query_drop(
        "create table vistora_federated_mysql_regions (id integer primary key, label varchar(64) not null)",
    )
        .await
        .unwrap();
    conn.query_drop(
        "insert into vistora_federated_mysql_regions (id, label) values (1, 'North'), (2, 'South')",
    )
    .await
    .unwrap();
    drop(conn);
    pool.disconnect().await.unwrap();

    let temp = TempDir::new().unwrap();
    let parquet_path = temp.path().join("orders.parquet");
    write_parquet(&parquet_path);

    let db_source = db_source_from_url(
        "mysql",
        &url,
        "vistora_federated_mysql_regions",
        "mysql_regions",
    );
    let result = execute_federated_query(federated_request(
        vec![file_source("parquet", &parquet_path), db_source],
        r#"
        select r.label, sum(o.amount) as total
        from orders o
        join mysql_regions r on o.customer_id = r.id
        group by r.label
        order by r.label
        "#,
    ))
    .await
    .unwrap();

    assert_eq!(result.row_count, 2);
    assert_eq!(result.rows[0][0], Value::String("North".to_string()));
    assert_eq!(as_f64(&result.rows[0][1]), 15.0);
}

#[tokio::test]
async fn clickhouse_table_can_be_registered_for_federated_query() {
    let Ok(url) = env::var("VISTORA_TEST_CLICKHOUSE_URL") else {
        return;
    };
    let source = db_source_from_url(
        "clickhouse",
        &url,
        "vistora_federated_clickhouse_customers",
        "ch_customers",
    );
    let pool = clickhouse_pool(&source).await;
    pool.client()
        .query("drop table if exists vistora_federated_clickhouse_customers")
        .execute()
        .await
        .unwrap();
    pool.client()
        .query(
            "create table vistora_federated_clickhouse_customers (id Int32, label String) engine = Memory",
        )
        .execute()
        .await
        .unwrap();
    pool.client()
        .query(
            "insert into vistora_federated_clickhouse_customers (id, label) values (1, 'North'), (2, 'South')",
        )
        .execute()
        .await
        .unwrap();

    let result = execute_federated_query(federated_request(
        vec![source],
        "select label from ch_customers order by id",
    ))
    .await
    .unwrap();

    assert_eq!(result.row_count, 2);
    assert_eq!(result.rows[0][0], Value::String("North".to_string()));
}

#[tokio::test]
async fn csv_can_join_with_clickhouse_table() {
    let Ok(url) = env::var("VISTORA_TEST_CLICKHOUSE_URL") else {
        return;
    };
    let source = db_source_from_url(
        "clickhouse",
        &url,
        "vistora_federated_clickhouse_regions",
        "ch_regions",
    );
    let pool = clickhouse_pool(&source).await;
    pool.client()
        .query("drop table if exists vistora_federated_clickhouse_regions")
        .execute()
        .await
        .unwrap();
    pool.client()
        .query(
            "create table vistora_federated_clickhouse_regions (id Int32, label String) engine = Memory",
        )
        .execute()
        .await
        .unwrap();
    pool.client()
        .query(
            "insert into vistora_federated_clickhouse_regions (id, label) values (1, 'North'), (2, 'South')",
        )
        .execute()
        .await
        .unwrap();

    let temp = TempDir::new().unwrap();
    let csv_path = temp.path().join("orders.csv");
    write_csv(&csv_path);

    let result = execute_federated_query(federated_request(
        vec![file_source("csv", &csv_path), source],
        r#"
        select r.label, sum(o.amount) as total
        from orders o
        join ch_regions r on o.customer_id = r.id
        group by r.label
        order by r.label
        "#,
    ))
    .await
    .unwrap();

    assert_eq!(result.row_count, 2);
    assert_eq!(result.rows[0][0], Value::String("North".to_string()));
    assert_eq!(as_f64(&result.rows[0][1]), 15.0);
}
