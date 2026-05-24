using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Credits : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 moveDirection = new Vector3(0, 1, 0);
    public float scrollSpeed = 2.0f;

    [Header("Scene Control")]
    public float totalDuration = 30f;

    void Update()
    {
        transform.Translate(moveDirection * scrollSpeed * Time.deltaTime);
    }
}