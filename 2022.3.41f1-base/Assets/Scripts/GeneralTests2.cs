using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralTests2 : MonoBehaviour
{
    private IEnumerator Start()
    {
        // frame 1
        Results.SceneAdditiveSingleSceneCount2 = SceneManager.sceneCount;
        Results.SceneAdditiveSingleLoadedSceneCount2 = SceneManager.loadedSceneCount;

        var prevSceneCount = SceneManager.sceneCount;
        var prevLoadedSceneCount = SceneManager.loadedSceneCount;

        // scene load additive -> 1f -> scene load non-additive
        var loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        Results.SceneAdditiveSingleLoadProgress3 = loadEmpty.progress;
        yield return null;
        // frame 2

        var loadGeneral3 = SceneManager.LoadSceneAsync("General3", LoadSceneMode.Single)!;
        Results.SceneAdditiveSingleLoadProgress4 = loadGeneral3.progress;

        Results.SceneAdditiveSingleSceneCount3 = SceneManager.sceneCount - prevSceneCount;
        Results.SceneAdditiveSingleLoadedSceneCount3 = SceneManager.loadedSceneCount - prevLoadedSceneCount;
    }
}