using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

[SuppressMessage("ReSharper", "UseStringInterpolation")]
[SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeEvident")]
public class TestFrameworkRuntime : MonoBehaviour
{
    private const string AssetPath = "Assets/TestFramework";
    public const string SceneAssetPath = AssetPath + "/Scenes";
    public const string PrefabAssetPath = AssetPath + "/Prefabs";
    public const string TestingScenePath = AssetPath + "/Scenes/general.unity";

    private static TestFrameworkRuntime _instance;
    private readonly List<Result> _generalTestResults = new List<Result>();
    private readonly List<Result> _movieTestResults = new List<Result>();
    private Test[] _generalTests;
    private Test[] _eventTests;
    private (MovieTestAttribute, Test[])[] _movieTests;

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

    private static void InstanceInitIfNot()
    {
        // don't handle modifications to game object here, do that in Awake
        if (_instance != null) return;
        var obj = new GameObject();
        obj.AddComponent<TestFrameworkRuntime>();
    }

    private void DiscoverTestsIfNot()
    {
        if (_generalTests != null && _movieTests != null && _eventTests != null) return;
        var generalTests = new List<Test>();
        var eventTests = new List<Test>();
        var movieTests = new List<(MovieTestAttribute, Test[])>();
#if UNITY_5_3_OR_NEWER
        var sceneCount = SceneManager.sceneCount;
        for (var i = 0; i < sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            var objs = scene.GetRootGameObjects();
            foreach (var monoBeh in objs.SelectMany(o => o.GetComponents<MonoBehaviour>()))
            {
                var monoBehType = monoBeh.GetType();
                var movieTestAttr = monoBehType.GetCustomAttribute<MovieTestAttribute>();
                var methods = GetTestFuncs(monoBehType);
                var testsIter = methods.Select(m => new Test($"{monoBehType.FullName}.{m.Name}", m, monoBeh,
                    m.GetCustomAttribute<TestAttribute>().Timing)).ToArray();
                if (movieTestAttr != null)
                {
                    foreach (var test in testsIter)
                    {
                        if (test.EventTiming.HasValue)
                        {
                            Debug.LogWarning(
                                $"Test {test.Name} is a movie test and the event timing argument is ineffective");
                        }
                    }

                    movieTests.Add((movieTestAttr, testsIter));
                    continue;
                }

                generalTests.AddRange(testsIter.Where(t => !t.EventTiming.HasValue).ToArray());
                eventTests.AddRange(testsIter.Where(t => t.EventTiming.HasValue).ToArray());
            }
        }
#else
            throw new NotImplementedException();
#endif
        _generalTests = generalTests.ToArray();
        _eventTests = eventTests.ToArray();
        _movieTests = movieTests.ToArray();
        Debug.Log($"Discovered {_generalTests.Length} general tests" +
                  $", {_eventTests.Length} event tests" +
                  $", and {_movieTests.Length} movie tests");
    }

    public static IEnumerable<MethodInfo> GetTestFuncs(Type type)
    {
        return type
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.GetCustomAttributes<TestAttribute>().Any(t => t.Timing == null));
    }

    public static void RunGeneral()
    {
        InstanceInitIfNot();
        _instance.DiscoverTestsIfNot();
        _instance.StartCoroutine(_instance.RunGeneralInternal());
    }

    private IEnumerator RunGeneralInternal()
    {
        foreach (var test in _generalTests)
        {
            yield return RunTest(test);
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

    private IEnumerator RunTest(Test test)
    {
        Debug.Log($"Running test {test.Name}");
        var executeIter = test.Execute();
        while (executeIter.MoveNext())
        {
            if (executeIter.Current is Result result)
            {
                _generalTestResults.Add(result);
                break;
            }

            yield return executeIter.Current;
        }

        // safety padding between tests, it won't be noticeable
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
        SceneManager.LoadScene(TestingScenePath);
    }

    private readonly Queue<Test> _pendingEventTests = new Queue<Test>();
    private Test? _currentEventTest;

    public static IEnumerator AwakeTestHook()
    {
        yield return _instance.EventHookInternal(EventTiming.Awake);
    }

    private IEnumerator EventHookInternal(EventTiming timing)
    {
        if (!_currentEventTest.HasValue || _currentEventTest.Value.EventTiming != timing)
            yield break;
        yield return RunTest(_currentEventTest.Value);
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

    private readonly struct Test
    {
        public readonly string Name;
        private readonly MethodInfo _method;
        private readonly bool _testDoesIter;
        private readonly MonoBehaviour _objInstance;
        public readonly EventTiming? EventTiming;

        public Test(string name, MethodInfo method, MonoBehaviour objInstance, EventTiming? eventTiming)
        {
            Name = name;
            _method = method;
            _objInstance = objInstance;
            EventTiming = eventTiming;
            _testDoesIter = method.ReturnType == typeof(IEnumerator<TestYield>);
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
                yield return new Result(Name, msg, false);
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
    }
}