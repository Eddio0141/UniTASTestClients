using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor
{
    [InitializeOnLoad]
    public static class TestFrameworkSetup
    {
        static TestFrameworkSetup()
        {
            Setup();
        }

        [MenuItem("Test/Setup")]
        private static void Setup()
        {
            Debug.Log("Loading UniTAS testing framework");
            // AssetDatabase.StartAssetEditing();

            var createPaths = new[] { TestFrameworkRuntime.SceneAssetPath, TestFrameworkRuntime.PrefabAssetPath };
            foreach (var path in createPaths)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }

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

            // AssetDatabase.StopAssetEditing();
            Debug.Log("Finished loading UniTAS testing framework");
        }
        
        [MenuItem("Test/Run General Tests")]
        private static void RunGeneralTests()
        {
            TestFrameworkRuntime.Run();
        }
    }
}