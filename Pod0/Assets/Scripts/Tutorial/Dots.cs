using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dots : MonoBehaviour
{
    [Header("Positions / Dot Objects")]
    public List<Transform> targetPositions;

    [Header("Audio")]
    public AudioClip collectionSound;
    [Range(0f, 1f)] public float volume = 0.7f;

    private int currentDotIndex = 0;
    private bool isTransitioning = false;

    // Public property for TutorialManager
    public bool IsCompleted { get; private set; } = false;

    void Start()
    {
        if (targetPositions == null || targetPositions.Count == 0)
        {
            Debug.LogError("Dots Manager: No target positions assigned in the inspector!");
            IsCompleted = true;
            return;
        }

        // Initialize and configure all the pre-existing dots in the scene
        for (int i = 0; i < targetPositions.Count; i++)
        {
            if (targetPositions[i] != null)
            {
                GameObject dotGo = targetPositions[i].gameObject;

                // 1. Force the color to Red initially
                SpriteRenderer sr = dotGo.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.color = Color.red;
                }

                // 2. Ensure their colliders are set to triggers so bullets pass through/register
                Collider2D col = dotGo.GetComponent<Collider2D>();
                if (col != null)
                {
                    col.isTrigger = true;
                }

                // 3. Dynamically inject a proxy script so the dot can tell this manager it was hit
                DotCollisionProxy proxy = dotGo.AddComponent<DotCollisionProxy>();
                proxy.Initialize(this, i);
            }
        }
    }

    // Called by the proxy script when a bullet hits a specific dot
    public void OnDotTriggerEntered(int dotIndex, Collider2D other)
    {
        // Only respond if the bullet hit the CURRENT active dot in the sequence
        if (other.CompareTag("PlayerBullet") && dotIndex == currentDotIndex && !isTransitioning && !IsCompleted)
        {
            StartCoroutine(HandleDotCollected());
        }
    }

    private IEnumerator HandleDotCollected()
    {
        isTransitioning = true;

        Transform currentTarget = targetPositions[currentDotIndex];
        
        if (currentTarget != null)
        {
            // Turn it green
            SpriteRenderer sr = currentTarget.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(0.1f, 0.9f, 0.1f);
            }

            // Play audio at the dot's physical location
            if (collectionSound != null)
            {
                AudioSource.PlayClipAtPoint(collectionSound, currentTarget.position, volume);
            }

            Debug.Log($"Target Dot {currentDotIndex + 1} Shot and Cleared!");
        }

        // Wait a brief moment showing the green color before turning it off
        yield return new WaitForSeconds(0.3f);

        // Disable the object entirely
        if (currentTarget != null)
        {
            currentTarget.gameObject.SetActive(false);
        }

        currentDotIndex++;
        isTransitioning = false;

        // Check if that was the last dot in the sequence
        if (currentDotIndex >= targetPositions.Count)
        {
            IsCompleted = true;
            Debug.Log("All dots cleared! Tutorial criteria met.");
        }
    }
}

// A tiny helper class automatically added to your individual dots at runtime
public class DotCollisionProxy : MonoBehaviour
{
    private Dots manager;
    private int myIndex;

    public void Initialize(Dots managerScript, int index)
    {
        manager = managerScript;
        myIndex = index;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (manager != null)
        {
            manager.OnDotTriggerEntered(myIndex, other);
        }
    }
}