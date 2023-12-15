using System.Collections;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace UI
{
    [RequireComponent(typeof(TextMeshProUGUI), typeof(RectTransform))]
    public class InputDisplay : MonoBehaviour
    {
        private TextMeshProUGUI _text;
        private RectTransform _textRectTransform;

        private RectTransform _parentRectTransform;

        public Test Test;
        public Test Test2;
        public Object SomeObject;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            if (_text == null)
            {
                Debug.LogError("InputDisplay: TextMeshPro component not found");
                return;
            }

            _parentRectTransform = transform.parent.GetComponent<RectTransform>();
            if (_parentRectTransform == null)
            {
                Debug.LogError("InputDisplay: RectTransform component not found");
                return;
            }

            _textRectTransform = GetComponent<RectTransform>();
            if (_textRectTransform == null)
            {
                Debug.LogError("InputDisplay: RectTransform component not found");
                return;
            }

            var rect = _parentRectTransform.rect;
            _textRectTransform.sizeDelta = new(rect.width, rect.height);

            // StartCoroutine(YieldNull());
            // StartCoroutine(YieldWaitForSeconds());
            // StartCoroutine(YieldWaitUntil());
            // StartCoroutine(YieldWaitWhile());

            // Application.targetFrameRate = 15;
            // Time.timeScale = 0.5f;
            Time.captureFramerate = 100;
            // Time.fixedDeltaTime = 0.025f;

            Debug.Log("Awake");

            Test.Value++;
            Test2.Value += 2;

            var objs = Resources.FindObjectsOfTypeAll<Test>();
            Debug.Log($"Tests: {objs.Length}");
            foreach (var testObj in objs)
            {
                Debug.Log($"name: {testObj.name}");
                Debug.Log($"test value is {testObj.Value}");
                Debug.Log($"obj null is {testObj.TestObj == null}");
            }
        }

        private void Update()
        {
            if (_text == null) return;

            var builder = new StringBuilder();

            var mouse = Mouse.current;
            if (mouse == null)
            {
                builder.AppendLine("Mouse not found");
            }
            else
            {
                builder.AppendLine($"Mouse: {mouse.position.ReadValue()}" +
                                   $", scroll: {mouse.scroll.ReadValue()}" +
                                   (mouse.leftButton.isPressed ? ", left click" : "") +
                                   (mouse.middleButton.isPressed ? ", middle click" : "") +
                                   (mouse.rightButton.isPressed ? ", right click" : ""));
            }

            var keyboard = Keyboard.current;
            builder.AppendLine(keyboard == null
                ? "Keyboard not found"
                : $"Keyboard: {string.Join(", ", keyboard.allKeys.Where(k => k.isPressed).Select(k => k.keyCode.ToString()))}");

            builder.AppendLine($"Test: {Test.Value}, obj null is {Test.TestObj == null}");
            builder.AppendLine($"Test2: {Test2.Value}, obj null is {Test2.TestObj == null}");

            _text.text = builder.ToString();

            if (Input.GetKeyDown(KeyCode.F1))
            {
                SceneManager.LoadScene(1);

                Debug.Log("=============================");
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                Test.Value++;
                Test2.Value += 2;
                Test.TestObj = SomeObject;
                Test2.TestObj = SomeObject;
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                var tests = Resources.FindObjectsOfTypeAll<Test>();
                foreach (var test in tests)
                {
                    Debug.Log($"found test {test.name}");
                }

                var tests2 = Resources.FindObjectsOfTypeAll<Test2>();
                foreach (var test in tests2)
                {
                    Debug.Log($"found test2 {test.name}");
                }
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                var newTest = ScriptableObject.CreateInstance<Test>();
                newTest.Value = Test2.Value * 2;
                newTest.name = "newTest";

                var newTest2 = ScriptableObject.CreateInstance("Test") as Test;
                if (newTest2 == null)
                {
                    Debug.Log("newTest2 is null");
                }
                else
                {
                    newTest2.Value = Test.Value * 3;
                    newTest2.name = "newTest2";
                }

                var newTest3 = Instantiate(Test);
                newTest3.Value = Test.Value * 4;
                newTest3.name = "newTest3";

                var newTest4 = (Test)ScriptableObject.CreateInstance(typeof(Test));
                newTest4.Value = Test.Value * 5;
                newTest4.name = "newTest4";
            }

            var v = Input.GetAxis("Horizontal");
            if (v != 0f)
            {
                Debug.Log("horizontal: " + v);
            }

            v = Input.GetAxisRaw("Horizontal");
            if (v != 0f)
            {
                Debug.Log("raw: " + v);
            }

            // Debug.Log(Input.GetKey("mouse 0"));
        }

        private IEnumerator YieldNull()
        {
            yield return null;
            Debug.Log("yield null");
        }

        private IEnumerator YieldWaitForSeconds()
        {
            yield return new WaitForSeconds(0f);
            Debug.Log("yield YieldWaitForSeconds");
        }

        private IEnumerator YieldWaitUntil()
        {
            yield return new WaitUntil(() => true);
            Debug.Log("yield YieldWaitUntil");
        }

        private IEnumerator YieldWaitWhile()
        {
            yield return new WaitWhile(() => false);
            Debug.Log("yield YieldWaitWhile");
        }

        public void Button1()
        {
            Debug.Log("Button1");
        }

        public void Button2()
        {
            Debug.Log("Button2");
        }
    }
}