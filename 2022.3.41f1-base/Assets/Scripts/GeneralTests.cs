using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralTests : MonoBehaviour
{
    private IEnumerator Start()
    {
        var startFrame = Time.frameCount - 1;
        Results.SceneNameInitial = SceneManager.GetSceneAt(0).name;

        // frame 1
        var loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        var emptyScene = SceneManager.GetSceneAt(1);
        Results.SceneAddedName = emptyScene.name;
        Results.SceneAddedIsLoaded = emptyScene.isLoaded;
        Results.SceneAddedRootCount = emptyScene.rootCount;
        Results.SceneAddedIsSubScene = emptyScene.isSubScene;
        Results.SceneAddedPath = emptyScene.path;
        Results.SceneAddedBuildIndex = emptyScene.buildIndex;
        Results.SceneAddedIsDirty = emptyScene.isDirty;
        Results.SceneAddedIsValid = emptyScene.IsValid();
        Results.SceneAddedProgress = loadEmpty.progress;
        Results.SceneAddedIsDone = loadEmpty.isDone;

        try
        {
            emptyScene.name = "foo";
        }
        catch (InvalidOperationException e)
        {
            Results.SceneAddedNameChangeInvalidOp = e.Message;
        }

        Results.AsyncLoadSceneCount = SceneManager.sceneCount;
        Results.AsyncLoadLoadedSceneCount = SceneManager.loadedSceneCount;
        loadEmpty.allowSceneActivation = false;
        loadEmpty.completed += _ =>
        {
            // frame 3
            // sceneCount to get count including loading / unloading
            Results.AsyncLoadCallbackSceneCount = SceneManager.sceneCount;
            Results.AsyncLoadCallbackLoadedSceneCount = SceneManager.loadedSceneCount;
            Results.AsyncLoadCallbackFrame = Time.frameCount - startFrame;

            var actualScene = SceneManager.GetSceneAt(1);
            Results.SceneAddedRealEqDummy = emptyScene == actualScene;
            Results.SceneAddedRealNeqDummy = emptyScene != actualScene;
            Results.SceneAddedRealEqualsDummy = emptyScene.Equals(actualScene);
            Results.SceneAddedRealName = emptyScene.name;
            Results.SceneAddedRealIsLoaded = emptyScene.isLoaded;
            Results.SceneAddedRealRootCount = emptyScene.rootCount;
            Results.SceneAddedRealIsSubScene = emptyScene.isSubScene;
            Results.SceneAddedRealPath = emptyScene.path;
            Results.SceneAddedRealBuildIndex = emptyScene.buildIndex;
            Results.SceneAddedRealIsDirty = emptyScene.isDirty;
            Results.SceneAddedRealIsValid = emptyScene.IsValid();
            Results.SceneAddedRealHandleEq0 = emptyScene.handle == 0;
            Results.SceneAddedRealHashCodeEq0 = emptyScene.GetHashCode() == 0;
        };

        yield return null;
        Results.SceneAddedIsDone2 = loadEmpty.isDone;
        // frame 2
        // loadEmpty 1f delay

        loadEmpty.allowSceneActivation = true;
        Results.AsyncLoadAllowLoadSceneCount = SceneManager.sceneCount;
        Results.AsyncLoadAllowLoadLoadedSceneCount = SceneManager.loadedSceneCount;

        yield return null;
        Results.SceneAddedIsDone3 = loadEmpty.isDone;
        Results.SceneAddedProgress2 = loadEmpty.progress;
        // frame 3

        Results.AsyncLoadAllowLoadNextFrameSceneCount = SceneManager.sceneCount;
        Results.AsyncLoadAllowLoadNextFrameLoadedSceneCount = SceneManager.loadedSceneCount;

        var unloadEmpty = SceneManager.UnloadSceneAsync("Empty")!;
        Results.AsyncUnloadSceneCount = SceneManager.sceneCount;
        Results.AsyncUnloadLoadedSceneCount = SceneManager.loadedSceneCount;
        Results.AsyncUnloadLoadedProgress = unloadEmpty.progress;
        Results.AsyncUnloadLoadedIsDone = unloadEmpty.isDone;
        unloadEmpty.completed += _ =>
        {
            // frame 4
            Results.AsyncUnloadCallbackSceneCount = SceneManager.sceneCount;
            Results.AsyncUnloadCallbackLoadedSceneCount = SceneManager.loadedSceneCount;
            Results.AsyncUnloadCallbackFrame = Time.frameCount - startFrame;
        };

        yield return null;
        // frame 4

        Results.AsyncUnloadLoadedProgress2 = unloadEmpty.progress;
        Results.AsyncUnloadLoadedIsDone2 = unloadEmpty.isDone;
        Results.AsyncUnloadAllowLoadNextFrameSceneCount = SceneManager.sceneCount;
        Results.AsyncUnloadAllowLoadNextFrameLoadedSceneCount = SceneManager.loadedSceneCount;

        yield return null;
        // frame 5

        loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        loadEmpty.completed += _ =>
        {
            // frame 7
            Results.AsyncLoadCallback2Frame = Time.frameCount - startFrame;
        };

        yield return null;
        // frame 6

        yield return null;
        // frame 7

        loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        loadEmpty.completed += _ =>
        {
            // frame 10
            Results.AsyncLoadCallback3Frame = Time.frameCount - startFrame;
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
        loadEmpty.completed += _ =>
        {
            // frame 15
            Results.AsyncLoadCallback4Frame = Time.frameCount - startFrame;
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
        loadEmpty.completed += _ =>
        {
            // frame 16
            Results.AsyncLoadSyncLoadCallbackFrame = Time.frameCount - startFrame;
        };
        SceneManager.LoadScene("Empty", LoadSceneMode.Additive);
        loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        loadEmpty.allowSceneActivation = false;
        loadEmpty.completed += _ =>
        {
            // frame 16
            Results.AsyncLoadSyncLoadCallback2Frame = Time.frameCount - startFrame;
        };

        yield return null;
        // frame 16

        yield return null;
        // frame 17

        loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        loadEmpty.completed += _ =>
        {
            // frame 19
            Results.AsyncLoadCallback5Frame = Time.frameCount - startFrame;
        };

        yield return null;
        // frame 18

        yield return null;
        // frame 19

        Results.AfterLoadsSceneCount = SceneManager.sceneCount;
        Results.AfterLoadsLoadedSceneCount = SceneManager.loadedSceneCount;

        // multiple unload at the same time conflicts, and only 1 unloads
        SceneManager.UnloadSceneAsync("Empty");
        Results.DoubleUnloadOperationIsNull = SceneManager.UnloadSceneAsync("Empty") == null;

        yield return null;
        // frame 20

        Results.AfterDoubleUnloadSceneCount = SceneManager.sceneCount;
        Results.AfterDoubleUnloadLoadedSceneCount = SceneManager.loadedSceneCount;

        SceneManager.LoadSceneAsync("Empty2", LoadSceneMode.Additive);

        yield return null;
        // frame 21

        yield return null;
        // frame 22

        unloadEmpty = SceneManager.UnloadSceneAsync("Empty2")!;
        unloadEmpty.completed += _ =>
        {
            // frame 23
            Results.DoubleUnloadDiffNameSuccess = true;
            Results.DoubleUnloadDiffNameBefore2 = !Results.DoubleUnloadDiffNameSuccess2;
        };
        unloadEmpty = SceneManager.UnloadSceneAsync("Empty")!;
        unloadEmpty.completed += _ =>
        {
            // frame 23
            Results.DoubleUnloadDiffNameSuccess2 = true;
        };

        yield return null;
        // frame 23

        // try unload name and id
        SceneManager.UnloadSceneAsync("Empty");
        // empty is id 3
        Results.DoubleUnloadNameIdSecondIsNull = SceneManager.UnloadSceneAsync(3) == null;

        yield return null;
        // frame 24

        var prevSceneCount = SceneManager.sceneCount;

        // try load / unload non-existent scene
        Application.logMessageReceived += LoadFailLogCheckAsync;
        // ReSharper disable once Unity.LoadSceneUnexistingScene
        loadEmpty = SceneManager.LoadSceneAsync("InvalidScene", LoadSceneMode.Additive);
        Application.logMessageReceived -= LoadFailLogCheckAsync;
        Results.SceneNonExistentAsyncLoadOpIsNull = loadEmpty == null;
        Results.SceneNonExistentAsyncLoadSceneCountDiff = SceneManager.sceneCount - prevSceneCount;

        Application.logMessageReceived += LoadFailLogCheckSync;
        // ReSharper disable once Unity.LoadSceneUnexistingScene
        SceneManager.LoadScene("InvalidScene", LoadSceneMode.Additive);
        Application.logMessageReceived -= LoadFailLogCheckSync;
        Results.SceneNonExistentSyncLoadSceneCountDiff = SceneManager.sceneCount - prevSceneCount;

        try
        {
            SceneManager.UnloadSceneAsync("InvalidScene");
        }
        catch (ArgumentException e)
        {
            Results.SceneNonExistentUnloadEx = e.Message;
        }

        try
        {
            // unload scene that was never touched
            SceneManager.UnloadSceneAsync(1);
        }
        catch (ArgumentException e)
        {
            Results.SceneNeverLoadedUnloadEx = e.Message;
        }

        loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        Results.SceneDoubleProgress = loadEmpty.progress;
        var loadEmpty2 = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        Results.SceneDoubleProgress2 = loadEmpty2.progress;
        
        yield return null;
        // frame 25

        Results.SceneDoubleProgress3 = loadEmpty.progress;
        Results.SceneDoubleProgress4 = loadEmpty2.progress;
        
        yield return null;
        // frame 26

        prevSceneCount = SceneManager.sceneCount;
        var prevLoadedSceneCount = SceneManager.loadedSceneCount;

        // scene load additive -> scene load non-additive
        loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        var loadGeneral2 = SceneManager.LoadSceneAsync("General2", LoadSceneMode.Single)!;

        Results.SceneAdditiveSingleLoadProgress = loadEmpty.progress;
        Results.SceneAdditiveSingleLoadProgress2 = loadGeneral2.progress;
        Results.SceneAdditiveSingleSceneCount = SceneManager.sceneCount - prevSceneCount;
        Results.SceneAdditiveSingleLoadedSceneCount = SceneManager.loadedSceneCount - prevLoadedSceneCount;
    }

    private static void LoadFailLogCheckAsync(string condition, string _, LogType type)
    {
        Results.SceneNonExistentAsyncLoadMsg = condition;
        Results.SceneNonExistentAsyncLoadMsgType = type.ToString();
    }

    private static void LoadFailLogCheckSync(string condition, string _, LogType type)
    {
        Results.SceneNonExistentSyncLoadMsg = condition;
        Results.SceneNonExistentSyncLoadMsgType = type.ToString();
    }
}