use std::{env, net::SocketAddr};

pub struct Config {
    pub bind_address: SocketAddr,
}

impl Config {
    pub fn from_env() -> anyhow::Result<Self> {
        let bind_address = env::var("VISION_ENGINE_BIND")
            .unwrap_or_else(|_| "127.0.0.1:7071".to_string())
            .parse()?;

        Ok(Self { bind_address })
    }
}
