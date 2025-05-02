using System.Diagnostics.CodeAnalysis;
using UnityEngine;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class InstantiateAsync__2022_3__latest : MonoBehaviour
{
    [TestInjectPrefab] public GameObject prefab;
    
    [Test]
    public void ForceSameFrameLoad()
    {
        // interesting enough, this makes it load on the same frame
        // var initOp = InstantiateAsync(prefab);
        // initOp.allowSceneActivation = false;
        // Assert.False("InstantiateWaitSceneActivation.before_load.isDone", initOp.isDone);
        // Assert.True(initOp.IsWaitingForSceneActivation());
        // initOp.allowSceneActivation = true;
        // Assert.True(initOp.isDone);
        // Assert.NotNull("InstantiateWaitSceneActivation.after_load.Result", initOp.Result);
    }
}