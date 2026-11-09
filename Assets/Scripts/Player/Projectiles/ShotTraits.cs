using UnityEngine;

// Rules that change how every player round flies. PlayerRound and the legacy Bullet both ask here,
// so an item changes all shots at once (guns, secondary fire, companions) and never one weapon.
public static class ShotTraits
{
    static int weaveParity;

    static int Count(UpgradeKind kind) => CombatUpgrades.Current != null ? CombatUpgrades.Current.Count(kind) : 0;

    // Alternate the weave direction per round, so paired muzzles braid around each other.
    public static float NextWeaveSign() => (weaveParity++ & 1) == 0 ? 1 : -1;

    // Corkscrew: a sine weave across the line of travel, in world units.
    const float WeaveFrequency = 15;

    static float WeaveAmplitude
    {
        get
        {
            int copies = Count(UpgradeKind.Corkscrew);
            return copies > 0 ? .26f + .08f * (copies - 1) : 0;
        }
    }

    public static float Weave(float flight, float sign) => sign * WeaveAmplitude * Mathf.Sin(flight * WeaveFrequency);

    // The weave's lateral velocity, for rounds moved by a rigidbody.
    public static float WeaveRate(float flight, float sign) => sign * WeaveAmplitude * WeaveFrequency * Mathf.Cos(flight * WeaveFrequency);

    // Slingshot Band: rounds leave slowly, accelerate over .6 s, and hit harder the farther they flew.
    public const float SlingshotReach = 12;

    public static bool Slung => Count(UpgradeKind.Slingshot) > 0;

    public static float SpeedScale(float flight) => Slung ? Mathf.Lerp(.55f, 1.3f, Mathf.SmoothStep(0, 1, flight / .6f)) : 1;

    public static float ReachDamage(float travelled)
    {
        int copies = Count(UpgradeKind.Slingshot);
        return copies > 0 ? Mathf.Lerp(.7f, 1.5f + .3f * (copies - 1), travelled / SlingshotReach) : 1;
    }

    // A slung round stretches as it gathers power, up to 30% longer.
    public static float Stretch(float travelled) => Slung ? Mathf.Lerp(.9f, 1.3f, travelled / SlingshotReach) : 1;

    // Glass Marbles: shot size follows the ship's current speed.
    public static float SizeScale => MarbleGauge.Scale;
}
