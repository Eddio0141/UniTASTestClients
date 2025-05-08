using UnityEngine;

/// <summary>
/// Note that this should not contain test processing, that must be done in TestFrameworkRuntime
/// </summary>
public class EventHooks : MonoBehaviour
{
    private void Awake()
    {
        StartCoroutine(TestFrameworkRuntime.AwakeTestHook());
    }
}