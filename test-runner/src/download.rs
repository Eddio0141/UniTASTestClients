#[cfg(target_os = "linux")]
use std::os::unix::fs::PermissionsExt;
use std::{fs, io::Cursor, path::Path, time::Duration};

use reqwest::redirect::Policy;
use zip::ZipArchive;

use crate::{Arch, Os, GAME_BIN_NAME};

pub fn dl_unitas(unitas_dir: &Path, use_local_unitas: bool) {
    if unitas_dir.is_dir() && use_local_unitas {
        println!(
            "skipping downloading unitas, already found UniTAS directory and --use-local-unitas is set"
        );
        return;
    }

    println!("downloading unitas");

    let url = "https://nightly.link/Eddio0141/UniTAS/workflows/build-on-push/main/Release.zip";

    let client = reqwest::blocking::Client::builder()
        .redirect(Policy::default())
        .build()
        .expect("failed to create client for downloading unitas");

    let response = client
        .get(url)
        .build()
        .expect("failed to build get request for downloading unitas");
    let bytes = client
        .execute(response)
        .expect("failed to download unitas")
        .bytes()
        .expect("failed to download unitas contents");

    println!("downloaded");

    fs::create_dir_all(unitas_dir).expect("failed to create directory for unitas");

    let mut archive =
        ZipArchive::new(Cursor::new(bytes)).expect("failed to load unitas as zip archive");

    println!("extracting");
    archive
        .extract(unitas_dir)
        .expect("failed to extract unitas");
    println!("extracted");

    println!("done");
}

pub fn dl_bepinex(dl_dir: &Path, os: &Os, arch: &Arch) {
    println!("downloading bepinex");
    let url = "https://api.github.com/repos/BepInEx/BepInEx/releases/latest";

    // github requires us to have User-Agent header
    let client = reqwest::blocking::Client::builder()
        .user_agent(env!("CARGO_PKG_NAME"))
        .build()
        .expect("failed to create reqwest client");

    let response = client
        .get(url)
        .send()
        .expect("failed to get response for BepInEx latest release");
    let text = response
        .text()
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

    let dl_link = json
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

            Some(url)
        })
        .expect("failed to find the BepInEx file in the latest release");

    if dl_dir.is_dir() {
        fs::remove_dir_all(dl_dir).expect("failed to remove old BepInEx dir");
    }

    // dl
    let response = client
        .get(dl_link)
        .build()
        .expect("failed to GET request for BepInEx download");
    let bytes = client
        .execute(response)
        .expect("failed to GET request for BepInEx download")
        .bytes()
        .expect("failed to get contents of latest BepInEx release");

    fs::create_dir_all(dl_dir).expect("failed to create dir for BepInEx download");

    let mut archive = ZipArchive::new(Cursor::new(bytes))
        .expect("failed to load latest BepInEx release as zip archive");

    archive
        .extract(dl_dir)
        .expect("failed to extract BepInEx download");

    println!("done");
}

pub fn dl_test_games(exe_dir: &Path) {
    println!("downloading unity games");

    let url = "https://nightly.link/Eddio0141/UniTASTestClients/workflows/build-on-push/main";

    let response = reqwest::blocking::get(url)
        .expect("failed to get latest unity test game builds")
        .text()
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
        let Some(i) = line.rfind('-') else {
            return Some((name, line));
        };

        if line[..i].ends_with(env!("CARGO_PKG_NAME")) {
            None
        } else {
            Some((name, line))
        }
    });

    let client = reqwest::blocking::Client::builder()
        .redirect(Policy::default())
        .timeout(Duration::from_secs(60))
        .build()
        .expect("failed to create client for downloading unity games");

    // now download from links
    for (name, link) in links {
        println!("downloading unity game `{name}` with link `{link}`");

        let response = client
            .get(link)
            .build()
            .expect("failed to build get request for downloading unity game");
        let bytes = client
            .execute(response)
            .expect("failed to download unity game")
            .bytes()
            .expect("failed to download unity game contents");

        println!("downloaded");

        let dl_dir = exe_dir.join(name);
        fs::create_dir_all(&dl_dir).expect("failed to create directory for unity game");

        let mut archive =
            ZipArchive::new(Cursor::new(bytes)).expect("failed to load unity game as zip archive");

        println!("extracting");
        archive
            .extract(&dl_dir)
            .expect("failed to extract unity game");
        println!("extracted");

        // chmod game binary
        #[cfg(target_os = "linux")]
        {
            println!("changing game binary execution permission");

            let game_bin = dl_dir.join(GAME_BIN_NAME);

            // set perms for execution
            let mut perms = game_bin
                .metadata()
                .expect("failed to get game file metadata")
                .permissions();

            perms.set_mode(0o744);

            fs::set_permissions(game_bin, perms)
                .expect("failed to set execute permissions for game");

            println!("applied mode u+x to game binary");
        }
    }

    println!("done");
}
