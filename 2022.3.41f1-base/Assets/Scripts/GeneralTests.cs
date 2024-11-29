using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralTests : MonoBehaviour
{
    private IEnumerator Start()
    {
        var startFrame = Time.frameCount - 1;

        // frame 1
        var loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
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
        };

        yield return null;
        // frame 2
        // loadEmpty 1f delay

        loadEmpty.allowSceneActivation = true;
        Results.AsyncLoadAllowLoadSceneCount = SceneManager.sceneCount;
        Results.AsyncLoadAllowLoadLoadedSceneCount = SceneManager.loadedSceneCount;

        yield return null;
        // frame 3

        Results.AsyncLoadAllowLoadNextFrameSceneCount = SceneManager.sceneCount;
        Results.AsyncLoadAllowLoadNextFrameLoadedSceneCount = SceneManager.loadedSceneCount;

        var unloadEmpty = SceneManager.UnloadSceneAsync("Empty")!;
        Results.AsyncUnloadSceneCount = SceneManager.sceneCount;
        Results.AsyncUnloadLoadedSceneCount = SceneManager.loadedSceneCount;
        unloadEmpty.completed += _ =>
        {
            // frame 4
            Results.AsyncUnloadCallbackSceneCount = SceneManager.sceneCount;
            Results.AsyncUnloadCallbackLoadedSceneCount = SceneManager.loadedSceneCount;
            Results.AsyncUnloadCallbackFrame = Time.frameCount - startFrame;
        };

        yield return null;
        // frame 4

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

        Results.GeneralTestsDone = true;
        Results.LogResults();
    }
}