using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UnityEngine;

[SuppressMessage("ReSharper", "NotAccessedField.Global")]
public static class Results
{
    // just after LoadSceneAsync call
    public static int SceneCountAsyncLoad;
    public static int LoadedSceneCountAsyncLoad;
    // just after allowSceneActivation true
    public static int SceneCountAsyncLoadAllowLoad;
    public static int LoadedSceneCountAsyncLoadAllowLoad;
    // 1f after allowSceneActivation true
    public static int SceneCountAsyncLoadAllowLoadNextFrame;
    public static int LoadedSceneCountAsyncLoadAllowLoadNextFrame;
    // on event callback
    public static int SceneCountAsyncLoadCallback;
    public static int LoadedSceneCountAsyncLoadCallback;

    // same thing as LoadSceneAsync checks but for unload, allowSceneActivation is not a thing neither
    public static int SceneCountAsyncUnload;
    public static int LoadedSceneCountAsyncUnload;
    public static int SceneCountAsyncUnloadAllowLoadNextFrame;
    public static int LoadedSceneCountAsyncUnloadAllowLoadNextFrame;
    public static int SceneCountAsyncUnloadCallback;
    public static int LoadedSceneCountAsyncUnloadCallback;

    public static bool GeneralTestsDone;

    public static void LogResults()
    {
        var fields = typeof(Results).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        foreach (var field in fields)
        {
            Debug.Log($"Result: {field.Name} = {field.GetValue(null)}");
        }
    }
}
