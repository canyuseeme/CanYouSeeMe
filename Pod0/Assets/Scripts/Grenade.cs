using UnityEngine;

public class Grenade : MonoBehaviour
{
    public float speed = 12f;
    public float lifeTime = 3f;
    private Rigidbody2D rb;

    [Header("Settings")]
    public GameObject bulletPrefab;
    
    public float stopThreshold = 0.001f;

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

    void FixedUpdate()
    {
        // Check if the object has practically stopped moving
        if (rb.linearVelocity.magnitude <= stopThreshold)
        {
            SpawnCompassBullets();

            // Optional: Destroy this object after it fires its payload
            Destroy(gameObject);
        }
    }

    void SpawnCompassBullets()
    {
        Vector2[] compassDirections = new Vector2[]
        {
            Vector2.up,
            new Vector2(1f, 1f).normalized,
            Vector2.right,
            new Vector2(1f, -1f).normalized,
            Vector2.down,
            new Vector2(-1f, -1f).normalized,
            Vector2.left,
            new Vector2(-1f, 1f).normalized
        };

        foreach (Vector2 dir in compassDirections)
        {
            // Calculate the rotation needed to face the direction (assuming your bullet asset faces Up by default)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; 
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // Spawn the bullet with the correct rotation
            GameObject bullet = Instantiate(bulletPrefab, transform.position, rotation);

            // Clean up
            Destroy(bullet, 3f);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {

        if (other.CompareTag("Boundary2"))
        {
            return;
        }

        Destroy(gameObject);
    }
}