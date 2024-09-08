#[cfg(target_family = "unix")]
use std::os::unix::fs::PermissionsExt;
use std::{fmt::Write, io::Cursor, path::Path};

use anyhow::Context;
use anyhow::Result;
use gh_api::Artifact;
use gh_api::ArtifactFilter;
use indicatif::{MultiProgress, ProgressBar, ProgressState, ProgressStyle};
use regex::Regex;
use serde_json::Value;
use tokio::{
    fs,
    task::{self, JoinSet},
};
use tokio_stream::StreamExt;
use zip::ZipArchive;

use crate::cli::ReplaceGame;
use crate::fs_utils;
use crate::UNIX_UNITY_EXE_NAME;
use crate::{Arch, Os};

mod gh_api;

pub async fn dl_unitas(
    unitas_dir: &Path,
    download_unitas: bool,
    pb: MultiProgress,
    gh_token: String,
) -> Result<()> {
    if unitas_dir.is_dir() {
        if !download_unitas {
            return Ok(());
        }

        // delete directory
        fs::remove_dir_all(unitas_dir)
            .await
            .expect("failed to remove existing UniTAS directory");
    }

    let artifact = gh_api::latest_artifacts(
        "Eddio0141",
        "UniTAS",
        &gh_token,
        "build-on-push.yml",
        "main",
        Some(ArtifactFilter::TargetName("UniTAS")),
    )
    .await
    .context("failed to get latest build of UniTAS")?;

    let artifact = artifact
        .first()
        .context("failed to get download link for UniTAS")?;

    let Artifact { link, dl_len, .. } = artifact;
    let dl_len = *dl_len;

    let response = gh_api::gh_api_client(link, &gh_token)
        .await
        .context("failed to get download client for downloading UniTAS")?
        .send()
        .await
        .context("failed to send request to download UniTAS")?;
    let mut bytes = response.bytes_stream();

    let mut dl_buff = Vec::with_capacity(dl_len as usize);

    let pb = pb.add(dl_progress_bar(dl_len));
    pb.set_message("downloading UniTAS");

    while let Some(chunk) = bytes.next().await {
        let chunk = chunk.expect("failed to get chunk for UniTAS download");
        for byte in chunk {
            dl_buff.push(byte);
        }

        pb.set_position(dl_buff.len() as u64);
    }

    pb.finish_with_message("downloaded UniTAS");

    fs::create_dir_all(unitas_dir)
        .await
        .expect("failed to create directory for unitas");

    let mut archive =
        ZipArchive::new(Cursor::new(dl_buff)).expect("failed to load unitas as zip archive");

    let unitas_dir = unitas_dir.to_path_buf();
    task::spawn_blocking(move || {
        archive
            .extract(unitas_dir)
            .expect("failed to extract unitas");
    })
    .await
    .unwrap();

    Ok(())
}

pub async fn dl_bepinex(dl_dir: &Path, os: &Os, arch: &Arch, pb: MultiProgress) {
    let url = "https://api.github.com/repos/BepInEx/BepInEx/releases/latest";

    // github requires us to have User-Agent header
    let client = reqwest::Client::builder()
        .user_agent(env!("CARGO_PKG_NAME"))
        .build()
        .expect("failed to create reqwest client");

    let response = client
        .get(url)
        .send()
        .await
        .expect("failed to get response for BepInEx latest release");
    let json: Value = response
        .json()
        .await
        .expect("failed to get contents of BepInEx latest release");

    let tag_name_clean = json
        .get("tag_name")
        .expect("failed to get tag name for BepInEx release")
        .as_str()
        .expect("failed to get tag name as string");
    let tag_name_clean = &tag_name_clean[1..];
    let file_name_postfix = format!("_{tag_name_clean}.zip");

    let release_name = format!("{os}_{arch}");

    let (dl_link, dl_size) = json
        .get("assets")
        .expect("failed to get assets in BepInEx release")
        .as_array()
        .expect("failed to parse assets as array in BepInEx release")
        .iter()
        .find_map(|artifact| {
            let name = artifact
                .get("name")
                .expect("failed to get name of BepInEx release")
                .as_str()
                .expect("failed to get name of BepInEx release");
            if name != format!("BepInEx_{release_name}{file_name_postfix}") {
                return None;
            }

            let url = artifact
                .get("browser_download_url")
                .expect("failed to get BepInEx download url")
                .as_str()
                .expect("failed to get BepInEx download url")
                .to_string();

            let size = artifact
                .get("size")
                .expect("failed to get BepInEx download size")
                .as_u64()
                .expect("BepInEx download size isn't u64");

            Some((url, size))
        })
        .expect("failed to find the BepInEx file in the latest release");

    let mut dl_buff = Vec::with_capacity(dl_size as usize);

    let remove_dl_dir_task = if dl_dir.is_dir() {
        let dl_dir = dl_dir.to_path_buf();
        let task = task::spawn(async move {
            fs::remove_dir_all(dl_dir)
                .await
                .expect("failed to remove old BepInEx dir");
        });
        Some(task)
    } else {
        None
    };

    // dl
    let response = client
        .get(dl_link)
        .build()
        .expect("failed to GET request for BepInEx download");

    let mut bytes = client
        .execute(response)
        .await
        .expect("failed to GET request for BepInEx download")
        .bytes_stream();

    let pb = pb.add(dl_progress_bar(dl_size));
    pb.set_message("downloading BepInEx");

    while let Some(chunk) = bytes.next().await {
        let chunk = chunk.expect("failed to get BepInEx download next chunk");
        for byte in chunk {
            dl_buff.push(byte);
        }
        pb.set_position(dl_buff.len() as u64);
    }

    pb.finish_with_message("downloaded BepInEx");

    if let Some(task) = remove_dl_dir_task {
        task.await.unwrap();
        fs::create_dir_all(dl_dir)
            .await
            .expect("failed to create dir for BepInEx download");
    }

    let dl_dir = dl_dir.to_owned();

    task::spawn_blocking(move || {
        let mut archive = ZipArchive::new(Cursor::new(dl_buff))
            .expect("failed to load latest BepInEx release as zip archive");

        archive
            .extract(dl_dir)
            .expect("failed to extract BepInEx download");
    })
    .await
    .unwrap();
}

