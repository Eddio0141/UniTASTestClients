use std::{fs, thread, time::Duration};

use crate::Os;

use super::{Test, TestArgs};

const MOVIE: &str = include_str!("unity_2022_3_41f1_base_movie.lua");

pub fn get() -> Test {
    Test {
        name: "2022.3.41f1-base",
        os: Os::Linux,
        test,
    }
}

fn test(mut test_args: TestArgs) {
    let movie_path = test_args.game_dir.join("movie.lua");
    fs::write(movie_path, MOVIE).expect("failed to write movie");

    let stream = &mut test_args.stream;

    stream.send("play('movie.lua')");

    loop {
        stream.send("print(movie_status().basically_running)");
        let response = stream.receive();
        if response == "false" {
            break;
        }
        thread::sleep(Duration::from_secs(1));
    }
    thread::sleep(Duration::from_secs(5));

    // check results
    stream.send("legacy_input_system_test = traverse('LegacyInputSystemTest')");
    stream.send("print(legacy_input_system_test.field('_jumpButtonDownCount').get_value())");

    let response = test_args.stream.receive();
    println!("response: {response}");
}
