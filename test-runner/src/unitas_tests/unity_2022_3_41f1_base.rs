use crate::Os;

use super::{Test, TestArgs};

// TODO: movie
const MOVIE: &str = r#"

"#;

pub fn get() -> Test {
    Test {
        name: "2022.3.41f1-base",
        os: Os::Linux,
        test,
    }
}

fn test(mut test_args: TestArgs) {
    test_args.stream.send(r#"print("hello world")"#);
    let response = test_args.stream.recieve();
    println!("response: {response}");
}
