use std::fs;

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
    stream.wait_for_movie_end();

    // check results

    // LegacyInputSystemTest.cs
    stream.send("legacy_input_system_test = traverse('LegacyInputSystemTest')");
    let legacy_input_system_test_fields = [
        "_jumpButtonDownCount",
        "_jumpButtonUpCount",
        "_spaceDownKeyCodeCount",
        "_spaceUpKeyCodeCount",
        "_spaceDownStringCount",
        "_spaceUpStringCount",
    ];
    for field in legacy_input_system_test_fields {
        stream.send(&format!(
            "print(legacy_input_system_test.field('{field}').get_value())"
        ));
        assert_eq!(
            stream.receive(),
            "5",
            "checking LegacyInputSystemTest.{field} field to be 5"
        );
    }

    // SceneTest.cs
    stream.send("scene_test = traverse('SceneTest')");

    stream.send("print(scene_test.field('_asyncOpCallbackProgress').get_value())");
    assert_eq!(
        stream.receive(),
        "1",
        "checking SceneTest callback progress value"
    );

    stream.send("print(scene_test.field('_asyncOpCallbackAllowSceneActivation').get_value())");
    assert_eq!(
        stream.receive(),
        "true",
        "checking SceneTest callback allow scene activation value"
    );

    stream.send("print(scene_test.field('_asyncOpCallbackIsDone').get_value())");
    assert_eq!(
        stream.receive(),
        "true",
        "checking SceneTest callback IsDone value"
    );

    stream.send("print(scene_test.field('_asyncOpDoneFrame').get_value())");
    assert_eq!(
        stream.receive(),
        "35",
        "checking SceneTest callback done timing frame"
    );

    // UGuiTest.cs
    stream.send("ugui_test = traverse('UGuiTest')");

    stream.send("print(ugui_test.field('_clickCount').get_value())");
    assert_eq!(
        stream.receive(),
        "5",
        "checking UGuiTest button press count"
    );
}
