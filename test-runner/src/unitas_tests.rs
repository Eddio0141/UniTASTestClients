use std::{
    fs,
    io::{Read, Write},
    net::{IpAddr, Ipv4Addr, SocketAddr, TcpStream},
    path::Path,
    process::{Command, Stdio},
    thread,
    time::Duration,
};

use crate::{fs_utils::copy_dir_all, Os, WIN_UNITY_EXE_NAME};

mod unity_2022_3_41f1_base;

pub fn get_linux_tests() -> Vec<Test> {
    vec![unity_2022_3_41f1_base::get()]
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
    stream: UniTasStream,
}

struct UniTasStream {
    stream: TcpStream,
    buf: Vec<u8>,
    buf_msg_len: [u8; 8], // u32 (int) length
}

impl UniTasStream {
    fn new(stream: TcpStream) -> Self {
        Self {
            stream,
            buf: Vec::new(),
            buf_msg_len: [0; 8],
        }
    }

    fn send(&mut self, content: &str) {
        // wait for the >>
        loop {
            if self.receive() == ">>" {
                break;
            }
        }

        let content_len_raw = content.len().to_le_bytes();
        let content = content.as_bytes();

        let content = [&content_len_raw, content].concat();

        self.stream
            .write_all(&content)
            .expect("failed to write to UniTAS TCP stream");
    }

    fn receive(&mut self) -> String {
        self.stream
            .read_exact(&mut self.buf_msg_len)
            .expect("failed to read length of message from UniTAS TCP stream");

        let msg_len = u64::from_le_bytes(self.buf_msg_len) as usize;

        self.buf.resize(msg_len, 0);
        self.stream
            .read_exact(&mut self.buf)
            .expect("failed to read from UniTAS TCP stream");

        String::from_utf8_lossy(&self.buf).trim_end().to_owned()
    }
}

impl Test {
    pub fn run(&self, exe_dir: &Path, bepinex_dir: &Path, logs_dir: &Path) {
        println!("test initialising for {}", self.name);

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
        println!("copying bepinex to game folder");
        copy_dir_all(bepinex_dir, &game_dir).expect("failed to copy BepInEx dir contents to game");
        println!("done");

        // execute game
        println!("executing unity game");
        let mut process = Command::new(game_dir.join(execute_bin))
            .current_dir(&game_dir)
            .arg("-batchmode")
            .stdout(Stdio::null())
            .stderr(Stdio::null())
            .spawn()
            .expect("failed to run unity game");

        let addr = SocketAddr::new(IpAddr::V4(Ipv4Addr::LOCALHOST), 8001);

        // now connect
        let mut stream = None;
        let fail_secs = 30usize;
        println!("starting TCP connection to UniTAS remote");
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

        println!("connected");

        // run tests
        let stream = UniTasStream::new(stream.unwrap());
        let test_args = TestArgs {
            game_dir: &game_dir,
            stream,
        };

        println!("test is initialised, running test");

        (self.test)(test_args);

        println!("test completed, copying log file");

        self.move_log(&game_dir, logs_dir);

        println!("done");

        println!("killing game");

        process.kill().expect("failed to stop running game");

        println!("done");
    }

    fn move_log(&self, game_dir: &Path, logs_dir: &Path) {
        // 2 seconds to flush usually
        thread::sleep(Duration::from_secs(2));

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
