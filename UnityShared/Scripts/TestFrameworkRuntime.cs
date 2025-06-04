using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[SuppressMessage("ReSharper", "UseStringInterpolation")]
[SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeEvident")]
[DefaultExecutionOrder(0)]
public class TestFrameworkRuntime : MonoBehaviour
{
    private const string AssetPath = "Assets/TestFramework";
    public const string SceneAssetPath = AssetPath + "/Scenes";
    public const string PrefabAssetPath = AssetPath + "/Prefabs";
    public const string TestingScenePath = AssetPath + "/Scenes/general.unity";

    private static TestFrameworkRuntime _instance;
    private readonly List<Result> _generalTestResults = new List<Result>();
    private readonly List<Result> _initTestResults = new List<Result>();
    private readonly List<Result> _movieTestResults = new List<Result>();
    private Test[] _generalTests;
    private Test[] _eventTests;
    private (MovieTestAttribute, Test[])[] _movieTests;
    private List<Test> _initTestsAwake;

    private bool _initTestsAwakeRan;

    /// <summary>
    /// Movie test to run by name, setting this flag will make certain events check / start running movie tests
    /// </summary>
    private static string _movieTestToRun;

    private bool _movieTestStarted;

    private void Awake()
    {
        if (_instance != null)
        {
            DestroyImmediate(gameObject);
            return;
        }

        DontDestroyOnLoad(this);
        _instance = this;
    }

    private bool _discoveredTests;

    private void DiscoverTestsIfNot()
    {
        if (_discoveredTests) return;
        _discoveredTests = true;
        var generalTests = new List<Test>();
        var eventTests = new List<Test>();
        var movieTests = new List<(MovieTestAttribute, Test[])>();
        var initTestsAwake = new List<Test>();

        foreach (var monoBeh in GetComponents<MonoBehaviour>())
        {
            var monoBehType = monoBeh.GetType();
            var movieTestAttr = monoBehType.GetCustomAttribute<MovieTestAttribute>();
            var methods = GetTestFuncs(monoBehType);
            var testsIter = methods.Select(m =>
            {
                var attr = m.GetCustomAttribute<TestAttribute>();
                return new Test($"{monoBehType.FullName}.{m.Name}", monoBehType.FullName, m, monoBeh, attr.EventTiming,
                    attr.InitTestTiming);
            }).ToArray();
            if (movieTestAttr != null)
            {
                foreach (var test in testsIter)
                {
                    if (test.EventTiming.HasValue)
                    {
                        // TODO: why warn here? the tests discovery isn't used in setup
                        Debug.LogWarning(
                            $"Test {test.Name} is a movie test and the event timing argument is ineffective");
                    }
                }

                movieTests.Add((movieTestAttr, testsIter));
                continue;
            }

            generalTests.AddRange(testsIter.Where(t => !t.EventTiming.HasValue && !t.InitTiming.HasValue));
            initTestsAwake.AddRange(testsIter.Where(t => t.InitTiming.HasValue));
            eventTests.AddRange(testsIter.Where(t => t.EventTiming.HasValue));
        }

        _generalTests = generalTests.ToArray();
        _eventTests = eventTests.ToArray();
        _movieTests = movieTests.ToArray();
        _initTestsAwake = initTestsAwake;
        Debug.Log($"Discovered {_generalTests.Length} general tests" +
                  $", {_eventTests.Length} event tests" +
                  $", {_movieTests.Length} movie tests" +
                  $", {_initTestsAwake.Count} init tests (Awake)");
    }

    public static IEnumerable<MethodInfo> GetTestFuncs(Type type)
    {
        return type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                               BindingFlags.NonPublic).Where(m => m.GetCustomAttributes<TestAttribute>().Any());
    }

    public static void RunGeneral()
    {
        if (!InstanceSetCheckAndLog()) return;
        _instance.DiscoverTestsIfNot();
        _instance.StartCoroutine(_instance.RunGeneralInternal());
    }

    private static bool InstanceSetCheckAndLog()
    {
        if (_instance != null) return true;

        Debug.LogError("wait for the test runner instance to be instantiated");
        return false;
    }

    private IEnumerator RunGeneralInternal()
    {
        foreach (var test in _generalTests)
        {
            yield return RunTest(test, _generalTestResults);
            yield return TestSafetyDelay();
        }

        foreach (var test in _eventTests)
        {
            _pendingEventTests.Enqueue(test);
        }

        EventTestStart();
    }

    private void GeneralTestsFinish()
    {
        Debug.Log("General tests finished");
        foreach (var result in _generalTestResults)
        {
            Debug.Log(result);
        }
    }

    private static IEnumerator RunTest(Test test, List<Result> results)
    {
        Debug.Log($"Running test {test.Name}");
        var executeIter = test.Execute();
        while (executeIter.MoveNext())
        {
            if (executeIter.Current is Result result)
            {
                results.Add(result);
                break;
            }

            yield return executeIter.Current;
        }
    }

    private static IEnumerator TestSafetyDelay()
    {
        for (var i = 0; i < 5; i++)
        {
            yield return null;
        }
    }

    private void EventTestStart()
    {
        if (_pendingEventTests.Count == 0)
        {
            GeneralTestsFinish();
            return;
        }

        // trigger Awake / Start event
        _currentEventTest = _pendingEventTests.Dequeue();
        Helper.Scene.LoadScene(TestingScenePath);
    }

    private readonly Queue<Test> _pendingEventTests = new Queue<Test>();
    private Test? _currentEventTest;

    /// <summary>
    /// Runs tests as init tests, meaning the tests run in parallel
    /// </summary>
    private IEnumerator RunInitTest(Test test)
    {
        yield return RunTest(test, _initTestResults);
        _initTestsAwake.Remove(test);

        InitTestsFinishCheckAndLog();
    }

    public static IEnumerator AwakeTestHook()
    {
        if (!InstanceSetCheckAndLog()) yield break;

        yield return _instance.MovieTestCheckAndRun(MovieTestTiming.Awake);
        yield return _instance.InitTestAwakeCheckAndRun();
        yield return _instance.EventHookInternal(EventTiming.Awake);
    }

    private IEnumerator MovieTestCheckAndRun(MovieTestTiming movieTestTiming)
    {
        if (_movieTestToRun != null && _movieTestStarted) yield break;
        DiscoverTestsIfNot();
        var testPairIdx = Array.FindIndex(_movieTests, t => t.Item1.Timing == movieTestTiming);
        if (testPairIdx < 0) yield break;
        _movieTestStarted = true;
        var testPair = _movieTests[testPairIdx];
        var tests = testPair.Item2.Where(t => t.TypeName == _movieTestToRun).ToArray();

        Debug.Log($"Running {tests.Length} movie tests");
        foreach (var test in testPair.Item2)
        {
            yield return RunTest(test, _movieTestResults);
        }
    }

    private IEnumerator InitTestAwakeCheckAndRun()
    {
        if (_initTestsAwakeRan) yield break;
        _initTestsAwakeRan = true;
        DiscoverTestsIfNot();
        foreach (var test in _initTestsAwake)
        {
            StartCoroutine(RunInitTest(test));
        }
    }

    private bool _initTestsFinishLogged;

    private void InitTestsFinishCheckAndLog()
    {
        if (_initTestsAwake.Count > 0 || _initTestsFinishLogged) return;
        _initTestsFinishLogged = true;

        Debug.Log("Init tests finished");
        foreach (var result in _initTestResults)
        {
            Debug.Log(result);
        }
    }

    private IEnumerator EventHookInternal(EventTiming timing)
    {
        if (!_currentEventTest.HasValue || _currentEventTest.Value.EventTiming != timing)
            yield break;
        yield return RunTest(_currentEventTest.Value, _generalTestResults);
        yield return TestSafetyDelay();
        EventTestStart();
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    [SuppressMessage("ReSharper", "StructCanBeMadeReadOnly")]
    private struct Result
    {
        public Result(string name, string message, bool success)
        {
            Name = name;
            Message = message;
            Success = success;
        }

        public readonly string Name;
        public readonly string Message;
        public readonly bool Success;

        public override string ToString()
        {
            return Success ? string.Format("success: {0}", Name) : string.Format("failure: {0}: {1}", Name, Message);
        }
    }

    private readonly struct Test : IEquatable<Test>
    {
        public readonly string Name;
        public readonly string TypeName;
        private readonly MethodInfo _method;
        private readonly bool _testDoesIter;
        private readonly MonoBehaviour _objInstance;
        public readonly EventTiming? EventTiming;
        public readonly InitTestTiming? InitTiming;

        public Test(string name, string typeName, MethodInfo method, MonoBehaviour objInstance,
            EventTiming? eventTiming,
            InitTestTiming? initTiming)
        {
            Name = name;
            TypeName = typeName;
            _method = method;
            _objInstance = objInstance;
            EventTiming = eventTiming;
            InitTiming = initTiming;
            _testDoesIter = method.ReturnType == typeof(IEnumerator<TestYield>);

            if (EventTiming != null && InitTiming != null)
            {
                throw new InvalidOperationException(
                    $"Test {name} has event timing and init timing specified, choose one, " +
                    "event timing are tests that can be ran at any point in the lifetime of unity games, " +
                    "init tests are ran automatically on the specified timing");
            }
        }

        private static string GetExceptionMsg(Exception ex)
        {
            if (ex.InnerException is AssertionException assertionException)
            {
                return assertionException.Message;
            }

            return ex.ToString();
        }

        /// <summary>
        /// Executes test, check return value for result
        /// </summary>
        /// <returns>Either unity coroutine yields or test result which indicates the test has finished</returns>
        public IEnumerator Execute()
        {
            string msg = null;
            var success = true;
            object testRet = null;
            try
            {
                testRet = _method.Invoke(_objInstance, Array.Empty<object>());
            }
            catch (Exception e)
            {
                success = false;
                msg = GetExceptionMsg(e);
            }

            if (!_testDoesIter || !success)
            {
                yield return new Result(Name, msg, success);
                yield break;
            }

            var iter = (IEnumerator<TestYield>)testRet;
            while (true)
            {
                bool moveNextResult;
                try
                {
                    moveNextResult = iter.MoveNext();
                }
                catch (Exception e)
                {
                    success = false;
                    msg = GetExceptionMsg(e);
                    break;
                }

                if (!moveNextResult) break;

                if (iter.Current == null)
                {
                    success = false;
                    msg = "Error: test yield returned null, which isn't expected";
                    break;
                }

                yield return iter.Current.Operation();
            }

            yield return new Result(Name, msg, success);
        }

        public bool Equals(Test other)
        {
            return Equals(_method, other._method);
        }

        public override bool Equals(object obj)
        {
            return obj is Test other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (_method != null ? _method.GetHashCode() : 0);
        }
    }
}

[SuppressMessage("ReSharper", "UseStringInterpolation")]
public static class Assert
{
    public static void Log(string name, LogType expectedType, string expectedLog, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        _logHookStore = new LogHookStore(name, expectedType, expectedLog, message, file, line);
        Application.logMessageReceived += LogHook;
    }

    private static LogHookStore _logHookStore;

    private static void LogHook(string condition, string _, LogType type)
    {
        Application.logMessageReceived -= LogHook;

        var name = _logHookStore.Name;
        var file = _logHookStore.File;
        var line = _logHookStore.Line;
        Result result;
        if (_logHookStore.ExpectedType == type && _logHookStore.ExpectedLog == condition)
        {
            result = new Result(name, null, true);
        }
        else
        {
            var fullMsg = new StringBuilder();
            fullMsg.AppendLine("assertion failed `expected_log` == `actual_log` && `expected_msg` == `actual_msg`{0}");
            if (_logHookStore.ExpectedType != type)
            {
                fullMsg.AppendLine(string.Format(" expected_log: {0}", _logHookStore.ExpectedType));
                fullMsg.AppendLine(string.Format("   actual_log: {0}", type));
            }

            if (_logHookStore.ExpectedLog != condition)
            {
                fullMsg.AppendLine(string.Format(" expected_msg: {0}", ShowHiddenChars(_logHookStore.ExpectedLog)));
                fullMsg.AppendLine(string.Format("   actual_msg: {0}", ShowHiddenChars(condition)));
            }

            var fullMsgStr = AssertMsg(name, fullMsg.ToString(), _logHookStore.Message, file, line);
            result = new Result(name, fullMsgStr, false);
        }

        LogAssert(name, file, line, result);
        TestResults.Add(result);
    }

    private static string ShowHiddenChars(string str)
    {
        if (str == null) return null;
        return str.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    public static void Null<T>(string name, T actual, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
        where T : class
    {
        Result result;
        if (actual == null)
            result = new Result(name, null, true);
        else
        {
            var fullMsg = AssertMsg(name, string.Format("assertion failed `actual` == null{{0}}\n actual: {0}", actual),
                message, file,
                line);
            result = new Result(name, fullMsg, false);
        }

        LogAssert(name, file, line, result);
        TestResults.Add(result);
    }

    public static void Null<T>(T actual, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
        where T : class
    {
        if (actual == null) return;
        throw new AssertionException(string.Format("assertion failed `actual` == null{{0}}\n actual: {0}", actual),
            message, file, line);
    }

    public static void NotNull<T>(T actual, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
        where T : class
    {
        if (actual != null) return;
        throw new AssertionException("assertion failed `actual` != null{0}", message, file, line);
    }

    public static void NotNull<T>(string name, T actual, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
        where T : class
    {
        Result result;
        if (actual == null)
        {
            var fullMsg = AssertMsg(name, "assertion failed `actual` != null{0}", message, file, line);
            result = new Result(name, fullMsg, false);
        }
        else
            result = new Result(name, null, true);

        LogAssert(name, file, line, result);
        TestResults.Add(result);
    }

    public static void True(bool success, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        if (success) return;
        throw new AssertionException("assertion failed{0}", message, file, line);
    }

    public static void True(string name, bool success, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        Result result;
        if (success)
            result = new Result(name, null, true);
        else
        {
            var fullMsg = AssertMsg(name, "assertion failed{0}", message, file, line);
            result = new Result(name, fullMsg, false);
        }

        LogAssert(name, file, line, result);
        TestResults.Add(result);
    }

    public static void False(bool success, string message = null, [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        if (!success) return;
        throw new AssertionException("assertion failed{0}", message, file, line);
    }

    public static void False(string name, bool success, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        Result result;
        if (success)
        {
            var fullMsg = AssertMsg(name, "assertion failed{0}", message, file, line);
            result = new Result(name, fullMsg, false);
        }
        else
            result = new Result(name, null, true);

        LogAssert(name, file, line, result);
        TestResults.Add(result);
    }

    public static void NotThrows(string name, Action action, string message = null,
        [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
    {
        Result result;
        try
        {
            action();
            result = new Result(name, null, true);
        }
        catch (Exception e)
        {
            var fullMsg = AssertMsg(name,
                string.Format("assertion failed `expected` no throw{{0}}\n actual: {0}: {1}", e.GetType().FullName,
                    e.Message), message,
                file, line);
            result = new Result(name, fullMsg, false);
        }

        LogAssert(name, file, line, result);
        TestResults.Add(result);
    }

    public static void Throws<T>(string name, T expected, Action action, string message = null,
        [CallerFilePath] string file = null, [CallerLineNumber] int line = 0)
        where
        T : Exception
    {
        Result result;
        try
        {
            action();
            var fullMsg = AssertMsg(name,
                string.Format("assertion failed `expected` throws{{0}}\n expected: {0}: {1}",
                    expected.GetType().FullName, expected.Message),
                message, file, line);
            result = new Result(name, fullMsg, false);
        }
        catch (Exception e)
        {
            if (e.GetType() == expected.GetType() && e.Message == expected.Message)
                result = new Result(name, null, true);
            else
            {
                var fullMsg = AssertMsg(name,
                    string.Format("assertion failed `expected` throws{{0}}\n expected: {0}: {1}\n   actual: {2}: {3}",
                        expected.GetType().FullName, expected.Message, e.GetType().FullName, e.Message),
                    message, file, line);
                result = new Result(name, fullMsg, false);
            }
        }

        LogAssert(name, file, line, result);
        TestResults.Add(result);
    }

    public static void NotEqual<T>(string name, T expected, T actual, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        NotEqualBase(name, expected, actual, Equals(expected, actual), file, line, message);
    }

    public static void Equal<T>(T expected, T actual, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        EqualBase(expected, actual, Equals(expected, actual), file, line, message);
    }

    public static void Equal<T>(string name, T expected, T actual, string message = null,
        [CallerFilePath] string file = null,
        [CallerLineNumber] int line = 0)
    {
        EqualBase(name, expected, actual, Equals(expected, actual), file, line, message);
    }

    public static void Equal(string name, double expected, double actual, double tolerance, string message = null,
        [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        EqualBase(name, expected, actual, Math.Abs(expected - actual) < tolerance, file, line, message);
    }

    private static void NotEqualBase<T>(string name, T expected, T actual, bool success, string file, int line,
        string message = null)
    {
        Result result;
        if (success)
        {
            var fullMsg = AssertMsg(name,
                string.Format("assertion failed `expected` != `actual`{{0}}\n expected: {0}\n   actual: {1}", expected,
                    actual), message,
                file,
                line);
            result = new Result(name, fullMsg, false);
        }
        else
        {
            result = new Result(name, null, true);
        }

        LogAssert(name, file, line, result);
        TestResults.Add(result);
    }

    private static void EqualBase<T>(T expected, T actual, bool success, string file, int line,
        string message = null)
    {
        if (success) return;
        var assertMsg = new StringBuilder();
        assertMsg.AppendLine("assertion failed `expected` == `actual`{0}");
        if (typeof(T) == typeof(string) && expected != null && actual != null)
        {
            var sExpected = (string)(object)expected;
            var sActual = (string)(object)actual;
            sExpected = ShowHiddenChars(sExpected);
            sActual = ShowHiddenChars(sActual);
            assertMsg.AppendLine(string.Format(" expected: {0}", sExpected));
            assertMsg.AppendLine(string.Format("   actual: {0}", sActual));
        }
        else
        {
            assertMsg.AppendLine(string.Format(" expected: {0}", expected));
            assertMsg.AppendLine(string.Format("   actual: {0}", actual));
        }

        throw new AssertionException(assertMsg.ToString(), message, file, line);
    }

    private static void EqualBase<T>(string name, T expected, T actual, bool success, string file, int line,
        string message = null)
    {
        Result result;
        if (success)
        {
            result = new Result(name, null, true);
        }
        else
        {
            var assertMsg = new StringBuilder();
            assertMsg.AppendLine("assertion failed `expected` == `actual`{0}");
            if (typeof(T) == typeof(string) && expected != null && actual != null)
            {
                var sExpected = (string)(object)expected;
                var sActual = (string)(object)actual;
                sExpected = ShowHiddenChars(sExpected);
                sActual = ShowHiddenChars(sActual);
                assertMsg.AppendLine(string.Format(" expected: {0}", sExpected));
                assertMsg.AppendLine(string.Format("   actual: {0}", sActual));
            }
            else
            {
                assertMsg.AppendLine(string.Format(" expected: {0}", expected));
                assertMsg.AppendLine(string.Format("   actual: {0}", actual));
            }

            var fullMsg = AssertMsg(name, assertMsg.ToString(), message, file, line);
            result = new Result(name, fullMsg, false);
        }

        LogAssert(name, file, line, result);
        TestResults.Add(result);
    }

    private static string AssertMsg(string name, string assertMsg, string userMsg, string file, int line)
    {
        userMsg = userMsg == null ? string.Empty : string.Format(": {0}", userMsg);
        return string.Format("test {0} failed at {1}:{2}:\n", name, file, line) + string.Format(assertMsg, userMsg);
    }

    private static void LogAssert(string name, string file, int line, Result result)
    {
        Debug.Log(string.Format("Assertion `{0}` at {1}:{2}, {3}", name, file, line,
            result.Success ? "success" : "failure"));
    }

    private static readonly List<Result> TestResults = new List<Result>();
#pragma warning disable CS1691 CS1692 CS0414 CS1696 // Field is assigned but its value is never used
    private static bool _testsDone;
#pragma warning restore CS1691 CS1692 CS0414 CS1696 // Field is assigned but its value is never used

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private static void Reset()
    {
        _testsDone = false;
        TestResults.Clear();
    }

    public static void Finish()
    {
        _testsDone = true;
        Debug.Log("tests finished");
        foreach (var result in TestResults)
        {
            Debug.Log(result);
        }
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
    [SuppressMessage("ReSharper", "StructCanBeMadeReadOnly")]
    private struct Result
    {
        public Result(string name, string message, bool success)
        {
            Name = name;
            Message = message;
            Success = success;
        }

        public readonly string Name;
        public readonly string Message;
        public readonly bool Success;

        public override string ToString()
        {
            return Success ? string.Format("success: {0}", Name) : string.Format("failure: {0}: {1}", Name, Message);
        }
    }

    [SuppressMessage("ReSharper", "StructCanBeMadeReadOnly")]
    private struct LogHookStore
    {
        public readonly string Name;
        public readonly LogType ExpectedType;
        public readonly string ExpectedLog;
        public readonly string Message;
        public readonly string File;
        public readonly int Line;

        public LogHookStore(string name, LogType expectedType, string expectedLog, string message, string file,
            int line)
        {
            Name = name;
            ExpectedType = expectedType;
            ExpectedLog = expectedLog;
            Message = message;
            File = file;
            Line = line;
        }
    }
}

[SuppressMessage("ReSharper", "UseStringInterpolation")]
public class AssertionException : Exception
{
    public AssertionException(string assertMsg, string userMsg, string file, int line) : base(AssertMsg(assertMsg,
        userMsg, file, line))
    {
    }

    private static string AssertMsg(string assertMsg, string userMsg, string file, int line)
    {
        userMsg = userMsg == null ? string.Empty : string.Format(": {0}", userMsg);
        return string.Format("Assertion failed at {0}:{1}:\n", file, line) + string.Format(assertMsg, userMsg);
    }
}

// test yield
public abstract class TestYield
{
    public abstract IEnumerator Operation();
}

public class UnityYield : TestYield
{
    private readonly object _yield;

    public UnityYield(object yield)
    {
        _yield = yield;
    }

    public override IEnumerator Operation()
    {
        yield return _yield;
    }
}

public class SceneSwitchYield : TestYield
{
    private readonly string _scenePath;

    public SceneSwitchYield(string scenePath)
    {
        _scenePath = scenePath;
    }

    public override IEnumerator Operation()
    {
        Helper.Scene.LoadScene(_scenePath);
        yield break;
    }
}

// test attributes
[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
public class TestAttribute : Attribute
{
    public readonly EventTiming? EventTiming;
    public readonly InitTestTiming? InitTestTiming;

    // for some reason unity hates it when the constructor takes the nullable as an argument
    public TestAttribute()
    {
        EventTiming = null;
        InitTestTiming = null;
    }

    public TestAttribute(EventTiming eventTiming)
    {
        EventTiming = eventTiming;
    }

    public TestAttribute(InitTestTiming initTestTiming)
    {
        InitTestTiming = initTestTiming;
    }
}

public enum EventTiming
{
    Awake
}

[AttributeUsage(AttributeTargets.Class)]
[MeansImplicitUse]
public class MovieTestAttribute : Attribute
{
    public readonly MovieTestTiming Timing;

    public MovieTestAttribute(MovieTestTiming timing)
    {
        Timing = timing;
    }
}

public enum MovieTestTiming
{
    Awake
}

public enum InitTestTiming
{
    Awake
}

// injection attributes

[AttributeUsage(AttributeTargets.Field)]
public abstract class TestInjectAttribute : Attribute
{
    protected const string AlreadyInjected = "Field already injected";

    /// <summary>
    /// Injects serialized property
    /// </summary>
    public abstract void InjectField(Type fieldType, SerializedProperty field);
}

public class TestInjectSceneAttribute : TestInjectAttribute
{
    public override void InjectField(Type fieldType, SerializedProperty field)
    {
        if (fieldType != typeof(string))
        {
            Debug.LogError("Field type is not string");
            return;
        }

        if (!string.IsNullOrEmpty(field.stringValue) &&
            AssetDatabase.AssetPathExists(field.stringValue))
        {
            Debug.Log(AlreadyInjected);
            return;
        }

        var scenePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(TestFrameworkRuntime.SceneAssetPath,
            "generated.unity"));

        Debug.Log($"Creating scene at `{scenePath}`");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
            NewSceneMode.Additive);
        if (!EditorSceneManager.SaveScene(scene, scenePath))
        {
            Debug.LogError($"Failed to save scene {scenePath}");
            return;
        }

        EditorSceneManager.CloseScene(scene, true);
        var scenes = EditorBuildSettings.scenes.ToList();
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();

        field.stringValue = scenePath;
    }
}

[SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeEvident")]
public class TestInjectPrefabAttribute : TestInjectAttribute
{
    public override void InjectField(Type fieldType, SerializedProperty field)
    {
        if (fieldType != typeof(GameObject))
        {
            Debug.LogError("Field type is not GameObject");
            return;
        }

        if (field.objectReferenceValue != null)
        {
            Debug.Log(AlreadyInjected);
            return;
        }

        var prefabBase = new GameObject();

        const string prefabName = "generated.prefab";
        var prefabPath =
            AssetDatabase.GenerateUniqueAssetPath(Path.Combine(TestFrameworkRuntime.PrefabAssetPath, prefabName));
        Debug.Log($"Creating prefab at `{prefabPath}`");
        PrefabUtility.SaveAsPrefabAsset(prefabBase, prefabPath, out var success);
        Object.DestroyImmediate(prefabBase);

        EditorApplication.delayCall += () =>
        {
            field.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            field.serializedObject.ApplyModifiedProperties();
            Helper.Scene.DelaySaveOpenScenes();
        };

        if (!success)
            Debug.LogError("Failed to save prefab");
    }
}

public static class Helper
{
    public static class Scene
    {
        public static void LoadScene(string scene)
        {
#if UNITY_5_3_OR_NEWER
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
#else
throw new NotImplementedException();
#endif
        }

        public static void DelaySaveOpenScenes()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorSceneManager.SaveOpenScenes())
                {
                    Debug.LogError("failed to save open scenes");
                }
            };
        }
    }
}