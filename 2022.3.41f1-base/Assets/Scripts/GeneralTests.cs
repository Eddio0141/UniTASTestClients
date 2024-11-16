using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralTests : MonoBehaviour
{
    private IEnumerator Start()
    {
        // scene async load
        var loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        loadEmpty.completed += _ =>
        {
            // sceneCount to get count including loading / unloading
            Results.SceneCountAfterAsyncLoad = SceneManager.sceneCount;
        };
        
        yield return loadEmpty;
        yield return null;
        
        var unloadEmpty = SceneManager.UnloadSceneAsync("Empty")!;
        unloadEmpty.completed += _ => { Results.SceneCountAfterAsyncUnload = SceneManager.sceneCount; };
        
        yield return unloadEmpty;

        Results.GeneralTestsDone = true;
    }
}