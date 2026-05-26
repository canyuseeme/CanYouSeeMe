using UnityEngine;
using UnityEngine.Rendering;

public class Bullet : MonoBehaviour
{
    public float speed = 12f;
    public float lifeTime = 3f;
    private Rigidbody2D rb;
    private bool hasCollided = false;

    public GameObject bulletHole;

    public AudioClip fireSound; 
    [Range(0f, 1f)] public float volume = 1f;
    public AudioClip deathSound;
    public AudioClip hitSound; 
    [Range(0f, 1f)] public float volume2 = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (fireSound != null) AudioSource.PlayClipAtPoint(fireSound, transform.position, volume);

        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasCollided) return;

        if (other.CompareTag("Boundary2")) 
        {
            return; 
        }

        hasCollided = true;

        //Debug.Log("COLLISION");

        if (other.CompareTag("Enemy"))
        {
            EnemyBrain brain = other.GetComponent<EnemyBrain>();
            EnemyBrainMelee brainMelee = other.GetComponent<EnemyBrainMelee>();
            
            if (brain != null || brainMelee != null)
            {
                bool isDead = false;

                if (brain != null)
                {
                    isDead = brain.TakeDamage(1);
                }
                else if (brainMelee != null)
                {
                    isDead = brainMelee.TakeDamage(1);
                }
                
                if (isDead)
                {
                    if (deathSound != null) AudioSource.PlayClipAtPoint(deathSound, transform.position, volume2);
                    Debug.Log("Enemy Killed");
                }
                else
                {
                    if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, transform.position, volume2/2);
                    Debug.Log("Enemy Hit");
                }
            }

            Destroy(gameObject);
            return; 
        }

        // Hit something else (Wall, Boundary, etc.) - Accuracy ruined!
        if (GameManager.Instance != null)
        {
            Instantiate(bulletHole, transform.position, transform.rotation);
            GameManager.Instance.star2Eligible = false;
        }
        
        Destroy(gameObject);
    }
}