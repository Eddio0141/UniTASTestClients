use std::{
    env::{self, current_exe},
    fmt::Display,
    fs::{self, create_dir_all},
    io::Cursor,
    os::unix::fs::PermissionsExt,
    path::Path,
};

use const_format::formatcp;
use fs_utils::copy_dir_all;
use unitas_tests::{get_linux_tests, get_win_tests};
use zip::ZipArchive;

mod fs_utils;
mod unitas_tests;

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

fn main() {
    // dirs in executable dir is all unity games for testing
    let current_dir = current_exe().expect("failed to get current exe dir");
    let current_dir = current_dir.parent().unwrap();

    // create logs dir
    fs::create_dir_all(current_dir.join("logs")).expect("failed to create logs dir");

    let os = match env::consts::OS {
        "linux" => Os::Linux,
        "windows" => Os::Windows,
        _ => panic!("unsupported os for testing"),
    };
    let arch = match env::consts::ARCH {
        "x86" => Arch::X86,
        "x86_64" => Arch::X64,
        _ => panic!("unsupported architecture for testing"),
    };

    let bepinex_dir = current_dir.join("BepInEx");
    dl_bepinex(&bepinex_dir, &os, &arch);
    setup_bepinex(&bepinex_dir, &arch);
    setup_unitas(current_dir, &bepinex_dir);
    setup_unitas_config(&bepinex_dir);

    // for all UniTAS logs
    let logs_dir = current_dir.join("logs");
    create_dir_all(&logs_dir).expect("failed to create folder for logs");

    let tests = match os {
        Os::Linux => get_linux_tests(),
        Os::Windows => get_win_tests(),
    };

    for test in tests {
        test.run(current_dir, &bepinex_dir, &logs_dir);
    }
}

fn setup_unitas(exe_dir: &Path, bepinex_dir: &Path) {
    println!("setting up UniTAS");

    // copy unitas in bepinex dir
    let unitas_dir = exe_dir.join("UniTAS");
    copy_dir_all(unitas_dir, bepinex_dir).expect("failed to copy UniTAS dir contents to game");

    println!("done");
}

fn game_bin_name(os: &Os, arch: &Arch) -> &'static str {
    match &os {
        Os::Linux => match arch {
            Arch::X64 => LINUX_UNITY_EXE_NAME,
            Arch::X86 => todo!(),
        },
        Os::Windows => WIN_UNITY_EXE_NAME,
    }
}

const GAME_BIN_NAME: &str = "build";
const WIN_UNITY_EXE_NAME: &str = formatcp!("{GAME_BIN_NAME}.exe");
const LINUX_UNITY_EXE_NAME: &str = formatcp!("{GAME_BIN_NAME}.x86_64");

fn setup_bepinex(bepinex_dir: &Path, arch: &Arch) {
    #[cfg(target_os = "linux")]
    {
        println!("configuring bepinex for linux");

        let run_bepinex_file = bepinex_dir.join("run_bepinex.sh");

        // modify run_bepinex to execute correct executable
        println!("modifying run_bepinex.sh to execute the correct executable");

        let mut run_bepinex_content =
            fs::read_to_string(&run_bepinex_file).expect("failed to open run_bepinex.sh");

        let find_key = "executable_name=";
        let find_index = run_bepinex_content
            .find(find_key)
            .expect("failed to find executable_name config in run_bepinex.sh");

        let exe_name = game_bin_name(&Os::Linux, arch);

        run_bepinex_content.insert_str(find_index + find_key.len() + 1, exe_name);

        fs::write(&run_bepinex_file, run_bepinex_content)
            .expect("failed to write to run_bepinex.sh");

        // set perms for execution
        let mut perms = run_bepinex_file
            .metadata()
            .expect("failed to get run_bepinex.sh metadata")
            .permissions();

        perms.set_mode(0o744);

        fs::set_permissions(run_bepinex_file, perms)
            .expect("failed to set execute permissions for run_bepinex.sh");

        println!("done");
    }
}

fn setup_unitas_config(bepinex_dir: &Path) {
    println!("writing UniTAS config");

    let cfg = bepinex_dir.join("BepInEx").join("config");

    fs::create_dir_all(&cfg).expect("failed to create directory for UniTAS config file");

    let cfg = cfg.join("UniTAS.cfg");
    let contents = r#"[Remote]
Enable = true
"#;

    fs::write(cfg, contents).expect("failed to write config for UniTAS");

    println!("done");
}

fn dl_bepinex(dl_dir: &Path, os: &Os, arch: &Arch) {
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
