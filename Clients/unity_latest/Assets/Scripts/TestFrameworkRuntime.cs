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

    private static TestFrameworkRuntime _instance;

    private readonly List<Result> _testResults = new List<Result>();
#pragma warning disable CS1691 CS1692 CS0414 // Field is assigned but its value is never used
    private bool _testsDone;
#pragma warning restore CS1691 CS1692 CS0414 // Field is assigned but its value is never used

    private Test[] _discoveredTests;
    private (string name, Test[])[] _movieTests;

    private static void InstanceInitIfNot()
    {
        if (_instance != null) return;
        var obj = new GameObject();
        DontDestroyOnLoad(obj);
        _instance = obj.AddComponent<TestFrameworkRuntime>();
    }

    public static IEnumerable<MethodInfo> GetTestFuncs(Type type)
    {
        return type
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                        BindingFlags.NonPublic).Where(m =>
                m.GetCustomAttributes<TestAttribute>().Any(t => t.Type == null));
    }

    private void DiscoverTestsIfNot()
    {
        if (_discoveredTests != null && _movieTests != null) return;
        
        var tests = new List<Test>();
        var movieTests = new List<(string, Test[])>();
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
                    m.GetCustomAttribute<TestAttribute>()?.Type));
                if (movieTestAttr != null)
                {
                    movieTests.Add((monoBehType.FullName, testsIter.ToArray()));
                    continue;
                }
                tests.AddRange(testsIter);
            }
        }
#else
            throw new NotImplementedException();
#endif
        _discoveredTests = tests.ToArray();
        _movieTests = movieTests.ToArray();
        Debug.Log($"Discovered {_discoveredTests.Length + _movieTests.Length} tests");
    }

    public static void Run()
    {
        InstanceInitIfNot();
        _instance.RunInternal();
    }

    private void RunInternal()
    {
        DiscoverTestsIfNot();
        StartCoroutine(RunInternalCoroutine(_discoveredTests.Where(t => t.SpecialTestType == null)));
    }

    private IEnumerator RunInternalCoroutine(IEnumerable<Test> tests)
    {
        foreach (var test in tests)
        {
            Debug.Log($"Running test {test.Name}");
            var executeIter = test.Execute();

            var success = true;
            string msg = null;
            while (true)
            {
                bool moveNextResult;
                try
                {
                    moveNextResult = executeIter.MoveNext();
                }
                catch (Exception e)
                {
                    success = false;
                    if (e.InnerException is AssertionException assertionException)
                    {
                        msg = assertionException.Message;
                    }
                    else
                    {
                        msg = e.ToString();
                    }

                    break;
                }

                if (!moveNextResult) break;
                yield return executeIter.Current;
            }

            var result = new Result(test.Name, msg, success);
            _testResults.Add(result);

            // safety padding between tests, it won't be noticeable
            for (var i = 0; i < 5; i++)
            {
                yield return null;
            }
        }

        _testsDone = true;
        Debug.Log("Tests finished");
        foreach (var result in _testResults)
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

    private readonly struct Test
    {
        public readonly string Name;
        private readonly MethodInfo _method;
        private readonly bool _testDoesIter;
        private readonly MonoBehaviour _objInstance;
        public readonly SpecialTestType? SpecialTestType;

        public Test(string name, MethodInfo method, MonoBehaviour objInstance, SpecialTestType? specialTestType)
        {
            Name = name;
            _method = method;
            _objInstance = objInstance;
            SpecialTestType = specialTestType;
            _testDoesIter = method.ReturnType == typeof(IEnumerator<TestYield>);
        }

        public IEnumerator Execute()
        {
            var testRet = _method.Invoke(_objInstance, Array.Empty<object>());
            if (!_testDoesIter)
            {
                yield break;
            }

            var iter = (IEnumerator<TestYield>)testRet;

            while (iter.MoveNext())
            {
                if (iter.Current == null)
                {
                    throw new InvalidOperationException("Test yield returned null which isn't expected");
                }

                yield return iter.Current.Operation();
            }
        }
    }
}