using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MovieTest : MonoBehaviour
{
    private int _jumpButtonDownCount;
    private int _jumpButtonUpCount;
    private int _spaceDownKeyCodeCount;
    private int _spaceUpKeyCodeCount;
    private int _spaceDownStringCount;
    private int _spaceUpStringCount;
    private static int _horizontalAxisMoveCount;

    private const int ButtonCountTest = 5;

    private readonly List<(UpdateType updateType, int frameCount, int renderedFrameCount)> _updates = new();

    private enum UpdateType
    {
        Awake,
        Start,
        FixedUpdate,
        Update
    }

    private void FixedUpdate()
    {
        _updates.Add((UpdateType.FixedUpdate, Time.frameCount, Time.renderedFrameCount));
    }

    private void Update()
    {
        _updates.Add((UpdateType.Update, Time.frameCount, Time.renderedFrameCount));
    }

    private void Awake()
    {
        _updates.Add((UpdateType.Awake, Time.frameCount, Time.renderedFrameCount));

        Assert.Equal("init.frame_count", 0, Time.frameCount);
        Assert.Equal("init.rendered_frame_count", 0, Time.renderedFrameCount);

        StartCoroutine(AwakeCoroutine());
        StartCoroutine(InputTest());
    }

    private static IEnumerator AwakeCoroutine()
    {
        // these are here since movie fps is guaranteed to be locked
        Assert.Equal("awake_coroutine.frame_count", 0, Time.frameCount);
        yield return new WaitForSeconds(0f);
        Assert.Equal("yield.wait_for_seconds.elapsed_frames", 1, Time.frameCount);
    }

    private IEnumerator Start()
    {
        _updates.Add((UpdateType.Start, Time.frameCount, Time.renderedFrameCount));

        Assert.Equal("start_coroutine.frame_count", 1, Time.frameCount);
        yield return new WaitForSeconds(0f);
        Assert.Equal("yield.wait_for_seconds.elapsed_frames", 2, Time.frameCount);

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

        startWaitForSeconds = Time.frameCount;
        yield return new WaitForSeconds(0f);
        Assert.Equal("yield.wait_for_seconds.elapsed_frames", 1, Time.frameCount - startWaitForSeconds);

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

        Assert.Equal("button_count", ButtonCountTest, _jumpButtonDownCount);
        Assert.Equal("button_count", ButtonCountTest, _jumpButtonUpCount);
        Assert.Equal("button_count", ButtonCountTest, _spaceDownKeyCodeCount);
        Assert.Equal("button_count", ButtonCountTest, _spaceUpKeyCodeCount);
        Assert.Equal("button_count", ButtonCountTest, _spaceDownStringCount);
        Assert.Equal("button_count", ButtonCountTest, _spaceUpStringCount);

        Assert.Equal("horizontal_axis_move_count", 6, _horizontalAxisMoveCount);

        yield return null;
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        yield return null;
        yield return null;

        var expectedUpdates = new[]
        {
            (UpdateType.Awake, 0, 0), // 0
            (UpdateType.Start, 1, 1),

            (UpdateType.FixedUpdate, 1, 1), // 2
            (UpdateType.Update, 1, 1),

            (UpdateType.FixedUpdate, 2, 2), // 4
            (UpdateType.Update, 2, 2),
            (UpdateType.Update, 3, 3),
            (UpdateType.FixedUpdate, 4, 4), // 7
            (UpdateType.Update, 4, 4),
            (UpdateType.Update, 5, 5)
        };

        for (var i = 0; i < expectedUpdates.Length; i++)
        {
            var expected = expectedUpdates[i];
            var actual = _updates[i];

            Assert.Equal($"update_order_{i}", expected, actual);
        }

        SceneManager.LoadScene("MovieTest2");
    }
}