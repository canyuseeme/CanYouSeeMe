using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMotor2D : MonoBehaviour
{
    public float rotationSpeed = 15f;
    public float maxVelocity = 5f;
    public float acceleration = 20f;
    public float deceleration = 15f;

    private Rigidbody2D rb;
    private Animator anim; // <-- ADD THIS LINE HERE

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>(); // <-- ADD THIS LINE HERE
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public void MoveTo(Vector2 goal, Vector2 currentPos)
    {
        Vector2 desiredVelocity = (goal - currentPos).normalized * maxVelocity;
        Vector2 velocityChange = desiredVelocity - rb.linearVelocity;
        Vector2 accelForce = velocityChange.normalized * acceleration;

        rb.AddForce(accelForce, ForceMode2D.Force);

        if (rb.linearVelocity.magnitude > maxVelocity)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;
        }
    }

    public void Brake()
    {
        if (rb.linearVelocity.magnitude > 0.05f)
        {
            Vector2 brakeDir = -rb.linearVelocity.normalized;
            rb.AddForce(brakeDir * deceleration, ForceMode2D.Force);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void FaceTarget(Vector2 targetPos, Vector2 currentPos)
    {
        Vector2 lookDir = (targetPos - currentPos).normalized;
        float targetAngle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
        rb.MoveRotation(Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, targetAngle), rotationSpeed * Time.fixedDeltaTime));
    }

    void Update()
    {
        if (anim == null) return;

        // Get the enemy's current movement speed
        float currentSpeed = rb.linearVelocity.magnitude;

        // Since enemies have a maxVelocity of 5 (faster than the player's 1.5), 
        // we use a smaller multiplier (0.15f) so the animation doesn't look ridiculously fast.
        float finalAnimSpeed = 0.3f + (currentSpeed * 0.15f);

        // Send it to the enemy's Animator parameter (Assuming it's named "Speed")
        anim.SetFloat("Speed", finalAnimSpeed);
    }
}