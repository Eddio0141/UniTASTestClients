use std::fs;

use crate::{unitas_tests::utils::assert_eq, Os};

use super::{Test, TestArgs};

use anyhow::{Context, Result};

const MOVIE: &str = include_str!("unity_2022_3_41f1_base_movie.lua");

pub fn get() -> Test {
    Test {
        name: "2022.3.41f1-base",
        os: Os::Linux,
        test,
    }
}

fn test(mut test_args: TestArgs) -> Result<bool> {
    let movie_path = test_args.game_dir.join("movie.lua");
    fs::write(&movie_path, MOVIE).with_context(|| {
        format!(
            "failed to write movie file to path `{}`",
            movie_path.display()
        )
    })?;

    let stream = &mut test_args.stream;

    stream.send("play('movie.lua')")?;
    stream.wait_for_movie_end()?;

    let mut res = true;

    // check results

    // LegacyInputSystemTest.cs
    stream.send("legacy_input_system_test = traverse('LegacyInputSystemTest')")?;
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
        ))?;
        if !assert_eq(
            &format!("jump button count field {field}"),
            stream.receive()?.as_str(),
            "5",
            || format!("checking LegacyInputSystemTest.{field} field to be 5"),
        ) {
            res = false;
        }
    }

    // SceneTest.cs
    stream.send("scene_test = traverse('SceneTest')")?;

    stream.send("print(scene_test.field('_asyncOpCallbackProgress').get_value())")?;
    if !assert_eq(
        "async op callback progress",
        stream.receive()?.as_str(),
        "1",
        || "async operation progress in callback isn't 1.0".to_string(),
    ) {
        res = false;
    }

    stream.send("print(scene_test.field('_asyncOpCallbackAllowSceneActivation').get_value())")?;
    if !assert_eq(
        "async operation callback allowSceneActivation",
        stream.receive()?.as_str(),
        "true",
        || "allowSceneActivation is false".to_string(),
    ) {
        res = false;
    }

    stream.send("print(scene_test.field('_asyncOpCallbackIsDone').get_value())")?;
    if !assert_eq(
        "async operation callback isDone",
        stream.receive()?.as_str(),
        "true",
        || "isDone isn't true".to_string(),
    ) {
        res = false;
    }

    stream.send("print(scene_test.field('_asyncOpDoneFrame').get_value())")?;
    if !assert_eq(
        "SceneTest async op done frame",
        stream.receive()?.as_str(),
        "35",
        || "checking SceneTest callback done timing frame".to_string(),
    ) {
        res = false;
    }

    // UGuiTest.cs
    stream.send("ugui_test = traverse('UGuiTest')")?;

    stream.send("print(ugui_test.field('_clickCount').get_value())")?;
    if !assert_eq(
        "UGuiTest click count",
        stream.receive()?.as_str(),
        "5",
        || "didn't match click count of 5".to_string(),
    ) {
        res = false;
    }

    Ok(res)
}
