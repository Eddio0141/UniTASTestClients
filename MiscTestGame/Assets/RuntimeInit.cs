using UnityEngine;

internal static class RuntimeInit
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Earliest()
    {
        Debug.Log("subsystem register");
    }
}