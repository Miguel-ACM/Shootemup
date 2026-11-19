using System.Collections.Generic;
using UnityEngine;

// Target selection is independent of whether the target is temporarily damageable.
public static class CombatTargets
{
    public static AEnemy MarkedInRange(Vector3 position, float radius)
    {
        var marked = FlareSignal.Marked;
        if (marked == null)
            return null;
        Vector2 point = marked.transform.position;
        if (marked is DistrictBoss && marked.ClosestDamagePoint(position, out var body))
            point = body;
        return (point - (Vector2)position).sqrMagnitude <= radius * radius ? marked : null;
    }

    public static Vector3 AimPoint(Vector3 from, AEnemy target) => target is CoreGantry gantry && gantry.Reactor != null
        ? gantry.Reactor.position : target.transform.position;

    public static Vector2 MarkedDirection(Vector3 from, AEnemy target)
    {
        if (target == null)
            return Vector2.up;
        Vector2 delta = (Vector2)(target.transform.position - from);
        if (target is DistrictBoss && target.ClosestDamagePoint(from, out var body))
            delta = body - (Vector2)from;
        return delta.sqrMagnitude > .0001f ? delta.normalized : Vector2.up;
    }

    public static AEnemy Nearest(Vector3 position, HashSet<AEnemy> excluded, float radius)
    {
        AEnemy result = null;
        float nearest = radius * radius;
        foreach (var enemy in AEnemy.Live)
        {
            if (enemy == null || !enemy.CanBeTargeted || excluded != null && excluded.Contains(enemy))
                continue;
            Vector2 target = enemy.transform.position;
            if (enemy is DistrictBoss && enemy.ClosestDamagePoint(position, out var body)) target = body;
            float distance = (target - (Vector2)position).sqrMagnitude;
            if (distance < nearest)
            {
                nearest = distance;
                result = enemy;
            }
        }
        return result;
    }
}
