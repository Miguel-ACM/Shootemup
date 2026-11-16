using UnityEngine;

// All direct player hits agree on damage eligibility before effects, feedback or score.
public static class CombatHit
{
    public static event System.Action<AEnemy> PlayerHit;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetObservers() => PlayerHit = null;
    public static float Apply(AEnemy enemy, float amount, Vector3 point, Vector2 incoming,
        int generation = 0, bool companion = false, bool fire = false, bool score = true, bool energy = false)
    {
        var result = Resolve(enemy, amount, point, incoming, generation, companion, fire, score, energy);
        return result.Accepted ? result.Requested : 0;
    }

    public static DamageResult Resolve(AEnemy enemy, float amount, Vector3 point, Vector2 incoming,
        int generation = 0, bool companion = false, bool fire = false, bool score = true, bool energy = false)
    {
        if (enemy == null || !enemy.CanReceiveDamage || amount <= 0 || !float.IsFinite(amount))
            return default;
        var upgrades = CombatUpgrades.Current;
        if (energy && upgrades != null && upgrades.Count(UpgradeKind.ColdCoil) > 0)
            ChilledHull.Apply(enemy, 2.25f);
        float dealt = upgrades != null ? upgrades.DirectHit(enemy, amount, generation, companion, fire, energy) : amount;
        enemy.HitAt(point, incoming);
        // Gum stores the hit for later. Preserve pre-death coatings and the original scoring payload;
        // Applied separately reports actual health removed after armour and overkill.
        var result = dealt > 0 ? enemy.ReceiveDamage(dealt) : new DamageResult(true, 0, 0);
        if (score && result.Accepted)
            Score.IncreaseScore(Mathf.RoundToInt(dealt));
        if (result.Accepted && !energy && generation == 0 && upgrades != null && enemy != null && enemy.CanReceiveDamage)
            RubberMallet.Nudge(enemy, incoming, upgrades.Count(UpgradeKind.RubberMallet));
        if (result.Accepted && generation == 0 && !companion && enemy != null && enemy.CanReceiveDamage)
            PlayerHit?.Invoke(enemy);
        return result;
    }

    // Burn and poison ticks: real damage without the impact spark, hit sound or hull flash,
    // so ten burn ticks a second read through their own flames instead of as constant hits.
    public static DamageResult Affliction(AEnemy target, float amount, DamageKind kind = DamageKind.Burn)
    {
        if (target == null || !target.CanReceiveDamage)
            return default;
        DamageNumbers.TickKind = kind;
        target.QuietDamage = true;
        var result = target.ReceiveDamage(amount);
        target.QuietDamage = false;
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
