#[cfg(target_os = "linux")]
use std::os::unix::fs::PermissionsExt;
use std::{io::Cursor, path::Path, time::Duration};

use reqwest::redirect::Policy;
use tokio::{
    fs,
    task::{self, JoinSet},
};
use tokio_stream::StreamExt;
use zip::ZipArchive;

use crate::{Arch, Os, GAME_BIN_NAME};

pub async fn dl_unitas(unitas_dir: &Path, download_unitas: bool) {
    if unitas_dir.is_dir() {
        if !download_unitas {
            println!("skipping downloading unitas, already found UniTAS directory and --download-unitas argument isn't passed");
            return;
        }

        // delete directory
        fs::remove_dir_all(unitas_dir)
            .await
            .expect("failed to remove existing UniTAS directory");
    }

    println!("downloading unitas");

    let url = "https://nightly.link/Eddio0141/UniTAS/workflows/build-on-push/main/Release.zip";

    let client = reqwest::Client::builder()
        .redirect(Policy::default())
        .build()
        .expect("failed to create client for downloading unitas");

    let response = client
        .get(url)
        .build()
        .expect("failed to build get request for downloading unitas");
    let bytes = client
        .execute(response)
        .await
        .expect("failed to download unitas")
        .bytes()
        .await
        .expect("failed to download unitas contents");

    println!("downloaded UniTAS");

    fs::create_dir_all(unitas_dir)
        .await
        .expect("failed to create directory for unitas");

    let mut archive =
        ZipArchive::new(Cursor::new(bytes)).expect("failed to load unitas as zip archive");

    println!("extracting UniTAS");
    archive
        .extract(unitas_dir)
        .expect("failed to extract unitas");
    println!("extracted UniTAS");
}

pub async fn dl_bepinex(dl_dir: &Path, os: &Os, arch: &Arch) {
    println!("downloading bepinex");
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
    let text = response
        .text()
        .await
        .expect("failed to get contents of BepInEx latest release");
    let json: serde_json::Value =
        serde_json::from_str(&text).expect("failed to parse github's json response");

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

            Some((url, size as usize))
        })
        .expect("failed to find the BepInEx file in the latest release");

    let mut dl_buff = Vec::with_capacity(dl_size);

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

    while let Some(chunk) = bytes.next().await {
        let chunk = chunk.expect("failed to get BepInEx download next chunk");
        for byte in chunk {
            dl_buff.push(byte);
        }
    }

    println!("downloaded BepInEx");

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

pub async fn dl_test_games(exe_dir: &Path) {
    let url = "https://nightly.link/Eddio0141/UniTASTestClients/workflows/build-on-push/main";

    let response = reqwest::get(url)
        .await
        .expect("failed to get latest unity test game builds")
        .text()
        .await
        .expect("failed to get text content for latest unity test game builds");

    // extract links
    let key = ".zip\"";
    let links = response.lines().filter_map(|mut line| {
        let i = line.find(key)?;

        line = &line[..i + key.len() - 1];

        // now find the other bracket, this shouldn't fail
        let i = line
            .rfind('"')
            .expect("failed to find matching bracket for zip download link");

        line = &line[i + 1..];

        // grab name
        let i = line.rfind(".").expect("failed to get name for artifact");
        let name = &line[..i];
        let i = line.rfind('/').expect("failed to get name for artifact");
        let name = &name[i + 1..];

        // filter out test-runner itself
        // named like test-runner-unix, test-runner-win
        let name = name.to_string();
        let Some(i) = line.rfind('-') else {
            return Some((name, line.to_string()));
        };

        if line[..i].ends_with(env!("CARGO_PKG_NAME")) {
            None
        } else {
            Some((name, line.to_string()))
        }
    });

    let mut dl_tasks = JoinSet::new();

    // now download from links
    for (name, link) in links {
        let exe_dir = exe_dir.to_path_buf();
        dl_tasks.spawn(async move {
            let client = reqwest::Client::builder()
                .redirect(Policy::default())
                .timeout(Duration::from_secs(60))
                .build()
                .expect("failed to create client for downloading unity games");

            println!("downloading unity game `{name}`");

            let response = client
                .get(link)
                .build()
                .expect("failed to build get request for downloading unity game");
            let bytes = client
                .execute(response)
                .await
                .expect("failed to download unity game")
                .bytes()
                .await
                .expect("failed to download unity game contents");

            println!("downloaded game `{name}`");

            let dl_dir = exe_dir.join(&name);
            fs::create_dir_all(&dl_dir)
                .await
                .expect("failed to create directory for unity game");

            let mut archive = ZipArchive::new(Cursor::new(bytes))
                .expect("failed to load unity game as zip archive");

            {
                let dl_dir = dl_dir.clone();
                task::spawn_blocking(move || {
                    println!("extracting game `{name}`");
                    archive
                        .extract(&dl_dir)
                        .expect("failed to extract unity game");
                    println!("extracted game `{name}");
                })
            }
            .await
            .unwrap();

            // chmod game binary
            #[cfg(target_os = "linux")]
            {
                let game_bin = dl_dir.join(GAME_BIN_NAME);

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
        });
    }

    while dl_tasks.join_next().await.is_some() {}

    println!("all games are downloaded and ready");
}
