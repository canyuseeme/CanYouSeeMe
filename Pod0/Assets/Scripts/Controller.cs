using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Controller : MonoBehaviour
{
    [Header("Shooting")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 20f;

    [Header("Ammo Settings")]
    public int maxAmmo = 6;
    public float reloadTime = 2f;
    private int currentAmmo;
    private bool isReloading = false;

    public AudioClip reloadSound;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Throttle Settings")]
    public float maxSpeed = 1.5f;
    public float acceleration = 1.5f;
    public float deceleration = 4.5f;

    [Header("Dead Zone")]
    public float stopRadius = 0.125f;

    private float currentSpeed = 0f;
    private Rigidbody2D rb;
    private Camera mainCam;

    private Vector2 contactNormal;
    private bool isTouchingWall;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;

        currentAmmo = maxAmmo;
        if (SceneManager.GetActiveScene().name != "WinScreen" && SceneManager.GetActiveScene().name != "LoseScreen")
        {
            GameManager.Instance.UpdateAmmoUI(currentAmmo);
        }

        rb.gravityScale = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        // Force zero friction so we don't "stick" to walls
        PhysicsMaterial2D mat = new PhysicsMaterial2D("SlugMaterial");
        mat.friction = 0;
        mat.bounciness = 0;
        rb.sharedMaterial = mat;
    }

    void Update()
    {
        // 1. INPUT & TARGET DATA
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldMousePos = mainCam.ScreenToWorldPoint(mousePos);
        worldMousePos.z = 0;

        Vector2 vectorToMouse = (Vector2)worldMousePos - rb.position;
        float distanceToMouse = vectorToMouse.magnitude;

        // 2. MOVEMENT (THROTTLE) LOGIC
        if (distanceToMouse <= stopRadius)
        {
            // Inside dead zone: slow down to a stop
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.deltaTime);
        }
        else
        {
            if (Mouse.current.leftButton.isPressed || Keyboard.current.wKey.isPressed)
                currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.deltaTime);
            else if (Mouse.current.rightButton.isPressed || Keyboard.current.sKey.isPressed)
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.deltaTime);
        }

        // 3. ROTATION LOGIC (WITH WALL-SLIDE FILTER)
        Vector2 finalLookDirection = vectorToMouse;

        if (isTouchingWall)
        {
            float dot = Vector2.Dot(vectorToMouse.normalized, contactNormal);

            if (dot < 0f)
            {
                // Calculate the two possible directions parallel to the wall (tangents)
                Vector2 tangent1 = new Vector2(-contactNormal.y, contactNormal.x);
                Vector2 tangent2 = new Vector2(contactNormal.y, -contactNormal.x);

                if (Vector2.Dot(vectorToMouse, tangent1) > Vector2.Dot(vectorToMouse, tangent2))
                {
                    finalLookDirection = tangent1;
                }
                else
                {
                    finalLookDirection = tangent2;
                }
            }
        }

        float targetAngle = Mathf.Atan2(finalLookDirection.y, finalLookDirection.x) * Mathf.Rad2Deg - 90f;
        rb.rotation = targetAngle;

        // 4. SHOOTING & RELOADING
        if (Keyboard.current.rKey.wasPressedThisFrame && currentAmmo < maxAmmo && !isReloading)
        {
            StartCoroutine(Reload());
        }

        // Shooting Logic
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (isReloading)
            {
                
            }
            else if (currentAmmo > 0)
            {
                Shoot();
                currentAmmo--;
                GameManager.Instance.UpdateAmmoUI(currentAmmo);
            }
            else
            {
                StartCoroutine(Reload());
            }
        }

        GameObject.Find("NR").GetComponent<AudioSource>().volume = 0.1f * (currentSpeed/maxSpeed) + 0.1f;
    }

    void FixedUpdate()
    {
        Vector2 desiredMove = transform.up * currentSpeed;
        rb.linearVelocity = desiredMove;
    }

    // Capture the wall normal when we touch it
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            isTouchingWall = true;
            contactNormal = collision.contacts[0].normal;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            isTouchingWall = false;
        }
    }

    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        bulletRb.linearVelocity = firePoint.up * bulletSpeed;
        Destroy(bullet, 3f);
    }

    IEnumerator Reload()
    {
        isReloading = true;

        if (reloadSound != null)
        {
            AudioSource.PlayClipAtPoint(reloadSound, transform.position, volume);
        }

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;

        GameManager.Instance.UpdateAmmoUI(currentAmmo);

        isReloading = false;
    }
}