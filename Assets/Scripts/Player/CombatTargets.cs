using System.Collections.Generic;
using UnityEngine;

// Target selection is independent of whether the target is temporarily damageable.
public static class CombatTargets
{
    public static AEnemy Nearest(Vector3 position, HashSet<AEnemy> excluded, float radius)
    {
        AEnemy result = null;
        float nearest = radius * radius;
        foreach (var enemy in AEnemy.Live)
        {
            if (enemy == null || !enemy.CanBeTargeted || excluded != null && excluded.Contains(enemy))
                continue;
            float distance = ((Vector2)(enemy.transform.position - position)).sqrMagnitude;
            if (distance < nearest)
            {
                nearest = distance;
                result = enemy;
            }
        }
        return result;
    }
}
