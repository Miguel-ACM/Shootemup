using UnityEngine;

// All direct player hits agree on damage eligibility before effects, feedback or score.
public static class CombatHit
{
    public static float Apply(AEnemy enemy, float amount, Vector3 point, Vector2 incoming,
        int generation = 0, bool companion = false, bool fire = false, bool score = true)
    {
        var result = Resolve(enemy, amount, point, incoming, generation, companion, fire, score);
        return result.Accepted ? result.Requested : 0;
    }

    public static DamageResult Resolve(AEnemy enemy, float amount, Vector3 point, Vector2 incoming,
        int generation = 0, bool companion = false, bool fire = false, bool score = true)
    {
        if (enemy == null || !enemy.CanReceiveDamage || amount <= 0 || !float.IsFinite(amount))
            return default;
        float dealt = CombatUpgrades.Current != null
            ? CombatUpgrades.Current.DirectHit(enemy, amount, generation, companion, fire) : amount;
        enemy.HitAt(point, incoming);
        // Gum stores the hit for later. Preserve pre-death coatings and the original scoring payload;
        // Applied separately reports actual health removed after armour and overkill.
        var result = dealt > 0 ? enemy.ReceiveDamage(dealt) : new DamageResult(true, 0, 0);
        if (score && result.Accepted)
            Score.IncreaseScore(Mathf.RoundToInt(dealt));
        return result;
    }

    public static DamageResult Tick(ICombatDamageable target, float amount, bool score = false)
    {
        if (target == null || !target.CanReceiveDamage)
            return default;
        var result = target.ReceiveDamage(amount);
        if (score && result.Accepted)
            Score.IncreaseScore(Mathf.RoundToInt(result.Requested));
        return result;
    }
}
