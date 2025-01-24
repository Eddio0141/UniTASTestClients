using System;
using System.Collections;
using UnityEngine;

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
        // Assert.False("before OnGUI call", _calledOnGUI);
    }

    private void Update()
    {
        if (_calledUpdate) return;
        _calledUpdate = true;
        Assert.True("after FixedUpdate call", _calledFixedUpdate);
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
    }

    private IEnumerator Start()
    {
        Assert.True("runtime init method, AfterSceneLoad", _afterSceneLoadCalled);

        yield return new WaitForEndOfFrame();
        Assert.True("after FixedUpdate call", _calledFixedUpdate);
        Assert.True("after Update call", _calledUpdate);
        // Assert.True("after OnGUI call", _calledOnGUI);
        yield return null;
        yield return null;
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