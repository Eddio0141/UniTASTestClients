using System.Diagnostics.CodeAnalysis;
using UnityEngine;

[SuppressMessage("ReSharper", "NotAccessedField.Local")]
public class UGuiTest : MonoBehaviour
{
    private static int _clickCount;
    
    public void Click()
    {
        _clickCount++;
        Debug.Log($"click number {_clickCount}");
    }
}
