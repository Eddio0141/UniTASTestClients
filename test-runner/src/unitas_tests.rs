#[cfg(target_family = "unix")]
use std::os::unix::process::ExitStatusExt;
use std::{
    collections::VecDeque,
    fs,
    io::{self, Read, Write},
    net::{IpAddr, Ipv4Addr, SocketAddr, TcpStream},
    path::Path,
    process::{Command, Stdio},
    thread,
    time::Duration,
};

use crate::{cli::Args, fs_utils::copy_dir_all_blocking, symbols, Os, WIN_UNITY_EXE_NAME};

use anyhow::{Context, Result};
use log::{debug, trace};
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
    received_queue: VecDeque<String>,
    ready_to_send: bool,
}

#[repr(u8)]
enum ReceivePrefix {
    Prefix = 0,
    Stdout = 1,
}

impl From<u8> for ReceivePrefix {
    fn from(value: u8) -> Self {
        match value {
            0 => ReceivePrefix::Prefix,
            1 => ReceivePrefix::Stdout,
            _ => unimplemented!(
                "invalid ReceivePrefix value `{value}`, forgot to implement new prefix on rust side?"
            ),
        }
    }
}

const HUMAN_PREFIX: &str = ">> ";

const ERR_PREFIX_READ_FAIL: &str = "failed to receive UniTAS remote prefix";

impl UniTasStream {
    fn new(mut stream: TcpStream) -> Result<Self, io::Error> {
        // add timeout to the stream
        let timeout = Some(Duration::from_secs(30));
        stream.set_read_timeout(timeout).unwrap();
        stream.set_write_timeout(timeout).unwrap();

        let mut buf = vec![0; HUMAN_PREFIX.len()];

        // initialise connection
        stream.read_exact(&mut buf)?;
        assert_eq!(
            String::from_utf8_lossy(&buf),
            HUMAN_PREFIX,
            "somehow there's a mismatch in expected initial message"
        );
        // verify we are a script
        buf.resize(1, 0);
        buf[0] = 0;
        stream.write_all(&buf)?;

        Ok(Self {
            stream,
            buf,
            buf_msg_len: [0; 8],
            ready_to_send: true,
            received_queue: VecDeque::new(),
        })
    }

    fn send(&mut self, content: &str) -> Result<()> {
        trace!("send to remote call with content `{content}`");
        if !self.ready_to_send {
            debug!("can't send message to remote yet, `ready_to_send` is false");

            // wait for prefix
            loop {
                self.buf.resize(size_of::<ReceivePrefix>(), 0);
                self.stream.read_exact(&mut self.buf).with_context(|| {
                    format!("{ERR_PREFIX_READ_FAIL}, can't send data to remote")
                })?;

                match ReceivePrefix::from(self.buf[0]) {
                    ReceivePrefix::Prefix => {
                        debug!("ready to send to remote");
                        break;
                    }
                    ReceivePrefix::Stdout => {
                        let msg = self.read_stdout()?;
                        debug!("got stdout message: `{msg}`, adding to queue");
                        self.received_queue.push_back(msg);
                    }
                }
            }
        }
        self.ready_to_send = false;

        let content_len_raw = content.len().to_le_bytes();
        let content = content.as_bytes();

        let content = [&content_len_raw, content].concat();

        self.stream
            .write_all(&content)
            .context("failed to send message to UniTAS remote")?;

        trace!("sent msg to remote, msg len: {}", content.len());

        Ok(())
    }

    fn receive(&mut self) -> Result<String> {
        trace!("receive call");

        if let Some(msg) = self.received_queue.pop_front() {
            trace!("found message in queue already, `{msg}`");
            return Ok(msg);
        }

        loop {
            self.buf.resize(size_of::<ReceivePrefix>(), 0);
            self.stream.read_exact(&mut self.buf).with_context(|| {
                format!("{ERR_PREFIX_READ_FAIL}, failed to receive data from remote")
            })?;

            match ReceivePrefix::from(self.buf[0]) {
                ReceivePrefix::Prefix => {
                    self.ready_to_send = true;
                    debug!("received prefix data from remote, ready to send");
                }
                ReceivePrefix::Stdout => break,
            }
        }

        trace!("reading stdout message");
        self.read_stdout()
    }

    fn read_stdout(&mut self) -> Result<String> {
        // use after reading message type prefix
        self.stream
            .read_exact(&mut self.buf_msg_len)
            .context("failed to read UniTAS remote response's message length")?;

        let msg_len = u64::from_le_bytes(self.buf_msg_len) as usize;

        self.buf.resize(msg_len, 0);
        self.stream
            .read_exact(&mut self.buf)
            .context("failed to read UniTAS remote response")?;

        let msg = String::from_utf8_lossy(&self.buf).trim_end().to_owned();

        debug!("received stdout msg: `{msg}`, len: `{msg_len}`");

        Ok(msg)
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
            .args(["-logFile", STDOUT_LOG_FILENAME])
            .stdout(Stdio::null())
            .stderr(Stdio::null())
            .stdin(Stdio::null())
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
        println!("connecting to UniTAS remote...");
        for i in 0..fail_secs {
            match TcpStream::connect_timeout(&addr, Duration::from_secs(30)) {
                Ok(s) => {
                    stream = Some(s);
                    break;
                }
                Err(err) => {
                    // last error?
                    if i == fail_secs - 1 {
                        process.kill().context("failed to stop running game")?;
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

        let stream = UniTasStream::new(stream.unwrap()).context("failed to initialise connection to UniTAS, verifying connection as a script has failed")?;

        println!("connected\n");

        let test_args = TestArgs {
            game_dir: &game_dir,
            stream,
        };

        println!("[{}]", self.name);

        // run tests
        let success = (self.test)(test_args);

        println!();
        process.kill().context("failed to stop running game")?;

        let status = process.wait().unwrap();
        self.move_log(&game_dir, logs_dir);

        let success = success?;
        println!("test completed");

        if success {
            Ok(())
        } else {
            let signal: Option<i32>;

            #[cfg(target_family = "unix")]
            {
                signal = status.signal();
            };
            #[cfg(not(target_family = "unix"))]
            {
                signal = None;
            };

            // check if not sigkill (process.kill() would terminate it with sigkill)
            let err = if status.success() || signal == Some(9) {
                BatchTestError::TestFail
            } else {
                BatchTestError::GameCrash {
                    code: status.code(),
                    signal,
                }
            };

            Err(err)
        }
    }

    fn move_log(&self, game_dir: &Path, logs_dir: &Path) {
        let log_src = game_dir.join(STDOUT_LOG_FILENAME);
        let log_dst = logs_dir.join(format!("{}-{STDOUT_LOG_FILENAME}", self.name));
        if let Err(err) = fs::copy(&log_src, &log_dst) {
            eprintln!(
                "{} failed to copy stdout log file from `{}` to `{}`: {err}",
                symbols::WARN,
                log_src.display(),
                log_dst.display()
            );
        }

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

        println!("moved log of last session into `{}`", logs_dir.display());
    }
}

const STDOUT_LOG_FILENAME: &str = "stdout.log";

#[derive(Error, Debug)]
pub enum BatchTestError {
    #[error("all test didn't complete successfully")]
    TestFail,
    #[error("game has crashed, exit code: {}", code.map(|c| c.to_string()).unwrap_or_else(|| format!("None, signal: {}", signal.map(|s| s.to_string()).unwrap_or_else(|| "None".to_string()))))]
    GameCrash {
        code: Option<i32>,
        signal: Option<i32>,
    },
    #[error(transparent)]
    Other(#[from] anyhow::Error),
}
