using System.Collections.Generic;
using UnityEngine;

public class Key : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The tag assigned to the Player GameObject")]
    public string playerTag = "Player";

    [Header("Targets")]
    [Tooltip("Drag all the walls you want to destroy here")]
    public List<GameObject> lockedWalls = new List<GameObject>();

    public AudioClip keySound;
    [Range(0f, 1f)] public float volume = 1f;

    void Start()
    {
        GetComponent<Rigidbody2D>().angularVelocity = 40f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object entering the trigger is the player
        if (other.CompareTag(playerTag))
        {
            if (keySound != null)
            {
                AudioSource.PlayClipAtPoint(keySound, transform.position, volume);
            }

            UnlockWalls();
        }
    }

    private void UnlockWalls()
    {
        // Loop through the list and destroy each wall
        foreach (GameObject wall in lockedWalls)
        {
            if (wall != null)
            {
                Destroy(wall);
            }
        }

        // Finally, destroy the key itself
        Destroy(gameObject);
    }
}