using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralTests4 : MonoBehaviour
{
    public static AsyncOperation LoadEmpty;
    public static AsyncOperation LoadEmpty2;

    private IEnumerator Start()
    {
        // frame 1
        Results.SceneAdditiveSingleSceneCount6 = SceneManager.sceneCount;
        Results.SceneAdditiveSingleLoadedSceneCount6 = SceneManager.loadedSceneCount;
        Results.SceneAdditiveSingleLoadProgress7 = GeneralTests3.LoadGeneral4.progress;
        Results.SceneAdditiveSingleLoadProgress8 = GeneralTests3.LoadEmpty.progress;

        yield return null;
        // frame 2

        Results.SceneAdditiveSingleSceneCount7 = SceneManager.sceneCount;
        Results.SceneAdditiveSingleLoadedSceneCount7 = SceneManager.loadedSceneCount;
        Results.SceneAdditiveSingleLoadProgress9 = GeneralTests3.LoadEmpty.progress;

        yield return null;
        // frame 3
        Results.SceneAdditiveSingleSceneCount8 = SceneManager.sceneCount;
        Results.SceneAdditiveSingleLoadedSceneCount8 = SceneManager.loadedSceneCount;

        yield return null;
        // frame 4

        // loadedSceneCount actually increases, so wack!
        Results.SceneAdditiveSingleSceneCount9 = SceneManager.sceneCount;
        Results.SceneAdditiveSingleLoadedSceneCount9 = SceneManager.loadedSceneCount;

        SceneManager.LoadSceneAsync("General5", LoadSceneMode.Single);
        LoadEmpty = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        LoadEmpty2 = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive);

        Results.SceneSingleAdditiveLoadProgress = LoadEmpty.progress;
        Results.SceneSingleAdditiveLoadProgress2 = LoadEmpty2.progress;
        Results.SceneSingleAdditiveSceneCount = SceneManager.sceneCount;
    }
}