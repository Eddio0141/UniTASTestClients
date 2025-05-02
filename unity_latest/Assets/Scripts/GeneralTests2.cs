using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralTests2 : MonoBehaviour
{
    [SerializeField] private GameObject cubePrefab;

    private IEnumerator Start()
    {
        InstantiateSceneSyncLoad();
        yield return ObjectInstantiateDuringScene1FDelay();
    }

    private void InstantiateSceneSyncLoad()
    {
        Assert.False("InstantiateSceneSyncLoad.isDone", GeneralTests.AsyncInit.isDone);
        GeneralTests.AsyncInit.allowSceneActivation = true;
        Assert.True("InstantiateSceneSyncLoad.object_not_null", GeneralTests.AsyncInit.Result[0] != null);
    }

    private IEnumerator ObjectInstantiateDuringScene1FDelay()
    {
        SceneManager.LoadSceneAsync("General3");
        var initOp = InstantiateAsync(cubePrefab);
        yield return null;
        // now in 1f delay
        Assert.True("ObjectInstantiateAfterAsyncLoad.isDone", initOp.isDone);
    }
}
