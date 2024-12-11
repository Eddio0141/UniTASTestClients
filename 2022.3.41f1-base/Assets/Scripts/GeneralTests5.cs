using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralTests5 : MonoBehaviour
{
    private IEnumerator Start()
    {
        // frame 1
        Assert.Equal("scene.loadedSceneCount", 1, SceneManager.loadedSceneCount);

        yield return null;
        // frame 2
        Assert.Equal("scene.loadedSceneCount", 2, SceneManager.loadedSceneCount);

        yield return null;
        // frame 3
        Assert.Equal("scene.loadedSceneCount", 3, SceneManager.loadedSceneCount);

        yield return null;
        // frame 4
        Assert.Equal("scene.loadedSceneCount", 4, SceneManager.loadedSceneCount);

        Assert.Finish();
    }
}