using UnityEngine;
using TMPro; // CRITICAL: This lets the script use TextMeshPro components

public class Mentor : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag the TextMeshPro - Text gameobject here")]
    public TextMeshProUGUI speechBubbleText;
    
    [Tooltip("Optional: Drag the background image panel here so it shuts off when empty")]
    public GameObject speechBubbleDisplay;

    public void ExecuteDirection(Transform target, string message)
    {
        // 1. Teleport to target location
        if (target != null)
        {
            transform.position = target.position;
        }

        // 2. Update the screen text layout
        if (speechBubbleText != null)
        {
            // If the message is completely empty, turn off the UI container
            if (string.IsNullOrEmpty(message))
            {
                if (speechBubbleDisplay != null) speechBubbleDisplay.SetActive(false);
            }
            else
            {
                if (speechBubbleDisplay != null) speechBubbleDisplay.SetActive(true);
                speechBubbleText.text = message;
            }
        }

        Debug.Log($"[Mentor Speech]: {message}");
    }
}