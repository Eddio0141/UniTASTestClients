using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralTests : MonoBehaviour
{
    private IEnumerator Start()
    {
        var loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        Results.SceneCountAsyncLoad = SceneManager.sceneCount;
        Results.LoadedSceneCountAsyncLoad = SceneManager.loadedSceneCount;
        loadEmpty.allowSceneActivation = false;
        loadEmpty.completed += _ =>
        {
            // sceneCount to get count including loading / unloading
            Results.SceneCountAsyncLoadCallback = SceneManager.sceneCount;
            Results.LoadedSceneCountAsyncLoadCallback = SceneManager.loadedSceneCount;
        };

        yield return null;
        loadEmpty.allowSceneActivation = true;
        Results.SceneCountAsyncLoadAllowLoad = SceneManager.sceneCount;
        Results.LoadedSceneCountAsyncLoadAllowLoad = SceneManager.loadedSceneCount;

        yield return null;

        Results.SceneCountAsyncLoadAllowLoadNextFrame = SceneManager.sceneCount;
        Results.LoadedSceneCountAsyncLoadAllowLoadNextFrame = SceneManager.loadedSceneCount;

        var unloadEmpty = SceneManager.UnloadSceneAsync("Empty")!;
        Results.SceneCountAsyncUnload = SceneManager.sceneCount;
        Results.LoadedSceneCountAsyncUnload = SceneManager.loadedSceneCount;
        unloadEmpty.completed += _ =>
        {
            Results.SceneCountAsyncUnloadCallback = SceneManager.sceneCount;
            Results.LoadedSceneCountAsyncUnloadCallback = SceneManager.loadedSceneCount;
        };

        yield return null;

        Results.SceneCountAsyncUnloadAllowLoadNextFrame = SceneManager.sceneCount;
        Results.LoadedSceneCountAsyncUnloadAllowLoadNextFrame = SceneManager.loadedSceneCount;

        yield return null;

        Results.GeneralTestsDone = true;
        Results.LogResults();
    }
}
