using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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

        _cam = Camera.main;

        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;

        StartCoroutine(FpsUpdate());
    }

    private bool _noRefresh;
    private Camera _cam;

    private readonly List<int> _fpsHistory = new();
    private int _fps;

    private IEnumerator FpsUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            _fps = _fpsHistory.Sum() / _fpsHistory.Count;
            _fpsHistory.Clear();
        }
        // ReSharper disable once IteratorNeverReturns
    }

    private void Update()
    {
        _fpsHistory.Add((int)(1f / Time.unscaledDeltaTime));

        // if (Time.time < 1f)
        //     Debug.Log("update");
        if (Input.GetKeyDown(KeyCode.H))
        {
            _noRefresh = !_noRefresh;
            if (_noRefresh)
            {
                _cam.rect = new(0f, 0f, 0f, 0f);
            }
            else
            {
                _cam.rect = new(0f, 0f, 1f, 1f);
            }
        }

        if (_text == null) return;

        var builder = new StringBuilder();

        builder.AppendLine($"fps: {_fps}");

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