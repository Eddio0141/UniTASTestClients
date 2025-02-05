using UnityEngine;

public class FrameAdvanceAnimator : MonoBehaviour
{
    private static int _timeTrigger = -1;
    private int _loadFrameCount;

    private void OnEnable()
    {
        _loadFrameCount = Time.frameCount;
    }

    public void TimeTrigger()
    {
        if (_timeTrigger != -1) return;
        _timeTrigger = Time.frameCount - _loadFrameCount;
        Debug.Log($"reached trigger at {_timeTrigger}");
    }
}