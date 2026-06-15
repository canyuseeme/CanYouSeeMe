using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum AIState { Idle, Active, Alert, Forfeit, Bait }

public class EnemyBrain : MonoBehaviour
{
    public bool isNV = false; //isNightVisionEnemy
    private Camera mainCamera;
    private Collider2D enemyCollider;

    [Header("Health Settings")]
    public int startingLives = 1;
    private int currentLives;
    public List<Sprite> healthSprites; // Drop your sprites here in order: 1 life, 2 lives, 3 lives...
    private SpriteRenderer spriteRenderer;

    [Header("State Machine")]
    public AIState currentState = AIState.Idle;
    public float thresholdX = 0.1f; 
    public float thresholdY = 0.01f; 
    public float drift = 0.25f;

    [Header("Bait Settings")]
    [Range(0f, 1f)] public float baitProbability = 0.15f; 
    public float baitDuration1 = 0.5f; 
    public float baitDuration2 = 2.0f; 
    public float baitSpeed = 10f;      

    [Header("Reaction Settings (Near Miss)")]
    public float nearMissRadius = 2.0f;
    public float reactionCooldown = 1.5f; //Prevents constant twitching
    public float maxAimOffset = 30f;
    private float lastReactionTime = 0f;
    private bool isReacting = false;

    [Header("Targets")]
    public Transform targetPoint;
    private VisibilityController playerVisibility;
    private Vector2 lastKnownPosition;
    private Vector2 spawnPoint;

    [Header("Leash Settings")]
    public float leashRadius = 10f;

    private EnemyMotor2D motor;
    private Pathfinder pathfinder;
    private StuckDetector stuckDetector;
    private EnemyShooter shooter;
    private Rigidbody2D rb;

    private int intersectionIndex = 0;
    private float alertTimer = 0f;
    private float idleTimer = 0f; 

    [Header("Alert Settings")]
    public float alertLimit = 5f;

    private Vector2 alertMoveDir;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        motor = GetComponent<EnemyMotor2D>();
        pathfinder = GetComponent<Pathfinder>();
        stuckDetector = GetComponent<StuckDetector>();
        shooter = GetComponent<EnemyShooter>();

        mainCamera = Camera.main;
        enemyCollider = GetComponent<Collider2D>();

        spawnPoint = rb.position;

        if (targetPoint != null)
        {
            playerVisibility = targetPoint.GetComponent<VisibilityController>();
        }

