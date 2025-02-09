using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MovieTest : MonoBehaviour
{
#pragma warning disable CS0414 // Field is assigned but its value is never used
    private static bool _movieTestRun;
#pragma warning restore CS0414 // Field is assigned but its value is never used

    private readonly struct UpdateInfo
    {
        private static bool Similar(float left, float right)
        {
            return Math.Abs(left - right) < 0.0001f;
        }

        private bool Equals(UpdateInfo other)
        {
            return _updateType == other._updateType && _frameCount == other._frameCount &&
                   _renderedFrameCount == other._renderedFrameCount && Similar(_time, other._time) &&
                   Similar(_timeSinceLevelLoad, other._timeSinceLevelLoad) && Similar(_fixedTime, other._fixedTime) &&
                   Similar(_unscaledTime, other._unscaledTime) &&
                   Similar(_realtimeSinceStartup, other._realtimeSinceStartup);
        }

        public override bool Equals(object obj)
        {
            return obj is UpdateInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)_updateType;
                hashCode = (hashCode * 397) ^ _frameCount;
                hashCode = (hashCode * 397) ^ _renderedFrameCount;
                hashCode = (hashCode * 397) ^ _time.GetHashCode();
                hashCode = (hashCode * 397) ^ _timeSinceLevelLoad.GetHashCode();
                hashCode = (hashCode * 397) ^ _fixedTime.GetHashCode();
                hashCode = (hashCode * 397) ^ _unscaledTime.GetHashCode();
                hashCode = (hashCode * 397) ^ _realtimeSinceStartup.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(UpdateInfo left, UpdateInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UpdateInfo left, UpdateInfo right)
        {
            return !left.Equals(right);
        }

        private readonly UpdateType _updateType;
        private readonly int _frameCount;
        private readonly int _renderedFrameCount;
        private readonly float _time;
        private readonly float _timeSinceLevelLoad;
        private readonly float _fixedTime;
        private readonly float _unscaledTime;
        private readonly float _realtimeSinceStartup;

        public UpdateInfo(UpdateType updateType, int frameCount, int renderedFrameCount, float time,
            float timeSinceLevelLoad, float fixedTime, float unscaledTime, float realtimeSinceStartup)
        {
            _updateType = updateType;
            _frameCount = frameCount;
            _renderedFrameCount = renderedFrameCount;
            _time = time;
            _timeSinceLevelLoad = timeSinceLevelLoad;
            _fixedTime = fixedTime;
            _unscaledTime = unscaledTime;
            _realtimeSinceStartup = realtimeSinceStartup;
        }

        public override string ToString()
        {
            return $"updateType: {_updateType}, frameCount: {_frameCount}, renderedFrameCount: {_renderedFrameCount}" +
                   $", time: {_time}, timeSinceLevelLoad: {_timeSinceLevelLoad}, fixedTime: {_fixedTime}" +
                   $", unscaledTime: {_unscaledTime}, realtimeSinceStartup: {_realtimeSinceStartup}";
        }
    }

    private readonly List<UpdateInfo> _updates = new();

    private enum UpdateType
    {
        Awake,
        Start,
        FixedUpdate,
        Update
    }

    private void FixedUpdate()
    {
        _updates.Add(new(UpdateType.FixedUpdate, Time.frameCount, Time.renderedFrameCount, Time.time,
            Time.timeSinceLevelLoad, Time.fixedTime, Time.unscaledTime, Time.realtimeSinceStartup));
    }

    private void Update()
    {
        _updates.Add(new(UpdateType.Update, Time.frameCount, Time.renderedFrameCount, Time.time,
            Time.timeSinceLevelLoad, Time.fixedTime, Time.unscaledTime, Time.realtimeSinceStartup));
    }

    private void Awake()
    {
        _updates.Add(new(UpdateType.Awake, Time.frameCount, Time.renderedFrameCount, Time.time,
            Time.timeSinceLevelLoad, Time.fixedTime, Time.unscaledTime, Time.realtimeSinceStartup));

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
        _updates.Add(new(UpdateType.Start, Time.frameCount, Time.renderedFrameCount, Time.time,
            Time.timeSinceLevelLoad, Time.fixedTime, Time.unscaledTime, Time.realtimeSinceStartup));

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
        yield return new WaitForSecondsRealtime(0.05f);
        Assert.Equal("yield.wait_for_seconds_realtime.elapsed_frames", 5, Time.frameCount - startWaitForSeconds);

        startWaitForSeconds = Time.frameCount;
        yield return new WaitForSecondsRealtime(1f);
        Assert.Equal("yield.wait_for_seconds_realtime.elapsed_frames", 100, Time.frameCount - startWaitForSeconds);

        startWaitForSeconds = Time.frameCount;
        yield return new WaitForSecondsRealtime(0f);
        Assert.Equal("yield.wait_for_seconds_realtime.elapsed_frames", 1, Time.frameCount - startWaitForSeconds);

        yield return new WaitForEndOfFrame();
        Time.timeScale = 1f;
        var time = Time.time;
        yield return null;
        Assert.Equal("time after timeScale to 1 from 0.5", 0.01f, Time.time - time, 0.0001f);

        yield return new WaitForEndOfFrame();
        Time.timeScale = 0.5f;
        time = Time.time;
        yield return null;
        Assert.Equal("time after timeScale to 0.5 from 1", 0.005f, Time.time - time, 0.0001f);

        Time.timeScale = 1f;
        yield return null;

        startWaitForSeconds = Time.frameCount;
        yield return new WaitForSecondsRealtime(0.05f);
        Assert.Equal("yield.wait_for_seconds_realtime.elapsed_frames", 5, Time.frameCount - startWaitForSeconds);

        startWaitForSeconds = Time.frameCount;
        yield return new WaitForSecondsRealtime(1f);
        Assert.Equal("yield.wait_for_seconds_realtime.elapsed_frames", 100, Time.frameCount - startWaitForSeconds);

        startWaitForSeconds = Time.frameCount;
        yield return new WaitForSecondsRealtime(0f);
        Assert.Equal("yield.wait_for_seconds_realtime.elapsed_frames", 1, Time.frameCount - startWaitForSeconds);

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
        while (!Input.GetKeyDown(KeyCode.Return))
            yield return null;

        _movieTestRun = true;

        for (var i = 0; i < 10; i++)
        {
            yield return null;
        }

        var expectedUpdates = new[]
        {
            new UpdateInfo(UpdateType.Awake, 0, 0, 0f, 0f, 0f, 0f, 0f), // 0
            new UpdateInfo(UpdateType.Start, 1, 1, 0.01f, 0.01f, 0f, 0.01f, 0.01f),

            new UpdateInfo(UpdateType.FixedUpdate, 1, 1, 0f, 0f, 0f, 0f, 0.01f), // 2
            new UpdateInfo(UpdateType.Update, 1, 1, 0.01f, 0.01f, 0f, 0.01f, 0.01f),

            new UpdateInfo(UpdateType.FixedUpdate, 2, 2, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f), // 4
            new UpdateInfo(UpdateType.Update, 2, 2, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f),
            new UpdateInfo(UpdateType.Update, 3, 3, 0.03f, 0.03f, 0.02f, 0.03f, 0.03f),
            new UpdateInfo(UpdateType.FixedUpdate, 4, 4, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f), // 7
            new UpdateInfo(UpdateType.Update, 4, 4, 0.04f, 0.04f, 0.04f, 0.04f, 0.04f),
            new UpdateInfo(UpdateType.Update, 5, 5, 0.05f, 0.05f, 0.04f, 0.05f, 0.05f)
        };

        for (var i = 0; i < expectedUpdates.Length; i++)
        {
            var expected = expectedUpdates[i];
            var actual = _updates[i];

            Assert.Equal($"update_order_{i}", expected, actual);
        }

        Time.timeScale = 1f;
        yield return null;
        SceneManager.LoadScene("MovieTest2");
    }
}