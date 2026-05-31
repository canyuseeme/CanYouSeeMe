using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dots : MonoBehaviour
{
    [Header("Positions")]
    [Tooltip("Drop empty GameObjects here to act as the target positions")]
    public List<Transform> targetPositions;

    [Header("Audio")]
    public AudioClip collectionSound;
    [Range(0f, 1f)] public float volume = 0.7f;

    [Header("Dot Settings")]
    public float dotRadius = 0.5f;
    private int segments = 36;

    private LineRenderer lineRenderer;
    private CircleCollider2D triggerCollider;
    private int currentDotIndex = 0;
    private bool isTransitioning = false;

    // Public property so TutorialManager knows when this phase is wiped out
    public bool IsCompleted { get; private set; } = false;

    void Start()
    {
        // 1. Setup LineRenderer for the target visual
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();
        ConfigureLineRenderer();

        // 2. Setup CircleCollider2D dynamically for physics detection
        triggerCollider = GetComponent<CircleCollider2D>();
        if (triggerCollider == null) triggerCollider = gameObject.AddComponent<CircleCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = dotRadius;

        // 3. Jump to the very first position
        MoveToCurrentTarget();
    }

    void MoveToCurrentTarget()
    {
        if (targetPositions == null || targetPositions.Count == 0 || currentDotIndex >= targetPositions.Count)
        {
            IsCompleted = true;
            lineRenderer.enabled = false;
            triggerCollider.enabled = false;
            return;
        }

        // Teleport this entire GameObject (Visuals + Collider) to the next target position
        transform.position = targetPositions[currentDotIndex].position;
        
        // Ensure visual is red and visible
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.enabled = true;
        triggerCollider.enabled = true;
        
        DrawDotCircle();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerBullet") && !isTransitioning && !IsCompleted)
        {
            StartCoroutine(HandleDotCollected());
        }
    }

    private IEnumerator HandleDotCollected()
    {
        isTransitioning = true;

        // 1. Instantly turn green
        lineRenderer.startColor = new Color(0.1f, 0.9f, 0.1f);
        lineRenderer.endColor = new Color(0.1f, 0.9f, 0.1f);

        // 2. Play the collection audio clip if assigned
        if (collectionSound != null)
        {
            AudioSource.PlayClipAtPoint(collectionSound, transform.position, volume);
        }

        Debug.Log($"Target Dot {currentDotIndex + 1} Cleared!");

        // 3. Stay green for a brief moment (0.3 seconds)
        yield return new WaitForSeconds(0.3f);

        // 4. Move to the next dot position index
        currentDotIndex++;
        isTransitioning = false;

        // 5. Update position or finish phase
        MoveToCurrentTarget();
    }

    void ConfigureLineRenderer()
    {
        lineRenderer.positionCount = segments + 1;
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = 0.06f;
        lineRenderer.endWidth = 0.06f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    }

    void DrawDotCircle()
    {
        float deltaTheta = (2f * Mathf.PI) / segments;
        float theta = 0f;

        for (int i = 0; i < segments + 1; i++)
        {
            float x = dotRadius * Mathf.Cos(theta);
            float y = dotRadius * Mathf.Sin(theta);
            lineRenderer.SetPosition(i, new Vector3(x, y, 5f));
            theta += deltaTheta;
        }
    }

    // --- NEW DEBUG GIZMOS CODE ---
    void OnDrawGizmos()
    {
        // 1. Draw a preview of ALL the target paths in a semi-transparent red
        if (targetPositions != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.35f); // Soft transparent red
            foreach (Transform targetPos in targetPositions)
            {
                if (targetPos != null)
                {
                    Gizmos.DrawWireSphere(targetPos.position, dotRadius);
                }
            }
        }
    }
}