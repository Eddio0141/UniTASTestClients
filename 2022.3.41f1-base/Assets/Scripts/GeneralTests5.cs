using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralTests5 : MonoBehaviour
{
    private IEnumerator Start()
    {
        // frame 1
        Results.SceneSingleAdditiveLoadedSceneCount = SceneManager.loadedSceneCount;
        Results.SceneSingleAdditiveLoadProgress3 = GeneralTests4.LoadEmpty.progress;
        Results.SceneSingleAdditiveLoadProgress4 = GeneralTests4.LoadEmpty2.progress;

        yield return null;
        // frame 2
        
        Results.SceneSingleAdditiveLoadProgress5 = GeneralTests4.LoadEmpty.progress;
        Results.SceneSingleAdditiveLoadProgress6 = GeneralTests4.LoadEmpty2.progress;

        yield return null;
        // frame 3

        yield return null;
        // frame 4

        Results.SceneSingleAdditiveLoadedSceneCount2 = SceneManager.loadedSceneCount; // 1st empty loads

        yield return null;
        // frame 5

        Results.SceneSingleAdditiveLoadedSceneCount3 = SceneManager.loadedSceneCount; // 2nd empty loads
        
        yield return null;
        // frame 6
        
        Results.SceneSingleAdditiveLoadedSceneCount4 = SceneManager.loadedSceneCount; // 3rd empty loads
        
        Results.GeneralTestsDone = true;
        Results.LogResults();
    }
}