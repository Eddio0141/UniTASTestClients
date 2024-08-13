#[cfg(target_os = "linux")]
use std::os::unix::fs::PermissionsExt;
use std::{
    env::{self, current_exe},
    fmt::Display,
    fs::{self, create_dir_all},
    path::Path,
};

use const_format::formatcp;
use download::{dl_bepinex, dl_test_games};
use fs_utils::copy_dir_all;
use unitas_tests::{get_linux_tests, get_win_tests};

mod download;
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
    dl_test_games(current_dir);
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
            Arch::X64 => GAME_BIN_NAME,
            Arch::X86 => todo!(),
        },
        // TODO: does windows require this
        Os::Windows => WIN_UNITY_EXE_NAME,
    }
}

const GAME_BIN_NAME: &str = "build";
const WIN_UNITY_EXE_NAME: &str = formatcp!("{GAME_BIN_NAME}.exe");

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
