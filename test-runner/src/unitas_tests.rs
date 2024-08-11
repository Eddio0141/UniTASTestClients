use std::{
    fs,
    net::{IpAddr, Ipv4Addr, SocketAddr, TcpStream},
    path::Path,
    process::{Command, Stdio},
    thread,
    time::Duration,
};

use crate::{fs_utils::copy_dir_all, Os, WIN_UNITY_EXE_NAME};

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
    stream: TcpStream,
}

impl Test {
    pub fn run(&self, exe_dir: &Path, bepinex_dir: &Path, logs_dir: &Path) {
        let game_dir = exe_dir.join(self.name);

        if !game_dir.is_dir() {
            panic!("game dir for test unity game doesn't exist");
        }

        // TODO: check what happens here for 32 bit unity games
        let execute_bin = match &self.os {
            Os::Linux => "run_bepinex.sh",
            Os::Windows => WIN_UNITY_EXE_NAME,
        };

        // copy bepinex before running of course
        copy_dir_all(bepinex_dir, &game_dir).expect("failed to copy BepInEx dir contents to game");

        // execute game
        println!("executing unity game");
        Command::new(game_dir.join(execute_bin))
            .current_dir(&game_dir)
            .stdout(Stdio::null())
            .stderr(Stdio::null())
            .spawn()
            .expect("failed to run unity game");

        let addr = SocketAddr::new(IpAddr::V4(Ipv4Addr::LOCALHOST), 8001);

        // now connect
        let mut stream = None;
        let fail_secs = 30usize;
        for i in 0..fail_secs {
            match TcpStream::connect_timeout(&addr, Duration::from_secs(30)) {
                Ok(s) => {
                    stream = Some(s);
                    break;
                }
                Err(err) => {
                    // last error?
                    if i == fail_secs - 1 {
                        eprintln!("failed to connect to UniTAS for {}: {err}", self.name);
                        self.move_log(&game_dir, logs_dir);
                        return;
                    }

                    // wait and try again
                    thread::sleep(Duration::from_secs(1));
                }
            }
        }

        println!("started new TCP connection");

        // run tests
        let test_args = TestArgs {
            game_dir: &game_dir,
            stream: stream.unwrap(),
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
