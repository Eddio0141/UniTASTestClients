using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UnityEngine;

[SuppressMessage("ReSharper", "NotAccessedField.Global")]
public static class Results
{
    // just after LoadSceneAsync call
    public static int AsyncLoadSceneCount;
    public static int AsyncLoadLoadedSceneCount;
    // just after allowSceneActivation true
    public static int AsyncLoadAllowLoadSceneCount;
    public static int AsyncLoadAllowLoadLoadedSceneCount;
    // 1f after allowSceneActivation true
    public static int AsyncLoadAllowLoadNextFrameSceneCount;
    public static int AsyncLoadAllowLoadNextFrameLoadedSceneCount;
    // on event callback
    public static int AsyncLoadCallbackSceneCount;
    public static int AsyncLoadCallbackLoadedSceneCount;
    public static int AsyncLoadCallbackFrame;

    // same thing as LoadSceneAsync checks but for unload, allowSceneActivation is not a thing neither
    public static int AsyncUnloadSceneCount;
    public static int AsyncUnloadLoadedSceneCount;
    public static int AsyncUnloadAllowLoadNextFrameSceneCount;
    public static int AsyncUnloadAllowLoadNextFrameLoadedSceneCount;
    public static int AsyncUnloadCallbackSceneCount;
    public static int AsyncUnloadCallbackLoadedSceneCount;
    public static int AsyncUnloadCallbackFrame;

    public static int AsyncLoadCallback2Frame;
    public static int AsyncLoadCallback3Frame;
    public static int AsyncLoadCallback4Frame;

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
