using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public static class Utils
{
    private static System.Random rng = new System.Random();
    /// <summary> Gets the value of specified key. Simply returns the default value if dic or key are null or specified key does not exists.</summary>
    public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dic, TKey key, TValue defaultValue = default(TValue))
    {
        return (dic != null && key != null && dic.TryGetValue(key, out TValue value)) ? value : defaultValue;
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public static Wave JoinWaves(List<Wave> waves)
    {
        if (waves.Count == 1)
        {
            return waves[0];
        }
        List<WaveUnit> units = new List<WaveUnit>();
        int newCost = 0;
        float duration = 0;
        for (int i = 0; i < waves.Count; i++)
        {
            foreach (WaveUnit u in waves[i].units)
            {
                units.Add((WaveUnit)u.Clone());
            }
            if (waves[i].duration > duration)
            {
                duration = waves[i].duration;
            }

            newCost += waves[i].cost;
        }

        units.OrderBy(x => x.time);

        return new Wave(newCost, duration, 1, 0, units);
    }

    public static GameObject FindClosestWithTag(Vector2 position, string tag, out float distance)
    {
        GameObject[] gos;
        gos = GameObject.FindGameObjectsWithTag(tag);
        GameObject closest = null;
        distance = Mathf.Infinity;
        Vector2 othergo;
        foreach (GameObject go in gos)
        {
            othergo = go.transform.position;
            Vector2 diff = othergo - position;
            float curDistance = diff.sqrMagnitude;
            if (curDistance < distance)
            {
                closest = go;
                distance = curDistance;
            }
        }
        return closest;
    }

    public static GameObject FindClosestRigidbodyWithTag(GameObject original, Vector2 position, string tag, out float distance)
    {
        GameObject[] gos;
        gos = GameObject.FindGameObjectsWithTag(tag);
        GameObject closest = null;
        distance = Mathf.Infinity;
        Rigidbody2D originalRigidbody = original.GetComponent<Rigidbody2D>();
        foreach (GameObject go in gos)
        {
            float curDistance = originalRigidbody.Distance(go.GetComponent<Collider2D>()).distance;
            if (curDistance < distance)
            {
                closest = go;
                distance = curDistance;
            }
        }
        return closest;
    }

    public static Quaternion ClampRotation(Quaternion q, Vector3 bounds)
    {
        q.x /= q.w;
        q.y /= q.w;
        q.w = 1.0f;

        float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);
        angleX = Mathf.Clamp(angleX, -bounds.x, bounds.x);
        q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

        float angleY = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.y);
        angleY = Mathf.Clamp(angleY, -bounds.y, bounds.y);
        q.y = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleY);

        return q;
    }

    public static Vector3 GetVelocityBetweenVectors(Vector3 a, Vector3 b)
    {
        float angle = Mathf.Atan2(b.y - a.y, b.x - a.x);
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), a.z);
    }

    public static Vector2 GetVelocityAngle(float a)
    {
        float angle = a * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    public static Vector3 RandomVectorInCircle(float xrange, float yrange)
    {
        Vector2 r = UnityEngine.Random.insideUnitCircle;
        return new Vector3(r.x * xrange, r.y * yrange, 0);
    }

    public static float GetAngleBetweenVectors(Vector2 a, Vector2 b)
    {
        Vector2 toOther = (a - b).normalized;
        float angle = Mathf.Atan2(toOther.y, toOther.x) * Mathf.Rad2Deg + 180;
        return angle;
    }

    public static float RandomVariateAngle(float a, float intensity)
    {
        return a + UnityEngine.Random.Range(-intensity, intensity);
    }
}