use std::{path::PathBuf, str::FromStr};

use anyhow::{bail, Context};
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
    /// Github token to use Github APIs to download nightly builds
    /// If you have `gh` cli tool, `gh auth token` would easily give you a token
    pub github_token: String,

    #[arg(short, long, value_parser = parse_replace_games)]
    /// Replace games to download with local games by name. Example: `2022.3.41f1-base=/home/yuu/local-game`
    /// You can specify multiple --replace-game for different games
    pub replace_game: Vec<ReplaceGame>,
}

impl Args {
    pub fn validate(&self) -> anyhow::Result<()> {
        for (i, replace) in self.replace_game.iter().enumerate() {
            if self
                .replace_game
                .iter()
                .enumerate()
                .any(|(j, replace_inner)| i != j && replace_inner.name == replace.name)
            {
                bail!(
                    "found duplicate replace-game entries with name `{}`",
                    replace.name
                );
            }
        }

        Ok(())
    }
}

#[derive(Clone)]
pub struct ReplaceGame {
    pub name: String,
    pub game_path: PathBuf,
}

fn parse_replace_games(str: &str) -> Result<ReplaceGame, anyhow::Error> {
    let Some((name, game_path)) = str.split_once('=') else {
        bail!("expected pattern of `game-name=path`")
    };

    let game_path = game_path.trim_matches('"');

    let game_path = PathBuf::from_str(game_path)
        .with_context(|| format!("failed to parse path `{game_path}` as a valid path"))?;

    if !game_path.is_dir() {
        bail!("game path `{}` is not a directory", game_path.display());
    }

    Ok(ReplaceGame {
        name: name.to_string(),
        game_path,
    })
}
