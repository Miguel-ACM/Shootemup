using UnityEngine;

// One hostile hue per district, in two tones: bright rounds and dark rounds (a deep body
// with a glowing outline in the same hue). Each attack is authored in one tone, so the
// tone tells the attack families apart, and the Polarity Engine's poles are the two tones.
public static class LevelProjectilePalette
{
    static readonly Material[,] shared = new Material[System.Enum.GetValues(typeof(LevelId)).Length, 4];
    public static Color Accent(LevelId level)
    {
        switch (level)
        {
            case LevelId.Fields:
                return new Color(1, .28f, .20f);
            case LevelId.Snow:
                return new Color(.88f, .10f, .22f);
            case LevelId.City:
                return new Color(1, .77f, .16f);
            case LevelId.Thunder:
                return new Color(1, .65f, .12f);
            case LevelId.Tunnel:
                return new Color(1, .23f, .59f);
            case LevelId.Desert:
                return new Color(.39f, .24f, 1);
            case LevelId.Candyland:
                return new Color(.65f, .95f, .12f);
            case LevelId.Space:
                return new Color(1, .34f, .46f);
            case LevelId.Underwater:
                return new Color(1, .63f, .22f);
            case LevelId.Hell:
                return new Color(.30f, 1, .78f);
            case LevelId.Heaven:
                return new Color(1, .48f, .08f);
            case LevelId.Sewers:
                return new Color(.29f, 1, .055f);
            case LevelId.Core:
                return new Color(1, .085f, .14f);
            case LevelId.Void:
                return new Color(.72f, .25f, 1);
            default:
                return new Color(1, .28f, .20f);
        }
    }

    public static Material Material(bool neutral = false, HostileTone tone = HostileTone.Bright)
    {
        int level = (int)LevelRun.Current, variant = (neutral ? 1 : 0) + (tone == HostileTone.Dark ? 2 : 0);
        if (shared[level, variant] != null)
            return shared[level, variant];
        var material = new Material(CombatVisuals.ProjectileMaterial) { name = LevelRun.Current + " shared hostile ink " + tone };
        material.SetColor("_SpriteTint", Accent(LevelRun.Current));
        material.SetFloat("_Neutralize", neutral ? 1 : 0);
        material.SetFloat("_Dark", tone == HostileTone.Dark ? 1 : 0);
        material.SetFloat("_Burn", 0);
        material.SetFloat("_Poison", 0);
        return shared[level, variant] = material;
    }
}