        currentLives = startingLives;
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateCharacterSprite();
    }

    void FixedUpdate()
    {
        if (targetPoint == null || playerVisibility == null) return;

        // --- NEAR MISS DETECTION ---
        if (!isReacting && Time.time >= lastReactionTime + reactionCooldown)
        {
            CheckForNearMisses();
        }

        // If the enemy is currently in a "Near Miss" reaction, freeze the normal brain logic
        if (isReacting) return; 
        // ---------------------------

        float vis = playerVisibility.CurrentVisibility;
        Debug.Log(vis);
        Vector2 A = rb.position;
        Vector2 B = targetPoint.position;

        switch (currentState)
        {
            case AIState.Idle:
                motor.Brake();
                
                idleTimer += Time.fixedDeltaTime;
                if (idleTimer >= 1f)
                {
                    idleTimer = 0f;
                    if (Random.value <= baitProbability)
                    {
                        StartCoroutine(BaitRoutine());
                    }
                }

                if (vis > thresholdX) TransitionToActive();
                break;

            case AIState.Bait:
                if (vis > thresholdY)
                {
                    TransitionToActive();
                }
                break;

            case AIState.Active:
                RunOriginalLogic(A, B);
                lastKnownPosition = B; 
                
                if (vis < thresholdY) TransitionToAlert();
                break;

            case AIState.Alert:
                alertTimer += Time.fixedDeltaTime;

                //motor.FaceTarget(rb.position + rb.linearVelocity, rb.position); //unnecessary
                motor.FaceTarget(A + alertMoveDir, A);

                if (vis > thresholdY) TransitionToActive();
                if (alertTimer >= alertLimit) StartCoroutine(ForfeitRoutine());
                break;

            case AIState.Forfeit:
                if (vis > thresholdX)
                {
                    TransitionToActive();
                }
                break;
        }

        DrawDebugCircle(spawnPoint, leashRadius, Color.red);
    }

    // --- REACTION LOGIC ---
    private void CheckForNearMisses()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(rb.position, nearMissRadius);
        foreach (var hit in hits)
        {
            // Requires the player's bullet prefab to be tagged "PlayerBullet"
            if (hit.CompareTag("PlayerBullet"))
            {
                StartCoroutine(ReactionRoutine());
                break;
            }
        }
    }

    private IEnumerator ReactionRoutine()
    {
        isReacting = true;
        lastReactionTime = Time.time;

        // 1. Slam on the brakes
        motor.Brake();

        // 2. Calculate a fast, slightly inaccurate snap toward the player
        Vector2 trueDir = (Vector2)targetPoint.position - rb.position;
        float randomAngle = Random.Range(-maxAimOffset, maxAimOffset);
        Vector2 offsetDir = Quaternion.Euler(0, 0, randomAngle) * trueDir;
        Vector2 lookSpot = rb.position + offsetDir;

        // 3. Snap aim and shoot
        motor.FaceTarget(lookSpot, rb.position);
        yield return new WaitForSeconds(0.15f); // Micro-pause to let the player register the snap
        
        if (shooter != null && IsTargetVisibleToCamera()) shooter.Shoot();
        
        yield return new WaitForSeconds(0.2f); // Recoil pause before carrying on

        isReacting = false;
    }

    // --- STATE TRANSITIONS ---
    private void TransitionToActive()
    {
        StopAllCoroutines(); 
        isReacting = false; // Safety reset
        idleTimer = 0f;
        currentState = AIState.Active;
    }

    private void TransitionToAlert()
    {
        currentState = AIState.Alert;
        alertTimer = 0f;
        
        alertMoveDir = Random.insideUnitCircle.normalized;
        rb.linearVelocity = alertMoveDir * drift; 
    }

    // --- COROUTINES ---
    private IEnumerator BaitRoutine()
    {
        currentState = AIState.Bait;
        
        float timer = 0f;
        Vector2 dashDirection = transform.up; 
        
        // Phase 1: Dash
        while (timer < baitDuration1)
        {
            if (currentState != AIState.Bait) yield break; 
            
            if (!isReacting) 
            {
                // Calculate a point far in front of where we are facing
                Vector2 dashTarget = rb.position + (Vector2)transform.up * 50f;
                
                // Use the motor! This allows for natural acceleration.
                // We pass 'dashSpeed' to the motor if your motor script supports speed overrides,
                // otherwise it will just use its max acceleration to get there.
                motor.MoveTo(dashTarget, rb.position); 
                
                timer += Time.fixedDeltaTime;
            }
            yield return new WaitForFixedUpdate();
        }

        // Phase 2: Natural Invisibility via Braking
        if (currentState == AIState.Bait)
        {
            timer = 0f;
            while (timer < baitDuration2)
            {
                if (currentState != AIState.Bait) yield break;
                
                if (!isReacting)
                {
                    motor.Brake(); // Braking to 0 speed naturally drops visibility!
                    timer += Time.fixedDeltaTime;
                }
                yield return new WaitForFixedUpdate();
            }
        }

        if (currentState == AIState.Bait)
        {
            idleTimer = 0f;
            currentState = AIState.Idle;
        }
    }

    private IEnumerator ForfeitRoutine()
    {
        currentState = AIState.Forfeit;
        motor.Brake();

        for (int i = 0; i < 3; i++)
        {
            // Pause forfeit shooting if they are currently doing a Near-Miss reaction
            yield return new WaitUntil(() => !isReacting);

            Vector2 spread = Random.insideUnitCircle * 1.2f;
            Vector2 targetWithSpread = lastKnownPosition + spread;
            float distToSpot = Vector2.Distance(rb.position, targetWithSpread);

            motor.FaceTarget(targetWithSpread, rb.position);
            yield return new WaitForSeconds(0.3f); 

            if (pathfinder.CheckLineOfSight(rb.position, targetWithSpread, distToSpot))
            {
                if (shooter != null && IsTargetVisibleToCamera()) shooter.Shoot();
            }
            
            yield return new WaitForSeconds(0.5f); 
        }

        currentState = AIState.Idle;
    }

    private void RunOriginalLogic(Vector2 A, Vector2 B)
    {
        Vector2 anchorPos = spawnPoint;
        Vector2 directVec = B - A;
        Vector2 directDir = directVec.normalized;
        float distToTarget = directVec.magnitude;

        float playerDistFromAnchor = Vector2.Distance(B, anchorPos);
        bool isPlayerInLeash = playerDistFromAnchor <= leashRadius;

        stuckDetector.UpdateStuckStatus(A, rb);
        if (stuckDetector.IsActuallyStuck)
        {
            intersectionIndex++;
            stuckDetector.ResetStuckStatus();
        }

        bool hasClearShot = pathfinder.CheckLineOfSight(A, B, distToTarget);
        Vector2 moveGoal = B;

        pathfinder.GetHandshakePoints(A, B, directDir, out List<Vector2> allIntersections, out Vector2[] eEnds, out Vector2[] tEnds, out bool[] eRayIntersects, out bool[] tIntersects);

        if (!isPlayerInLeash)
        {
            motor.Brake();
        }
        else
        {
            if (!hasClearShot || stuckDetector.IsActuallyStuck)
            {
                if (allIntersections.Count > 0)
                {
                    int finalIndex = intersectionIndex % allIntersections.Count;
                    moveGoal = allIntersections[finalIndex];
                }
            }
            else
            {
                moveGoal = A; 
            }

            bool canStopHere = hasClearShot && !stuckDetector.IsTouchingWall;
            if (canStopHere) motor.Brake();
            else motor.MoveTo(moveGoal, A);
        }

        motor.FaceTarget(B, A);
        if (hasClearShot && shooter != null && IsTargetVisibleToCamera()) 
        {
            shooter.Shoot();
        }
    }

    //isNV
    private bool IsTargetVisibleToCamera()
    {
        // If the restriction isn't active, or we are missing references, default to allowing shooting
        if (!isNV) return true;
        if (mainCamera == null || enemyCollider == null) return true;

        // Calculate the 6 planes of the camera's viewing area
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        
        // Check if the enemy's collider bounds intersect with the camera view
        return GeometryUtility.TestPlanesAABB(planes, enemyCollider.bounds);
    }

    // --- HEALTH & DAMAGE ---
    public bool TakeDamage(int damage)
    {
        currentLives -= damage;
        
        if (currentLives <= 0)
        {
            Die();
            return true; 
        }
        
        UpdateCharacterSprite();
        StartCoroutine(Flash());
        return false; 
    }

    private IEnumerator Flash()
    {
        VisibilityController myVisibility = GetComponent<VisibilityController>();
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        
        if (myVisibility != null && sr != null)
        {
            myVisibility.enabled = false;

            Color enemyColor = sr.color;
            enemyColor.a = 1.0f; //flash
            sr.color = enemyColor;
            yield return new WaitForSeconds(0.1f);
            
            myVisibility.enabled = true;
        }
    }

    private void UpdateCharacterSprite()
    {
        if (spriteRenderer == null || healthSprites == null || healthSprites.Count == 0) return;

        // Map current lives to the list index (e.g., 3 lives = index 2, 1 life = index 0)
        int spriteIndex = currentLives - 1;

        if (spriteIndex >= 0 && spriteIndex < healthSprites.Count)
        {
            spriteRenderer.sprite = healthSprites[spriteIndex];
        }
    }

    private void Die()
    {
        VisibilityController visScript = GetComponent<VisibilityController>();
        if (visScript != null) visScript.enabled = false;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color enemyColor = sr.color;
            enemyColor.a = 1.0f; //flash
            sr.color = enemyColor;
        }

        Destroy(gameObject, 0.1f); //Slight delay to allow flash upon death
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = Application.isPlaying ? (Vector3)spawnPoint : transform.position;
        Gizmos.DrawWireSphere(center, leashRadius);

        // Draw the Near Miss radius so you can adjust it easily
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, nearMissRadius);
    }

    private void DrawDebugCircle(Vector2 center, float radius, Color color)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            Vector2 p1 = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius;
            Vector2 p2 = center + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * radius;
            Debug.DrawLine(p1, p2, color);
        }
    }
}