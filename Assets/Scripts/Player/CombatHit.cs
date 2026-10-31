using UnityEngine;

// All direct player hits agree on damage eligibility before effects, feedback or score.
public static class CombatHit
{
    public static float Apply(AEnemy enemy, float amount, Vector3 point, Vector2 incoming,
        int generation = 0, bool companion = false, bool fire = false, bool score = true)
    {
        if (enemy == null || !enemy.CanReceiveDamage || amount <= 0)
            return 0;
        float dealt = CombatUpgrades.Current != null
            ? CombatUpgrades.Current.DirectHit(enemy, amount, generation, companion, fire) : amount;
        enemy.HitAt(point, incoming);
        enemy.damage(dealt);
        if (score)
            Score.IncreaseScore(Mathf.RoundToInt(dealt));
        return dealt;
    }
}
