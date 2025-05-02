using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [InitializeOnLoad]
    public static class TestFramework__unity_first__unity_latest
    {
        private const string AssetPath = "Assets/TestFramework";
        private const string SceneAssetPath = AssetPath + "/Scenes";

        static TestFramework__unity_first__unity_latest()
        {
            Debug.Log("Loading UniTAS testing framework");

            if (!Directory.Exists(SceneAssetPath))
            {
                Directory.CreateDirectory(SceneAssetPath);
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
                    var testMethods = TestFramework.GetTestFuncs(type);

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
                        const string injectFailMsg = "Inject used on an unsupported type";
                        const string alreadyInjected = "Field already injected";

                        switch (attr)
                        {
                            case TestInjectSceneAttribute:
                            {
                                if (fieldType != typeof(string))
                                    break;
                                if (!string.IsNullOrEmpty(field.stringValue) &&
                                    AssetDatabase.AssetPathExists(field.stringValue))
                                {
                                    Debug.Log(alreadyInjected);
                                    continue;
                                }

                                string scenePath;
                                while (true)
                                {
                                    var sceneName = $"{Guid.NewGuid()}.unity";
                                    scenePath = Path.Combine(SceneAssetPath, sceneName);
                                    if (EditorBuildSettings.scenes.All(s => s.path != scenePath))
                                        break;
                                }

                                Debug.Log($"Creating scene at `{scenePath}`");
                                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
                                    NewSceneMode.Additive);
                                if (!EditorSceneManager.SaveScene(scene, scenePath))
                                {
                                    Debug.LogError($"Failed to save scene {scenePath}");
                                    break;
                                }

                                EditorSceneManager.CloseScene(scene, true);

                                field.stringValue = scenePath;

                                Debug.Log("Done");

                                continue;
                            }
                            default:
                                throw new ArgumentOutOfRangeException(nameof(attr));
                        }

                        Debug.LogError(injectFailMsg);
                    }

                    prop.ApplyModifiedProperties();
                }

                if (EditorSceneManager.SaveScene(currentScene)) continue;
                Debug.LogError("Failed to save scene");
            }

            Debug.Log("Finished loading UniTAS testing framework");
        }
    }
}