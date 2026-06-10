using UnityEngine;

public class Mentor : MonoBehaviour
{
    // Optional: If you use TextMeshPro later, you can uncomment this line
    // public TMPro.TextMeshProUGUI speechBubbleText;

    public void ExecuteDirection(Transform target, string message)
    {
        // 1. Instantly teleport to the target position
        if (target != null)
        {
            transform.position = target.position;
        }

        // 2. Output what the mentor is saying to the console
        Debug.Log($"[Mentor Speech]: {message}");

        // 3. UI Hook Setup (Ready for whenever you build your canvas UI)
        // if (speechBubbleText != null) speechBubbleText.text = message;
    }
}