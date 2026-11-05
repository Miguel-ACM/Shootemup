using UnityEngine;

public static class BeamContact
{
    public static bool Touches(AEnemy enemy, Vector2 start, Vector2 end, float radius, out Vector2 point)
    {
        point = start;
        if (enemy == null) return false;
        var shapes = enemy.DamageShapes;
        for (int i = 0; i < shapes.Count; i++)
            if (Touches(shapes[i], start, end, radius, out point)) return true;
        return false;
    }
    public static bool Touches(Collider2D collider, Vector2 start, Vector2 end, float radius, out Vector2 point)
    {
        point = start;
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
            return false;
        Vector2 segment = end - start;
        float lengthSquared = Mathf.Max(.000001f, segment.sqrMagnitude);
        point = start + segment * Mathf.Clamp01(Vector2.Dot((Vector2)collider.bounds.center - start, segment) / lengthSquared);
        for (int i = 0; i < 3; i++)
        {
            Vector2 surface = collider.ClosestPoint(point);
            point = start + segment * Mathf.Clamp01(Vector2.Dot(surface - start, segment) / lengthSquared);
        }
        return (collider.ClosestPoint(point) - point).sqrMagnitude <= radius * radius;
    }
}
