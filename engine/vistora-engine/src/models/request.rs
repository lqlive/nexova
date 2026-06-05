use serde::Deserialize;

use crate::{engine::types::DataSourceKind, error::EngineError};

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct DataSourceRequest {
    pub data_source: DataSourceConnection,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct QueryRequest {
    pub data_source: DataSourceConnection,
    pub sql: String,
    pub limit: Option<u32>,
    pub timeout_ms: Option<u64>,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct FederatedQueryRequest {
    pub data_sources: Vec<DataSourceConnection>,
    pub sql: String,
    pub limit: Option<u32>,
    pub timeout_ms: Option<u64>,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct SchemaColumnsRequest {
    pub data_source: DataSourceConnection,
    pub schema: Option<String>,
    pub table: String,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct DataSourceConnection {
    #[serde(rename = "type")]
    pub source_type: String,
    // Database connection fields (postgres / mysql / clickhouse / mongodb).
    pub connection_string: Option<String>,
    pub host: Option<String>,
    pub port: Option<u16>,
    pub database: Option<String>,
    pub username: Option<String>,
    pub password: Option<String>,
    /// Database schema used by federated query table registration.
    /// PostgreSQL defaults to `public`; MySQL defaults to the current database.
    pub schema: Option<String>,
    // File based connection fields (csv / json / excel / parquet / files).
    pub path: Option<String>,
    /// Logical table name exposed to SQL for a single file source.
    /// Defaults to the sanitized file name when omitted.
    pub table: Option<String>,
    /// Logical table name exposed to federated SQL.
    /// Defaults to `table` for database sources or the discovered file table name.
    pub alias: Option<String>,
    /// Whether the first CSV row contains column headers. Defaults to true.
    pub has_header: Option<bool>,
    /// Single character CSV delimiter. Defaults to `,`.
    pub delimiter: Option<String>,
    /// Excel sheet name. Defaults to the first sheet.
    pub sheet: Option<String>,
    /// Optional schema override for file sources.
    /// Useful for CSV date/timestamp/decimal columns that would otherwise be strings.
    pub schema_override: Option<Vec<ColumnSchemaOverride>>,
}

#[derive(Debug, Clone, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ColumnSchemaOverride {
    pub name: String,
    #[serde(rename = "type")]
    pub column_type: String,
    pub nullable: Option<bool>,
    pub precision: Option<u8>,
    pub scale: Option<i8>,
}

impl DataSourceConnection {
    pub fn kind(&self) -> Result<DataSourceKind, EngineError> {
        match self.source_type.to_ascii_lowercase().as_str() {
            "postgres" | "postgresql" => Ok(DataSourceKind::Postgres),
            "mysql" => Ok(DataSourceKind::MySql),
            "clickhouse" | "click_house" => Ok(DataSourceKind::ClickHouse),
            "mongodb" | "mongo" => Ok(DataSourceKind::MongoDb),
            "sqlite" | "sqlite3" => Ok(DataSourceKind::Sqlite),
            "csv" => Ok(DataSourceKind::Csv),
            "json" | "ndjson" => Ok(DataSourceKind::Json),
            "excel" | "xlsx" | "xls" => Ok(DataSourceKind::Excel),
            "parquet" | "pq" => Ok(DataSourceKind::Parquet),
            "file" | "files" | "folder" | "directory" => Ok(DataSourceKind::Files),
            _ => Err(EngineError::UnsupportedDataSource),
        }
    }

    /// Filesystem path for a file source, validated to be present.
    pub fn require_path(&self) -> Result<&str, EngineError> {
        self.path
            .as_deref()
            .map(str::trim)
            .filter(|path| !path.is_empty())
            .ok_or_else(|| EngineError::InvalidConnection("missing file path".to_string()))
    }

    pub fn connection_string(&self) -> Result<String, EngineError> {
        if self.kind()? == DataSourceKind::MongoDb {
            if let Some(connection_string) = self
                .connection_string
                .as_deref()
                .map(str::trim)
                .filter(|value| !value.is_empty())
            {
                return Ok(connection_string.to_string());
            }
        }

        let host = self.require_field(self.host.as_deref(), "host")?;
        let port = self
            .port
            .ok_or_else(|| EngineError::InvalidConnection("missing port".to_string()))?;
        let database =
            urlencoding::encode(self.require_field(self.database.as_deref(), "database")?);
        let username =
            urlencoding::encode(self.require_field(self.username.as_deref(), "username")?);
        let password = urlencoding::encode(self.password.as_deref().unwrap_or_default());

        Ok(match self.kind()? {
            DataSourceKind::Postgres => {
                format!("postgres://{username}:{password}@{host}:{port}/{database}")
            }
            DataSourceKind::MySql => {
                format!("mysql://{username}:{password}@{host}:{port}/{database}")
            }
            DataSourceKind::ClickHouse => {
                format!("http://{host}:{port}")
            }
            DataSourceKind::MongoDb => {
                format!("mongodb://{username}:{password}@{host}:{port}/{database}")
            }
            DataSourceKind::Sqlite => return Err(EngineError::UnsupportedDataSource),
            DataSourceKind::Csv
            | DataSourceKind::Json
            | DataSourceKind::Excel
            | DataSourceKind::Parquet
            | DataSourceKind::Files => return Err(EngineError::UnsupportedDataSource),
        })
    }

    fn require_field<'a>(
        &self,
        value: Option<&'a str>,
        field: &str,
    ) -> Result<&'a str, EngineError> {
        value
            .map(str::trim)
            .filter(|value| !value.is_empty())
            .ok_or_else(|| EngineError::InvalidConnection(format!("missing {field}")))
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn source(source_type: &str) -> DataSourceConnection {
        DataSourceConnection {
            source_type: source_type.to_string(),
            connection_string: None,
            host: Some("localhost".to_string()),
            port: Some(5432),
            database: Some("demo db".to_string()),
            username: Some("user@example.com".to_string()),
            password: Some("p@ss word".to_string()),
            schema: None,
            path: None,
            table: None,
            alias: None,
            has_header: None,
            delimiter: None,
            sheet: None,
            schema_override: None,
        }
    }

    fn file_source(source_type: &str, path: &str) -> DataSourceConnection {
        DataSourceConnection {
            source_type: source_type.to_string(),
            connection_string: None,
            host: None,
            port: None,
            database: None,
            username: None,
            password: None,
            schema: None,
            path: Some(path.to_string()),
            table: None,
            alias: None,
            has_header: None,
            delimiter: None,
            sheet: None,
            schema_override: None,
        }
    }

    #[test]
    fn kind_supports_postgres_aliases() {
        assert_eq!(source("postgres").kind().unwrap(), DataSourceKind::Postgres);
        assert_eq!(
            source("postgresql").kind().unwrap(),
            DataSourceKind::Postgres
        );
    }

    #[test]
    fn kind_supports_mysql() {
        assert_eq!(source("mysql").kind().unwrap(), DataSourceKind::MySql);
    }

    #[test]
    fn kind_supports_clickhouse() {
        assert_eq!(
            source("clickhouse").kind().unwrap(),
            DataSourceKind::ClickHouse
        );
    }

    #[test]
    fn kind_supports_mongodb() {
        assert_eq!(source("mongodb").kind().unwrap(), DataSourceKind::MongoDb);
        assert_eq!(source("mongo").kind().unwrap(), DataSourceKind::MongoDb);
    }

    #[test]
    fn kind_supports_sqlite() {
        assert_eq!(
            file_source("sqlite", "C:/data/demo.db").kind().unwrap(),
            DataSourceKind::Sqlite
        );
        assert_eq!(
            file_source("sqlite3", "C:/data/demo.db").kind().unwrap(),
            DataSourceKind::Sqlite
        );
    }

    #[test]
    fn kind_supports_file_sources() {
        assert_eq!(
            file_source("csv", "/tmp/a.csv").kind().unwrap(),
            DataSourceKind::Csv
        );
        assert_eq!(
            file_source("json", "/tmp/a.json").kind().unwrap(),
            DataSourceKind::Json
        );
        assert_eq!(
            file_source("xlsx", "/tmp/a.xlsx").kind().unwrap(),
            DataSourceKind::Excel
        );
        assert_eq!(
            file_source("parquet", "/tmp/a.parquet").kind().unwrap(),
            DataSourceKind::Parquet
        );
        assert_eq!(
            file_source("pq", "/tmp/a.parquet").kind().unwrap(),
            DataSourceKind::Parquet
        );
        assert_eq!(
            file_source("files", "/tmp/data").kind().unwrap(),
            DataSourceKind::Files
        );
    }

    #[test]
    fn require_path_rejects_blank() {
        let mut source = file_source("csv", "   ");
        assert!(matches!(
            source.require_path(),
            Err(EngineError::InvalidConnection(_))
        ));

        source.path = Some("/tmp/a.csv".to_string());
        assert_eq!(source.require_path().unwrap(), "/tmp/a.csv");
    }

    #[test]
    fn connection_string_rejects_file_sources() {
        assert!(matches!(
            file_source("csv", "/tmp/a.csv").connection_string(),
            Err(EngineError::UnsupportedDataSource)
        ));
    }

    #[test]
    fn kind_rejects_unknown_source_type() {
        assert!(matches!(
            source("unknown-db").kind(),
            Err(EngineError::UnsupportedDataSource)
        ));
    }

    #[test]
    fn connection_string_percent_encodes_postgres_credentials() {
        let connection_string = source("postgres").connection_string().unwrap();

        assert_eq!(
            connection_string,
            "postgres://user%40example.com:p%40ss%20word@localhost:5432/demo%20db"
        );
    }

    #[test]
    fn connection_string_uses_mysql_scheme() {
        let mut source = source("mysql");
        source.port = Some(3306);

        let connection_string = source.connection_string().unwrap();

        assert_eq!(
            connection_string,
            "mysql://user%40example.com:p%40ss%20word@localhost:3306/demo%20db"
        );
    }

    #[test]
    fn connection_string_uses_clickhouse_url() {
        let mut source = source("clickhouse");
        source.port = Some(8123);

        let connection_string = source.connection_string().unwrap();

        assert_eq!(connection_string, "http://localhost:8123");
    }

    #[test]
    fn connection_string_uses_mongodb_uri_when_provided() {
        let mut source = source("mongodb");
        source.connection_string = Some(
            "mongodb://root:password@localhost:27017/mongo_db?authSource=admin&tls=false"
                .to_string(),
        );

        let connection_string = source.connection_string().unwrap();

        assert_eq!(
            connection_string,
            "mongodb://root:password@localhost:27017/mongo_db?authSource=admin&tls=false"
        );
    }
}
