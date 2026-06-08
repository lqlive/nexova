//! S3 / object-store backed file sources.
//!
//! A source is treated as S3 when its `path` is an `s3://bucket/prefix` URL.
//! Credentials are never carried in the request: they are resolved from the
//! engine-side S3 config file (JSON), keyed by bucket name. The file location
//! defaults to `s3.config.json` and can be overridden with the
//! `VISTORA_S3_CONFIG` environment variable.

use std::{
    collections::HashMap,
    path::Path,
    sync::{Arc, OnceLock},
};

use futures::StreamExt;
use object_store::{
    ObjectMeta, ObjectStore, ObjectStoreExt, aws::AmazonS3Builder, path::Path as ObjectPath,
};
use serde::Deserialize;
use url::Url;

use crate::{engine::types::DataSourceKind, error::EngineError, models::DataSourceConnection};

/// A single object resolved on an S3-compatible store, ready for registration
/// into a DataFusion [`SessionContext`](datafusion::prelude::SessionContext).
#[derive(Clone)]
pub(super) struct RemoteObject {
    pub(super) store: Arc<dyn ObjectStore>,
    pub(super) object_path: ObjectPath,
    /// Fully qualified `s3://bucket/key` URL used by DataFusion readers.
    pub(super) url: String,
    /// `s3://bucket` URL used to register the object store on a context.
    pub(super) base_url: Url,
}

/// Per-bucket S3 settings loaded from the engine config file.
#[derive(Debug, Clone, Default, Deserialize)]
#[serde(rename_all = "camelCase")]
struct S3Settings {
    region: Option<String>,
    endpoint: Option<String>,
    access_key_id: Option<String>,
    secret_access_key: Option<String>,
    session_token: Option<String>,
    allow_http: Option<bool>,
}

#[derive(Debug, Clone, Default, Deserialize)]
struct S3Config {
    #[serde(default)]
    buckets: HashMap<String, S3Settings>,
}

static S3_CONFIG: OnceLock<S3Config> = OnceLock::new();

fn config() -> &'static S3Config {
    S3_CONFIG.get_or_init(load_config)
}

fn load_config() -> S3Config {
    let path = std::env::var("VISTORA_S3_CONFIG").unwrap_or_else(|_| "s3.config.json".to_string());
    match std::fs::read_to_string(&path) {
        Ok(content) => match serde_json::from_str::<S3Config>(&content) {
            Ok(config) => config,
            Err(error) => {
                tracing::error!(path = %path, error = %error, "failed to parse S3 config file");
                S3Config::default()
            }
        },
        Err(error) => {
            tracing::debug!(path = %path, error = %error, "S3 config file not loaded");
            S3Config::default()
        }
    }
}

/// Whether the source path points at an S3-compatible object store.
pub(super) fn is_s3(source: &DataSourceConnection) -> bool {
    source
        .path
        .as_deref()
        .map(|value| value.trim_start().to_ascii_lowercase().starts_with("s3://"))
        .unwrap_or(false)
}

struct S3Target {
    store: Arc<dyn ObjectStore>,
    bucket: String,
    base_url: Url,
    /// Object key or prefix within the bucket (may be empty).
    key: String,
}

fn build_target(source: &DataSourceConnection) -> Result<S3Target, EngineError> {
    let raw = source.require_path()?;
    let url = Url::parse(raw)
        .map_err(|error| EngineError::InvalidConnection(format!("invalid s3 url '{raw}': {error}")))?;
    let bucket = url
        .host_str()
        .filter(|host| !host.is_empty())
        .ok_or_else(|| {
            EngineError::InvalidConnection("s3 url is missing a bucket name".to_string())
        })?
        .to_string();
    let key = url.path().trim_start_matches('/').to_string();

    let settings = config().buckets.get(&bucket).ok_or_else(|| {
        EngineError::InvalidConnection(format!(
            "no S3 configuration found for bucket '{bucket}'; add it to the engine S3 config file (VISTORA_S3_CONFIG)"
        ))
    })?;

    let mut builder = AmazonS3Builder::new().with_bucket_name(&bucket);
    if let Some(value) = nonempty(&settings.region) {
        builder = builder.with_region(value);
    }
    if let Some(value) = nonempty(&settings.endpoint) {
        builder = builder.with_endpoint(value);
    }
    if let Some(value) = nonempty(&settings.access_key_id) {
        builder = builder.with_access_key_id(value);
    }
    if let Some(value) = nonempty(&settings.secret_access_key) {
        builder = builder.with_secret_access_key(value);
    }
    if let Some(value) = nonempty(&settings.session_token) {
        builder = builder.with_token(value);
    }
    if settings.allow_http.unwrap_or(false) {
        builder = builder.with_allow_http(true);
    }

    let store = builder.build().map_err(|error| {
        EngineError::InvalidConnection(format!("failed to build S3 store: {error}"))
    })?;
    let base_url = Url::parse(&format!("s3://{bucket}"))
        .map_err(|error| EngineError::InvalidConnection(format!("invalid s3 base url: {error}")))?;

    Ok(S3Target {
        store: Arc::new(store),
        bucket,
        base_url,
        key,
    })
}

