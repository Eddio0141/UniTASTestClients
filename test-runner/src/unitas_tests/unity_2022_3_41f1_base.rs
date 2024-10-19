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

    stream.send("full_access(true)")?;
    stream.receive()?;

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

    stream.send("print(legacy_input_system_test.field('_horizontalAxisMoveCount').get_value())")?;
    if !assert_eq(
        "horizontal axis move count",
        stream.receive()?.as_str(),
        "6",
        || "checking LegacyInputSystemTest._horizontalAxisMoveCount field".to_string(),
    ) {
        res = false;
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
        "19",
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

    // multiple fixed updates in a row
    // in this case, this pattern is made

    // ...
    // f: 0.02
    // f: 0.04
    // u: 0.04
    // f: 0.06
    // f: 0.08
    // u: 0.08
    // f: 0.10
    // f: 0.12
    // u: 0.12
    let max_fixed_update_count = 4u8;

    stream.send(&format!(
        r#"time = traverse('UnityEngine.Time')
time.property('maximumDeltaTime').set_value(0.3333333)
time.property('fixedDeltaTime').set_value(0.02)
time.property('timeScale').set_value(1)
service('ITimeWrapper').capture_frame_time = 0.04

wait_for_update = true
fixed_update_count = 0
update_count = 0

printed_results = false

patch("UniTAS.Patcher.Implementations.UnityEvents.UnityEvents.InvokeFixedUpdate", function(this)
    if traverse(this).field("_calledFixedUpdate").get_value() or wait_for_update then
        return
    end

    if fixed_update_count >= {max_fixed_update_count} then
        if not printed_results then
            print(fixed_update_count)
            print(update_count)
            printed_results = true
        end
        return
    end

    fixed_update_count = fixed_update_count + 1
end, "method")
patch("UniTAS.Patcher.Implementations.UnityEvents.UnityEvents.InvokeUpdate", function(this)
    if traverse(this).field("_updated").get_value() or fixed_update_count >= {max_fixed_update_count} then
        return
    end

    wait_for_update = false
    update_count = update_count + 1
end, "method")
"#
    ))?;

    // ignore messages
    for _ in 0..4 {
        stream.receive()?;
    }

    if !assert_eq(
        "multiple fixed update: fixed update count",
        stream.receive()?.as_str(),
        &max_fixed_update_count.to_string(),
        || "mismatch in FixedUpdate count".to_string(),
    ) {
        res = false;
    }

    if !assert_eq(
        "multiple fixed update: update count",
        stream.receive()?.as_str(),
        "2",
        || "mismatch in Update count".to_string(),
    ) {
        res = false;
    }

    // FrameAdvanceAnimator.cs
    stream.send(
        r#"event_coroutine(function()
    service("ISceneWrapper").load_scene("FrameAdvancing")
    service('ITimeWrapper').capture_frame_time = 0.01
    local y = coroutine.yield

    y("UpdateUnconditional")

    local fa = service("IFrameAdvancing")
    local fa_update_mode = traverse("FrameAdvanceMode").field("Update").get_value()

    fa.FrameAdvance(1, fa_update_mode)
    for _ = 1, 250 do
        y("UpdateUnconditional")
    end

    local timeTriggerFrame = traverse("FrameAdvanceAnimator").field("_timeTriggerTime");
    -- local timeTriggerFrame = traverse("FrameAdvanceAnimator").field("_timeTriggerFrame"); -- TODO: once frame advancing fixes time env inaccuracies
    -- https://github.com/Eddio0141/UniTAS/issues/238
    local timeTriggerLegacyFrame = traverse("FrameAdvanceLegacyAnimation").field("_timeTriggerTime");
    local timeTriggerLegacyBlendFrame = traverse("FrameAdvanceLegacyAnimation").field("_timeTriggerTimeBlend");
    print("f1 Animator: " .. timeTriggerFrame.get_value())
    print("f1 legacy Animation: " .. timeTriggerLegacyFrame.get_value())
    print("f1 legacy Animation blend: " .. timeTriggerLegacyBlendFrame.get_value())

    fa.TogglePause() -- resume

    for _ = 1, 150 do
        y("UpdateUnconditional")
    end

    print("f2 Animator: " .. timeTriggerFrame.get_value())
    print("f2 legacy Animation: " .. timeTriggerLegacyFrame.get_value())
    print("f2 legacy Animation blend: " .. timeTriggerLegacyBlendFrame.get_value())
end)"#,
    )?;

    // if !assert_eq(
    //     "frame advancing: paused Animator",
    //     stream.receive()?.as_str(),
    //     "f1: -1",
    //     || "mismatch in initial value of Animator trigger tracker value".to_string(),
    // ) {
    //     res = false;
    // }

    println!("{}", stream.receive()?);
    println!("{}", stream.receive()?);
    println!("{}", stream.receive()?);
    println!("{}", stream.receive()?);
    println!("{}", stream.receive()?);
    println!("{}", stream.receive()?);

    // if !assert_eq(
    //     "frame advancing: Animator trigger reach check",
    //     stream.receive()?.as_str(),
    //     "94",
    //     || "mismatch in animation completion frame".to_string(),
    // ) {
    //     res = false;
    // }

    Ok(res)
}
