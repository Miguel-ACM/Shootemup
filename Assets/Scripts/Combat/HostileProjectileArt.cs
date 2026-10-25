using System;
using UnityEngine;

// Approved white pixel silhouettes, with gameplay colours supplied separately.
public static class HostileProjectileArt
{
    enum Shape { Needle, Helix, Shell, Ricochet, Suspended, Missile, Plasma, Mine, Disc, Carrier, Side }

    static Shape Kind(Type type)
    {
        if(type==typeof(NeedleRound))return Shape.Needle;
        if(type==typeof(HelixRound))return Shape.Helix;
        if(type==typeof(BurstShell))return Shape.Shell;
        if(type==typeof(RicochetBolt))return Shape.Ricochet;
        if(type==typeof(SuspendedRound))return Shape.Suspended;
        if(type==typeof(GuidedMissile))return Shape.Missile;
        if(type==typeof(PlasmaOrb))return Shape.Plasma;
        if(type==typeof(ProximityMine))return Shape.Mine;
        if(type==typeof(ReturningDisc))return Shape.Disc;
        if(type==typeof(ScatterCarrier))return Shape.Carrier;
        return Shape.Side;
    }
    public static Color Accent(Type type)
    {
        if(type==typeof(SimpleRound))return new Color(1,.46f,.13f);
        switch(Kind(type))
        {
            case Shape.Needle:return new Color(1,.22f,.27f);
            case Shape.Helix:return new Color(1,.30f,.65f);
            case Shape.Shell:return new Color(1,.67f,.13f);
            case Shape.Ricochet:return new Color(.76f,.97f,.16f);
            case Shape.Suspended:return new Color(.29f,.52f,1);
            case Shape.Missile:return new Color(.70f,.36f,1);
            case Shape.Plasma:return new Color(.17f,.94f,.94f);
            case Shape.Mine:return new Color(1,.28f,.12f);
            case Shape.Disc:return new Color(.90f,.31f,.94f);
            case Shape.Carrier:return new Color(.24f,.92f,.55f);
            default:return new Color(.25f,.60f,1);
        }
    }
    public static Sprite Sprite(Type type,bool large=false)=>ProjectileSprites.Load(Name(type));
    public static string Name(Type type)
    {
        if(type==typeof(SimpleRound))return "standard";
        switch(Kind(type))
        {
            case Shape.Needle:return "needle";
            case Shape.Helix:return "helix";
            case Shape.Shell:return "burst_shell";
            case Shape.Ricochet:return "ricochet";
            case Shape.Suspended:return "delayed";
            case Shape.Missile:return "missile";
            case Shape.Plasma:return "plasma";
            case Shape.Mine:return "mine";
            case Shape.Disc:return "returning_disc";
            case Shape.Carrier:return "side_carrier";
            default:return "laser_side_shot";
        }
    }
}
