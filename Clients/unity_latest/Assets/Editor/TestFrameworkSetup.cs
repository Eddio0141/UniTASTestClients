using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Editor
{
    public static class TestFrameworkSetup
    {
        [MenuItem("Test/Setup")]
        private static void Setup()
        {
            Debug.Log("Loading UniTAS testing framework");

            SetupFilesAndDirs();
            SetupTestScene();

            // TODO: refactor to make tests be a single component
            var origScene = SceneManager.GetActiveScene();
            foreach (var buildSettingsScene in EditorBuildSettings.scenes)
            {
                var currentScene = origScene;
                if (origScene.path != buildSettingsScene.path)
                {
                    try
                    {
                        currentScene = EditorSceneManager.OpenScene(buildSettingsScene.path, OpenSceneMode.Additive);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to open scene `{buildSettingsScene.path}`: {e}");
                        continue;
                    }
                }

                var monoBehs = currentScene.GetRootGameObjects().Where(obj => obj != null)
                    .SelectMany(obj => obj.GetComponents<MonoBehaviour>());

                foreach (var monoBeh in monoBehs)
                {
                    var type = monoBeh.GetType();
                    var testMethods = TestFrameworkRuntime.GetTestFuncs(type);

                    var invalidTest = false;
                    foreach (var testMethod in testMethods)
                    {
                        if (testMethod.ReturnType == typeof(void) ||
                            testMethod.ReturnType == typeof(IEnumerator<TestYield>)) continue;
                        Debug.LogError("Test return type must be void or IEnumerable<TestYield>");
                        invalidTest = true;
                        break;
                    }

                    if (invalidTest)
                        continue;

                    var injectFields = type
                        .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Select(f => (f.Name, f.GetCustomAttribute<TestInjectAttribute>(true), f.FieldType))
                        .Where(tuple => tuple.Item2 != null);
                    var prop = new SerializedObject(monoBeh);
                    foreach (var (fieldName, attr, fieldType) in injectFields)
                    {
                        var field = prop.FindProperty(fieldName);
                        if (field == null)
                        {
                            Debug.LogError($"Field {fieldName} not found");
                            continue;
                        }

                        Debug.Log($"Injecting field {type.FullName}.{fieldName}");
                        attr.InjectField(fieldType, field);
                    }

                    prop.ApplyModifiedProperties();
                }

                if (EditorSceneManager.SaveScene(currentScene)) continue;
                Debug.LogError("Failed to save scene");
            }

            Debug.Log("Finished loading UniTAS testing framework");
        }

        private static void SetupFilesAndDirs()
        {
            const string scriptsDir = "Assets/Scripts";
            const string editorDir = "Assets/Editor";

            // not generating editor dir, how else is this script running then?
            var createPaths = new[]
                { TestFrameworkRuntime.SceneAssetPath, TestFrameworkRuntime.PrefabAssetPath, scriptsDir };
            foreach (var path in createPaths)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

            // TODO: figure out which unity version didn't work with symlinks

            var repoDir = Directory.GetCurrentDirectory();
            while (Path.GetFileName(repoDir) != "UniTASTestClients")
            {
                repoDir = Directory.GetParent(repoDir)?.FullName;
                if (repoDir != null) continue;
                Debug.LogError("Failed to find repository base directory, failed file setup");
                return;
            }

            var sharedDir = Path.Combine(repoDir, "UnityShared");
            UnityEngine.Assertions.Assert.IsTrue(Directory.Exists(sharedDir));
            var sharedScriptsDir = Path.Combine(sharedDir, "Scripts");
            UnityEngine.Assertions.Assert.IsTrue(Directory.Exists(sharedScriptsDir));
            var sharedEditorDir = Path.Combine(sharedDir, "Editor");

            // link everything
            var links = new[] { (sharedScriptsDir, scriptsDir), (sharedEditorDir, editorDir) };

            foreach (var (sourceDir, destDir) in links)
            {
                foreach (var sourceFile in Directory.GetFiles(sourceDir, "*.cs", SearchOption.TopDirectoryOnly))
                {
                    var destFile = Path.Combine(destDir, Path.GetFileName(sourceFile));
                    if (File.Exists(destFile))
                    {
                        File.Delete(destFile);
                    }

                    SymlinkFile(sourceFile, destFile);
                }
            }
        }

        private static void SymlinkFile(string source, string target)
        {
            var exceptions = new List<Exception>();
            try
            {
                // TODO: handle errors
                symlink(source, target);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                try
                {
                    // TODO: handle errors
                    CreateSymbolicLink(target, source, SymbolicLink.File);
                }
                catch (Exception ex2)
                {
                    exceptions.Add(ex2);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        [DllImport("kernel32.dll")]
        private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName,
            SymbolicLink dwFlags);

        [DllImport("libc.so")]
        private static extern int symlink(string oldname, string newname);

        private enum SymbolicLink
        {
            File = 0,
        }

        private static void SetupTestScene()
        {
            var saveScene = false;
            var scene = AssetDatabase.AssetPathExists(TestFrameworkRuntime.TestingScenePath)
                ? EditorSceneManager.OpenScene(TestFrameworkRuntime.TestingScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            const string testObjName = "Tests";
            const string eventHooksObjName = "EventHooks";

            var testObj = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .FirstOrDefault(o => o.name == testObjName);
            if (testObj == null)
            {
                testObj = new GameObject(testObjName);
                saveScene = true;
            }

            if (testObj.GetComponent<TestFrameworkRuntime>() == null)
            {
                testObj.AddComponent<TestFrameworkRuntime>();
                saveScene = true;
            }

            var eventHooksObj = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .FirstOrDefault(o => o.name == eventHooksObjName);
            if (eventHooksObj == null)
            {
                eventHooksObj = new GameObject(eventHooksObjName);
                saveScene = true;
            }

            if (eventHooksObj.GetComponent<EventHooks>() == null)
            {
                eventHooksObj.AddComponent<EventHooks>();
                saveScene = true;
            }

            if (saveScene)
                EditorSceneManager.SaveScene(scene, TestFrameworkRuntime.TestingScenePath);
        }

        [MenuItem("Test/Run General Tests")]
        private static void RunGeneralTests()
        {
            TestFrameworkRuntime.RunGeneral();
        }
    }
}