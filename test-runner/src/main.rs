#[cfg(target_os = "linux")]
use std::os::unix::fs::PermissionsExt;
use std::{
    env::{self, current_exe},
    fmt::Display,
    path::Path,
    process::ExitCode,
};

use anyhow::{bail, Context, Result};
use clap::Parser;
use cli::Args;
use const_format::formatcp;
use download::{dl_bepinex, dl_test_games, dl_unitas};
use fs_utils::copy_dir_all;
use indicatif::MultiProgress;
use tokio::{
    fs,
    task::{self, JoinSet},
};
use unitas_tests::{get_linux_tests, get_win_tests};

mod cli;
mod download;
mod fs_utils;
mod symbols;
mod unitas_tests;

#[derive(Clone)]
enum Os {
    Linux,
    Windows,
}

impl Display for Os {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(
            f,
            "{}",
            match self {
                Os::Linux => "linux",
                Os::Windows => "win",
            }
        )
    }
}

#[derive(Clone)]
enum Arch {
    X64,
    X86,
}

impl Display for Arch {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        write!(
            f,
            "{}",
            match self {
                Arch::X64 => "x64",
                Arch::X86 => "x86",
            }
        )
    }
}

#[tokio::main]
async fn main() -> Result<ExitCode> {
    // dirs in executable dir is all unity games for testing
    let current_exe = current_exe().context("failed to get current exe dir")?;
    let current_dir = current_exe.parent().unwrap();
    let bepinex_dir = current_dir.join("BepInEx");
    let unitas_dir = current_dir.join("UniTAS");

    let args = Args::parse();

    // os & arch
    let os = match env::consts::OS {
        "linux" => Os::Linux,
        "windows" => Os::Windows,
        _ => bail!("unsupported os for testing"),
    };
    let arch = match env::consts::ARCH {
        "x86" => Arch::X86,
        "x86_64" => Arch::X64,
        _ => bail!("unsupported architecture for testing"),
    };

    let tests = match os {
        Os::Linux => get_linux_tests(),
        Os::Windows => get_win_tests(),
    };

    // start download and setup tasks
    let pb = MultiProgress::new();

    let dl_bepinex_task = {
        let bepinex_dir = bepinex_dir.clone();
        let arch = arch.clone();
        let pb = pb.clone();
        task::spawn(async move {
            dl_bepinex(&bepinex_dir, &os, &arch, pb).await;
        })
    };
    let dl_unitas_task = {
        let unitas_dir = unitas_dir.clone();
        let pb = pb.clone();
        let token = args.github_token.to_owned();
        task::spawn(async move { dl_unitas(&unitas_dir, args.download_unitas, pb, token).await })
    };

    let dl_games_task = {
        let current_dir = current_dir.to_path_buf();
        let token = args.github_token.to_owned();
        task::spawn(async move { dl_test_games(&current_dir, pb, token).await })
    };

    // wait for bepinex download
    dl_bepinex_task.await.unwrap();

    let mut post_bepinex_dl_tasks = JoinSet::new();

    {
        let bepinex_dir = bepinex_dir.clone();
        post_bepinex_dl_tasks
            .spawn(async move { setup_unitas_config(&bepinex_dir, args.port).await });
    }

    {
        let bepinex_dir = bepinex_dir.clone();
        let arch = arch.clone();
        post_bepinex_dl_tasks.spawn(async move { setup_bepinex(&bepinex_dir, &arch).await });
    }

    // for all UniTAS logs
    let logs_dir = current_dir.join("logs");
    fs::create_dir_all(&logs_dir)
        .await
        .context("failed to create folder for logs")?;

    // wait for unitas and bepinex dl
    dl_unitas_task.await.unwrap()?;
    setup_unitas(&unitas_dir, &bepinex_dir).await?;

    dl_games_task.await.unwrap()?;

    while post_bepinex_dl_tasks.join_next().await.is_some() {}

    // run
    for test in tests {
        test.run(current_dir, &bepinex_dir, &logs_dir, &args)?;
    }

    Ok(ExitCode::SUCCESS)
}

async fn setup_unitas(unitas_dir: &Path, bepinex_dir: &Path) -> Result<()> {
    copy_dir_all(unitas_dir, bepinex_dir)
        .await
        .with_context(|| {
            format!(
                "failed to copy UniTAS dir contents from `{}` to game folder `{}`",
                unitas_dir.display(),
                bepinex_dir.display()
            )
        })
}

fn game_bin_name(os: &Os, arch: &Arch) -> &'static str {
    match &os {
        Os::Linux => match arch {
            Arch::X64 => GAME_BIN_NAME,
            Arch::X86 => todo!(),
        },
        // TODO: does windows require this
        Os::Windows => WIN_UNITY_EXE_NAME,
    }
}

const GAME_BIN_NAME: &str = "build";
const WIN_UNITY_EXE_NAME: &str = formatcp!("{GAME_BIN_NAME}.exe");

async fn setup_bepinex(bepinex_dir: &Path, arch: &Arch) -> Result<()> {
    #[cfg(target_os = "linux")]
    {
        let run_bepinex_file = bepinex_dir.join("run_bepinex.sh");

        // modify run_bepinex to execute correct executable
        let mut run_bepinex_content =
            fs::read_to_string(&run_bepinex_file)
                .await
                .with_context(|| {
                    format!(
                        "failed to open run_bepinex.sh from `{}`",
                        run_bepinex_file.display()
                    )
                })?;

        let find_key = "executable_name=";
        let find_index = run_bepinex_content
            .find(find_key)
            .context("failed to find executable_name config in run_bepinex.sh")?;

        let exe_name = game_bin_name(&Os::Linux, arch);

        run_bepinex_content.insert_str(find_index + find_key.len() + 1, exe_name);

        fs::write(&run_bepinex_file, run_bepinex_content)
            .await
            .with_context(|| {
                format!(
                    "failed to write run_bepinex.sh content to `{}`",
                    run_bepinex_file.display()
                )
            })?;

        // set perms for execution
        let mut perms = run_bepinex_file
            .metadata()
            .with_context(|| {
                format!(
                    "failed to get run_bepinex.sh metadata from path `{}`",
                    run_bepinex_file.display()
                )
            })?
            .permissions();

        perms.set_mode(0o744);

        fs::set_permissions(&run_bepinex_file, perms)
            .await
            .with_context(|| {
                format!(
                    "failed to set execute permissions for run_bepinex.sh at path `{}`",
                    run_bepinex_file.display()
                )
            })?;
    }

    Ok(())
}

async fn setup_unitas_config(bepinex_dir: &Path, port: u16) -> Result<()> {
    let cfg = bepinex_dir.join("BepInEx").join("config");

    fs::create_dir_all(&cfg).await.with_context(|| {
        format!(
            "failed to create directory for UniTAS config file at path `{}`",
            cfg.display()
        )
    })?;

    let cfg = cfg.join("UniTAS.cfg");
    let contents = format!(
        r#"[Remote]
Enable = true
Port = {port}
"#
    );

    fs::write(&cfg, contents).await.with_context(|| {
        format!(
            "failed to write config for UniTAS to path `{}`",
            cfg.display()
        )
    })
}
