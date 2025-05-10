using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
            var testObj = InitTestScene();
            SetupTestScene(testObj);

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

                    RelativeSymlinkFile(sourceFile, destFile);
                }
            }
        }

        private static void RelativeSymlinkFile(string source, string target)
        {
            var targetWorking = Directory.GetParent(target)?.FullName;
            if (targetWorking == null)
            {
                throw new ArgumentException($"path `{target}` doesn't have a parent directory", nameof(target));
            }

            // find out how much we need to go back to reach source dir

            var sourceRel = string.Empty;
            while (!source.StartsWith(targetWorking))
            {
                targetWorking = Directory.GetParent(targetWorking)?.FullName;
                if (targetWorking == null)
                {
                    throw new InvalidOperationException("Directory.GetParent returned null, this should never happen" +
                                                        $", source: `{source}, target: {target}");
                }

                sourceRel += $"..{Path.DirectorySeparatorChar}";
            }

            // now push rest of the path
            source = sourceRel + source[(targetWorking.Length + 1)..];

            var plat = Environment.OSVersion.Platform;
            var success = plat switch
            {
                PlatformID.Unix => symlink(source, target) == 0,
                PlatformID.Win32NT or PlatformID.Win32S or PlatformID.Win32Windows or PlatformID.WinCE =>
                    CreateSymbolicLink(target, source, SymbolicLink.File),
                _ => throw new NotImplementedException($"symlink operation not implemented for platform {plat}")
            };

            if (!success)
            {
                throw new Exception($"symlink failed: error code {Marshal.GetLastWin32Error()}");
            }
        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName,
            SymbolicLink dwFlags);

        [DllImport("libc", SetLastError = true)]
        private static extern int symlink(string oldname, string newname);

        private enum SymbolicLink
        {
            File = 0
        }

        private static GameObject InitTestScene()
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

            var buildScenes = EditorBuildSettings.scenes.ToList();
            buildScenes.Add(new EditorBuildSettingsScene(TestFrameworkRuntime.TestingScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();

            return testObj;
        }

        private static void SetupTestScene(GameObject tests)
        {
            foreach (var monoBeh in tests.GetComponents<MonoBehaviour>())
            {
                var type = monoBeh.GetType();
                var testMethods = TestFrameworkRuntime.GetTestFuncs(type);

                var invalidTest = false;
                var hasTests = false;
                foreach (var testMethod in testMethods)
                {
                    hasTests = true;
                    if (testMethod.ReturnType == typeof(void) ||
                        testMethod.ReturnType == typeof(IEnumerator<TestYield>)) continue;
                    Debug.LogError("Test return type must be void or IEnumerable<TestYield>");
                    invalidTest = true;
                    break;
                }

                if (invalidTest || !hasTests)
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

            if (!EditorSceneManager.SaveOpenScenes())
            {
                Debug.LogError("failed to save open scenes");
            }
        }

        [MenuItem("Test/Run General Tests")]
        private static void RunGeneralTests()
        {
            TestFrameworkRuntime.RunGeneral();
        }
    }
}