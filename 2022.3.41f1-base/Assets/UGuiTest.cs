using System.Diagnostics.CodeAnalysis;
using UnityEngine;

[SuppressMessage("ReSharper", "NotAccessedField.Local")]
public class UGui : MonoBehaviour
{
    private static int _clickCount;
    
    public void Click()
    {
        _clickCount++;
    }
}
