use tower_http::trace::TraceLayer;
use tracing_subscriber::{layer::SubscriberExt, util::SubscriberInitExt};
use vistora_engine::{api, config::Config, engine::backend::BackendRegistry};

#[tokio::main]
async fn main() -> anyhow::Result<()> {
    tracing_subscriber::registry()
        .with(
            tracing_subscriber::EnvFilter::try_from_default_env().unwrap_or_else(|_| "info".into()),
        )
        .with(tracing_subscriber::fmt::layer())
        .init();

    let config = Config::from_env()?;
    let registry = BackendRegistry::new();
    let app = api::router(registry).layer(TraceLayer::new_for_http());

    let listener = tokio::net::TcpListener::bind(config.bind_address).await?;
    tracing::info!("vistora-engine listening on {}", config.bind_address);
    axum::serve(listener, app).await?;

    Ok(())
}
