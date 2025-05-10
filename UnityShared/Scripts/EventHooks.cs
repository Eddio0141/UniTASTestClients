using UnityEngine;

/// <summary>
/// Note that this class should not contain test processing
/// </summary>
public class EventHooks : MonoBehaviour
{
    private void Awake()
    {
        StartCoroutine(TestFrameworkRuntime.AwakeTestHook());
    }
}