pub async fn dl_test_games(
    exe_dir: &Path,
    pb: MultiProgress,
    gh_token: String,
    replace_games: Vec<ReplaceGame>,
) -> Result<()> {
    let artifacts = gh_api::latest_artifacts(
        "Eddio0141",
        "UniTASTestClients",
        &gh_token,
        "build-on-push.yml",
        "main",
        Some(ArtifactFilter::ExcludeNamePattern(
            Regex::new(&format!("{}.*", env!("CARGO_PKG_NAME"))).unwrap(),
        )),
    )
    .await
    .context("failed to get latest build of UniTAS test games")?;

    let mut dl_tasks: JoinSet<std::result::Result<(), anyhow::Error>> = JoinSet::new();

    // now download from links
    for artifact in artifacts {
        let Artifact { link, dl_len, name } = artifact;

        let use_local_file = replace_games.iter().find_map(|replace_game| {
            if replace_game.name == name {
                Some(replace_game.game_path.to_owned())
            } else {
                None
            }
        });

        let exe_dir = exe_dir.to_path_buf();
        let pb = pb.clone();
        let gh_token = gh_token.clone();
        dl_tasks.spawn(async move {
            let dl_dir = exe_dir.join(&name);

            if let Some(use_local_folder) = use_local_file {
                fs_utils::copy_dir_all(&use_local_folder, &dl_dir)
                    .await
                    .with_context(|| {
                        format!(
                            "failed to copy game folder from `{}` to `{}`",
                            use_local_folder.display(),
                            dl_dir.display()
                        )
                    })?;

                return Ok(());
            }

            let dl_fail_err = |name, link| {
                format!("failed to get response for downloading game `{name}` with link `{link}`")
            };

            let response = gh_api::gh_api_client(&link, &gh_token)
                .await
                .with_context(|| dl_fail_err(&name, &link))?
                .send()
                .await
                .with_context(|| dl_fail_err(&name, &link))?;

            let mut dl_buff = Vec::with_capacity(dl_len as usize);

            let mut bytes = response.bytes_stream();

            let pb = pb.add(dl_progress_bar(dl_len));
            pb.set_message(format!("downloading game `{name}`"));

            while let Some(chunk) = bytes.next().await {
                let chunk = chunk.expect("failed to download unity game contents");

                for byte in chunk {
                    dl_buff.push(byte);
                }
                pb.set_position(dl_buff.len() as u64);
            }

            pb.finish_with_message(format!("downloaded game `{name}`"));

            fs::create_dir_all(&dl_dir)
                .await
                .expect("failed to create directory for unity game");

            let mut archive = ZipArchive::new(Cursor::new(dl_buff))
                .expect("failed to load unity game as zip archive");

            {
                let dl_dir = dl_dir.clone();
                task::spawn_blocking(move || {
                    archive
                        .extract(&dl_dir)
                        .expect("failed to extract unity game");
                })
            }
            .await
            .unwrap();

            // chmod game binary
            #[cfg(target_family = "unix")]
            {
                let game_bin = dl_dir.join(UNIX_UNITY_EXE_NAME);

                // set perms for execution
                let mut perms = game_bin
                    .metadata()
                    .expect("failed to get game file metadata")
                    .permissions();

                perms.set_mode(0o744);

                fs::set_permissions(game_bin, perms)
                    .await
                    .expect("failed to set execute permissions for game");
            }

            Ok(())
        });
    }

    while let Some(res) = dl_tasks.join_next().await {
        res.unwrap()?;
    }

    Ok(())
}

fn dl_progress_bar(dl_size: u64) -> ProgressBar {
    let pb = ProgressBar::new(dl_size);
    pb.set_style(
        ProgressStyle::with_template(
            "[{elapsed_precise}] [{bar:40.cyan/blue}] {bytes:>7}/{total_bytes:7} ({bytes_per_sec}) {msg}",
        )
        .unwrap()
        .with_key("eta", |state: &ProgressState, w: &mut dyn Write| {
            write!(w, "{:.1}s", state.eta().as_secs_f64()).unwrap()
        })
        .progress_chars("##-"),
    );

    pb
}
