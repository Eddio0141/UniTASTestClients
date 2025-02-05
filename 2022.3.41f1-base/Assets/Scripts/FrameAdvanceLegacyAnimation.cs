using UnityEngine;

public class FrameAdvanceLegacyAnimation : MonoBehaviour
{
    private static int _timeTrigger = -1;
    private static int _timeTriggerBlend = -1;

    private int _loadFrameCount;

    [SerializeField] private Animation anim;

    private void OnEnable()
    {
        _loadFrameCount = Time.frameCount;

        foreach (AnimationState state in anim)
        {
            state.wrapMode = WrapMode.Loop;
            if (state.name == "LegacyAnimationBlend")
            {
                state.speed = 0.9f;
            }
        }

        anim.Play("LegacyAnimationMove");
        anim.Blend("LegacyAnimationBlend");
    }

    public void TimeTrigger()
    {
        if (_timeTrigger != -1) return;
        _timeTrigger = Time.frameCount - _loadFrameCount;
        Debug.Log($"reached trigger at {_timeTrigger}");
    }

    public void TimeTriggerBlend()
    {
        if (_timeTriggerBlend != -1) return;
        _timeTriggerBlend = Time.frameCount - _loadFrameCount;
        Debug.Log($"reached blend trigger at {_timeTriggerBlend}");
    }
}