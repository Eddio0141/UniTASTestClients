using System.Collections;
using UnityEngine;

public class MovieTest2 : MonoBehaviour
{
    private IEnumerator Start()
    {
        Assert.Equal("time_since_level_load", 0f, Time.timeSinceLevelLoad, 0.0001f);
        yield return null;
        Assert.Equal("time_since_level_load", 0.01f, Time.timeSinceLevelLoad, 0.0001f);
        yield return new WaitForEndOfFrame();
        Assert.Equal("time_since_level_load", 0.01f, Time.timeSinceLevelLoad, 0.0001f);

        // keyboard
        Assert.False("Input.GetKeyDown.KeyCode", Input.GetKeyDown(KeyCode.A));
        Assert.False("Input.GetKeyDown.String", Input.GetKeyDown("A"));
        yield return null;
        Assert.True("Input.GetKeyDown.KeyCode", Input.GetKeyDown(KeyCode.A));
        Assert.True("Input.GetKeyDown.String", Input.GetKeyDown("A"));

        for (var i = 0; i <= 5; i++)
        {
            yield return null;
            yield return null;
            Assert.Equal("ugui.click_count", i, _clickCount);
        }
        
        Assert.Equal("Input.mousePosition", Vector3.zero, Input.mousePosition);
        yield return null;
        Assert.Equal("Input.mousePosition", new Vector3(123f, 456f), Input.mousePosition);

        Assert.Finish();
    }

    private int _clickCount;

    public void Click()
    {
        _clickCount++;
    }
}