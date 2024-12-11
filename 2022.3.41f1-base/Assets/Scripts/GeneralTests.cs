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

        // frame 1
        var loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        var emptyScene = SceneManager.GetSceneAt(1);
        Assert.Equal("scene.get_scene_at.name", "Empty", emptyScene.name);
        Assert.Equal("scene.get_scene_at.isLoaded", false, emptyScene.isLoaded);
        Assert.Equal("scene.get_scene_at.rootCount", 0, emptyScene.rootCount);
        Assert.Equal("scene.get_scene_at.isSubScene", false, emptyScene.isSubScene);
        Assert.Equal("scene.get_scene_at.path", "Assets/Scenes/Empty.unity", emptyScene.path);
        Assert.Equal("scene.get_scene_at.buildIndex", 3, emptyScene.buildIndex);
        Assert.Equal("scene.get_scene_at.isDirty", false, emptyScene.isDirty);
        Assert.Equal("scene.get_scene_at.IsValid", true, emptyScene.IsValid());
        Assert.Equal("scene.op.progress", 0.9f, loadEmpty.progress, 0.0001f);
        Assert.Equal("scene.op.isDone", false, loadEmpty.isDone);

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
            Assert.Equal("scene.dummy_scene_struct.isLoaded", true, emptyScene.isLoaded);
            Assert.Equal("scene.dummy_scene_struct.rootCount", 1, emptyScene.rootCount);
            Assert.Equal("scene.dummy_scene_struct.isSubScene", false, emptyScene.isSubScene);
            Assert.Equal("scene.dummy_scene_struct.path", "Assets/Scenes/Empty.unity", emptyScene.path);
            Assert.Equal("scene.dummy_scene_struct.buildIndex", 3, emptyScene.buildIndex);
            Assert.Equal("scene.dummy_scene_struct.isDirty", false, emptyScene.isDirty);
            Assert.Equal("scene.dummy_scene_struct.IsValid", true, emptyScene.IsValid());
            Assert.NotEqual("scene.dummy_scene_struct.handle", 0, emptyScene.handle);
            Assert.NotEqual("scene.dummy_scene_struct.hash_code", 0, emptyScene.GetHashCode());
        };

        yield return null;
        Assert.Equal("scene.op.isDone", false, loadEmpty.isDone);
        // frame 2
        // loadEmpty 1f delay

        loadEmpty.allowSceneActivation = true;
        Assert.Equal("scene.sceneCount", 2, SceneManager.sceneCount);
        Assert.Equal("scene.loadedSceneCount", 1, SceneManager.loadedSceneCount);

        yield return null;
        Assert.Equal("scene.op.isDone", true, loadEmpty.isDone);
        Assert.Equal("scene.op.progress", 1f, loadEmpty.progress, 0.0001f);
        // frame 3

        Assert.Equal("scene.sceneCount", 2, SceneManager.sceneCount);
        Assert.Equal("scene.loadedSceneCount", 2, SceneManager.loadedSceneCount);

        var unloadEmpty = SceneManager.UnloadSceneAsync("Empty")!;
        Assert.Equal("scene.sceneCount", 2, SceneManager.sceneCount);
        Assert.Equal("scene.loadedSceneCount", 1, SceneManager.loadedSceneCount);
        Assert.Equal("scene.unload_op.progress", 0f, unloadEmpty.progress, 0.0001f);
        Assert.Equal("scene.unload_op.isDone", false, unloadEmpty.isDone);
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
        Assert.Equal("scene.unload_op.isDone", true, unloadEmpty.isDone);
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
        Assert.Null("scene.unload.missing", SceneManager.UnloadSceneAsync("Empty"));

        yield return null;
        // frame 20

        Assert.Equal("scene.sceneCount", 7, SceneManager.sceneCount);
        Assert.Equal("scene.loadedSceneCount", 7, SceneManager.loadedSceneCount);

        SceneManager.LoadSceneAsync("Empty2", LoadSceneMode.Additive);

        yield return null;
        // frame 21

        yield return null;
        // frame 22

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

        yield return null;
        // frame 25

        Assert.Equal("scene.op.progress", 0.9f, loadEmpty.progress, 0.0001f);
        Assert.Equal("scene.op.progress", 0.9f, loadEmpty2.progress, 0.0001f);

        yield return null;
        // frame 26

        Assert.Equal("scene.op.isDone", true, loadEmpty.isDone);
        Assert.Equal("scene.op.isDone", false, loadEmpty2.isDone);

        yield return null;
        // frame 27

        Assert.Equal("scene.op.isDone", true, loadEmpty2.isDone);

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