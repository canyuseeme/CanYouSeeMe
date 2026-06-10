using UnityEngine;

public class Circles : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Circle Settings")]
    public float radius = 4f;
    [SerializeField] private int segments = 72;

    [Header("Time")]
    public float REQUIRED_DURATION = 5f;

    private LineRenderer lineRenderer;
    private Rigidbody2D playerRb;
    private const float MAX_SPEED = 1.5f;

    [Header("Mentor Connection")]
    public Mentor mentor;
    public MentorDirection[] directions;

    // --- New Timer Variables ---
    private float greenTimer = 0f;
    private bool wasGreen = false;

    // Public property so the Manager can check if this drill is done
    public bool IsCompleted { get; private set; } = false;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
        }

        ConfigureLineRenderer();
        DrawInGameCircle();
    }

    void Update()
    {
        // Don't run any logic if this specific circle is already finished
        if (IsCompleted || player == null || playerRb == null) return;

        // --- 1. DISTANCE CALCULATION ---
        float distanceToCenter = Vector2.Distance(transform.position, player.position);
        float distanceToLine = Mathf.Abs(distanceToCenter - radius);
        float distancePercentage = Mathf.Max(0f, 1f - (distanceToLine / radius)) * 100f;

        // --- 2. SPEED CALCULATION ---
        float currentSpeed = playerRb.linearVelocity.magnitude; 
        float speedPercentage = Mathf.Clamp((currentSpeed / MAX_SPEED) * 100f, 0f, 100f);

        // --- 3. MASTER CALCULATION ---
        float masterPercentage = (distancePercentage + speedPercentage) / 2f;

        // --- 4. OUTPUT CATEGORIES TO CONSOLE ---
        //Debug.Log($"Distance - {distancePercentage:F1}% | Speed - {speedPercentage:F1}% | Master - {masterPercentage:F1}% | Timer - {greenTimer:F1}s");

        // --- 5. DYNAMIC VISUAL & TIMER FEEDBACK ---
        if (masterPercentage > 90f)
        {
            // Turn bright green
            lineRenderer.startColor = new Color(0.1f, 0.9f, 0.1f);
            lineRenderer.endColor = new Color(0.1f, 0.9f, 0.1f);

            // Increment timer while they stay above 90%
            greenTimer += Time.deltaTime;

            // Trigger completion once they hold it for 5 consecutive seconds
            if (greenTimer >= REQUIRED_DURATION)
            {
                IsCompleted = true;
                lineRenderer.enabled = false; // Make the visual circle disappear
                Debug.Log($"{gameObject.name} Completed Successfully!");
            }

            wasGreen = true;
        }
        else
        {
            if (wasGreen)
            {
                TriggerDirection(0);
            }
            wasGreen = false;

            // Revert back to default red
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;

            // CRITICAL: Reset timer immediately to 0 if they break the streak
            greenTimer = 0f;
        }
    }

    void ConfigureLineRenderer()
    {
        lineRenderer.positionCount = segments + 1;
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = 0.08f;
        lineRenderer.endWidth = 0.08f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }

    void DrawInGameCircle()
    {
        float deltaTheta = (2f * Mathf.PI) / segments;
        float theta = 0f;

        for (int i = 0; i < segments + 1; i++)
        {
            float x = radius * Mathf.Cos(theta);
            float y = radius * Mathf.Sin(theta);
            
            lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
            theta += deltaTheta;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private void TriggerDirection(int directionIndex)
    {
        if (mentor == null) return;
        
        // Safety check to ensure you filled out enough directions in the inspector array
        if (directions != null && directionIndex < directions.Length)
        {
            mentor.ExecuteDirection(directions[directionIndex].position, directions[directionIndex].message);
        }
        else
        {
            Debug.LogWarning($"TutorialManager: Tried to trigger direction index {directionIndex}, but it hasn't been set up in the Directions array!");
        }
    }
}