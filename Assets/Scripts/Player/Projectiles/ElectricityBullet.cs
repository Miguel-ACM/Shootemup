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
        if (Time.deltaTime <= 0)
            return;
        if (!shoot)
        {

            List<AEnemy> hitEnemies;
            lineDrawer.SetCoatings(PlayerShotPalette.Burning, PlayerShotPalette.Toxic);
            float scale = Mathf.Sqrt(Mathf.Max(.01f, transform.localScale.x));
            float width = .09f * Mathf.Clamp(scale, .65f, 1.8f);
            List<Vector2> path = GetPoints(transform.position, width, Mathf.Clamp(scale, .8f, 1.45f), out hitEnemies);
            lineDrawer.DrawLine(path.ToArray(), transform.position.z, width, true);
            Vector3? lastContact = null;
            foreach (AEnemy enemy in hitEnemies)
            {
                if (enemy == null || !enemy.CanBeTargeted)
                    continue;
                Vector3 at = enemy.transform.position;
                lastContact = at;
                int point = path.IndexOf((Vector2)at);
                Vector2 incoming = point > 0 ? path[point] - path[point - 1] : Vector2.up;
                SparkPlug.ArcContact(enemy);
                if (CombatHit.Apply(enemy, damage, at, incoming, score: false, energy: true) > 0)
                    SoundManager.PlayGated("hitElectric", .025f, .1f);
            }
            if (lastContact.HasValue)
                SparkPlug.ExtraHop(lastContact.Value, hitEnemies, damage, score: false);
            shoot = true;
            Invoke("Delete", 0.1f);
        }

    }

    private void Delete()
    {
        Destroy(lineDrawerObject);
        Destroy(gameObject);
    }

    private List<Vector2> GetPoints(Vector2 origPoint, float width, float reachScale, out List<AEnemy> hitEnemies)
    {
        Vector2 currPoint = origPoint;
        List<Vector2> path = new List<Vector2>();
        hitEnemies = new List<AEnemy>();
        float radius = (1f + followness * 2) * reachScale;
        float currAngle = 90f;
        float angleLimitLeft = 175f;
        float angleLimitRight = 5f;
        float nextAngle;
        float distance;
        Vector2 nextPoint;
        path.Add(origPoint);
        float i = 0;
        while (currPoint.x > Camera.LeftLimit - 1f && currPoint.x < Camera.RightLimit + 1f && currPoint.y < Camera.UpLimit + 2f)
        {
            float minDistance = float.PositiveInfinity;
            nextAngle = currAngle;
            nextPoint = new Vector2(currPoint.x + Mathf.Cos(Mathf.Deg2Rad * currAngle) * radius,
                                    currPoint.y + Mathf.Sin(Mathf.Deg2Rad * currAngle) * radius);
            AEnemy hitObject = null;
            foreach (AEnemy candidate in AEnemy.Live)
            {
                if (candidate == null || !candidate.CanBeTargeted || hitEnemies.Contains(candidate) ||
                    ((Vector2)candidate.transform.position - currPoint).sqrMagnitude > radius * radius)
                {
                    continue;
                }
                float angle = Utils.GetAngleBetweenVectors(currPoint, candidate.transform.position);
                if (angle < angleLimitLeft && angle > angleLimitRight)
                {
                    distance = Vector2.Distance(currPoint, candidate.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nextPoint = candidate.transform.position;
                        nextAngle = angle;
                        hitObject = candidate;
                    }
                }
            }
            if (CombatWalls.SweepShot(currPoint, nextPoint, width * .5f, out var wall, out _, out var block))
            {
                path.Add(wall);
                if (block != null && block.CanReceiveDamage)
                    hitEnemies.Add(block);
                break;
            }
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

    void OnDestroy()
    {
        if (lineDrawerObject != null)
            Destroy(lineDrawerObject);
    }
}
