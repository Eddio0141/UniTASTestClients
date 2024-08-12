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
    stream.send("runner = service('IMovieRunner')");

    loop {
        stream.send("print(runner.MovieEnd)");
        let response = stream.recieve();
        if response == "true" {
            break;
        }
        thread::sleep(Duration::from_secs(1));
    }

    // check results
    // stream.send("legacy_input_system_test = traverse('LegacyInputSystemTest')");

    // let response = test_args.stream.recieve();
    // println!("response: {response}");
}
