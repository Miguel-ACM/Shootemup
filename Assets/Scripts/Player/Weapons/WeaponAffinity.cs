using UnityEngine;

// How strongly a gun answers each shot stat (Miguel, 14 November 2026: some weapons should improve
// more with attack speed, damage or number of bullets). 1 keeps a stat's ordinary effect; above 1
// the gun gains more than the stat itself, below 1 less. Stats are measured against the hull's own
// base, so a frame's starting damage or shot count never counts as an upgrade.
public readonly struct WeaponAffinity
{
    public readonly float rate, damage, count;

    public WeaponAffinity(float rate, float damage, float count)
    {
        this.rate = rate;
        this.damage = damage;
        this.count = count;
    }

    public static readonly WeaponAffinity Neutral = new WeaponAffinity(1, 1, 1);

    const float BaseRate = 10;
    const float Floor = .45f, Ceiling = 3.2f;

    static float Ratio(float value, float baseline) => Mathf.Max(.05f, value) / Mathf.Max(.05f, baseline);

    static float Bend(float ratio, float exponent) => Mathf.Clamp(Mathf.Pow(ratio, exponent), Floor, Ceiling);

    /// <summary>Extra cadence on top of the ordinary fire-rate stat.</summary>
    public float RateFactor => Bend(Ratio(GameRules.playerShootSpeed, BaseRate), rate - 1);

    /// <summary>Extra damage per round from the damage and shot-count stats.</summary>
    /// <param name="countScaling">How the gun already answers shot count: 1 when every shot adds
    /// a muzzle, 0 when it ignores the count.</param>
    public float DamageFactor(float countScaling = 1)
    {
        var hull = ShipFrames.Current;
        float damageRatio = Ratio(GameRules.playerShootDamage, hull.shootDamage);
        float countRatio = Ratio(GameRules.playerShootNumber, Mathf.Max(1, hull.shootNumber));
        return Bend(damageRatio, damage - 1) * Bend(countRatio, count - countScaling);
    }
}
