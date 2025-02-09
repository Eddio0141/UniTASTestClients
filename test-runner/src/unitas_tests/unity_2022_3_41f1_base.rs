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

    stream.send("print(traverse('MovieTest').field('_movieTestRun').GetValue())")?;
    ctx.assert_eq(
        &true.to_string(),
        &stream.receive()?,
        "movie test",
        "failed to run movie tests",
    );

    // for movie results
    ctx.get_assert_results(stream)?;
    ctx.reset_assert_results(stream)?;

    // frame advancing test

    // sanity check
    stream.send("service('ITimeWrapper').capture_frame_time = 0.01 service('ISceneManagerWrapper').load_scene('FrameAdvancing')")?;
    ctx.get_assert_results(stream)?;
    ctx.reset_assert_results(stream)?;

    // actual test
    /*
        stream.send(
            r#"event_coroutine(function()
        local y = coroutine.yield

        local fa = service("IFrameAdvancing")
        local fa_update_mode = traverse("FrameAdvanceMode").field("Update").get_value()
        fa.FrameAdvance(1, fa_update_mode)

        local frameAdvancing_YieldNull = traverse("FrameAdvancing").field("_yieldNull");

        service("ISceneManagerWrapper").load_scene("FrameAdvancing")
        y("UpdateUnconditional")

        for _ = 1, 250 do
            y("UpdateUnconditional")
        end

        print(frameAdvancing_YieldNull.GetValue())

        for _ = 1, 5 do
            fa.FrameAdvance(1, fa_update_mode)
            y("UpdateActual")
            print(frameAdvancing_YieldNull.GetValue())
            y("UpdateUnconditional")
            y("UpdateUnconditional")
            y("UpdateUnconditional")
            y("UpdateUnconditional")
            y("UpdateUnconditional")
        end

        fa.TogglePause() -- resume

        for _ = 1, 150 do
            y("UpdateActual")
        end
    end)"#,
        )?;

        // frame advancing checks
        ctx.assert_eq(
            &0.to_string(),
            &stream.receive()?,
            "Frame advancing: yield null check",
            "Mismatch in reach stage",
        );
        for i in 0..5u8 {
            ctx.assert_eq(
                &i.to_string(),
                &stream.receive()?,
                &format!("Frame advancing: yield null check {i}"),
                "Mismatch in reach stage",
            );
        }

        // final check
        ctx.get_assert_results(stream)?;
        ctx.reset_assert_results(stream)?;
        */

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
patch("UniTAS.Patcher.Implementations.UnityEvents.UnityEvents.InvokeLastUpdate", function(this)
    if wait_for_fixed_update or update_count >= {frame_count} then
        return
    end

    if traverse(this).field("_calledLastUpdate").GetValue() then
        print("last update was called twice in a single update, this is absolutely invalid")
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

    Ok(())
}
