using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.SceneManagement;

[SuppressMessage("ReSharper", "NotAccessedField.Local")]
public class SceneTest : MonoBehaviour
{
    private static float _asyncOpCallbackProgress;
    private static bool _asyncOpCallbackAllowSceneActivation;
    private static bool _asyncOpCallbackIsDone;
    private static int _asyncOpDoneFrame;
    
    public void StartTest()
    {
        StartCoroutine(StartTestCoroutine());
    }

    private static IEnumerator StartTestCoroutine()
    {
        var op = SceneManager.LoadSceneAsync("Scene2");
        op!.allowSceneActivation = false;
        op.completed += a =>
        {
            _asyncOpCallbackProgress = a.progress;
            _asyncOpCallbackAllowSceneActivation = a.allowSceneActivation;
            _asyncOpCallbackIsDone = a.isDone;
            _asyncOpDoneFrame = Time.frameCount;
        };

        for (var i = 0; i < 5; i++)
        {
            yield return null;
        }

        op.allowSceneActivation = true;
    }
}
