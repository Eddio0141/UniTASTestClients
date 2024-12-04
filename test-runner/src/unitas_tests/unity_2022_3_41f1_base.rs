use std::fs;

use crate::{assert_eq, Os};

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

    let mut res = true;

    let fields = [
        (
            "AsyncLoadSceneCount",
            "2",
            "Scene count not matching after LoadSceneAsync call",
        ),
        (
            "AsyncLoadLoadedSceneCount",
            "1",
            "Loaded scene count not matching after LoadSceneAsync call",
        ),
        (
            "AsyncLoadAllowLoadSceneCount",
            "2",
            "Scene count is not matching after allowSceneActivation true",
        ),
        (
            "AsyncLoadAllowLoadLoadedSceneCount",
            "1",
            "Loaded scene count is not matching after allowSceneActivation true",
        ),
        (
            "AsyncLoadAllowLoadNextFrameSceneCount",
            "2",
            "Scene count 1f after frame",
        ),
        (
            "AsyncLoadAllowLoadNextFrameLoadedSceneCount",
            "2",
            "Loaded scene count 1f after frame",
        ),
        (
            "AsyncLoadCallbackSceneCount",
            "2",
            "Scene count in AsyncOperation event callback for load",
        ),
        (
            "AsyncLoadCallbackLoadedSceneCount",
            "2",
            "Loaded scene count in AsyncOperation event callback for load",
        ),
        (
            "AsyncUnloadSceneCount",
            "2",
            "Scene count after AsyncSceneUnload call",
        ),
        (
            "AsyncUnloadLoadedSceneCount",
            "1",
            "Loaded scene count after AsyncSceneUnload call",
        ),
        (
            "AsyncUnloadAllowLoadNextFrameSceneCount",
            "1",
            "Scene count after 1f for scene unload",
        ),
        (
            "AsyncUnloadAllowLoadNextFrameLoadedSceneCount",
            "1",
            "Loaded scene count after 1f for scene unload",
        ),
        (
            "AsyncUnloadCallbackSceneCount",
            "1",
            "Scene count in AsyncOperation callback for unload",
        ),
        (
            "AsyncUnloadCallbackLoadedSceneCount",
            "1",
            "Loaded scene count in AsyncOperation callback for unload",
        ),
        (
            "AsyncLoadCallbackFrame",
            "3",
            "Callback time of the Async Load is wrong",
        ),
        (
            "AsyncUnloadCallbackFrame",
            "4",
            "Callback time of the Async Unload is wrong",
        ),
        (
            "AsyncLoadCallback2Frame",
            "7",
            "Callback time of Async Load is wrong, there should be a frame delay, then load happens",
        ),
        (
            "AsyncLoadCallback3Frame",
            "10",
            "Callback time of Async Load is wrong, stalling the scene load during the frame delay would not redo the frame delay"
        ),
        (
            "AsyncLoadCallback4Frame",
            "15",
            "Callback time of Async Load is wrong, frame delay should be done a while ago"
        ),
        (
            "AsyncLoadSyncLoadCallbackFrame",
            "16",
            "Callback time of Async Load should have no delay, since a non-async scene load was called",
        ),
        (
            "AsyncLoadSyncLoadCallback2Frame",
            "16",
            "Callback time of Async Load should have no delay, since a non-async scene load was called",
        ),
        (
            "AsyncLoadCallback5Frame",
            "19",
            "UniTAS didn't restore the frame delay after doing a sync scene load",
        ),
        (
            "AfterLoadsSceneCount",
            "8",
            "Total scene count doesn't match",
        ),
        (
            "AfterLoadsLoadedSceneCount",
            "8",
            "Total loaded scene count doesn't match",
        ),
        (
            "AfterDoubleUnloadSceneCount",
            "7",
            "Scene count for double async unload should just decrease scene count by 1",
        ),
        (
            "AfterDoubleUnloadLoadedSceneCount",
            "7",
            "Loaded scene count for double async unload should just decrease scene count by 1",
        ),
        (
            "DoubleUnloadOperationIsNull",
            "true",
            "The double unload operation's second call isn't null"
        ),
        (
            "DoubleUnloadDiffNameSuccess",
            "true",
            "Somehow failed to unload two scenes under different names at the same time",
        ),
        (
            "DoubleUnloadDiffNameBefore2",
            "true",
            "Order of double unload is wrong",
        ),
        (
            "DoubleUnloadNameIdSecondIsNull",
            "true",
            "Two unload operations in a row with matching name and ID will still not work"
        ),
        (
            "SceneNameInitial",
            "General",
            "Initial scene should be the general testing scene",
        ),
        (
            "SceneAddedName",
            "Empty",
            "Additional scene should be here as soon as the scene load is invoked",
        ),
        (
            "SceneAddedIsLoaded",
            "false",
            "Loading should be true",
        ),
        (
            "SceneAddedRootCount",
            "0",
            "Root count is 0 during load",
        ),
        (
            "SceneAddedIsSubScene",
            "false",
            "The scene loading isn't a sub scene",
        ),
        (
            "SceneAddedPath",
            "Assets/Scenes/Empty.unity",
            "Asset path isn't matching",
        ),
        (
            "SceneAddedBuildIndex",
            "3",
            "Loading scene build index isnt matching",
        ),
        (
"SceneAddedIsDirty",
"false",
"Scene can't be dirty during load",
        ),
        (
            "SceneAddedIsValid",
            "true",
            "Loading scene should be valid",
        ),
        (
            "SceneAddedRealEqDummy",
            "true",
            "Dummy scene struct should match real scene struct",
        ),
        (
            "SceneAddedRealEqualsDummy",
            "true",
            "Dummy scene struct should match real scene struct",
        ),
        (
            "SceneAddedRealNeqDummy",
            "false",
            "Dummy scene struct should match real scene struct",
        ),
    ];

    stream.run_general_tests(&fields, &mut res)?;

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
        assert_eq!(
            format!("jump button count field {field}"),
            "5",
            stream.receive()?,
            format!("checking LegacyInputSystemTest.{field} field to be 5"),
            res
        );
    }

    stream.send("print(legacy_input_system_test.field('_horizontalAxisMoveCount').get_value())")?;
    assert_eq!(
        "horizontal axis move count", "6",
        stream.receive()?,
        "checking LegacyInputSystemTest._horizontalAxisMoveCount field", res
    );

    // SceneTest.cs
    stream.send("scene_test = traverse('SceneTest')")?;

    stream.send("print(scene_test.field('_asyncOpCallbackProgress').get_value())")?;
    assert_eq!(
        "async op callback progress", "1",
        stream.receive()?,
        "async operation progress in callback isn't 1.0", res
    );

    stream.send("print(scene_test.field('_asyncOpCallbackAllowSceneActivation').get_value())")?;
    assert_eq!(
        "async operation callback allowSceneActivation", "true",
        stream.receive()?,
        "allowSceneActivation is false", res
    );

    stream.send("print(scene_test.field('_asyncOpCallbackIsDone').get_value())")?;
    assert_eq!(
        "async operation callback isDone", "true",
        stream.receive()?,
        "isDone isn't true", res
    );

    stream.send("print(scene_test.field('_asyncOpDoneFrame').get_value())")?;
    assert_eq!(
        "SceneTest async op done frame", "19",
        stream.receive()?,
        "checking SceneTest callback done timing frame", res
    );

    // UGuiTest.cs
    stream.send("ugui_test = traverse('UGuiTest')")?;

    stream.send("print(ugui_test.field('_clickCount').get_value())")?;
    assert_eq!(
        "UGuiTest click count", "5",
        stream.receive()?,
        "didn't match click count of 5", res
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
    let max_fixed_update_count = 4u8;

    // TODO: create standard way to easily read things
    // TODO: on unity side, create a "results" class containing all results of the test
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

    assert_eq!(
        "multiple fixed update: fixed update count",
        max_fixed_update_count.to_string(),
        stream.receive()?,
        "mismatch in FixedUpdate count",
        res
    );

    assert_eq!(
        "multiple fixed update: update count", "2",
        stream.receive()?,
        "mismatch in Update count", res
    );

    stream.send("full_access(true)")?;
    stream.receive()?;

    // struct test
    stream.send(
        r#"local StructTest = traverse("StructTest")
        
        print("StructTest._constrainedTestSuccess: " .. tostring(StructTest.field("_constrainedTestSuccess").get_value()))
        "#,
    )?;

    assert_eq!(
        "StructTest: constrained opcode test", "StructTest._constrainedTestSuccess: true",
        stream.receive()?,
        "UniTAS failed to properly handle the constrained opcode", res
    );

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

    Ok(res)
}
