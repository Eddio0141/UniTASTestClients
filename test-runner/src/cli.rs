use clap::Parser;

#[derive(Parser)]
#[command(version, about)]
pub struct Args {
    #[arg(long, default_value_t = 8080)]
    /// Port to use for the TCP connection between this tool and UniTAS
    pub port: u16,

    #[arg(long)]
    /// Force downloads nightly UniTAS instead of using locally available one
    pub download_unitas: bool,

    #[arg(long)]
    /// Github token to use Github APIs to download nightly builds. If you have `gh` cli tool, `gh auth token` would easily give you a token
    pub github_token: String,
}
