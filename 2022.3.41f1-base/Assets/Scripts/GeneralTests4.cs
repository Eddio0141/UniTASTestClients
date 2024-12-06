using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralTests4 : MonoBehaviour
{
    private IEnumerator Start()
    {
        // frame 1
        Results.SceneAdditiveSingleSceneCount6 = SceneManager.sceneCount;
        Results.SceneAdditiveSingleLoadedSceneCount6 = SceneManager.loadedSceneCount;

        yield return null;
        // frame 2
        
        Results.SceneAdditiveSingleSceneCount7 = SceneManager.sceneCount;
        Results.SceneAdditiveSingleLoadedSceneCount7 = SceneManager.loadedSceneCount;
        
        yield return null;
        // frame 3
        Results.SceneAdditiveSingleSceneCount8 = SceneManager.sceneCount;
        Results.SceneAdditiveSingleLoadedSceneCount8 = SceneManager.loadedSceneCount;
        
        yield return null;
        // frame 4
        
        // loadedSceneCount actually increases, so wack!
        Results.SceneAdditiveSingleSceneCount9 = SceneManager.sceneCount;
        Results.SceneAdditiveSingleLoadedSceneCount9 = SceneManager.loadedSceneCount;
        
        Results.GeneralTestsDone = true;
        Results.LogResults();
    }
}