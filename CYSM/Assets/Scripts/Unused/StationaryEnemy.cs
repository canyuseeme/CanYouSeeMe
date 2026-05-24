using UnityEngine;

public enum EnemyState { Scanning, LockedOn, HighAlert, BlindFire, LostTrack }

public class StationaryEnemy : MonoBehaviour
{
    [Header("Detection")]
    public Transform player;
    public LayerMask obstacleMask;
    public float detectionRange = 10f;
    public float viewAngle = 90f;
    public float visibilityThreshold = 0.01f;

    [Header("Frequency Settings")]
    public float slowestTick = 1.0f;
    public float fastestTick = 0.1f;

    [Header("High Alert Settings")]
    public float highAlertDuration = 3.0f;
    public float alertTickMultiplier = 0.1f;
    private float alertTimer;

    [Header("Blind Fire Settings")]
    public int blindShotsAmount = 3;
    public float sprayAngle = 15f;
    private int shotsFiredInBlindMode = 0;
    private Vector2 lastKnownPosition;

    [Header("Scanning & Shooting")]
    public float scanSpeed = 1f;
    public float scanRange = 45f;
    public GameObject enemyBulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.5f;

    private EnemyState currentState = EnemyState.Scanning;
    private float nextFireTime;
    private float frequencyTimer;
    private Quaternion initialRotation;
    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        initialRotation = transform.rotation;

        sr.color = new Color(1, 1, 1, 0);
    }

    void Update()
    {
        if (player == null) return;

        bool playerInSights = IsPlayerInCone();
        float playerAlpha = player.GetComponent<SpriteRenderer>().color.a;

        switch (currentState)
        {
            case EnemyState.Scanning:
                HandleScanning(playerInSights, playerAlpha);
                break;
            case EnemyState.LockedOn:
                HandleLockedOn(playerInSights, playerAlpha);
                break;
            case EnemyState.HighAlert:
                HandleHighAlert(playerInSights, playerAlpha);
                break;
            case EnemyState.BlindFire:
                HandleBlindFire();
                break;
            case EnemyState.LostTrack:
                HandleLostTrack();
                break;
        }
    }

    void HandleScanning(bool inSights, float alpha)
    {
        float angle = Mathf.Sin(Time.time * scanSpeed) * scanRange;
        transform.rotation = initialRotation * Quaternion.Euler(0, 0, angle);

        if (inSights && alpha > visibilityThreshold)
        {
            float currentTickRate = Mathf.Lerp(slowestTick, fastestTick, alpha);
            frequencyTimer += Time.deltaTime;

            if (frequencyTimer >= currentTickRate)
            {
                EnterLockedOn();
            }
        }
        else { frequencyTimer = 0; }
    }

    void EnterLockedOn()
    {
        frequencyTimer = 0;
        alertTimer = 0; // Reset alert for next time
        currentState = EnemyState.LockedOn;
    }

    void HandleLockedOn(bool inSights, float alpha)
    {
        lastKnownPosition = player.position;
        LookAt(lastKnownPosition, 10f);

        if (Time.time >= nextFireTime)
        {
            Shoot(0);
            nextFireTime = Time.time + fireRate;
        }

        if (!inSights || alpha <= visibilityThreshold)
        {
            // Transition to High Alert - Reset timers so they start fresh
            alertTimer = 0;
            frequencyTimer = 0;
            currentState = EnemyState.HighAlert;
        }
    }

    void HandleHighAlert(bool inSights, float alpha)
    {
        LookAt(lastKnownPosition, 15f); // Faster snap during alert

        if (inSights && alpha > visibilityThreshold)
        {
            // Aggressive re-detection
            float currentTickRate = Mathf.Lerp(slowestTick, fastestTick, alpha) * alertTickMultiplier;
            frequencyTimer += Time.deltaTime;

            if (frequencyTimer >= currentTickRate)
            {
                EnterLockedOn();
                return;
            }
        }

        alertTimer += Time.deltaTime;
        if (alertTimer >= highAlertDuration)
        {
            shotsFiredInBlindMode = 0;
            currentState = EnemyState.BlindFire;
        }
    }

    void HandleBlindFire()
    {
        LookAt(lastKnownPosition, 5f);

        if (Time.time >= nextFireTime)
        {
            float randomOffset = Random.Range(-sprayAngle, sprayAngle);
            Shoot(randomOffset);

            shotsFiredInBlindMode++;
            nextFireTime = Time.time + fireRate;

            if (shotsFiredInBlindMode >= blindShotsAmount)
            {
                frequencyTimer = 0;
                currentState = EnemyState.LostTrack;
            }
        }
    }

    void HandleLostTrack()
    {
        // Smoothly look at the last spot
        LookAt(lastKnownPosition, 5f);

        frequencyTimer += Time.deltaTime;
        if (frequencyTimer >= 1.5f)
        {
            // Finalize new scan center ONLY when we are done searching
            Vector2 dir = lastKnownPosition - (Vector2)transform.position;
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            initialRotation = Quaternion.Euler(0, 0, targetAngle);

            frequencyTimer = 0;
            currentState = EnemyState.Scanning;
        }
    }

    void LookAt(Vector2 target, float speed)
    {
        Vector2 dir = target - (Vector2)transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, angle), Time.deltaTime * speed);
    }

    void Shoot(float angleOffset)
    {
        if (enemyBulletPrefab == null || firePoint == null) return;
        Quaternion bulletRotation = transform.rotation * Quaternion.Euler(0, 0, angleOffset);
        Instantiate(enemyBulletPrefab, firePoint.position, bulletRotation);
    }
    bool IsPlayerInCone()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > detectionRange) return false;
        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        if (Vector2.Angle(transform.up, dirToPlayer) < viewAngle / 2f)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, detectionRange, obstacleMask);
            return (hit.collider != null && hit.collider.transform == player);
        }
        return false;
    }

    // GIZMOS: Visualizes the vision cone in the Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Vector3 leftBoundary = Quaternion.AngleAxis(-viewAngle / 2f, Vector3.forward) * transform.up;
        Vector3 rightBoundary = Quaternion.AngleAxis(viewAngle / 2f, Vector3.forward) * transform.up;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * detectionRange);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * detectionRange);

        if (currentState != EnemyState.Scanning)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastKnownPosition, 0.5f);
        }
    }
}