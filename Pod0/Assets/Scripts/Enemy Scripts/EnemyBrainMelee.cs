using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum MeleeAIState { Idle, Active, Alert, Forfeit, Bait }

public class EnemyBrainMelee : MonoBehaviour
{
    [Header("Health Settings")]
    public int startingLives = 1;
    private int currentLives;
    public List<Sprite> healthSprites; // Drop your sprites here in order: 1 life, 2 lives, 3 lives...
    private SpriteRenderer spriteRenderer;

    [Header("Attack Settings")]
    public float attackCooldown = 1.5f; // Time in seconds between hits
    private float lastAttackTime = 0f;

    [Header("State Machine")]
    public MeleeAIState currentState = MeleeAIState.Idle;
    public float thresholdX = 0.1f; 
    public float thresholdY = 0.01f; 
    public float drift = 0.25f;

    [Header("Bait Settings")]
    [Range(0f, 1f)] public float baitProbability = 0.15f; 
    public float baitDuration1 = 0.5f; 
    public float baitDuration2 = 2.0f; 

    [Header("Reaction Settings (Near Miss)")]
    public float nearMissRadius = 2.0f;
    public float reactionCooldown = 1.5f; 
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

        spawnPoint = rb.position;
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

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

        // If reacting to a gunshot, suspend normal brain logic
        if (isReacting) return; 

        float vis = playerVisibility.CurrentVisibility;
        Vector2 A = rb.position;
        Vector2 B = targetPoint.position;

        switch (currentState)
        {
            case MeleeAIState.Idle:
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

            case MeleeAIState.Bait:
                if (vis > thresholdY)
                {
                    TransitionToActive();
                }
                break;

            case MeleeAIState.Active:
                // Update last known position ONLY if we can actually see the player well enough
                if (vis > thresholdY) lastKnownPosition = B; 
                
                RunMeleeLogic(A, B, vis);
                break;

            case MeleeAIState.Alert:
                alertTimer += Time.fixedDeltaTime;

                // Fixed: Only applying one clean rotation toward the drift direction
                motor.FaceTarget(A + alertMoveDir, A);

                if (vis > thresholdY) TransitionToActive();
                if (alertTimer >= alertLimit) StartCoroutine(ForfeitRoutine());
                break;

            case MeleeAIState.Forfeit:
                if (vis > thresholdX)
                {
                    TransitionToActive();
                }
                break;
        }

