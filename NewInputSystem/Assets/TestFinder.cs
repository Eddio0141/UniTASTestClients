using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestFinder : MonoBehaviour
{
    public Test2 Test;

    private void Start()
    {
        var tests = Resources.FindObjectsOfTypeAll<Test>();
        Debug.Log($"found test count {tests.Length}");
        foreach (var test in tests)
        {
            Debug.Log(test.name);
            Debug.Log(test.Value);
            Debug.Log(test.TestObj == null);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SceneManager.LoadScene(0);
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            Test.Value++;
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            var tests = Resources.FindObjectsOfTypeAll<Test>();
            foreach (var test in tests)
            {
                Debug.Log($"found test {test.name}");
            }
        }
    }
}