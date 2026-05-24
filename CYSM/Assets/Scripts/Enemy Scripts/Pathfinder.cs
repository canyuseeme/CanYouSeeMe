using UnityEngine;
using System.Collections.Generic;

public class Pathfinder : MonoBehaviour
{
    public float rayLength = 50f;
    public float fanAngle = 180f;
    public int rayCount = 20;
    public LayerMask wallLayer;

    public bool CheckLineOfSight(Vector2 start, Vector2 target, float dist)
    {
        bool oldQueries = Physics2D.queriesStartInColliders;
        Physics2D.queriesStartInColliders = false;
        RaycastHit2D hit = Physics2D.Raycast(start, (target - start).normalized, dist, wallLayer);
        Physics2D.queriesStartInColliders = oldQueries;
        return hit.collider == null;
    }

    public void GetHandshakePoints(Vector2 A, Vector2 B, Vector2 directDir, out List<Vector2> intersections, out Vector2[] eEnds, out Vector2[] tEnds, out bool[] eIntersects, out bool[] tIntersects)
    {
        intersections = new List<Vector2>();
        eEnds = new Vector2[rayCount];
        tEnds = new Vector2[rayCount];
        eIntersects = new bool[rayCount];
        tIntersects = new bool[rayCount];

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

        for (int i = 0; i < rayCount; i++)
        {
            for (int j = 0; j < rayCount; j++)
            {
                Vector2 intersectPoint;
                if (GetIntersectionPoint(A, eEnds[i], B, tEnds[j], out intersectPoint))
                {
                    eIntersects[i] = true;
                    tIntersects[j] = true;
                    intersections.Add(intersectPoint);
                }
            }
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
}