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
            InitDirs();
            var (sharedScriptsDir, sharedEditorDir, testsDir) = GetRepoDirs();
            LinkRunnerFiles(sharedScriptsDir, sharedEditorDir);
            InitTestScene();
            LinkAndAddTests(testsDir);

            Debug.Log("Domain reload");
            DomainReload();
        }

        private static void DomainReload()
        {
#if UNITY_2019_3_OR_NEWER
            EditorUtility.RequestScriptReload();
#else
            var dummyScript = Path.Combine(TestFrameworkRuntime.AssetPath, "dummyScript.cs");
            File.Create(dummyScript).Dispose();
            AssetDatabase.ImportAsset(dummyScript);
#endif
        }

        private static bool _preventAfterReload;

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void AfterReload()
        {
            if (_preventAfterReload)
            {
                Debug.LogError("prevented AfterReload call happening recursively, something is causing this");
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AfterReload;
                return;
            }

            if (EditorApplication.isPlaying) return;

            _preventAfterReload = true;

            var testObj = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .FirstOrDefault(o => o.name == TestObjName);

            if (testObj != null)
            {
                AddTests(testObj);
                SetupTestScene(testObj);

                if (!EditorSceneManager.SaveOpenScenes())
                {
                    Debug.LogError("failed to save opened scenes");
                }
            }

            Debug.Log("Finished loading UniTAS testing framework");
            _preventAfterReload = false;
        }


        private const string ScriptsDir = "Assets/Scripts";
        private const string TestsDir = ScriptsDir + "/Tests";

        private static (string sharedScriptsDir, string sharedEditorDir, string testsDir) GetRepoDirs()
        {
            var repoDir = Directory.GetCurrentDirectory();
            while (Path.GetFileName(repoDir) != "UniTASTestClients")
            {
                repoDir = Directory.GetParent(repoDir)?.FullName;
                if (repoDir != null) continue;
                throw new Exception("Failed to find repository base directory, failed file setup");
            }

            var sharedDir = Path.Combine(repoDir, "UnityShared");
            AssertDirExists(sharedDir);
            var sharedScriptsDir = Path.Combine(sharedDir, "Scripts");
            AssertDirExists(sharedScriptsDir);
            var sharedEditorDir = Path.Combine(sharedDir, "Editor");
            AssertDirExists(sharedEditorDir);
            var testsDir = Path.Combine(sharedDir, "Tests");
            AssertDirExists(testsDir);
            return (sharedScriptsDir, sharedEditorDir, testsDir);
        }

        private static void AssertDirExists(string dir)
        {
            UnityEngine.Assertions.Assert.IsTrue(Directory.Exists(dir));
        }

        private static void InitDirs()
        {
            var createPaths = new[]
            {
                TestFrameworkRuntime.SceneAssetPath, TestFrameworkRuntime.PrefabAssetPath, TestsDir
            };
            foreach (var path in createPaths)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
        }

        private static void LinkRunnerFiles(string sharedScriptsDir, string sharedEditorDir)
        {
            const string editorDir = "Assets/Editor";

            // TODO: figure out which unity version didn't work with symlinks

            // link everything
            var links = new[] { (sharedScriptsDir, ScriptsDir), (sharedEditorDir, editorDir) };
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

        private static void LinkAndAddTests(string testsDir)
        {
            foreach (var sourceFile in Directory.GetFiles(testsDir, "*.cs", SearchOption.TopDirectoryOnly))
            {
                var fileNameNoExt = Path.GetFileNameWithoutExtension(sourceFile);
                if (!MatchesVersion(fileNameNoExt))
                {
                    continue;
                }

                Debug.Log($"found matching test file `{fileNameNoExt}`");
                var destFile = Path.Combine(TestsDir, Path.GetFileName(sourceFile));
                if (File.Exists(destFile))
                {
                    File.Delete(destFile);
                }

                RelativeSymlinkFile(sourceFile, destFile);
            }
        }

        private static void AddTests(GameObject testObj)
        {
            if (!Directory.Exists(TestsDir))
            {
                Debug.LogWarning($"tests directory `{TestsDir}` doesn't exist");
                return;
            }

            foreach (var testPath in Directory.GetFiles(TestsDir, "*.cs", SearchOption.TopDirectoryOnly))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(testPath);
                var scriptType = script.GetClass();
                if (testObj.GetComponent(scriptType) != null) continue;
                testObj.AddComponent(scriptType);
            }
        }

        private const string TinySep = "_";
        private const string BigSep = "__";

        private static bool MatchesVersion(string testName)
        {
            var exampleTestName = $"`Category{BigSep}2022{TinySep}3{TinySep}41{BigSep}2023{TinySep}3`";
            var versionStartIdx = testName.IndexOf(BigSep, StringComparison.InvariantCulture);
            switch (versionStartIdx)
            {
                case -1:
                    throw new InvalidOperationException(
                        $"test name `{testName}` is formatted wrong, missing initial `{BigSep}` before stating" +
                        " minimum unity version like so: " + exampleTestName);
                case 0:
                    Debug.LogWarning($"test name `{testName}` has got no category prefixed in the name like so: " +
                                     exampleTestName);
                    break;
            }

            var fullVersionRaw = testName[(versionStartIdx + BigSep.Length)..];
            var versionSepIdx = fullVersionRaw.IndexOf(BigSep, StringComparison.InvariantCulture);
            switch (versionSepIdx)
            {
                case -1:
                    throw new InvalidOperationException(
                        $"test name `{testName}` doesn't have a max version defined for the test, only the min version" +
                        "you need to add the maximum inclusive version like so: " + exampleTestName);
                case 0:
                    throw new InvalidOperationException(
                        $"test name `{testName}` minimum version is non-existent, you need to define it like so: " +
                        exampleTestName);
            }

            var versionMinRaw = fullVersionRaw[..versionSepIdx];
            var versionMaxRaw = fullVersionRaw[(versionSepIdx + BigSep.Length)..];
            if (versionMaxRaw.Trim().Length == 0)
            {
                throw new InvalidOperationException(
                    $"test name `{testName}` maximum version is non-existent, you need to define it like so: " +
                    exampleTestName);
            }

            using var versionMin = GetVersionFromRaw(versionMinRaw).GetEnumerator();
            using var versionMax = GetVersionFromRaw(versionMaxRaw).GetEnumerator();
            var currentVersion = Application.unityVersion.Split('.').Select(v => int.Parse(v.Replace('f', '0')))
                .ToArray();
            foreach (var currentVersionEntry in currentVersion)
            {
                if (!versionMin.MoveNext())
                {
                    break;
                }

                if (currentVersionEntry > versionMin.Current) break;
                if (currentVersionEntry < versionMin.Current) return false;
            }

            foreach (var currentVersionEntry in currentVersion)
            {
                if (!versionMax.MoveNext())
                {
                    break;
                }

                if (currentVersionEntry < versionMax.Current) break;
                if (currentVersionEntry > versionMax.Current) return false;
            }

            return true;
        }

        private static IEnumerable<int> GetVersionFromRaw(string rawVersion)
        {
            var split = rawVersion.Split(TinySep);
            return split.Select(v =>
            {
                if (int.TryParse(v.Replace('f', '0'), out var success))
                {
                    return success;
                }

                throw new InvalidOperationException(
                    $"invalid version: `{rawVersion}`, make sure each version number is separated by `{TinySep}`");
            });
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
                                                        $", source: `{source}`, target: `{target}`");
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

        private const string TestObjName = "Tests";

        private static void InitTestScene()
        {
            var saveScene = false;
            var scene = AssetDatabase.AssetPathExists(TestFrameworkRuntime.TestingScenePath)
                ? EditorSceneManager.OpenScene(TestFrameworkRuntime.TestingScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            const string eventHooksObjName = "EventHooks";
            var testObj = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .FirstOrDefault(o => o.name == TestObjName);
            if (testObj == null)
            {
                testObj = new GameObject(TestObjName);
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

            if (saveScene) EditorSceneManager.SaveScene(scene, TestFrameworkRuntime.TestingScenePath);
            var buildScenes = EditorBuildSettings.scenes.ToList();
            buildScenes.Add(new EditorBuildSettingsScene(TestFrameworkRuntime.TestingScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        private static void SetupTestScene(GameObject tests)
        {
            foreach (var monoBeh in tests.GetComponents<MonoBehaviour>())
            {
                if (monoBeh == null) continue;
                var type = monoBeh.GetType();
                var testMethods = TestFrameworkRuntime.GetTestFuncs(type);
                var invalidTest = false;
                var hasTests = false;
                foreach (var testMethod in testMethods)
                {
                    hasTests = true;
                    if (testMethod.ReturnType == typeof(void) ||
                        testMethod.ReturnType == typeof(IEnumerator<TestYield>))
                        continue;
                    Debug.LogError("Test return type must be void or IEnumerable<TestYield>");
                    invalidTest = true;
                    break;
                }

                if (invalidTest || !hasTests) continue;
                var injectFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
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
                    InjectField(attr, fieldType, field);
                }

                prop.ApplyModifiedProperties();
            }
        }

        private const string AlreadyInjected = "Field already injected";

        private static void InjectField(TestInjectAttribute attr, Type fieldType, SerializedProperty field)
        {
            switch (attr)
            {
                case TestInjectSceneAttribute:
                    InjectFieldTestInjectScene(fieldType, field);
                    break;
                case TestInjectPrefabAttribute:
                    InjectFieldTestInjectPrefab(fieldType, field);
                    break;
            }
        }

        private static void InjectFieldTestInjectScene(Type fieldType, SerializedProperty field)
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
            scenes.Add(new EditorBuildSettingsScene(scenePath.Substring("Assets/".Length), true));
            EditorBuildSettings.scenes = scenes.ToArray();

            field.stringValue = scenePath;
        }

        private static void InjectFieldTestInjectPrefab(Type fieldType, SerializedProperty field)
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
                HelperEditor.DelaySaveOpenScenes();
            };

            if (!success)
                Debug.LogError("Failed to save prefab");
        }

        [MenuItem("Test/Run General Tests")]
        private static void RunGeneralTests()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("click play first");
                return;
            }

            TestFrameworkRuntime.RunGeneralTests();
        }
    }
}

public static class HelperEditor
{
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