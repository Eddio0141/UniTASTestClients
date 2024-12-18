using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralTests : MonoBehaviour
{
    public static AsyncOperation LoadEmpty2;

    private IEnumerator Start()
    {
        // StructTest
        Assert.NotThrows("struct.constrained_opcode", () => _ = new StructTest("bar"));

        var startFrame = Time.frameCount;
        Assert.Equal("scene.initial", "General", SceneManager.GetSceneAt(0).name);

        // TODO: general scene isn't loaded by normal means, so this test fails
        // either: isolate general test as its own unity game, make general test default scene, use reflection on test runner to add this scene

        // Assert.Null("scene.unload.current_only_scene", SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene()));

        // Empty has yet to be loaded
        Assert.Throws("scene.unload.missing", new ArgumentException("Scene to unload is invalid"),
            () => SceneManager.UnloadSceneAsync("Empty"));

        // frame 1
        var loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        var emptyScene = SceneManager.GetSceneAt(1);
        Assert.Equal("scene.get_scene_at.name", "Empty", emptyScene.name);
        Assert.False("scene.get_scene_at.isLoaded", emptyScene.isLoaded);
        Assert.Equal("scene.get_scene_at.rootCount", 0, emptyScene.rootCount);
        Assert.False("scene.get_scene_at.isSubScene", emptyScene.isSubScene);
        Assert.Equal("scene.get_scene_at.path", "Assets/Scenes/Empty.unity", emptyScene.path);
        Assert.Equal("scene.get_scene_at.buildIndex", 3, emptyScene.buildIndex);
        Assert.False("scene.get_scene_at.isDirty", emptyScene.isDirty);
        Assert.True("scene.get_scene_at.IsValid", emptyScene.IsValid());
        Assert.Equal("scene.op.progress", 0.9f, loadEmpty.progress, 0.0001f);
        Assert.False("scene.op.isDone", loadEmpty.isDone);

        Assert.Throws("scene.get_scene_at.set_name",
            new InvalidOperationException(
                "Setting a name on a saved scene is not allowed (the filename is used as name). Scene: 'Assets/Scenes/Empty.unity'"),
            () => emptyScene.name = "foo");

        Assert.Equal("scene.sceneCount", 2, SceneManager.sceneCount);
        Assert.Equal("scene.loadedSceneCount", 1, SceneManager.loadedSceneCount);
        loadEmpty.allowSceneActivation = false;
        loadEmpty.completed += _ =>
        {
            // frame 3
            // sceneCount to get count including loading / unloading
            Assert.Equal("scene.sceneCount", 2, SceneManager.sceneCount);
            Assert.Equal("scene.loadedSceneCount", 2, SceneManager.loadedSceneCount);
            Assert.Equal("scene.op.callback_time", 2, Time.frameCount - startFrame);

            var actualScene = SceneManager.GetSceneAt(1);
            Assert.True("scene.dummy_scene_struct.eq", emptyScene == actualScene);
            Assert.False("scene.dummy_scene_struct.neq", emptyScene != actualScene);
            Assert.True("scene.dummy_scene_struct.Equals", emptyScene.Equals(actualScene));
            Assert.Equal("scene.dummy_scene_struct.name", "Empty", emptyScene.name);
            Assert.True("scene.dummy_scene_struct.isLoaded", emptyScene.isLoaded);
            Assert.Equal("scene.dummy_scene_struct.rootCount", 1, emptyScene.rootCount);
            Assert.False("scene.dummy_scene_struct.isSubScene", emptyScene.isSubScene);
            Assert.Equal("scene.dummy_scene_struct.path", "Assets/Scenes/Empty.unity", emptyScene.path);
            Assert.Equal("scene.dummy_scene_struct.buildIndex", 3, emptyScene.buildIndex);
            Assert.False("scene.dummy_scene_struct.isDirty", emptyScene.isDirty);
            Assert.True("scene.dummy_scene_struct.IsValid", emptyScene.IsValid());
            Assert.NotEqual("scene.dummy_scene_struct.handle", 0, emptyScene.handle);
            Assert.NotEqual("scene.dummy_scene_struct.hash_code", 0, emptyScene.GetHashCode());
        };

        Assert.Throws("scene.dummy_scene_struct.set_active",
            new ArgumentException(
                "SceneManager.SetActiveScene failed; scene 'Empty' is not loaded and therefore cannot be set active"),
            () => SceneManager.SetActiveScene(emptyScene));

        var emptyScene2 = SceneManager.GetSceneByName("Empty");

        Assert.Throws("scene.dummy_scene_struct.set_active",
            new ArgumentException(
                "SceneManager.SetActiveScene failed; scene 'Empty' is not loaded and therefore cannot be set active"),
            () => SceneManager.SetActiveScene(emptyScene2));

        var emptyScene3 = SceneManager.GetSceneByPath("Assets/Scenes/Empty.unity");

        Assert.Throws("scene.dummy_scene_struct.set_active",
            new ArgumentException(
                "SceneManager.SetActiveScene failed; scene 'Empty' is not loaded and therefore cannot be set active"),
            () => SceneManager.SetActiveScene(emptyScene3));

        yield return null;
        Assert.False("scene.op.isDone", loadEmpty.isDone);
        // frame 2
        // loadEmpty 1f delay

        loadEmpty.allowSceneActivation = true;
        Assert.Equal("scene.sceneCount", 2, SceneManager.sceneCount);
        Assert.Equal("scene.loadedSceneCount", 1, SceneManager.loadedSceneCount);

        yield return null;
        Assert.True("scene.op.isDone", loadEmpty.isDone);
        Assert.Equal("scene.op.progress", 1f, loadEmpty.progress, 0.0001f);

        var general = SceneManager.GetActiveScene();
        Assert.True("scene.dummy_scene_struct.set_active", SceneManager.SetActiveScene(emptyScene));
        SceneManager.SetActiveScene(general);
        Assert.True("scene.dummy_scene_struct.set_active", SceneManager.SetActiveScene(emptyScene2));
        SceneManager.SetActiveScene(general);
        Assert.True("scene.dummy_scene_struct.set_active", SceneManager.SetActiveScene(emptyScene3));

        // frame 3

        Assert.Equal("scene.sceneCount", 2, SceneManager.sceneCount);
        Assert.Equal("scene.loadedSceneCount", 2, SceneManager.loadedSceneCount);

        var unloadEmpty = SceneManager.UnloadSceneAsync("Empty")!;
        Assert.Equal("scene.sceneCount", 2, SceneManager.sceneCount);
        Assert.Equal("scene.loadedSceneCount", 1, SceneManager.loadedSceneCount);
        Assert.Equal("scene.unload_op.progress", 0f, unloadEmpty.progress, 0.0001f);
        Assert.False("scene.unload_op.isDone", unloadEmpty.isDone);
        var startFrame3 = Time.frameCount;
        unloadEmpty.completed += _ =>
        {
            // frame 4
            Assert.Equal("scene.sceneCount", 1, SceneManager.sceneCount);
            Assert.Equal("scene.loadedSceneCount", 1, SceneManager.loadedSceneCount);
            Assert.Equal("scene.unload.frame", 1, Time.frameCount - startFrame3);
        };

        yield return null;
        // frame 4

        Assert.Equal("scene.unload_op.progress", 1f, unloadEmpty.progress, 0.0001f);
        Assert.True("scene.unload_op.isDone", unloadEmpty.isDone);
        Assert.Equal("scene.sceneCount", 1, SceneManager.sceneCount);
        Assert.Equal("scene.loadedSceneCount", 1, SceneManager.loadedSceneCount);

        yield return null;
        // frame 5

        loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        var startFrame4 = Time.frameCount;
        loadEmpty.completed += _ =>
        {
            // frame 7
            Assert.Equal("scene.load.frame", 2, Time.frameCount - startFrame4);
        };

        yield return null;
        // frame 6

        yield return null;
        // frame 7

        loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        var startFrame5 = Time.frameCount;
        loadEmpty.completed += _ =>
        {
            // frame 10
            Assert.Equal("scene.load.frame", 3, Time.frameCount - startFrame5);
        };

        yield return null;
        // frame 8
        // loadEmpty 1f delay
        loadEmpty.allowSceneActivation = false; // doing this would already have the 1f delay erased

        yield return null;
        // frame 9
        loadEmpty.allowSceneActivation = true;

        yield return null;
        // frame 10

        loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        loadEmpty.allowSceneActivation = false;
        var startFrame6 = Time.frameCount;
        loadEmpty.completed += _ =>
        {
            // frame 15
            Assert.Equal("scene.load.frame", 5, Time.frameCount - startFrame6);
        };

        yield return null;
        // frame 11
        yield return null;
        // frame 12
        yield return null;
        // frame 13
        yield return null;
        // frame 14
        loadEmpty.allowSceneActivation = true;

        yield return null;
        // frame 15

        loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        loadEmpty.allowSceneActivation = false;
        var startFrame7 = Time.frameCount;
        loadEmpty.completed += _ =>
        {
            // frame 16
            Assert.Equal("scene.load.frame", 1, Time.frameCount - startFrame7);
        };
        SceneManager.LoadScene("Empty", LoadSceneMode.Additive);
        loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        loadEmpty.allowSceneActivation = false;
        loadEmpty.completed += _ =>
        {
            // frame 16
            Assert.Equal("scene.load.frame", 1, Time.frameCount - startFrame7);
        };

        yield return null;
        // frame 16

        yield return null;
        // frame 17

        loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        var startFrame8 = Time.frameCount;
        loadEmpty.completed += _ =>
        {
            // frame 19
            Assert.Equal("scene.load.frame", 2, Time.frameCount - startFrame8);
        };

        yield return null;
        // frame 18

        yield return null;
        // frame 19

        Assert.Equal("scene.sceneCount", 8, SceneManager.sceneCount);
        Assert.Equal("scene.loadedSceneCount", 8, SceneManager.loadedSceneCount);

        // multiple unload at the same time conflicts, and only 1 unloads
        SceneManager.UnloadSceneAsync("Empty");
        Assert.Null("scene.unload.invalid", SceneManager.UnloadSceneAsync("Empty"));
        var sceneCount = SceneManager.sceneCount;
        Assert.False("scene.scene_struct.isLoaded", SceneManager.GetSceneAt(1).isLoaded);
        var firstEmpty = true;
        for (var i = 0; i < sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.name != "Empty") continue;

            if (firstEmpty)
            {
                firstEmpty = false;
                Assert.Null("scene.unload.invalid", SceneManager.UnloadSceneAsync(scene));
            }
            else
                Assert.NotNull("scene.unload.valid", SceneManager.UnloadSceneAsync(scene));
        }

        yield return null;
        // frame 20

        Assert.Equal("scene.sceneCount", 1, SceneManager.sceneCount);
        Assert.Equal("scene.loadedSceneCount", 1, SceneManager.loadedSceneCount);

        SceneManager.LoadScene("Empty", LoadSceneMode.Additive);

        yield return null;

        SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(1));
        Assert.Null("scene.unload.invalid", SceneManager.UnloadSceneAsync("Empty"));

        yield return null;

        SceneManager.LoadScene("Empty", LoadSceneMode.Additive);
        SceneManager.LoadScene("Empty", LoadSceneMode.Additive);

        yield return null;

        SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(1));
        Assert.Null("scene.unload.valid", SceneManager.UnloadSceneAsync("Empty"));

        yield return null;

        SceneManager.LoadScene("Empty", LoadSceneMode.Additive);
        SceneManager.LoadScene("Empty", LoadSceneMode.Additive);
        SceneManager.LoadScene("Empty", LoadSceneMode.Additive);
        SceneManager.LoadScene("Empty2", LoadSceneMode.Additive);

        SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!.completed += _ => throw new Exception("foo");

        yield return null;

        unloadEmpty = SceneManager.UnloadSceneAsync("Empty2")!;
        var emptyUnloadFinish = false;
        var empty2UnloadFinish = false;
        unloadEmpty.completed += _ =>
        {
            // frame 23
            empty2UnloadFinish = true;
            Assert.False("scene.op_order", emptyUnloadFinish);
        };
        unloadEmpty = SceneManager.UnloadSceneAsync("Empty")!;
        unloadEmpty.completed += _ =>
        {
            emptyUnloadFinish = true;
            // frame 23
        };

        yield return null;
        // frame 23

        Assert.True("scene.op.completed_callback", emptyUnloadFinish);
        Assert.True("scene.op.completed_callback", empty2UnloadFinish);

        // try unload name and id
        SceneManager.UnloadSceneAsync("Empty");
        // empty is id 3
        Assert.Null("scene.unload.missing", SceneManager.UnloadSceneAsync(3));

        yield return null;
        // frame 24

        var prevSceneCount = SceneManager.sceneCount;

        // try load / unload non-existent scene
        Assert.Log("scene.load.missing", LogType.Error,
            "Scene 'InvalidScene' couldn't be loaded because it has not been added to the build settings or the AssetBundle has not been loaded." +
            Environment.NewLine +
            "To add a scene to the build settings use the menu File->Build Settings...");
        // ReSharper disable once Unity.LoadSceneUnexistingScene
        loadEmpty = SceneManager.LoadSceneAsync("InvalidScene", LoadSceneMode.Additive);
        Assert.Null("scene.unload.missing", loadEmpty);
        Assert.Equal("scene.sceneCount", 0, SceneManager.sceneCount - prevSceneCount);

        Assert.Log("scene.load.missing", LogType.Error,
            "Scene 'InvalidScene' couldn't be loaded because it has not been added to the build settings or the AssetBundle has not been loaded." +
            Environment.NewLine +
            "To add a scene to the build settings use the menu File->Build Settings...");
        // ReSharper disable once Unity.LoadSceneUnexistingScene
        SceneManager.LoadScene("InvalidScene", LoadSceneMode.Additive);
        Assert.Equal("scene.sceneCount", 0, SceneManager.sceneCount - prevSceneCount);

        Assert.Throws("scene.unload.missing", new ArgumentException("Scene to unload is invalid"),
            () => SceneManager.UnloadSceneAsync("InvalidScene"));
        // unload scene that was never touched
        Assert.Throws("scene.unload.missing", new ArgumentException("Scene to unload is invalid"),
            () => SceneManager.UnloadSceneAsync(1));

        loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        Assert.Equal("scene.op.progress", 0.9f, loadEmpty.progress, 0.0001f);
        var loadEmpty2 = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        Assert.Equal("scene.op.progress", 0.9f, loadEmpty2.progress, 0.0001f);
        unloadEmpty = SceneManager.UnloadSceneAsync("Empty")!;

        yield return null;
        // frame 25

        Assert.Equal("scene.op.progress", 0.9f, loadEmpty.progress, 0.0001f);
        Assert.Equal("scene.op.progress", 0.9f, loadEmpty2.progress, 0.0001f);
        Assert.False("scene.op.isDone", unloadEmpty.isDone);

        yield return null;
        // frame 26

        Assert.True("scene.op.isDone", loadEmpty.isDone);
        Assert.False("scene.op.isDone", loadEmpty2.isDone);
        Assert.False("scene.op.isDone", unloadEmpty.isDone);

        yield return null;
        // frame 27

        Assert.True("scene.op.isDone", loadEmpty2.isDone);
        Assert.True("scene.op.isDone", unloadEmpty.isDone);

        yield return null;
        // frame 28

        Assert.True("scene.op.isDone", unloadEmpty.isDone);

        SceneManager.LoadScene("Empty2", LoadSceneMode.Additive);

        yield return null;

        loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        loadEmpty2 = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        unloadEmpty = SceneManager.UnloadSceneAsync("Empty")!;
        SceneManager.LoadScene("Empty", LoadSceneMode.Additive);
        var unloadEmpty2 = SceneManager.UnloadSceneAsync("Empty2")!;

        yield return null;
        // frame 29

        Assert.True("scene.op.isDone", loadEmpty.isDone);
        Assert.True("scene.op.isDone", loadEmpty2.isDone);
        Assert.True("scene.op.isDone", unloadEmpty.isDone);
        Assert.True("scene.op.isDone", unloadEmpty2.isDone);

        SceneManager.LoadScene("Empty2", LoadSceneMode.Additive);

        yield return null;

        unloadEmpty = SceneManager.UnloadSceneAsync("Empty")!;
        unloadEmpty2 = SceneManager.UnloadSceneAsync("Empty2")!;
        loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;

        yield return null;

        Assert.True("scene.op.isDone", unloadEmpty.isDone);
        Assert.True("scene.op.isDone", unloadEmpty2.isDone);
        Assert.False("scene.op.isDone", loadEmpty.isDone);

        yield return null;

        Assert.True("scene.op.isDone", loadEmpty.isDone);

        prevSceneCount = SceneManager.sceneCount;
        var prevLoadedSceneCount = SceneManager.loadedSceneCount;

        // scene load additive -> scene load non-additive
        loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        var startFrame2 = Time.frameCount;
        loadEmpty.completed += _ => { Assert.Equal("scene.op.load_frame", 2, Time.frameCount - startFrame2); };
        var loadGeneral2 = SceneManager.LoadSceneAsync("General2", LoadSceneMode.Single)!;
        loadGeneral2.completed += _ => { Assert.Equal("scene.op.load_frame", 3, Time.frameCount - startFrame2); };
        LoadEmpty2 = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        LoadEmpty2.completed += _ => { Assert.Equal("scene.op.load_frame", 4, Time.frameCount - startFrame2); };

        Assert.Equal("scene.op.progress", 0.9f, loadEmpty.progress, 0.0001f);
        Assert.Equal("scene.op.progress", 0.9f, LoadEmpty2.progress, 0.0001f);
        Assert.Equal("scene.op.progress", 0.9f, loadGeneral2.progress, 0.0001f);
        Assert.Equal("scene.sceneCount", 3, SceneManager.sceneCount - prevSceneCount);
        Assert.Equal("scene.loadedSceneCount", 0, SceneManager.loadedSceneCount - prevLoadedSceneCount);

        yield return null;
        yield return null;
        // loadEmpty

        yield return null;
        // General2 (this won't run)
    }

    private readonly struct StructTest
    {
        private readonly string _dummyMsg;

        static StructTest()
        {
            // test opcode `constrained` and `callvirt` being together, this should not throw
            _ = new StructTest("foo").ToString();
        }

        public StructTest(string dummyMsg)
        {
            _dummyMsg = dummyMsg;
        }

        public override string ToString()
        {
            return _dummyMsg;
        }
    }
}