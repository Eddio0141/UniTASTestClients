using System;
using UnityEngine;

public class A : MonoBehaviour
{
    private void Update()
    {
        transform.position += transform.up * (Time.deltaTime * 0.1f);
    }

    private void OnBecameVisible()
    {
        Debug.Log($"visible: {Time.time}");
    }

    private void OnBecameInvisible()
    {
        Debug.Log($"invisible: {Time.time}");
    }
}
