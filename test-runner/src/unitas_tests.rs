use std::{
    fs,
    net::{IpAddr, Ipv4Addr, SocketAddr, TcpStream},
    path::Path,
    process::Command,
    time::Duration,
};

use crate::{fs_utils::copy_dir_all, Os};

mod unity_2022_3_41f1_base;

pub fn get_linux_tests() -> Vec<Test> {
    vec![]
}

pub fn get_win_tests() -> Vec<Test> {
    vec![]
}

pub struct Test {
    name: &'static str,
    os: Os,
    test: fn(test_args: TestArgs),
}

struct TestArgs<'a> {
    game_dir: &'a Path,
}

impl Test {
    pub fn run(&self, exe_dir: &Path, bepinex_dir: &Path, logs_dir: &Path) {
        let game_dir = exe_dir.join(self.name);

        if !game_dir.is_dir() {
            panic!("game dir for test unity game doesn't exist");
        }

        let game_bin = "build";
        let game_bin = match &self.os {
            Os::Linux => format!("{game_bin}.x86_64"),
            Os::Windows => format!("{game_bin}.exe"),
        };

        // copy bepinex before running of course
        copy_dir_all(bepinex_dir, &game_dir).expect("failed to copy BepInEx dir contents to game");

        // write movie files if required

        // execute game
        Command::new(game_dir.join(game_bin))
            .spawn()
            .expect("failed to run unity game");

        // now connect
        let addr = SocketAddr::new(IpAddr::V4(Ipv4Addr::new(127, 0, 0, 1)), 8001);
        if let Err(err) = TcpStream::connect_timeout(&addr, Duration::from_secs(30)) {
            eprintln!("failed to connect to UniTAS for {}: {err}", self.name);
            self.move_log(&game_dir, logs_dir);
            return;
        }

        // run tests
        let test_args = TestArgs {
            game_dir: &game_dir,
        };
        (self.test)(test_args);
    }

    fn move_log(&self, game_dir: &Path, logs_dir: &Path) {
        let log_src = game_dir.join("BepInEx").join("UniTAS.log");
        let log_dst = logs_dir.join(format!("{}.log", self.name));
        if let Err(err) = fs::copy(&log_src, &log_dst) {
            eprintln!(
                "failed to copy log file from `{}` to `{}`: {err}",
                log_src.display(),
                log_dst.display()
            );
        }
    }
}
