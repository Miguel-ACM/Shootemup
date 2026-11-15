using System;
using UnityEngine;

// Seeded per-district twists, shown with their icon in the left margin: more elite ships,
// more coins, or hostile rounds 20% faster. Each has its own chance, so a district can have
// none, one or several. Void, a lone boss, never takes them.
[Flags]
public enum LevelModifier
{
    None = 0, Elites = 1, Coins = 2, FastFire = 4
}

public static class LevelModifiers
{
    public static readonly LevelModifier[] All = { LevelModifier.Elites, LevelModifier.Coins, LevelModifier.FastFire };
    const int ChancePercent = 22;

    // Debug runs can pin a set; null follows the seed.
    public static LevelModifier? Forced
    {
        get; set;
    }

    public static LevelModifier Current => For(LevelRun.Current, LevelRun.Seed, LevelRun.Stage);

    public static LevelModifier For(LevelId level, int seed, int stage)
    {
        // Plains stays plain: no MORE ELITES, MORE COINS or FAST FIRE (Miguel, 15 November 2026).
        if (level == LevelId.Void || level == LevelId.Fields)
            return LevelModifier.None;
        if (Forced.HasValue)
            return Forced.Value;
        var result = LevelModifier.None;
        foreach (var modifier in All)
            if (Hash(seed, stage * 5113 + (int)level * 271 + (int)modifier * 97) % 100 < ChancePercent)
                result |= modifier;
        return result;
    }

    public static bool Has(LevelModifier modifier) => (Current & modifier) != 0;

    public static float ShotSpeed => Has(LevelModifier.FastFire) ? 1.2f : 1;
    public static float CoinChance => Has(LevelModifier.Coins) ? 2 : 1;
    public static int ExtraCoins => Has(LevelModifier.Coins) ? 1 : 0;
    public static float CoinBurst => Has(LevelModifier.Coins) ? 1.5f : 1;

    public static string Label(LevelModifier modifier)
    {
        switch (modifier)
        {
            case LevelModifier.Elites:
                return "MORE ELITES";
            case LevelModifier.Coins:
                return "MORE COINS";
            default:
                return "FAST FIRE +20%";
        }
    }

    public static Sprite Icon(LevelModifier modifier)
    {
        int index = Array.IndexOf(All, modifier);
        if (icons[index] != null)
            return icons[index];
        var texture = Resources.Load<Texture2D>("EncounterIcons/Level" + (modifier == LevelModifier.Elites ? "Elites" : modifier == LevelModifier.Coins ? "Coins" : "FastFire"));
        if (texture == null)
            throw new InvalidOperationException("Missing level modifier icon: " + modifier);
        return icons[index] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 32, 0, SpriteMeshType.FullRect);
    }

    static readonly Sprite[] icons = new Sprite[3];

    static int Hash(int value, int salt)
    {
        unchecked
        {
            uint x = (uint)value ^ (uint)salt * 0x9e3779b9u;
            x ^= x >> 16;
            x *= 0x7feb352du;
            x ^= x >> 15;
            x *= 0x846ca68bu;
            return (int)((x ^ (x >> 16)) & 0x7fffffffu);
        }
    }
}
