using UnityEngine;

// Turns a hostile round into the player's own: the round dissolves and a friendly needle
// leaves its position toward the nearest enemy. Shared by every deflecting or reflecting item.
public static class BulletReflection
{
    public const float Speed = 15;
    static readonly Color Spark = new Color(1f, .86f, .45f);

    /// <summary>True for rounds that can be sent back; beams are emitters, not rounds.</summary>
    public static bool CanReflect(AEnemyBullet round) => round != null && !(round is PulseLaser);

    /// <summary>Reflects one hostile round as a player round. Returns null when it cannot be reflected.</summary>
    public static PlayerRound Reflect(AEnemyBullet round, float damageScale = 1)
    {
        if (!CanReflect(round))
            return null;
        Vector3 p = round.transform.position;
        Vector2 direction = Toward(p);
        PSXImpactEffects.Burst(p, Spark, false, direction);
        round.Vanish();
        var shot = PlayerRound.Spawn(p, direction, GameRules.playerShootDamage * 2.5f * damageScale, Speed, PlayerDelivery.Needle);
        if (shot != null)
            shot.generation = 1;
        return shot;
    }

    /// <summary>The reflected heading: the nearest targetable enemy, or straight up the arena.</summary>
    public static Vector2 Toward(Vector3 from)
    {
        var target = CombatUpgrades.Nearest(from, null, 40);
        if (target == null)
            return Vector2.up;
        Vector2 offset = target.transform.position - from;
        return offset.sqrMagnitude > .0001f ? offset.normalized : Vector2.up;
    }
}
