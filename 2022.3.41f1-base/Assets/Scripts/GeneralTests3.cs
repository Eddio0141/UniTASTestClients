using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralTests3 : MonoBehaviour
{
    public static AsyncOperation LoadGeneral4;
    public static AsyncOperation LoadEmpty;

    private IEnumerator Start()
    {
        // frame 1
        Results.SceneAdditiveSingleSceneCount4 = SceneManager.sceneCount;
        Results.SceneAdditiveSingleLoadedSceneCount4 = SceneManager.loadedSceneCount;

        yield return null;

        // scene non-additive -> 1f -> scene load additive
        LoadGeneral4 = SceneManager.LoadSceneAsync("General4", LoadSceneMode.Single)!;
        LoadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        Results.SceneAdditiveSingleLoadProgress5 = LoadGeneral4.progress;
        Results.SceneAdditiveSingleLoadProgress6 = LoadEmpty.progress;

        Results.SceneAdditiveSingleSceneCount5 = SceneManager.sceneCount;
        Results.SceneAdditiveSingleLoadedSceneCount5 = SceneManager.loadedSceneCount;
    }
}