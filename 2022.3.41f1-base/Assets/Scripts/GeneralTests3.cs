using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralTests3 : MonoBehaviour
{
    public static AsyncOperation LoadGeneral4;
    public static AsyncOperation LoadEmpty;

    private IEnumerator Start()
    {
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        yield return null;

        Assert.Equal("scene.sceneCount", 2, SceneManager.sceneCount);
        Assert.Equal("scene.loadedSceneCount", 2, SceneManager.loadedSceneCount);

        yield return null;

        // scene non-additive -> 1f -> scene load additive
        LoadGeneral4 = SceneManager.LoadSceneAsync("General4", LoadSceneMode.Single)!;
        LoadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;

        Assert.Equal("scene.sceneCount", 3, SceneManager.sceneCount);
        Assert.Equal("scene.loadedSceneCount", 1, SceneManager.loadedSceneCount);
    }
}