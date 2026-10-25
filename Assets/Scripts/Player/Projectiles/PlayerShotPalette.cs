using UnityEngine;

public static class PlayerShotPalette
{
    public static readonly Color Poison = new Color(.42f, .70f, .58f);
    public static readonly Color Fire = new Color(1, .15f, .07f);
    static readonly Gradient[] bodies = new Gradient[4], cores = new Gradient[4];
    public static bool Burning => CombatUpgrades.Current != null && CombatUpgrades.Current.Count(UpgradeKind.Gasoline) > 0;
    public static bool Toxic => CombatUpgrades.Current != null && CombatUpgrades.Current.Count(UpgradeKind.Poison) > 0;
    public static Gradient Electricity(bool fire, bool poison, bool core)
    {
        int key = (fire ? 1 : 0) + (poison ? 2 : 0);
        var cache = core ? cores : bodies;
        if (cache[key] != null)
            return cache[key];
        Color start = poison ? Poison : fire ? Fire : new Color(.12f, .42f, 1);
        Color end = fire ? Fire : poison ? Poison : new Color(.25f, .65f, 1);
        if (core)
        {
            start = Color.Lerp(start, Color.white, .35f);
            end = Color.Lerp(end, Color.white, .35f);
        }
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(start, 0), new GradientColorKey(start, .45f), new GradientColorKey(end, .55f), new GradientColorKey(end, 1) },
            new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
        return cache[key] = gradient;
    }
}
