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
}
