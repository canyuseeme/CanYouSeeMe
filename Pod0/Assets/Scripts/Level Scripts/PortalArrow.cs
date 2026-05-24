using UnityEngine;

public class PortalArrow : MonoBehaviour
{
    [Header("Settings")]
    public Transform player;
    public string portalTag = "Portal";
    public float hoverOffset = 1.5f;

    private Transform targetPortal;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        // Hide the arrow at the start
        if (spriteRenderer != null) spriteRenderer.enabled = false;
    }

    void Update()
    {
        // 1. If we don't have a portal yet, try to find one
        if (targetPortal == null)
        {
            GameObject portalObj = GameObject.FindGameObjectWithTag(portalTag);
            if (portalObj != null)
            {
                targetPortal = portalObj.transform;
                if (spriteRenderer != null) spriteRenderer.enabled = true;
            }
            else
            {
                return;
            }
        }

        // 2. Hover above the player
        if (player != null)
        {
            transform.position = player.position + new Vector3(0, hoverOffset, 0);
        }

        // 3. Point at the portal
        Vector3 direction = targetPortal.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
    }
}