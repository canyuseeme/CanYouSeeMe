using UnityEngine;

public class VisibilityController : MonoBehaviour
{
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    [Header("Visibility Settings")]
    public float PV = 3.37555f;
    public float maxSpeedReference = 1.5f;

    public float CurrentVisibility { get; private set; }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (rb == null || sr == null) return;

        // Using linearVelocity magnitude to handle the enemy's movement
        float currentSpeed = rb.linearVelocity.magnitude;

        float speedPercent = currentSpeed / maxSpeedReference;
        //float visibility = Mathf.Pow(speedPercent, PV);
        float visibility = speedPercent;
        CurrentVisibility = Mathf.Clamp01(visibility); // Store it here

        Color c = sr.color;
        c.a = Mathf.Clamp01(visibility);
        sr.color = c;
    }
}