using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricityBullet : ABullet
{
    private GameObject lineDrawerObject;
    private LineDrawer lineDrawer;
    bool shoot;

    public void Awake()
    {
        lineDrawerObject = Instantiate(Resources.Load<GameObject>("Bullets/LineDrawer"));
        lineDrawer = lineDrawerObject.GetComponent<LineDrawer>();
       

    }

    public void Update()
    {
        if (!shoot)
        {

            List<Rigidbody2D> hitEnemies;
            List<Vector2> path = GetPoints(transform.position, out hitEnemies);
            lineDrawer.DrawLine(path.ToArray(), transform.position.z, 0.02f);
            foreach (Rigidbody2D rb in hitEnemies)
            {
                rb.gameObject.GetComponent<AEnemy>().damage(damage);
            }
            shoot = true;
            Invoke("Delete", 0.1f);
        }

    }

    private void Delete()
    {
        Destroy(lineDrawerObject);
        Destroy(gameObject);
    }

    private List<Vector2> GetPoints(Vector2 origPoint, out List<Rigidbody2D> hitEnemies)
    {
        Vector2 currPoint = origPoint;
        List<Vector2> path = new List<Vector2>();
        hitEnemies = new List<Rigidbody2D>();
        float radius = 1f + (followness * 2);
        float currAngle = 90f;
        float angleLimitLeft = 175f; float angleLimitRight = 5f;
        float nextAngle; float distance; Vector2 nextPoint;
        path.Add(origPoint);
        float i = 0;
        while (currPoint.x > Camera.LeftLimit - 1f && currPoint.x < Camera.RightLimit + 1f && currPoint.y < Camera.UpLimit + 2f)
        {
            Collider2D[] results = Physics2D.OverlapCircleAll(currPoint, radius, LayerMask.GetMask("Enemy"));
            float minDistance = float.PositiveInfinity;
            nextAngle = currAngle;
            nextPoint = new Vector2(currPoint.x + Mathf.Cos(Mathf.Deg2Rad * currAngle) * radius,
                                    currPoint.y + Mathf.Sin(Mathf.Deg2Rad * currAngle) * radius);
            Rigidbody2D hitObject = null;
            foreach (Collider2D col in results)
            {
                if (hitEnemies.Contains(col.attachedRigidbody))
                {
                    continue;
                }
                float angle = Utils.GetAngleBetweenVectors(currPoint, col.transform.position);
                if (angle < angleLimitLeft && angle > angleLimitRight)
                {
                    distance = Vector2.Distance(currPoint, col.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nextPoint = col.transform.position;
                        nextAngle = angle;
                        hitObject = col.attachedRigidbody;
                    }
                }
            }
            //results.
            if (hitObject != null)
            {
                hitEnemies.Add(hitObject);
            }
            currAngle = nextAngle;
            currPoint = nextPoint;
            angleLimitRight = Mathf.Max(5f, currAngle - 85f);
            angleLimitLeft = Mathf.Min(175f, currAngle + 85f);
            path.Add(currPoint);
            if (i > 1000)
                break;
            i++;
        }

        return path;
    }


    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}
