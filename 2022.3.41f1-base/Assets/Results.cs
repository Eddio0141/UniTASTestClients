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
    public static float AsyncUnloadLoadedProgress;
    public static bool AsyncUnloadLoadedIsDone;
    public static float AsyncUnloadLoadedProgress2;
    public static bool AsyncUnloadLoadedIsDone2;

    public static int AsyncLoadCallback2Frame;
    public static int AsyncLoadCallback3Frame;
    public static int AsyncLoadCallback4Frame;

    public static int AsyncLoadSyncLoadCallbackFrame;
    public static int AsyncLoadSyncLoadCallback2Frame;

    // for testing UniTAS swapping from sync load mode, back to async mode, which re-enables 1 frame delay
    public static int AsyncLoadCallback5Frame;

    public static int AfterLoadsSceneCount;
    public static int AfterLoadsLoadedSceneCount;

    public static bool DoubleUnloadOperationIsNull;
    public static int AfterDoubleUnloadSceneCount;
    public static int AfterDoubleUnloadLoadedSceneCount;

    public static bool DoubleUnloadDiffNameSuccess;
    public static bool DoubleUnloadDiffNameBefore2;
    public static bool DoubleUnloadDiffNameSuccess2;

    public static bool DoubleUnloadNameIdSecondIsNull;

    public static bool GeneralTestsDone;

    public static string SceneNameInitial;

    public static string SceneAddedName;
    public static bool SceneAddedIsLoaded;
    public static int SceneAddedRootCount;
    public static bool SceneAddedIsSubScene;
    public static string SceneAddedPath;
    public static int SceneAddedBuildIndex;
    public static bool SceneAddedIsDirty;
    public static bool SceneAddedIsValid;
    public static string SceneAddedNameChangeInvalidOp;
    public static float SceneAddedProgress;
    public static bool SceneAddedIsDone;
    public static bool SceneAddedIsDone2;
    public static bool SceneAddedIsDone3;
    public static float SceneAddedProgress2;

    // TODO: when loading is proper, handle and hashcode must be checked

    public static bool SceneAddedRealEqDummy;
    public static bool SceneAddedRealEqualsDummy;
    public static bool SceneAddedRealNeqDummy;
    public static string SceneAddedRealName;
    public static bool SceneAddedRealIsLoaded;
    public static int SceneAddedRealRootCount;
    public static bool SceneAddedRealIsSubScene;
    public static string SceneAddedRealPath;
    public static int SceneAddedRealBuildIndex;
    public static bool SceneAddedRealIsDirty;
    public static bool SceneAddedRealIsValid;
    public static bool SceneAddedRealHandleEq0;
    public static bool SceneAddedRealHashCodeEq0;

    public static string SceneNonExistentUnloadEx;
    public static string SceneNonExistentAsyncLoadMsg;
    public static string SceneNonExistentAsyncLoadMsgType;
    public static bool SceneNonExistentAsyncLoadOpIsNull;
    public static int SceneNonExistentAsyncLoadSceneCountDiff;
    public static string SceneNonExistentSyncLoadMsg;
    public static string SceneNonExistentSyncLoadMsgType;
    public static int SceneNonExistentSyncLoadSceneCountDiff;
    public static string SceneNeverLoadedUnloadEx;

    public static int SceneAdditiveSingleSceneCount;
    public static int SceneAdditiveSingleLoadedSceneCount;
    public static int SceneAdditiveSingleSceneCount2;
    public static int SceneAdditiveSingleLoadedSceneCount2;
    public static float SceneAdditiveSingleLoadProgress;
    public static float SceneAdditiveSingleLoadProgress2;

    public static int SceneAdditiveSingleSceneCount3;
    public static int SceneAdditiveSingleLoadedSceneCount3;
    public static int SceneAdditiveSingleSceneCount4;
    public static int SceneAdditiveSingleLoadedSceneCount4;
    public static float SceneAdditiveSingleLoadProgress3;
    public static float SceneAdditiveSingleLoadProgress4;

    public static int SceneAdditiveSingleSceneCount5;
    public static int SceneAdditiveSingleLoadedSceneCount5;
    public static int SceneAdditiveSingleSceneCount6;
    public static int SceneAdditiveSingleLoadedSceneCount6;
    public static int SceneAdditiveSingleSceneCount7;
    public static int SceneAdditiveSingleLoadedSceneCount7;
    public static int SceneAdditiveSingleSceneCount8;
    public static int SceneAdditiveSingleLoadedSceneCount8;
    public static int SceneAdditiveSingleSceneCount9;
    public static int SceneAdditiveSingleLoadedSceneCount9;
    public static float SceneAdditiveSingleLoadProgress5;
    public static float SceneAdditiveSingleLoadProgress6;

    public static void LogResults()
    {
        var fields = typeof(Results).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        foreach (var field in fields)
        {
            Debug.Log($"Result: {field.Name} = {field.GetValue(null)}");
        }
    }
}