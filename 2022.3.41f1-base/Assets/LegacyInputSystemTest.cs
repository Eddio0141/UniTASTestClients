using System.Collections;
using UnityEngine;

public class LegacyInputSystemTest : MonoBehaviour
{
    private static int _jumpButtonDownCount;
    private static int _jumpButtonUpCount;

    private static int _spaceDownKeyCodeCount;
    private static int _spaceUpKeyCodeCount;
    private static int _spaceDownStringCount;
    private static int _spaceUpStringCount;

    private const int ButtonCountTest = 5;

    private SceneTest _sceneTest;

    private void Start()
    {
        _sceneTest = gameObject.GetComponent<SceneTest>();
        StartCoroutine(Test());
    }

    private IEnumerator Test()
    {
        while (_jumpButtonDownCount + _jumpButtonUpCount + _spaceDownKeyCodeCount + _spaceUpKeyCodeCount +
               _spaceDownStringCount + _spaceUpStringCount < ButtonCountTest * 6)
        {
            if (Input.GetButtonDown("Jump"))
                _jumpButtonDownCount++;

            if (Input.GetButtonUp("Jump"))
                _jumpButtonUpCount++;

            if (Input.GetKeyDown(KeyCode.Space))
                _spaceDownKeyCodeCount++;

            if (Input.GetKeyUp(KeyCode.Space))
                _spaceUpKeyCodeCount++;

            if (Input.GetKeyDown("space"))
                _spaceDownStringCount++;

            if (Input.GetKeyUp("space"))
                _spaceUpStringCount++;

            yield return null;
        }

        Debug.Log("space press check done");

        _sceneTest.StartTest();
    }
}