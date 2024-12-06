using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralTests3 : MonoBehaviour
{
    private void Start()
    {
        // frame 1
        Results.SceneAdditiveSingleSceneCount4 = SceneManager.sceneCount;
        Results.SceneAdditiveSingleLoadedSceneCount4 = SceneManager.loadedSceneCount;

        // scene non-additive -> 1f -> scene load additive
        var loadGeneral4 = SceneManager.LoadSceneAsync("General4", LoadSceneMode.Single)!;
        var loadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        Results.SceneAdditiveSingleLoadProgress5 = loadGeneral4.progress;
        Results.SceneAdditiveSingleLoadProgress6 = loadEmpty.progress;

        Results.SceneAdditiveSingleSceneCount5 = SceneManager.sceneCount;
        Results.SceneAdditiveSingleLoadedSceneCount5 = SceneManager.loadedSceneCount;
    }
}