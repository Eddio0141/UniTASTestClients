using System;
using UnityEngine;

internal static class RuntimeInitTest
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void BeforeSplashScreen()
    {
        Debug.Log("BeforeSplashScreen");
        throw new Exception("foo");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void SubsystemRegistration()
    {
        Debug.Log("SubsystemRegistration");
        throw new Exception("foo");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BeforeSceneLoad()
    {
        Debug.Log("BeforeSceneLoad");
        throw new Exception("foo");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void AfterAssembliesLoaded()
    {
        Debug.Log("AfterAssembliesLoaded");
        throw new Exception("foo");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AfterSceneLoad()
    {
        Debug.Log("AfterSceneLoad");
        throw new Exception("foo");
    }
}