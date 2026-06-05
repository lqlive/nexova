pub const DEFAULT_LIMIT: u32 = 1_000;
pub const MAX_LIMIT: u32 = 5_000;
pub const DEFAULT_TIMEOUT_MS: u64 = 30_000;

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum DataSourceKind {
    Postgres,
    MySql,
    ClickHouse,
    MongoDb,
    Sqlite,
    Csv,
    Json,
    Excel,
    Parquet,
    Files,
}
