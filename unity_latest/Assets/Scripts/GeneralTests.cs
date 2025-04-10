using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralTests : MonoBehaviour
{
    [SerializeField] private GameObject cubePrefab;

    private IEnumerator Start()
    {
        yield return InstantiateWaitSceneActivation();
        yield return ForceObjectInitBlocking();
        yield return InstantiateAsyncTriple();
        yield return InstantiateAsyncSceneLoadStall();

        Assert.Finish();
    }

    private IEnumerator InstantiateWaitSceneActivation()
    {
        // interesting enough, this makes it load on the same frame
        var initOp = InstantiateAsync(cubePrefab);
        initOp.allowSceneActivation = false;
        Assert.False("InstantiateWaitSceneActivation.before_load.isDone", initOp.isDone);
        Assert.True("InstantiateWaitSceneActivation.before_load.IsWaitingForSceneActivation",
            initOp.IsWaitingForSceneActivation());
        initOp.allowSceneActivation = true;
        Assert.True("InstantiateWaitSceneActivation.after_load.isDone", initOp.isDone);
        Assert.NotNull("InstantiateWaitSceneActivation.after_load.Result", initOp.Result);

        // real stall test
        initOp = InstantiateAsync(cubePrefab);
        Assert.False("InstantiateWaitSceneActivation.after_load_waiting.IsWaitingForSceneActivation",
            initOp.IsWaitingForSceneActivation());
        initOp.allowSceneActivation = false;
        Assert.True("InstantiateWaitSceneActivation.after_load_waiting.IsWaitingForSceneActivation",
            initOp.IsWaitingForSceneActivation());
        Assert.False("InstantiateWaitSceneActivation.after_load_waiting.isDone", initOp.isDone);
        Assert.Null("InstantiateWaitSceneActivation.after_load_waiting.Result", initOp.Result);
        yield return null;
        Assert.Null("InstantiateWaitSceneActivation.after_load_waiting.Result", initOp.Result);
        yield return null;
        Assert.Null("InstantiateWaitSceneActivation.after_load_waiting.Result", initOp.Result);
        yield return null;
        Assert.Null("InstantiateWaitSceneActivation.after_load_waiting.Result", initOp.Result);
        yield return null;
        Assert.Null("InstantiateWaitSceneActivation.after_load_waiting.Result", initOp.Result);
        initOp.allowSceneActivation = true;
        // again, result is ready immediately unlike scene loading
        Assert.True("InstantiateWaitSceneActivation.after_load.isDone", initOp.isDone);
        Assert.False("InstantiateWaitSceneActivation.after_load.IsWaitingForSceneActivation",
            initOp.IsWaitingForSceneActivation());
    }

    private IEnumerator ForceObjectInitBlocking()
    {
        // try force object init immediate
        var frameCount = Time.frameCount;
        var initOp = InstantiateAsync(cubePrefab);
        var initOp2 = InstantiateAsync(cubePrefab);
        var completedCalled = false;
        var completedCalledNonBlocking = false;
        initOp.completed += _ =>
        {
            Assert.Equal("ForceObjectInitBlocking.blocking.load_time", 0, Time.frameCount - frameCount);
            completedCalled = true;
        };
        initOp2.completed += _ => { completedCalledNonBlocking = true; };
        initOp.WaitForCompletion();
        Assert.True("ForceObjectInitBlocking.blocking.completed_called", completedCalled);
        Assert.True("ForceObjectInitBlocking.blocking.isDone", initOp.isDone);
        Assert.False("ForceObjectInitBlocking.blocking.IsWaitingForSceneActivation",
            initOp.IsWaitingForSceneActivation());
        Assert.False("ForceObjectInitBlocking.nonblocking.isDone", initOp2.isDone);
        Assert.False("ForceObjectInitBlocking.nonblocking.completed_called", completedCalledNonBlocking);
        Assert.False("ForceObjectInitBlocking.nonblocking.IsWaitingForSceneActivation",
            initOp2.IsWaitingForSceneActivation());
        // TODO: why does yield return on the op not work, must test too
        // yield return initOp2;
        yield return null;
        Assert.True("ForceObjectInitBlocking.nonblocking.completed_called", completedCalledNonBlocking);
    }

    private IEnumerator InstantiateAsyncTriple()
    {
        // normal, 1f async object init
        var initOp = InstantiateAsync(cubePrefab);
        var initOp2 = InstantiateAsync(cubePrefab);
        var initOp3 = InstantiateAsync(cubePrefab);
        Assert.Null("InstantiateAsyncTriple.before_load.Result", initOp.Result);
        Assert.Null("InstantiateAsyncTriple.before_load.Result", initOp2.Result);
        Assert.Null("InstantiateAsyncTriple.before_load.Result", initOp3.Result);
        Assert.False("InstantiateAsyncTriple.before_load.IsWaitingForSceneActivation",
            initOp.IsWaitingForSceneActivation());
        Assert.False("InstantiateAsyncTriple.before_load.IsWaitingForSceneActivation",
            initOp2.IsWaitingForSceneActivation());
        Assert.False("InstantiateAsyncTriple.before_load.IsWaitingForSceneActivation",
            initOp3.IsWaitingForSceneActivation());
        var frameCount = Time.frameCount;
        var completeCalled = false;
        initOp.completed += _ =>
        {
            Assert.Equal("InstantiateAsyncTriple.load_time", 1, Time.frameCount - frameCount);
            completeCalled = true;
        };
        initOp2.completed += _ =>
        {
            Assert.Equal("InstantiateAsyncTriple.load_time", 1, Time.frameCount - frameCount);
        };
        initOp3.completed += _ =>
        {
            Assert.Equal("InstantiateAsyncTriple.load_time", 1, Time.frameCount - frameCount);
        };
        Assert.False("InstantiateAsyncTriple.before_load.isDone", initOp.isDone);
        Assert.False("InstantiateAsyncTriple.before_load.isDone", initOp2.isDone);
        Assert.False("InstantiateAsyncTriple.before_load.isDone", initOp3.isDone);
        Assert.False("InstantiateAsyncTriple.before_load.complete_called", completeCalled);
        yield return new WaitForEndOfFrame();
        Assert.False("InstantiateAsyncTriple.before_load.isDone", initOp.isDone);
        Assert.False("InstantiateAsyncTriple.before_load.complete_called", completeCalled);
        Assert.Null("InstantiateAsyncTriple.before_load.Result", initOp.Result);
        Assert.Null("InstantiateAsyncTriple.before_load.Result", initOp2.Result);
        Assert.Null("InstantiateAsyncTriple.before_load.Result", initOp3.Result);
        yield return null;
        Assert.True("InstantiateAsyncTriple.after_load.isDone", initOp.isDone);
        Assert.True("InstantiateAsyncTriple.after_load.isDone", initOp2.isDone);
        Assert.True("InstantiateAsyncTriple.after_load.isDone", initOp3.isDone);
        Assert.False("InstantiateAsyncTriple.after_load.IsWaitingForSceneActivation",
            initOp.IsWaitingForSceneActivation());
        Assert.False("InstantiateAsyncTriple.after_load.IsWaitingForSceneActivation",
            initOp2.IsWaitingForSceneActivation());
        Assert.False("InstantiateAsyncTriple.after_load.IsWaitingForSceneActivation",
            initOp3.IsWaitingForSceneActivation());
    }

    private IEnumerator InstantiateAsyncSceneLoadStall()
    {
        var initOp = InstantiateAsync(cubePrefab);
        var loadEmptyScene = SceneManager.LoadSceneAsync("Empty", LoadSceneMode.Additive)!;
        loadEmptyScene.allowSceneActivation = false;
        yield return null;
        Assert.True("InstantiateAsyncSceneLoadStall.isDone", initOp.isDone);
        
        // what about loading with blocking thread? would that force the scene load or not
        initOp = InstantiateAsync(cubePrefab);
        initOp.WaitForCompletion();
        Assert.True("InstantiateAsyncSceneLoadStall.isDone", initOp.isDone);
        yield return null;
        Assert.False("InstantiateAsyncSceneLoadStall.scene.loaded", loadEmptyScene.isDone);
    }
}