fn nonempty(value: &Option<String>) -> Option<&str> {
    value
        .as_deref()
        .map(str::trim)
        .filter(|value| !value.is_empty())
}

/// Build the object store and its base URL for registration on a context.
pub(super) fn store_for(
    source: &DataSourceConnection,
) -> Result<(Arc<dyn ObjectStore>, Url), EngineError> {
    let target = build_target(source)?;
    Ok((target.store, target.base_url))
}

/// Discover the tables backing an S3 source (single object or prefix listing).
pub(super) async fn discover(
    source: &DataSourceConnection,
    requested_kind: DataSourceKind,
) -> Result<Vec<super::FileTable>, EngineError> {
    let target = build_target(source)?;

    // A key with a recognized extension is treated as a single object.
    if !target.key.is_empty() {
        if let Some(kind) = super::kind_from_extension(Path::new(&target.key)) {
            let name = super::explicit_table_name(source)
                .unwrap_or_else(|| table_name_from_key(&target.key));
            return Ok(vec![super::FileTable {
                name,
                kind,
                location: super::Location::Remote(RemoteObject {
                    store: target.store.clone(),
                    object_path: ObjectPath::from(target.key.clone()),
                    url: object_url(&target.bucket, &target.key),
                    base_url: target.base_url.clone(),
                }),
            }]);
        }
    }

    // Otherwise treat the key as a prefix and list matching objects.
    let prefix = (!target.key.is_empty()).then(|| ObjectPath::from(target.key.clone()));
    let mut stream = target.store.list(prefix.as_ref());
    let mut tables = Vec::new();
    while let Some(meta) = stream.next().await {
        let meta =
            meta.map_err(|error| EngineError::FileSource(format!("list S3 objects: {error}")))?;
        let key = meta.location.as_ref().to_string();
        let Some(kind) = super::kind_from_extension(Path::new(&key)) else {
            continue;
        };
        if !super::kind_matches(requested_kind, kind) {
            continue;
        }
        tables.push(super::FileTable {
            name: table_name_from_key(&key),
            kind,
            location: super::Location::Remote(RemoteObject {
                store: target.store.clone(),
                object_path: meta.location.clone(),
                url: object_url(&target.bucket, &key),
                base_url: target.base_url.clone(),
            }),
        });
    }

    if tables.is_empty() {
        return Err(EngineError::FileSource(
            "no supported files found for this data source".to_string(),
        ));
    }

    super::ensure_unique_table_names(&tables)?;
    Ok(tables)
}

/// Download an object's full contents (used for CSV schema inference and Excel).
pub(super) async fn read_object(
    store: &Arc<dyn ObjectStore>,
    path: &ObjectPath,
) -> Result<Vec<u8>, EngineError> {
    let result = store
        .get(path)
        .await
        .map_err(|error| EngineError::FileSource(format!("get S3 object: {error}")))?;
    let bytes = result
        .bytes()
        .await
        .map_err(|error| EngineError::FileSource(format!("read S3 object: {error}")))?;
    Ok(bytes.to_vec())
}

/// Fingerprint of the objects backing the source, used for cache invalidation.
pub(super) async fn signature(source: &DataSourceConnection) -> Result<String, EngineError> {
    let target = build_target(source)?;

    if !target.key.is_empty() && super::kind_from_extension(Path::new(&target.key)).is_some() {
        let meta = target
            .store
            .head(&ObjectPath::from(target.key.clone()))
            .await
            .map_err(|error| EngineError::FileSource(format!("head S3 object: {error}")))?;
        return Ok(object_fingerprint(&meta));
    }

    let prefix = (!target.key.is_empty()).then(|| ObjectPath::from(target.key.clone()));
    let mut stream = target.store.list(prefix.as_ref());
    let mut parts = Vec::new();
    while let Some(meta) = stream.next().await {
        let meta =
            meta.map_err(|error| EngineError::FileSource(format!("list S3 objects: {error}")))?;
        if super::kind_from_extension(Path::new(meta.location.as_ref())).is_some() {
            parts.push(object_fingerprint(&meta));
        }
    }
    parts.sort();
    Ok(parts.join("|"))
}

fn object_fingerprint(meta: &ObjectMeta) -> String {
    let etag = meta.e_tag.clone().unwrap_or_default();
    let modified = meta.last_modified.timestamp_millis();
    format!(
        "{}:{}:{}:{}",
        meta.location.as_ref(),
        meta.size,
        modified,
        etag
    )
}

fn object_url(bucket: &str, key: &str) -> String {
    format!("s3://{bucket}/{key}")
}

fn table_name_from_key(key: &str) -> String {
    let file = key.rsplit('/').next().unwrap_or(key);
    let stem = file.rsplit_once('.').map(|(stem, _)| stem).unwrap_or(file);
    super::sanitize_table_name(stem)
}
