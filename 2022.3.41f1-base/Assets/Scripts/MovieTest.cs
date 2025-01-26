using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MovieTest : MonoBehaviour
{
    private static int _jumpButtonDownCount;
    private static int _jumpButtonUpCount;

    private static int _spaceDownKeyCodeCount;
    private static int _spaceUpKeyCodeCount;
    private static int _spaceDownStringCount;
    private static int _spaceUpStringCount;

    [UsedImplicitly] private static int _horizontalAxisMoveCount;

    private const int ButtonCountTest = 5;

    private void Awake()
    {
        StartCoroutine(AwakeCoroutine());
        StartCoroutine(InputTest());
    }

    private static IEnumerator AwakeCoroutine()
    {
        // these are here since movie fps is guaranteed to be locked
        yield return new WaitForSeconds(1f);
        Assert.Equal("yield.wait_for_seconds.elapsed_frames", 101, Time.frameCount);
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);
        Assert.Equal("yield.wait_for_seconds.elapsed_frames", 102, Time.frameCount);

        yield return null;

        var startWaitForSeconds = Time.frameCount;
        yield return new WaitForSeconds(1f);
        Assert.Equal("yield.wait_for_seconds.elapsed_frames", 101, Time.frameCount - startWaitForSeconds);

        Time.timeScale = 0.5f;
        startWaitForSeconds = Time.frameCount;
        yield return new WaitForSeconds(1f);
        Assert.Equal("yield.wait_for_seconds.timeScaleHalf.elapsed_frames", 201, Time.frameCount - startWaitForSeconds);

        startWaitForSeconds = Time.frameCount;
        yield return new WaitForSecondsRealtime(1f);
        Assert.Equal("yield.wait_for_seconds_realtime.elapsed_frames", 101, Time.frameCount - startWaitForSeconds);

        Time.timeScale = 1f;
        startWaitForSeconds = Time.frameCount;
        yield return new WaitForSecondsRealtime(1f);
        Assert.Equal("yield.wait_for_seconds_realtime.elapsed_frames", 101, Time.frameCount - startWaitForSeconds);

        startWaitForSeconds = Time.frameCount;
        yield return new WaitForSeconds(0.005f);
        Assert.Equal("yield.wait_for_seconds.elapsed_frames", 1, Time.frameCount - startWaitForSeconds);

        startWaitForSeconds = Time.frameCount;
        yield return new WaitForSeconds(0.105f);
        Assert.Equal("yield.wait_for_seconds.elapsed_frames", 11, Time.frameCount - startWaitForSeconds);

        yield return null;
        Assert.Finish(); // init tests end
    }

    private IEnumerator InputTest()
    {
        while (_jumpButtonDownCount + _jumpButtonUpCount + _spaceDownKeyCodeCount + _spaceUpKeyCodeCount +
               _spaceDownStringCount + _spaceUpStringCount < ButtonCountTest * 6)
        {
            if (Input.GetAxis("Horizontal") != 0)
                _horizontalAxisMoveCount++;

            if (Input.GetButtonDown("Jump"))
                _jumpButtonDownCount++;

            if (Input.GetButtonUp("Jump"))
                _jumpButtonUpCount++;

            if (Input.GetKeyDown(KeyCode.Space))
                _spaceDownKeyCodeCount++;

            if (Input.GetKeyUp(KeyCode.Space))
                _spaceUpKeyCodeCount++;

            if (Input.GetKeyDown("space"))
                _spaceDownStringCount++;

            if (Input.GetKeyUp("space"))
                _spaceUpStringCount++;

            yield return null;
        }

        Debug.Log("space press check done");

        SceneManager.LoadScene("MovieTest2");
    }
}