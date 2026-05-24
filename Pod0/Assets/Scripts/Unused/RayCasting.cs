using UnityEngine;
using System.Collections.Generic; // Added for the List

public class EnemyHandshakeAI : MonoBehaviour
{
    [Header("Targets")]
    public Transform targetPoint;

    [Header("Settings")]
    public float rotationSpeed = 15f;
    public float rayLength = 50f;
    public LayerMask wallLayer;
    public int rayCount = 20;
    public float fanAngle = 180f;

    [Header("Stuck Detection")]
    public float stuckCheckInterval = 0.1f;
    public float stuckDistanceThreshold = 0.01f;

    [Header("Movement Settings")]
    public float maxVelocity = 5f;    // The fastest the enemy can move
    public float acceleration = 20f;  // Rate of speed increase
    public float deceleration = 15f;  // Rate of speed decrease

    private Rigidbody2D rb;
    private Vector2 lastPosition;
    private float stuckTimer;
    private bool isActuallyStuck;
    private int intersectionIndex = 0; // To cycle through blue lines

    private EnemyShooter shooter;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        lastPosition = rb.position;
        shooter = GetComponent<EnemyShooter>();
    }

    void FixedUpdate()
    {
        if (targetPoint == null) return;

        Vector2 A = rb.position;
        Vector2 B = targetPoint.position;
        Vector2 directVec = B - A;
        Vector2 directDir = directVec.normalized;
        float distToTarget = directVec.magnitude;

        // 1. STUCK DETECTION (Optimized)
        float distMoved = Vector2.Distance(A, lastPosition);

        // Only run the timer if we are touching a wall AND barely moving
        if (isTouchingWall && distMoved < stuckDistanceThreshold)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= stuckCheckInterval)
            {
                isActuallyStuck = true;
                intersectionIndex++;
                stuckTimer = 0;
            }
        }
        else
        {
            // Reset if we are clear of walls or moving fine
            stuckTimer = 0;
            isActuallyStuck = false;

            // Optional: Reset intersectionIndex once we are totally clear and moving
            if (!isTouchingWall && distMoved > stuckDistanceThreshold) intersectionIndex = 0;
        }

        lastPosition = A;

        // 2. PINK LINE CHECK
        bool oldQueries = Physics2D.queriesStartInColliders;
        Physics2D.queriesStartInColliders = false;
        RaycastHit2D directHit = Physics2D.Raycast(A, directDir, distToTarget, wallLayer);
        Physics2D.queriesStartInColliders = oldQueries;

        Vector2 moveGoal = B; 
        bool pathIsBlocked = directHit.collider != null;

        // 3. GENERATE ALL SCOUTS (PRESERVING ALL VISUALS)
        Vector2[] eEnds = new Vector2[rayCount];
        Vector2[] tEnds = new Vector2[rayCount];
        bool[] eRayIntersects = new bool[rayCount];
        bool[] tRayIntersects = new bool[rayCount];
        
        // List to store ALL intersection points found this frame
        List<Vector2> allIntersections = new List<Vector2>();

        float startAngle = -fanAngle / 2f;
        float angleStep = fanAngle / (rayCount - 1);

        for (int i = 0; i < rayCount; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector2 eDir = Quaternion.Euler(0, 0, angle) * directDir;
            Vector2 tDir = Quaternion.Euler(0, 0, -angle) * (-directDir);

            RaycastHit2D eHit = Physics2D.Raycast(A, eDir, rayLength, wallLayer);
            RaycastHit2D tHit = Physics2D.Raycast(B, tDir, rayLength, wallLayer);

            eEnds[i] = eHit.collider ? eHit.point : A + (eDir * rayLength);
            tEnds[i] = tHit.collider ? tHit.point : B + (tDir * rayLength);
        }

        // FIND ALL INTERSECTIONS
        for (int i = 0; i < rayCount; i++)
        {
            for (int j = 0; j < rayCount; j++)
            {
                Vector2 intersectPoint;
                if (GetIntersectionPoint(A, eEnds[i], B, tEnds[j], out intersectPoint))
                {
                    eRayIntersects[i] = true;
                    tRayIntersects[j] = true;
                    allIntersections.Add(intersectPoint);
                }
            }
        }

        // 4. MOVEMENT LOGIC
        bool hasClearShot = !pathIsBlocked;

        if (!hasClearShot || isActuallyStuck)
        {
            if (allIntersections.Count > 0)
            {
                // We don't have a clear shot, so the goal is the intersection
                int finalIndex = intersectionIndex % allIntersections.Count;
                moveGoal = allIntersections[finalIndex];
            }
            else
            {
                // If no intersections found, default back to target B to keep searching
                moveGoal = B;
            }
        }
        else
        {
            // We HAVE a clear shot. Stop moving by setting goal to current position.
            moveGoal = rb.position;
        }

        // 5. PHYSICS & SHOOTING
        bool canStopHere = !pathIsBlocked && !isTouchingWall;

        if (canStopHere)
        {
            // --- DECELERATION LOGIC ---
            if (rb.linearVelocity.magnitude > 0.05f)
            {
                // Apply force in the opposite direction of current movement
                Vector2 brakeDir = -rb.linearVelocity.normalized;
                rb.AddForce(brakeDir * deceleration, ForceMode2D.Force);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }

            if (shooter != null) shooter.Shoot();
        }
        else
        {
            // --- ACCELERATION LOGIC ---
            // 1. Determine where we want to go and at what speed
            Vector2 desiredVelocity = (moveGoal - A).normalized * maxVelocity;

            // 2. Calculate the difference between current and desired velocity
            Vector2 velocityChange = desiredVelocity - rb.linearVelocity;

            // 3. Apply acceleration to bridge that gap
            // We use ForceMode2D.Force which accounts for mass (F = m * a)
            Vector2 accelForce = velocityChange.normalized * acceleration;

            rb.AddForce(accelForce, ForceMode2D.Force);

            // 4. Manual Velocity Cap (Safety)
            if (rb.linearVelocity.magnitude > maxVelocity)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;
            }
        }

        // Always rotate to face the target B
        Vector2 lookDir = (B - A).normalized;
        float targetAngle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
        rb.MoveRotation(Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, targetAngle), rotationSpeed * Time.fixedDeltaTime));

        // 6. DEBUG VISUALS (FULLY PRESERVED)
        // Pink line
        Debug.DrawLine(A, directHit.collider ? directHit.point : B, Color.magenta);
        
        // Green/Cyan rays
        for (int i = 0; i < rayCount; i++)
        {
            Debug.DrawLine(A, eEnds[i], eRayIntersects[i] ? Color.cyan : Color.green);
            Debug.DrawLine(B, tEnds[i], tRayIntersects[i] ? Color.cyan : Color.green);
        }
    }

    private bool GetIntersectionPoint(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.zero;
        float den = (p4.y - p3.y) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.y - p1.y);
        if (den == 0) return false;
        float ua = ((p4.x - p3.x) * (p1.y - p3.y) - (p4.y - p3.y) * (p1.x - p3.x)) / den;
        float ub = ((p2.x - p1.x) * (p1.y - p3.y) - (p2.y - p1.y) * (p1.x - p3.x)) / den;
        if (ua >= 0 && ua <= 1 && ub >= 0 && ub <= 1)
        {
            intersection = p1 + ua * (p2 - p1);
            return true;
        }
        return false;
    }

    private bool isTouchingWall;

    void OnCollisionStay2D(Collision2D collision)
    {
        // Check if what we are touching is on the wall layer
        if (((1 << collision.gameObject.layer) & wallLayer) != 0)
        {
            isTouchingWall = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & wallLayer) != 0)
        {
            isTouchingWall = false;
        }
    }
}