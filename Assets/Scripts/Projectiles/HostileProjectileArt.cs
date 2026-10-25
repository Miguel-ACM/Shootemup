using System;
using UnityEngine;

// Approved white pixel silhouettes, with gameplay colours supplied separately.
public static class HostileProjectileArt
{
    enum Shape
    {
        Needle, Helix, Shell, Ricochet, Suspended, Missile, Plasma, Mine, Disc, Carrier, Side
    }

    static Shape Kind(Type type)
    {
        if (type == typeof(NeedleRound))
            return Shape.Needle;
        if (type == typeof(HelixRound))
            return Shape.Helix;
        if (type == typeof(BurstShell))
            return Shape.Shell;
        if (type == typeof(RicochetBolt))
            return Shape.Ricochet;
        if (type == typeof(SuspendedRound))
            return Shape.Suspended;
        if (type == typeof(GuidedMissile))
            return Shape.Missile;
        if (type == typeof(PlasmaOrb))
            return Shape.Plasma;
        if (type == typeof(ProximityMine))
            return Shape.Mine;
        if (type == typeof(ReturningDisc))
            return Shape.Disc;
        if (type == typeof(ScatterCarrier))
            return Shape.Carrier;
        return Shape.Side;
    }

    public static Color Accent(Type type)
    {
        return LevelProjectilePalette.Accent(LevelRun.Current);
    }

    public static Sprite Sprite(Type type, bool large = false) => ProjectileSprites.Load(Name(type));
    public static string Name(Type type)
    {
        if (type == typeof(SimpleRound))
            return "standard";
        switch (Kind(type))
        {
            case Shape.Needle:
                return "needle";
            case Shape.Helix:
                return "helix";
            case Shape.Shell:
                return "burst_shell";
            case Shape.Ricochet:
                return "ricochet";
            case Shape.Suspended:
                return "delayed";
            case Shape.Missile:
                return "missile";
            case Shape.Plasma:
                return "plasma";
            case Shape.Mine:
                return "mine";
            case Shape.Disc:
                return "returning_disc";
            case Shape.Carrier:
                return "side_carrier";
            default:
                return "laser_side_shot";
        }
    }
}
