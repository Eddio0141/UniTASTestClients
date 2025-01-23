using System.Collections;
using UnityEngine;

public class InitTests : MonoBehaviour
{
    private bool _calledFixedUpdate;
    private bool _calledUpdate;
    // private bool _calledOnGUI;
    
    private void FixedUpdate()
    {
        if (_calledFixedUpdate) return;
        _calledFixedUpdate = true;
        Assert.False("before Update call", _calledUpdate);
        // Assert.False("before OnGUI call", _calledOnGUI);
    }

    private void Update()
    {
        if (_calledUpdate) return;
        _calledUpdate = true;
        Assert.True("after FixedUpdate call", _calledFixedUpdate);
        // Assert.False("before OnGUI call", _calledOnGUI);
    }
    
    // private void OnGUI()
    // {
    //     _calledOnGUI = true;
    // }

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        Assert.True("after FixedUpdate call", _calledFixedUpdate);
        Assert.True("after Update call", _calledUpdate);
        // Assert.True("after OnGUI call", _calledOnGUI);
        yield return null;
        yield return null;
        yield return null;
        yield return null;

        Assert.Finish();
    }
}