using UnityEngine;

public class StuckDetector : MonoBehaviour
{
    public float stuckCheckInterval = 0.1f;
    public float stuckDistanceThreshold = 0.01f;
    public LayerMask wallLayer;

    public bool IsTouchingWall { get; private set; }
    public bool IsActuallyStuck { get; private set; }

    private float stuckTimer;
    private Vector2 lastPosition;

    public void UpdateStuckStatus(Vector2 currentPos, Rigidbody2D rb)
    {
        float distMoved = Vector2.Distance(currentPos, lastPosition);

        if (IsTouchingWall && distMoved < stuckDistanceThreshold)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= stuckCheckInterval)
            {
                IsActuallyStuck = true;
                stuckTimer = 0;
            }
        }
        else
        {
            stuckTimer = 0;
            IsActuallyStuck = false;
        }
        lastPosition = currentPos;
    }

    public void ResetStuckStatus() => IsActuallyStuck = false;

    void OnCollisionStay2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & wallLayer) != 0) IsTouchingWall = true;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & wallLayer) != 0) IsTouchingWall = false;
    }
}