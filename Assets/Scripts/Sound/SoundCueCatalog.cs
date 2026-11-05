using System;
using System.Collections.Generic;
using UnityEngine;

// Existing recordings supply quiet defaults for newly audible events. Sound Lab choices are opt-in.
public static class SoundCueCatalog
{
    public sealed class Cue
    {
        public readonly string Id, Label, Group, Resource;
        public readonly float Volume, Pitch, Rest;
        public readonly bool Loop;
        public Cue(string id, string label, string group, string resource, float volume, float pitch = 1, float rest = .08f, bool loop = false)
        {
            Id = id;
            Label = label;
            Group = group;
            Resource = resource;
            Volume = volume;
            Pitch = pitch;
            Rest = rest;
            Loop = loop;
        }
    }

    const string Enemy = "Enemy projectiles", Player = "Player weapons", Beam = "Lasers", Hit = "Hits & feedback";
    public static readonly Cue[] All =
    {
        new Cue("enemySimple", "Round bullet launch", Enemy, "Sounds/shot1", .035f, .85f),
        new Cue("enemyNeedle", "Needle launch", Enemy, "Sounds/shot1", .03f, 1.35f),
        new Cue("enemyPlasma", "Plasma launch", Enemy, "Sounds/Items/static", .035f, .8f, .12f),
        new Cue("enemyMissile", "Missile launch", Enemy, "Sounds/Items/fan", .04f, 1.2f, .13f),
        new Cue("enemyBurst", "Shell launch", Enemy, "Sounds/Items/cannon", .045f, .9f, .11f),
        new Cue("enemyDisc", "Returning disc launch", Enemy, "Sounds/Items/roll", .035f, 1.3f, .1f),
        new Cue("enemyRicochet", "Ricochet launch", Enemy, "Sounds/shot1", .03f, 1.1f, .09f),
        new Cue("enemyHelix", "Helix launch", Enemy, "Sounds/Items/fan", .03f, 1.55f, .1f),
        new Cue("enemySuspended", "Delayed bullet launch", Enemy, "Sounds/Items/capacitor", .03f, .9f, .11f),
        new Cue("enemyScatter", "Carrier launch", Enemy, "Sounds/Items/cannon", .04f, .75f, .11f),
        new Cue("enemyMine", "Mine deployment", Enemy, "Sounds/Items/capacitor", .035f, .7f, .13f),
        new Cue("enemyWave", "Side shot launch", Enemy, "Sounds/Items/static", .03f, 1.3f, .12f),
        new Cue("enemyGiant", "Giant orb launch", Enemy, "Sounds/Items/cannon", .045f, .65f, .16f),
        new Cue("shellBurst", "Shell / mine detonation", "Explosions", "Sounds/enemyExplosion1", .055f, 1.25f, .12f),
        new Cue("ricochetHit", "Bullet wall bounce", Hit, "Sounds/hit1", .025f, 1.6f, .1f),
        new Cue("plasmaBreak", "Plasma destroyed", Hit, "Sounds/Items/shatter", .05f, 1.3f, .12f),
        new Cue("hitElectric", "Electricity impact", Hit, "Sounds/Items/static", .025f, 1, .1f),
        new Cue("shotElectric", "Electricity", Player, "Sounds/shot1", .1f),
        new Cue("shotAK45", "AK45", Player, "Sounds/shot1", .1f),
        new Cue("shotCompanion", "Companion fire", Player, "Sounds/shot1", .065f),
        new Cue("shotNeedle", "Needle cannon", Player, "Sounds/shot1", .07f),
        new Cue("shotRail", "Rail driver", Player, "Sounds/shot1", .07f),
        new Cue("shotScatter", "Scatter cannon", Player, "Sounds/shot1", .07f),
        new Cue("shotShell", "Shell weapons", Player, "Sounds/shot1", .07f),
        new Cue("shotDisc", "Disc thrower", Player, "Sounds/shot1", .07f),
        new Cue("shotMissile", "Missile rack", Player, "Sounds/shot1", .07f),
        new Cue("shotFlame", "Flame shots", Player, "Sounds/shot1", .07f),
        new Cue("shotSecondary", "Secondary fire", Player, "Sounds/shot1", .025f),
        new Cue("beamCharge", "Enemy laser charge", Beam, "Sounds/Beam/charge", .2f),
        new Cue("beamFire", "Enemy laser ignition", Beam, "Sounds/Beam/ignite", .24f),
        new Cue("beamSustain", "Enemy laser sustain", Beam, "Sounds/Beam/sustain", .15f, .7f, loop: true),
        new Cue("beamRelease", "Laser release", Beam, "Sounds/Beam/release", .14f),
        new Cue("playerBeamCharge", "Player laser charge", Beam, "Sounds/Beam/charge", .16f),
        new Cue("shotBeam", "Player laser ignition", Beam, "Sounds/Beam/player_ignite", .18f),
        new Cue("playerBeamSustain", "Player laser sustain", Beam, "Sounds/Beam/player_sustain", .11f, .7f, loop: true),
        new Cue("heavenBeamCharge", "Heaven laser charge", Beam, "Sounds/Beam/charge", .2f),
        new Cue("heavenBeamFire", "Heaven laser ignition", Beam, "Sounds/Beam/ignite", .24f, .78f),
        new Cue("heavenBeamSustain", "Heaven laser sustain", Beam, "Sounds/Beam/sustain", .15f, .55f, loop: true)
    };

    static readonly Dictionary<string, Cue> lookup = Build();
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    static Dictionary<string, Cue> Build()
    {
        var result = new Dictionary<string, Cue>(StringComparer.Ordinal);
        foreach (var cue in All)
            result.Add(cue.Id, cue);
        return result;
    }
    public static Cue Find(string id) => id != null && lookup.TryGetValue(id, out var cue) ? cue : null;
    public static AudioClip DefaultClip(string id)
    {
        var cue = Find(id);
        if (cue == null)
            return null;
        if (!clips.TryGetValue(id, out var clip))
            clips[id] = clip = Resources.Load<AudioClip>(cue.Resource);
        return clip;
    }
    public static void Warm()
    {
        foreach (var cue in All)
            DefaultClip(cue.Id);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => clips.Clear();

    public static string Projectile(Type type)
    {
        if (type == typeof(PulseLaser)) return null; // Attached beams already own charge, ignition and sustain.
        if (type == typeof(NeedleRound)) return "enemyNeedle";
        if (type == typeof(PlasmaOrb)) return "enemyPlasma";
        if (type == typeof(GuidedMissile)) return "enemyMissile";
        if (type == typeof(BurstShell)) return "enemyBurst";
        if (type == typeof(ReturningDisc)) return "enemyDisc";
        if (type == typeof(RicochetBolt)) return "enemyRicochet";
        if (type == typeof(HelixRound)) return "enemyHelix";
        if (type == typeof(SuspendedRound)) return "enemySuspended";
        if (type == typeof(ScatterCarrier)) return "enemyScatter";
        if (type == typeof(ProximityMine)) return "enemyMine";
        if (type == typeof(LaserSideRound)) return "enemyWave";
        if (type == typeof(GiantOrb)) return "enemyGiant";
        return "enemySimple";
    }
}
