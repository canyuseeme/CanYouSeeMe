using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 12f;
    public float lifeTime = 3f;
    private Rigidbody2D rb;

    public GameObject blood;

    public AudioClip fireSound;
    [Range(0f, 1f)] public float volume = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = transform.up * speed;

        if (fireSound != null)
        {
            AudioSource.PlayClipAtPoint(fireSound, transform.position, volume);
        }

        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if we hit the player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player hit");

            // Tell the manager the player was hit
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseLife();
            }

            //BLOOD
            Instantiate(blood, transform.position, transform.rotation);

            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Boundary2"))
        {
            return;
        }

        Destroy(gameObject);
    }
}