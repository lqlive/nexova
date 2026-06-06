# Vistora

Vistora is a Superset-like BI prototype with a C# product/API layer and a Rust query engine.

## Current Scope

- ASP.NET Core API for data sources, SQL execution, query history, datasets, charts, and dashboards.
- Rust `vistora-engine` service for PostgreSQL, MySQL, ClickHouse, MongoDB, and file (CSV/JSON/Excel/Parquet) connection tests, schema discovery, read-only SQL queries, and DataFusion federated queries.
- In-memory metadata storage for the first iteration.

## Repository Layout

```text
  assets/                         Sample CSV files, SQL examples, and request snippets.
  csharp/
    Vistora.slnx
    src/
      Vistora/                    ASP.NET Core API host.
      Vistora.Core/               BI metadata entities and store abstractions.
      Vistora.Database.PostgreSQL/ PostgreSQL EF Core context and store implementations.
  rust/
    vistora-engine/               Rust query engine and file/database connectors.
```

## Run Locally

Start the Rust query engine:

```powershell
cd rust/vistora-engine
cargo run
```

Rust engine layout:

```text
rust/vistora-engine/
  Cargo.toml
  src/
    lib.rs
    main.rs
    config.rs
    error.rs
    api/
      mod.rs
      query.rs
      schema.rs
      health.rs
    engine/
      mod.rs
      backend/
        mod.rs
        registry.rs
      executor.rs
      providers/
        mod.rs
        postgres.rs
        mysql.rs
        clickhouse.rs
        mongodb.rs
        sqlite.rs
      federated/
        mod.rs
        files.rs
        databases.rs
      files/
        mod.rs
        csv.rs
        json.rs
        excel.rs
        parquet.rs
      types.rs
    models/
      mod.rs
      request.rs
      response.rs
  tests/
    file_sources.rs
    federated_query.rs
```

Start the C# API:

```powershell
dotnet run --project csharp/src/Vistora/Vistora.csproj
```

The API expects the engine at `http://localhost:7071` by default. You can change this in `csharp/src/Vistora/appsettings.json`.

C# project layout:

```text
csharp/
  Vistora.slnx
  src/
    Vistora/
      Program.cs
      Apis/
    Vistora.Core/
      Entities/
      Store/
        IDataSourceStore.cs
        IDatasetStore.cs
        InMemoryDataSourceStore.cs
        InMemoryDatasetStore.cs
    Vistora.Database.PostgreSQL/
      PostgreSQLContext.cs
      Management/
      Store/
```

## Data Source Connection Shape

```json
{
  "name": "Local Postgres",
  "type": "postgres",
  "connection": {
    "host": "localhost",
    "port": "5432",
    "database": "demo",
    "username": "postgres",
    "password": "password"
  }
}
```

Supported `type` values:

- `postgres`
- `mysql`
- `clickhouse`
- `mongodb`
- `csv`
- `json` (newline-delimited / JSON Lines)
- `excel` (`xlsx` / `xls`)
- `parquet`
- `files` (directory or mixed file source)

### File data sources

Database sources use `datafusion-table-providers` for PostgreSQL, MySQL,
ClickHouse, and MongoDB single-source queries and federated queries. Vistora registers
database tables into DataFusion through provider factories instead of
implementing its own database `TableProvider`.

ClickHouse uses the HTTP endpoint expected by the provider, typically port
`8123`:

```json
{
  "type": "clickhouse",
  "host": "localhost",
  "port": 8123,
  "database": "default",
  "username": "default",
  "password": ""
}
```

MongoDB can use a direct connection string, matching the provider's native
configuration:

```json
{
  "type": "mongodb",
  "connectionString": "mongodb://root:password@localhost:27017/mongo_db?authSource=admin&tls=false",
  "host": "localhost",
  "port": 27017,
  "database": "mongo_db",
  "username": "root",
  "password": "password"
}
```

File sources are powered by DataFusion (CSV/JSON/Parquet) and `calamine` + Arrow
(Excel) inside `vistora-engine`. Instead of `host`/`port`/`database`/`username`,
they use a file or directory path:

```json
{
  "type": "csv",
  "path": "C:/assets/sales.csv",
  "hasHeader": true,
  "delimiter": ","
}
```

For a single file, the default table name comes from the file name, e.g.
`C:/assets/sales.csv` is registered as `sales`. You can override it with `table`.

For a directory, each supported file is registered as a table:

```json
{
  "type": "files",
  "path": "C:/assets"
}
```

If the directory contains `sales.csv`, `events.ndjson`, and `customers.parquet`,
queries can reference `sales`, `events`, and `customers`.

File connection fields:

- `path` (required): absolute path to the file readable by the engine process.
- `table` (optional, single file): table name referenced in SQL; defaults to the sanitized file name.
- `hasHeader` (optional, CSV/Excel): whether the first row is the header, defaults to `true`.
- `delimiter` (optional, CSV): single-character delimiter, defaults to `,`.
- `sheet` (optional, Excel): sheet name, defaults to the first sheet.

Table names are sanitized to DataFusion-friendly identifiers: non-alphanumeric
characters become `_`, and names that start with a digit are prefixed with `t_`.
Query them with the registered table name, e.g. `select * from sales`.

### Federated Query

`POST /query/federated` runs one SQL statement against multiple registered data
sources in the same federation-aware DataFusion context. File sources are
registered directly with DataFusion. PostgreSQL, MySQL, ClickHouse, and MongoDB are
registered through `datafusion-table-providers`; Vistora does not implement its
own database `TableProvider`.

```json
{
  "dataSources": [
    { "type": "csv", "path": "C:/assets/sales.csv" },
    { "type": "parquet", "path": "C:/assets/customers.parquet" }
  ],
  "sql": "select * from sales join customers on sales.customer_id = customers.id",
  "limit": 100
}
```

For database sources in federated query, `table` is required and `alias` is the
table name visible to SQL. If `alias` is omitted, it defaults to `table`.
PostgreSQL `schema` defaults to `public`; MySQL and ClickHouse `schema` default
to the current `database`. MongoDB uses the database from the connection string
or `database` field, and `table` refers to the collection name.

```json
{
  "dataSources": [
    { "type": "csv", "path": "C:/assets/orders.csv" },
    {
      "type": "postgres",
      "host": "localhost",
      "port": 5432,
      "database": "demo",
      "username": "postgres",
      "password": "password",
      "schema": "public",
      "table": "customers",
      "alias": "pg_customers"
    }
  ],
  "sql": "select c.segment, sum(o.amount) from orders o join pg_customers c on o.customer_id = c.id group by c.segment",
  "limit": 100
}
```

## Notes

- The Rust engine only allows `SELECT` and `WITH` queries.
- Queries are wrapped with a bounded `LIMIT`; the maximum first-version limit is `5000`.
- Metadata is in-memory for now and will reset when the API process restarts.
- Excel columns are inferred as numbers when every non-empty cell is numeric, otherwise text.
