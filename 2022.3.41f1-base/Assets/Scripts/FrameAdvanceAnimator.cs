using UnityEngine;
using UnityEngine.SceneManagement;

public class FrameAdvanceAnimator : MonoBehaviour
{
    // TODO: wait for https://github.com/Eddio0141/UniTAS/issues/238 issue to be fixed with TimeEnv reporting wrong frame count
    // private static int _timeTriggerFrame = -1;
    private static float _timeTriggerTime = -1;

    // TODO: ^
    // private int _loadFrameCount;
    private float _loadFrameTime;

    private void Awake()
    {
        // TODO: ^
        // SceneManager.sceneLoaded += (_, _) => { _loadFrameCount = Time.frameCount; };
        SceneManager.sceneLoaded += (_, _) => { _loadFrameTime = Time.time; };
    }

    public void TimeTrigger()
    {
        // TODO: ^
        // if (_timeTriggerFrame != -1) return;
        // _timeTriggerFrame = Time.frameCount - _loadFrameCount;
        // Debug.Log($"reached trigger at {Time.timeSinceLevelLoad} ({_timeTriggerFrame}f)");
        
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (_timeTriggerTime != -1) return;
        _timeTriggerTime = Time.time - _loadFrameTime;
        Debug.Log($"reached trigger at {Time.time}");
    }
}