        DrawDebugCircle(spawnPoint, leashRadius, Color.red);
    }

    // --- MELEE ATTACK COLLISION ---
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the collided object is the player
        if (collision.transform == targetPoint)
        {
            TryExecuteAttack();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Keeps checking if they stay stuck together
        if (collision.transform == targetPoint)
        {
            TryExecuteAttack();
        }
    }

    private void TryExecuteAttack()
    {
        // Only attack if enough time has passed since the last strike
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            
            // Trigger your game manager logic
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseLife();
            }
            
            // Optional: You could call motor.Brake() here for a tiny fraction 
            // of a second to simulate "hit recovery" bounce-back if you want!
        }
    }

    // --- REACTION LOGIC (Aggressive Turn) ---
    private void CheckForNearMisses()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(rb.position, nearMissRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("PlayerBullet"))
            {
                StartCoroutine(ReactionRoutine(hit.transform.position));
                break;
            }
        }
    }

    private IEnumerator ReactionRoutine(Vector2 bulletPos)
    {
        isReacting = true;
        lastReactionTime = Time.time;

        // 1. Slam on brakes
        motor.Brake();

        // 2. Snap instantly toward where the bullet came from
        motor.FaceTarget(bulletPos, rb.position);
        
        // 3. Brief micro-pause to process the threat, then immediately charge
        yield return new WaitForSeconds(0.2f); 
        
        TransitionToActive();
    }

    // --- STATE TRANSITIONS ---
    private void TransitionToActive()
    {
        StopAllCoroutines(); 
        isReacting = false; 
        idleTimer = 0f;
        currentState = MeleeAIState.Active;
    }

    private void TransitionToAlert()
    {
        currentState = MeleeAIState.Alert;
        alertTimer = 0f;
        
        alertMoveDir = Random.insideUnitCircle.normalized;
        rb.linearVelocity = alertMoveDir * drift; 
    }

    // --- COROUTINES ---
    private IEnumerator BaitRoutine()
    {
        currentState = MeleeAIState.Bait;
        
        float timer = 0f;
        
        // Phase 1: Dash
        while (timer < baitDuration1)
        {
            if (currentState != MeleeAIState.Bait) yield break; 
            
            if (!isReacting) 
            {
                Vector2 dashTarget = rb.position + (Vector2)transform.up * 50f;
                motor.MoveTo(dashTarget, rb.position); 
                timer += Time.fixedDeltaTime;
            }
            yield return new WaitForFixedUpdate();
        }

        // Phase 2: Natural Invisibility via Braking
        if (currentState == MeleeAIState.Bait)
        {
            timer = 0f;
            while (timer < baitDuration2)
            {
                if (currentState != MeleeAIState.Bait) yield break;
                
                if (!isReacting)
                {
                    motor.Brake(); 
                    timer += Time.fixedDeltaTime;
                }
                yield return new WaitForFixedUpdate();
            }
        }

        if (currentState == MeleeAIState.Bait)
        {
            idleTimer = 0f;
            currentState = MeleeAIState.Idle;
        }
    }

    private IEnumerator ForfeitRoutine()
    {
        currentState = MeleeAIState.Forfeit;
        motor.Brake();

        // Blind sweep: Investigate 3 random nearby points before giving up
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitUntil(() => !isReacting);

            Vector2 sweepSpot = lastKnownPosition + Random.insideUnitCircle * 2.5f;
            
            // Turn toward the noise/spot
            motor.FaceTarget(sweepSpot, rb.position);
            
            // Take a short, tentative step toward it
            motor.MoveTo(sweepSpot, rb.position);
            yield return new WaitForSeconds(0.6f); 
            
            motor.Brake();
            yield return new WaitForSeconds(0.4f);
        }

        currentState = MeleeAIState.Idle;
    }

    // --- RELENTLESS MELEE PURSUIT LOGIC ---
    private void RunMeleeLogic(Vector2 A, Vector2 B, float vis)
    {
        float playerDistFromAnchor = Vector2.Distance(B, spawnPoint);
        bool isPlayerInLeash = playerDistFromAnchor <= leashRadius;

        if (!isPlayerInLeash)
        {
            motor.Brake();
            return;
        }

        stuckDetector.UpdateStuckStatus(A, rb);
        if (stuckDetector.IsActuallyStuck)
        {
            intersectionIndex++;
            stuckDetector.ResetStuckStatus();
        }

        // If player is visible, charge them. If not, charge last known position.
        Vector2 targetPos = (vis > thresholdX) ? B : lastKnownPosition;
        float distToTarget = Vector2.Distance(A, targetPos);
        Vector2 directDir = (targetPos - A).normalized;

        bool hasClearPath = pathfinder.CheckLineOfSight(A, targetPos, distToTarget);
        Vector2 moveGoal = targetPos;

        if (!hasClearPath || stuckDetector.IsActuallyStuck)
        {
            pathfinder.GetHandshakePoints(A, targetPos, directDir, out List<Vector2> allIntersections, out Vector2[] eEnds, out Vector2[] tEnds, out bool[] eRayIntersects, out bool[] tIntersects);
            
            if (allIntersections != null && allIntersections.Count > 0)
            {
                int finalIndex = intersectionIndex % allIntersections.Count;
                moveGoal = allIntersections[finalIndex];
            }
        }

        // If we reached the last known spot and STILL can't see the player, start searching
        if (distToTarget < 0.05f && vis < thresholdY)
        {
            TransitionToAlert();
        }
        else
        {
            // Never brake! Run straight through them.
            motor.MoveTo(moveGoal, A);
            
            // Face where we are running, or face the player directly if we have LOS
            motor.FaceTarget(hasClearPath ? targetPos : moveGoal, A); 
        }
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
        if (myVisibility != null && spriteRenderer != null)
        {
            myVisibility.enabled = false;

            Color enemyColor = spriteRenderer.color;
            enemyColor.a = 1.0f; // Flash visible
            spriteRenderer.color = enemyColor;
            
            yield return new WaitForSeconds(0.1f);
            
            myVisibility.enabled = true;
        }
    }

    private void UpdateCharacterSprite()
    {
        if (spriteRenderer == null || healthSprites == null || healthSprites.Count == 0) return;

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

        if (spriteRenderer != null)
        {
            Color enemyColor = spriteRenderer.color;
            enemyColor.a = 1.0f;
            spriteRenderer.color = enemyColor;
        }

        Destroy(gameObject, 0.1f); 
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 center = Application.isPlaying ? (Vector3)spawnPoint : transform.position;
        Gizmos.DrawWireSphere(center, leashRadius);

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