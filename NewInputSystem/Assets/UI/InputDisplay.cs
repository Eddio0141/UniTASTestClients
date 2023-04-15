using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI
{
    [RequireComponent(typeof(TextMeshProUGUI), typeof(RectTransform))]
    public class InputDisplay : MonoBehaviour
    {
        private TextMeshProUGUI _text;
        private RectTransform _textRectTransform;

        private RectTransform _parentRectTransform;

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

            _text.text = builder.ToString();
        }
    }
}