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
}
