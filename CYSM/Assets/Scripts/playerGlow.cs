using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class playerGlow : MonoBehaviour
{
    private string prefKey = "CrosshairEnabled";

    private SpriteRenderer spriteRen;
    private Image uiImage;

    void Awake()
    {
        // Automatically find whatever is rendering this object
        spriteRen = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<Image>();
    }

    void Start()
    {
        // Load preference (Default to 1/True)
        bool isEnabled = PlayerPrefs.GetInt(prefKey, 1) == 1;
        ApplyVisibility(isEnabled);
    }

    void Update()
    {
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            ToggleCrosshair();
        }
    }

    void ToggleCrosshair()
    {
        bool currentState = false;
        
        if (spriteRen != null) currentState = spriteRen.enabled;
        else if (uiImage != null) currentState = uiImage.enabled;

        bool newState = !currentState;
        ApplyVisibility(newState);

        // Save it
        PlayerPrefs.SetInt(prefKey, newState ? 1 : 0);
        PlayerPrefs.Save();
    }

    void ApplyVisibility(bool state)
    {
        // Toggle the component so the script stays active
        if (spriteRen != null) spriteRen.enabled = state;
        if (uiImage != null) uiImage.enabled = state;
    }
}