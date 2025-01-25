using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InitTests : MonoBehaviour
{
    private bool _calledFixedUpdate;
    private bool _calledUpdate;
    // private bool _calledOnGUI;

    private void FixedUpdate()
    {
        if (_calledFixedUpdate) return;
        _calledFixedUpdate = true;
        Assert.False("before Update call", _calledUpdate);
        Assert.Equal("init.frame_count", 1, Time.frameCount);
        Assert.Equal("init.rendered_frame_count", 1, Time.renderedFrameCount);
        // Assert.False("before OnGUI call", _calledOnGUI);
    }

    private void Update()
    {
        if (_calledUpdate) return;
        _calledUpdate = true;
        Assert.True("after FixedUpdate call", _calledFixedUpdate);
        Assert.Equal("init.frame_count", 1, Time.frameCount);
        Assert.Equal("init.rendered_frame_count", 1, Time.renderedFrameCount);
        // Assert.False("before OnGUI call", _calledOnGUI);
    }

    // private void OnGUI()
    // {
    //     _calledOnGUI = true;
    // }

    static InitTests()
    {
        Assert.False("runtime init method: SubsystemRegistration", _subsystemRegistrationCalled);
        Assert.False("runtime init method: AfterAssembliesLoaded", _afterAssembliesLoaded);
        Assert.False("runtime init method: BeforeSplashScreen", _beforeSplashScreenCalled);
        Assert.False("runtime init method: BeforeSceneLoad", _beforeSceneLoadCalled);
        Assert.False("runtime init method, AfterSceneLoad", _afterSceneLoadCalled);
    }

    private void Awake()
    {
        Assert.True("runtime init method: SubsystemRegistration", _subsystemRegistrationCalled);
        Assert.True("runtime init method: AfterAssembliesLoaded", _afterAssembliesLoaded);
        Assert.True("runtime init method: BeforeSplashScreen", _beforeSplashScreenCalled);
        Assert.True("runtime init method: BeforeSceneLoad", _beforeSceneLoadCalled);
        Assert.False("runtime init method, AfterSceneLoad", _afterSceneLoadCalled);
        Assert.Equal("init.frame_count", 0, Time.frameCount);
        Assert.Equal("init.rendered_frame_count", 0, Time.renderedFrameCount);
        StartCoroutine(AwakeCoroutine());
    }

    private IEnumerator AwakeCoroutine()
    {
        Assert.Equal("init.frame_count", 0, Time.frameCount);
        Assert.Equal("init.rendered_frame_count", 0, Time.renderedFrameCount);
        yield return null;
        Assert.Equal("init.frame_count", 1, Time.frameCount);
        Assert.Equal("init.rendered_frame_count", 1, Time.renderedFrameCount);
    }

    private IEnumerator Start()
    {
        Assert.True("runtime init method, AfterSceneLoad", _afterSceneLoadCalled);
        Assert.Equal("init.frame_count", 1, Time.frameCount);
        Assert.Equal("init.rendered_frame_count", 1, Time.renderedFrameCount);

        yield return new WaitForEndOfFrame();
        Assert.Equal("init.frame_count", 1, Time.frameCount);
        Assert.Equal("init.rendered_frame_count", 1, Time.renderedFrameCount);
        Assert.True("after FixedUpdate call", _calledFixedUpdate);
        Assert.True("after Update call", _calledUpdate);
        // Assert.True("after OnGUI call", _calledOnGUI);
        yield return null;
        Assert.Equal("init.frame_count", 2, Time.frameCount);
        Assert.Equal("init.rendered_frame_count", 2, Time.renderedFrameCount);
        yield return null;

        Assert.Null("scene.unload.current_only_scene", SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene()));

        yield return null;
        yield return null;

        Assert.Finish();
    }

    private static bool _beforeSplashScreenCalled;
    private static bool _subsystemRegistrationCalled;
    private static bool _beforeSceneLoadCalled;
    private static bool _afterAssembliesLoaded;
    private static bool _afterSceneLoadCalled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void BeforeSplashScreen()
    {
        _beforeSplashScreenCalled = true;
        Debug.Log("BeforeSplashScreen");
        throw new Exception("foo");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void SubsystemRegistration()
    {
        _subsystemRegistrationCalled = true;
        Debug.Log("SubsystemRegistration");
        throw new Exception("foo");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BeforeSceneLoad()
    {
        _beforeSceneLoadCalled = true;
        Debug.Log("BeforeSceneLoad");
        throw new Exception("foo");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void AfterAssembliesLoaded()
    {
        _afterAssembliesLoaded = true;
        Debug.Log("AfterAssembliesLoaded");
        throw new Exception("foo");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterSceneLoad()
    {
        _afterSceneLoadCalled = true;
        Debug.Log("AfterSceneLoad");
        throw new Exception("foo");
    }
}