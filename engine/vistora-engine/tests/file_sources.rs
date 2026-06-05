use std::{path::Path, sync::Arc};

use datafusion::arrow::{
    array::{Float64Array, StringArray},
    datatypes::{DataType, Field, Schema},
    record_batch::RecordBatch,
};
use parquet::arrow::ArrowWriter;
use rust_xlsxwriter::Workbook;
use serde_json::Value;
use tempfile::TempDir;
use vistora_engine::{
    engine::{backend::BackendRegistry, executor},
    models::{ColumnSchemaOverride, DataSourceConnection, FederatedQueryRequest, QueryRequest},
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

fn query_request(source: DataSourceConnection, sql: &str) -> QueryRequest {
    QueryRequest {
        data_source: source,
        sql: sql.to_string(),
        limit: Some(100),
        timeout_ms: Some(30_000),
    }
}

fn federated_query_request(sources: Vec<DataSourceConnection>, sql: &str) -> FederatedQueryRequest {
    FederatedQueryRequest {
        data_sources: sources,
        sql: sql.to_string(),
        limit: Some(100),
        timeout_ms: Some(30_000),
    }
}

fn write_csv(path: &Path) {
    std::fs::write(path, "region,amount\nnorth,10.5\nsouth,20.0\nnorth,4.5\n").unwrap();
}

fn write_regions_csv(path: &Path) {
    std::fs::write(
        path,
        "region,label\nnorth,North Region\nsouth,South Region\n",
    )
    .unwrap();
}

fn write_timeseries_csv(path: &Path) {
    std::fs::write(
        path,
        "event_id,order_time,amount\n1,2026-01-05T09:00:05,12.50\n2,2026-01-05T09:00:35,7.25\n3,2026-01-05T09:01:05,5.00\n",
    )
    .unwrap();
}

fn write_ndjson(path: &Path) {
    std::fs::write(
        path,
        r#"{"event":"view","count":2}
{"event":"click","count":5}
"#,
    )
    .unwrap();
}

fn write_parquet(path: &Path) {
    let schema = Arc::new(Schema::new(vec![
        Field::new("region", DataType::Utf8, false),
        Field::new("amount", DataType::Float64, false),
    ]));
    let batch = RecordBatch::try_new(
        schema.clone(),
        vec![
            Arc::new(StringArray::from(vec!["north", "south"])) as _,
            Arc::new(Float64Array::from(vec![15.0, 25.0])) as _,
        ],
    )
    .unwrap();

    let file = std::fs::File::create(path).unwrap();
    let mut writer = ArrowWriter::try_new(file, schema, None).unwrap();
    writer.write(&batch).unwrap();
    writer.close().unwrap();
}

fn write_excel(path: &Path) {
    let mut workbook = Workbook::new();
    {
        let worksheet = workbook.add_worksheet();
        worksheet.write_string(0, 0, "region").unwrap();
        worksheet.write_string(0, 1, "amount").unwrap();
        worksheet.write_string(1, 0, "north").unwrap();
        worksheet.write_number(1, 1, 7.0).unwrap();
        worksheet.write_string(2, 0, "south").unwrap();
        worksheet.write_number(2, 1, 9.0).unwrap();
    }
    workbook.save(path).unwrap();
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
async fn csv_single_file_uses_file_name_as_table_name() {
    let temp = TempDir::new().unwrap();
    let csv_path = temp.path().join("sales.csv");
    write_csv(&csv_path);

    let registry = BackendRegistry::new();
    let source = file_source("csv", &csv_path);

    let tables = executor::list_tables(&registry, &source).await.unwrap();
    assert_eq!(tables.len(), 1);
    assert_eq!(tables[0].name, "sales");
    assert_eq!(tables[0].table_type, "CSV");

    let columns = executor::list_columns(&registry, &source, None, "sales".to_string())
        .await
        .unwrap();
    assert_eq!(
        columns
            .iter()
            .map(|column| column.name.as_str())
            .collect::<Vec<_>>(),
        vec!["region", "amount"]
    );

    let result = executor::query(
        &registry,
        query_request(
            source,
            "select region, sum(amount) as total from sales group by region order by region",
        ),
    )
    .await
    .unwrap();

    assert_eq!(result.row_count, 2);
    assert_eq!(result.rows[0][0], Value::String("north".to_string()));
    assert_eq!(as_f64(&result.rows[0][1]), 15.0);
}

#[tokio::test]
async fn csv_infers_timestamp_without_explicit_cast() {
    let temp = TempDir::new().unwrap();
    let csv_path = temp.path().join("events.csv");
    write_timeseries_csv(&csv_path);

    let registry = BackendRegistry::new();
    let source = file_source("csv", &csv_path);

    let columns = executor::list_columns(&registry, &source, None, "events".to_string())
        .await
        .unwrap();
    let order_time = columns
        .iter()
        .find(|column| column.name == "order_time")
        .expect("order_time column");
    assert!(order_time.column_type.starts_with("Timestamp"));

    let result = executor::query(
        &registry,
        query_request(
            source,
            "select date_bin(interval '1 minute', order_time, timestamp '1970-01-01 00:00:00') as bucket, sum(amount) as total from events group by bucket order by bucket",
        ),
    )
    .await
    .unwrap();

    assert_eq!(result.row_count, 2);
}

#[tokio::test]
async fn csv_schema_override_controls_decimal_metadata() {
    let temp = TempDir::new().unwrap();
    let csv_path = temp.path().join("payments.csv");
    std::fs::write(&csv_path, "id,amount\n1,12.50\n2,\n").unwrap();

    let registry = BackendRegistry::new();
    let mut source = file_source("csv", &csv_path);
    source.schema_override = Some(vec![
        ColumnSchemaOverride {
            name: "id".to_string(),
            column_type: "int64".to_string(),
            nullable: Some(false),
            precision: None,
            scale: None,
        },
        ColumnSchemaOverride {
            name: "amount".to_string(),
            column_type: "decimal".to_string(),
            nullable: Some(true),
            precision: Some(12),
            scale: Some(2),
        },
    ]);

    let columns = executor::list_columns(&registry, &source, None, "payments".to_string())
        .await
        .unwrap();
    let amount = columns
        .iter()
        .find(|column| column.name == "amount")
        .expect("amount column");

    assert_eq!(amount.column_type, "Decimal128(12, 2)");
    assert!(amount.nullable);
    assert_eq!(amount.precision, Some(12));
    assert_eq!(amount.scale, Some(2));
}

#[tokio::test]
async fn files_directory_exposes_each_supported_file_as_a_table() {
    let temp = TempDir::new().unwrap();
    write_csv(&temp.path().join("sales.csv"));
    write_ndjson(&temp.path().join("events.ndjson"));
    write_parquet(&temp.path().join("customers.parquet"));
    std::fs::write(temp.path().join("notes.txt"), "ignored").unwrap();

    let registry = BackendRegistry::new();
    let source = file_source("files", temp.path());

    let mut table_names = executor::list_tables(&registry, &source)
        .await
        .unwrap()
        .into_iter()
        .map(|table| table.name)
        .collect::<Vec<_>>();
    table_names.sort();

    assert_eq!(table_names, vec!["customers", "events", "sales"]);

    let result = executor::query(
        &registry,
        query_request(
            source,
            "select event, count from events order by count desc limit 1",
        ),
    )
    .await
    .unwrap();

    assert_eq!(result.row_count, 1);
    assert_eq!(result.rows[0][0], Value::String("click".to_string()));
    assert_eq!(result.rows[0][1], Value::Number(5.into()));
}

#[tokio::test]
async fn parquet_file_can_be_queried_through_datafusion() {
    let temp = TempDir::new().unwrap();
    let parquet_path = temp.path().join("sales.parquet");
    write_parquet(&parquet_path);

    let registry = BackendRegistry::new();
    let source = file_source("parquet", &parquet_path);

    let result = executor::query(
        &registry,
        query_request(
            source,
            "select region, amount from sales order by amount desc",
        ),
    )
    .await
    .unwrap();

    assert_eq!(result.row_count, 2);
    assert_eq!(result.rows[0][0], Value::String("south".to_string()));
    assert_eq!(as_f64(&result.rows[0][1]), 25.0);
}

#[tokio::test]
async fn excel_file_can_be_registered_and_queried() {
    let temp = TempDir::new().unwrap();
    let xlsx_path = temp.path().join("report.xlsx");
    write_excel(&xlsx_path);

    let registry = BackendRegistry::new();
    let source = file_source("excel", &xlsx_path);

    let columns = executor::list_columns(&registry, &source, None, "report".to_string())
        .await
        .unwrap();
    assert_eq!(
        columns
            .iter()
            .map(|column| column.name.as_str())
            .collect::<Vec<_>>(),
        vec!["region", "amount"]
    );

    let result = executor::query(
        &registry,
        query_request(
            source,
            "select region, amount from report order by amount desc",
        ),
    )
    .await
    .unwrap();

    assert_eq!(result.row_count, 2);
    assert_eq!(result.rows[0][0], Value::String("south".to_string()));
    assert_eq!(as_f64(&result.rows[0][1]), 9.0);
}

#[tokio::test]
async fn explicit_table_name_is_supported_for_single_files() {
    let temp = TempDir::new().unwrap();
    let csv_path = temp.path().join("2026 sales.csv");
    write_csv(&csv_path);

    let registry = BackendRegistry::new();
    let mut source = file_source("csv", &csv_path);
    source.table = Some("sales_upload".to_string());

    let tables = executor::list_tables(&registry, &source).await.unwrap();
    assert_eq!(tables[0].name, "sales_upload");

    let result = executor::query(
        &registry,
        query_request(source, "select count(*) as total_rows from sales_upload"),
    )
    .await
    .unwrap();

    assert_eq!(result.row_count, 1);
    assert_eq!(result.rows[0][0], Value::Number(3.into()));
}

#[tokio::test]
async fn federated_query_can_join_multiple_file_sources() {
    let temp = TempDir::new().unwrap();
    let sales_path = temp.path().join("sales.csv");
    let regions_path = temp.path().join("regions.csv");
    write_csv(&sales_path);
    write_regions_csv(&regions_path);

    let sales = file_source("csv", &sales_path);
    let regions = file_source("csv", &regions_path);

    let registry = BackendRegistry::new();
    let result = executor::execute_federated_query(
        &registry,
        federated_query_request(
            vec![sales, regions],
            r#"
        select r.label, sum(s.amount) as total
        from sales s
        join regions r on s.region = r.region
        group by r.label
        order by r.label
        "#,
        ),
    )
    .await
    .unwrap();

    assert_eq!(result.row_count, 2);
    assert_eq!(result.rows[0][0], Value::String("North Region".to_string()));
    assert_eq!(as_f64(&result.rows[0][1]), 15.0);
    assert_eq!(result.rows[1][0], Value::String("South Region".to_string()));
    assert_eq!(as_f64(&result.rows[1][1]), 20.0);
}

#[tokio::test]
async fn federated_query_rejects_duplicate_table_names() {
    let temp = TempDir::new().unwrap();
    let first = temp.path().join("first.csv");
    let second = temp.path().join("second.csv");
    write_csv(&first);
    write_csv(&second);

    let mut first_source = file_source("csv", &first);
    first_source.table = Some("sales".to_string());
    let mut second_source = file_source("csv", &second);
    second_source.table = Some("sales".to_string());

    let registry = BackendRegistry::new();
    let error = executor::execute_federated_query(
        &registry,
        federated_query_request(vec![first_source, second_source], "select * from sales"),
    )
    .await
    .unwrap_err();

    assert!(
        error
            .to_string()
            .contains("duplicate federated table name 'sales'")
    );
}
