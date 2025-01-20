use std::fs;

use crate::Os;

use super::{Test, TestArgs, TestCtx};

use anyhow::{Context, Result};

const MOVIE: &str = include_str!("unity_2022_3_41f1_base_movie.lua");

pub fn get() -> Test {
    Test {
        name: "2022.3.41f1-base",
        os: Os::Linux,
        test,
    }
}

fn test(ctx: &mut TestCtx, mut args: TestArgs) -> Result<()> {
    let movie_path = args.game_dir.join("movie.lua");
    fs::write(&movie_path, MOVIE).with_context(|| {
        format!(
            "failed to write movie file to path `{}`",
            movie_path.display()
        )
    })?;

    let stream = &mut args.stream;

    stream.send("full_access(true)")?;
    stream.receive()?;

    ctx.run_general_tests(stream)?;

    stream.send("play('movie.lua')")?;
    stream.wait_for_movie_end()?;

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
        ctx.assert_eq(
            "5",
            &stream.receive()?,
            &format!("jump button count field {field}"),
            &format!("checking LegacyInputSystemTest.{field} field to be 5"),
        );
    }

    stream.send("print(legacy_input_system_test.field('_horizontalAxisMoveCount').get_value())")?;
    ctx.assert_eq(
        "6",
        &stream.receive()?,
        "horizontal axis move count",
        "checking LegacyInputSystemTest._horizontalAxisMoveCount field",
    );

    // SceneTest.cs
    stream.send("scene_test = traverse('SceneTest')")?;

    stream.send("print(scene_test.field('_asyncOpCallbackProgress').get_value())")?;
    ctx.assert_eq(
        "1",
        &stream.receive()?,
        "async op callback progress",
        "async operation progress in callback isn't 1.0",
    );

    stream.send("print(scene_test.field('_asyncOpCallbackAllowSceneActivation').get_value())")?;
    ctx.assert_eq(
        "true",
        &stream.receive()?,
        "async operation callback allowSceneActivation",
        "allowSceneActivation is false",
    );

    stream.send("print(scene_test.field('_asyncOpCallbackIsDone').get_value())")?;
    ctx.assert_eq(
        "true",
        &stream.receive()?,
        "async operation callback isDone",
        "isDone isn't true",
    );

    stream.send("print(scene_test.field('_asyncOpDoneFrame').get_value())")?;
    ctx.assert_eq(
        "19",
        &stream.receive()?,
        "SceneTest async op done frame",
        "checking SceneTest callback done timing frame",
    );

    // UGuiTest.cs
    stream.send("ugui_test = traverse('UGuiTest')")?;

    stream.send("print(ugui_test.field('_clickCount').get_value())")?;
    ctx.assert_eq(
        "5",
        &stream.receive()?,
        "UGuiTest click count",
        "didn't match click count of 5",
    );

    let frame_count = 100u8;

    // unitas updates

    // fixed update: 0.02
    // ---
    // update: 0.02
    // late update: 0.02 - not included here
    // end of frame: 0.02
    // last update: 0.02
    // ---
    // update: 0.03
    // late update: 0.03 - not included here
    // end of frame: 0.03
    // last update: 0.03

    stream.send(&format!(
        r#"time = traverse('UnityEngine.Time')
service('ITimeWrapper').capture_frame_time = 0.01

wait_for_fixed_update = true
wait_for_last = true
fixed_update_count = 0
update_count = 0
end_of_frame_count = 0
last_update_count = 0

printed_results = 0

local reverse_invoker = service("IPatchReverseInvoker")
local fixed_time = traverse("UnityEngine.Time").property("fixedTime")

patch("UniTAS.Patcher.Implementations.UnityEvents.UnityEvents.InvokeFixedUpdate", function(this)

    reverse_invoker.invoking = true
    local fixed_time = fixed_time.GetValue()
    reverse_invoker.invoking = false
    if traverse(this).field("_prevFixedTime").GetValue() == fixed_time then
        return
    end

    wait_for_fixed_update = false

    if wait_for_last then
        return
    end

    if update_count >= {frame_count} then
        if printed_results < 2 then
            print(fixed_update_count)
            print(update_count)
            print(end_of_frame_count)
            print(last_update_count)
            printed_results = printed_results + 1

            -- test #2 init
            if printed_results == 1 then
                time.property('maximumDeltaTime').set_value(0.3333333)
                time.property('fixedDeltaTime').set_value(0.02)
                time.property('timeScale').set_value(1)
                service('ITimeWrapper').capture_frame_time = 0.04
                wait_for_fixed_update = true
                wait_for_last = true

                fixed_update_count = 0
                update_count = 0
                end_of_frame_count = 0
                last_update_count = 0
            end
        else
            return
        end
    end

    fixed_update_count = fixed_update_count + 1
end, "method")
patch("UniTAS.Patcher.Implementations.UnityEvents.UnityEvents.InvokeUpdate", function(this)
    if wait_for_last or wait_for_fixed_update or update_count >= {frame_count} or traverse(this).field("_updated").get_value() then
        return
    end

    update_count = update_count + 1
end, "method")
patch("UniTAS.Patcher.Implementations.UnityEvents.UnityEvents.InvokeEndOfFrame", function(this)
    if wait_for_last or wait_for_fixed_update or update_count >= {frame_count} or traverse(this).field("_endOfFrameUpdated").get_value() then
        return
    end

    end_of_frame_count = end_of_frame_count + 1
end, "method")
patch("UniTAS.Patcher.Implementations.UnityEvents.UnityEvents.InvokeLastUpdate", function()
    if wait_for_fixed_update or update_count >= {frame_count} then
        return
    end

    last_update_count = last_update_count + 1
    wait_for_last = false
end, "method")
"#
    ))?;

    // ignore messages
    for _ in 0..8 {
        stream.receive()?;
    }

    // fixed_update_count
    // update_count
    // end_of_frame_count
    // last_update_count
    ctx.assert_eq(
        &(frame_count / 2).to_string(),
        &stream.receive()?,
        "unitas updates: fixed update count",
        "mismatch in update count",
    );
    ctx.assert_eq(
        &frame_count.to_string(),
        &stream.receive()?,
        "unitas updates: update count",
        "mismatch in update count",
    );
    ctx.assert_eq(
        &(frame_count - 1).to_string(),
        &stream.receive()?,
        "unitas updates: end of frame count",
        "mismatch in update count",
    );
    ctx.assert_eq(
        &frame_count.to_string(),
        &stream.receive()?,
        "unitas updates: last update count",
        "mismatch in update count",
    );

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

    // fixed_update_count
    // update_count
    // end_of_frame_count
    // last_update_count
    ctx.assert_eq(
        &(frame_count * 2 + 1).to_string(),
        &stream.receive()?,
        "unitas updates: fixed update count",
        "mismatch in update count",
    );
    ctx.assert_eq(
        &frame_count.to_string(),
        &stream.receive()?,
        "unitas updates: update count",
        "mismatch in update count",
    );
    ctx.assert_eq(
        &(frame_count - 1).to_string(),
        &stream.receive()?,
        "unitas updates: end of frame count",
        "mismatch in update count",
    );
    ctx.assert_eq(
        &frame_count.to_string(),
        &stream.receive()?,
        "unitas updates: last update count",
        "mismatch in update count",
    );

    stream.send("full_access(true)")?;
    stream.receive()?;

    // FrameAdvanceAnimator.cs
    stream.send(
        r#"event_coroutine(function()
    service("ISceneManagerWrapper").load_scene("FrameAdvancing")
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

    // assert_eq!(
    //     "frame advancing: paused Animator",
    //     stream.receive()?,
    //     "f1: -1",
    //     "mismatch in initial value of Animator trigger tracker value",
    //     res
    // );

    println!("{}", stream.receive()?);
    println!("{}", stream.receive()?);
    println!("{}", stream.receive()?);
    println!("{}", stream.receive()?);
    println!("{}", stream.receive()?);
    println!("{}", stream.receive()?);

    // assert_eq!(
    //     "frame advancing: Animator trigger reach check",
    //     stream.receive()?,
    //     "94",
    //     "mismatch in animation completion frame",
    //     res
    // );

    Ok(())
}
