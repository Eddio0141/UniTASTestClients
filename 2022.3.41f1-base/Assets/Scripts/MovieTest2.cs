using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

[SuppressMessage("ReSharper", "NotAccessedField.Local")]
public class MovieTest2 : MonoBehaviour
{
    private IEnumerator Start()
    {
        for (var i = 0; i < 50; i++)
        {
            yield return null;
        }

        Assert.Equal("ugui.click_count", 5, _clickCount);

        Assert.Finish();
    }

    private int _clickCount;

    public void Click()
    {
        _clickCount++;
        Debug.Log($"click number {_clickCount}");
    }
}