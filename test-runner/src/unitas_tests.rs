use std::{
    fs,
    io::{Read, Write},
    net::{IpAddr, Ipv4Addr, SocketAddr, TcpStream},
    path::Path,
    process::{Command, Stdio},
    thread,
    time::Duration,
};

use crate::{cli::Args, fs_utils::copy_dir_all_blocking, symbols, Os, WIN_UNITY_EXE_NAME};

use anyhow::{Context, Result};
use thiserror::Error;

mod unity_2022_3_41f1_base;
mod utils;

pub fn get_linux_tests() -> Vec<Test> {
    vec![unity_2022_3_41f1_base::get()]
}

pub fn get_win_tests() -> Vec<Test> {
    vec![]
}

pub struct Test {
    name: &'static str,
    os: Os,
    test: fn(test_args: TestArgs) -> Result<bool>,
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
        // add timeout to the stream
        let timeout = Some(Duration::from_secs(30));
        stream.set_read_timeout(timeout).unwrap();
        stream.set_write_timeout(timeout).unwrap();

        Self {
            stream,
            buf: Vec::new(),
            buf_msg_len: [0; 8],
        }
    }

    fn send(&mut self, content: &str) -> Result<()> {
        // wait for the >>
        loop {
            if self.receive()? == ">>" {
                break;
            }
        }

        let content_len_raw = content.len().to_le_bytes();
        let content = content.as_bytes();

        let content = [&content_len_raw, content].concat();

        self.stream
            .write_all(&content)
            .context("failed to send message to UniTAS remote")
    }

    fn receive(&mut self) -> Result<String> {
        self.stream
            .read_exact(&mut self.buf_msg_len)
            .context("failed to read UniTAS remote response's message length")?;

        let msg_len = u64::from_le_bytes(self.buf_msg_len) as usize;

        self.buf.resize(msg_len, 0);
        self.stream
            .read_exact(&mut self.buf)
            .context("failed to read UniTAS remote response")?;

        Ok(String::from_utf8_lossy(&self.buf).trim_end().to_owned())
    }

    fn wait_for_movie_end(&mut self) -> Result<()> {
        loop {
            self.send("print(movie_status().basically_running)")?;
            if self.receive()? == "false" {
                return Ok(());
            }
            thread::sleep(Duration::from_secs(1));
        }
    }
}

impl Test {
    pub fn run(
        &self,
        exe_dir: &Path,
        bepinex_dir: &Path,
        logs_dir: &Path,
        args: &Args,
    ) -> Result<(), BatchTestError> {
        println!("test initialising for {}", self.name);

        let game_dir = exe_dir.join(self.name);

        if !game_dir.is_dir() {
            panic!("game dir for test unity game doesn't exist");
        }

        let execute_bin = match &self.os {
            Os::Linux => "run_bepinex.sh",
            Os::Windows => WIN_UNITY_EXE_NAME,
        };
        let execute_bin = game_dir.join(execute_bin);

        // copy bepinex before running of course
        copy_dir_all_blocking(bepinex_dir, &game_dir).with_context(|| {
            format!(
                "failed to copy BepInEx dir from `{}` to game folder `{}`",
                bepinex_dir.display(),
                game_dir.display()
            )
        })?;

        // execute game
        println!("executing unity game");
        let mut process = Command::new(&execute_bin)
            .current_dir(&game_dir)
            .arg("-batchmode")
            .arg("-nographics")
            .stdout(Stdio::null())
            .stderr(Stdio::null())
            .spawn()
            .with_context(|| {
                format!(
                    "failed to run unity game, attempted to run `{}`",
                    execute_bin.display()
                )
            })?;

        let addr = SocketAddr::new(IpAddr::V4(Ipv4Addr::LOCALHOST), args.port);

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
                        self.move_log(&game_dir, logs_dir);
                        return Err(anyhow::Error::new(err)
                            .context(format!(
                                "failed to connect to UniTAS after {fail_secs} seconds"
                            ))
                            .into());
                    }

                    // wait and try again
                    thread::sleep(Duration::from_secs(1));
                }
            }
        }

        println!("connected\n");

        // run tests
        let stream = UniTasStream::new(stream.unwrap());
        let test_args = TestArgs {
            game_dir: &game_dir,
            stream,
        };

        println!("[{}]", self.name);

        let success = (self.test)(test_args)?;

        println!("\ntest completed");

        self.move_log(&game_dir, logs_dir);

        process.kill().context("failed to stop running game")?;

        if success {
            Ok(())
        } else {
            Err(BatchTestError::TestFail)
        }
    }

    fn move_log(&self, game_dir: &Path, logs_dir: &Path) {
        // 2 seconds to flush usually
        thread::sleep(Duration::from_millis(2500));

        let log_src = game_dir.join("BepInEx").join("UniTAS.log");
        let log_dst = logs_dir.join(format!("{}.log", self.name));
        if let Err(err) = fs::copy(&log_src, &log_dst) {
            eprintln!(
                "{} failed to copy log file from `{}` to `{}`: {err}",
                symbols::WARN,
                log_src.display(),
                log_dst.display()
            );
        }
    }
}

#[derive(Error, Debug)]
pub enum BatchTestError {
    #[error("all test didn't complete successfully")]
    TestFail,
    #[error(transparent)]
    Other(#[from] anyhow::Error),
}
