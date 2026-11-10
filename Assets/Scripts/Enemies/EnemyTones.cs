using System;
using System.Collections.Generic;

/// <summary>
/// The hand-picked default tone of each enemy's attacks. Enemies that fire both tones
/// switch per attack with UseTone; bosses choose a tone for every card and accent.
/// Unlisted enemies fire bright.
/// </summary>
public static class EnemyTones
{
    // Lasers are always bright and never absorbed, so laser-only craft are not listed.
    static readonly Dictionary<Type, HostileTone> defaults = new Dictionary<Type, HostileTone>
    {
        // Plains: aimed arrows and tank bursts are bright; furrows, reeds and bombs are dark.
        { typeof(ScoutInterceptor), HostileTone.Bright },
        { typeof(PatrolGunship), HostileTone.Bright },
        { typeof(MeadowTank), HostileTone.Bright },
        { typeof(HeavyBomber), HostileTone.Bright },
        { typeof(HarrowBomber), HostileTone.Dark },
        { typeof(ReedSkimmer), HostileTone.Dark },
        { typeof(FieldsHeavyWing), HostileTone.Dark },

        // Snow: re-aimed chevrons and aimed shells are bright; sliding sheets, hanging
        // icicles and falling snow are dark.
        { typeof(ColdWeatherFighter), HostileTone.Bright },
        { typeof(FrostAuger), HostileTone.Bright },
        { typeof(ShieldTransport), HostileTone.Bright },
        { typeof(AvalancheBomber), HostileTone.Bright },
        { typeof(SiegeGunship), HostileTone.Dark },
        { typeof(HailLantern), HostileTone.Dark },
        { typeof(IcicleWeaver), HostileTone.Dark },
        { typeof(SnowSiegeCraft), HostileTone.Dark },

        // City: headlights along one aim are bright; lobbed cargo, skids and street
        // lattices are dark.
        { typeof(AttackDrone), HostileTone.Bright },
        { typeof(CommandGunship), HostileTone.Bright },
        { typeof(CourierDiver), HostileTone.Bright },
        { typeof(CityCommandCarrier), HostileTone.Bright },
        { typeof(TerraceGunboat), HostileTone.Dark },
        { typeof(MetroLancer), HostileTone.Dark },

        // Tunnel pairs fast with slow: fast ricochets and arrows are bright, the slow
        // bouncing rounds, mines and wall runners threaded between them are dark.
        { typeof(WallTurret), HostileTone.Bright },
        { typeof(MaintenanceDrone), HostileTone.Bright },
        { typeof(ArmoredCarrier), HostileTone.Bright },
        { typeof(RivetMantis), HostileTone.Bright },
        { typeof(TunnelDefensePlatform), HostileTone.Bright },
        { typeof(BaffleSpinner), HostileTone.Dark },

        // Thunder: lightning is bright like its lasers; heavy ordnance (homing missiles,
        // plasma) is dark.
        { typeof(RelayDrone), HostileTone.Bright },
        { typeof(CapacitorKite), HostileTone.Bright },
        { typeof(FaradayPylon), HostileTone.Bright },
        { typeof(StormReactor), HostileTone.Bright },
        { typeof(StormSkimmer), HostileTone.Dark },

        // Desert: rounds that lunge or burst at the pilot are bright; sand puffs, wing dust
        // and mines that carry on are dark.
        { typeof(SandStriker), HostileTone.Bright },
        { typeof(SandWorm), HostileTone.Bright },
        { typeof(SandEmitter), HostileTone.Dark },
        { typeof(DuneMoth), HostileTone.Dark },
        { typeof(MirageObelisk), HostileTone.Dark },
        { typeof(ScarabDredger), HostileTone.Dark },

        // Candyland: aimed bonbons, wafers and popping gumdrops are bright; the spread of
        // ricocheting mints is dark, and ribbon gliders trail one ribbon of each.
        { typeof(BonbonSkirmisher), HostileTone.Bright },
        { typeof(WaferDart), HostileTone.Bright },
        { typeof(GumdropMortar), HostileTone.Bright },
        { typeof(RibbonGlider), HostileTone.Bright },
        { typeof(TaffyDuelist), HostileTone.Bright },
        { typeof(MintSpinner), HostileTone.Dark },

        // Space: aimed needles are bright; ricochets and towed comets are dark; the
        // binary stars and eclipsing moons pair one bead of each tone.
        { typeof(OrbitalScout), HostileTone.Bright },
        { typeof(OrbitalMoonlet), HostileTone.Bright },
        { typeof(EclipseSail), HostileTone.Bright },
        { typeof(OrbitalWarden), HostileTone.Bright },
        { typeof(OrbitalCarrier), HostileTone.Dark },
        { typeof(CometTug), HostileTone.Dark },

        // Underwater: aimed darts and torpedoes are bright; currents, ink clouds and
        // plumes are dark.
        { typeof(ReefDart), HostileTone.Bright },
        { typeof(Bathysphere), HostileTone.Bright },
        { typeof(DepthMine), HostileTone.Bright },
        { typeof(MantaRay), HostileTone.Dark },
        { typeof(JetSquid), HostileTone.Dark },
        { typeof(NautilusBell), HostileTone.Dark },
        { typeof(ReefLeviathan), HostileTone.Dark },

        // Hell: souls that stare and dart, imp helixes and censer plasma are bright;
        // broadsides, scissors, embers and needle fans are dark.
        { typeof(CinderImp), HostileTone.Bright },
        { typeof(InfernalFerryman), HostileTone.Bright },
        { typeof(SkullRevenant), HostileTone.Dark },
        { typeof(GhostGalleon), HostileTone.Dark },
        { typeof(BoneHarrier), HostileTone.Dark },
        { typeof(AshCenser), HostileTone.Dark },

        // Heaven: its lances are bright, so most of its rounds are dark against them; only
        // the votive spears' aimed stacks are bright.
        { typeof(VotiveSpear), HostileTone.Bright },
        { typeof(ThroneBarge), HostileTone.Dark },
        { typeof(PrismCantor), HostileTone.Dark },
        { typeof(AegisReliquary), HostileTone.Dark },
        { typeof(DawnGatekeeper), HostileTone.Dark },
    };

    public static HostileTone Default(Type enemy) => defaults.TryGetValue(enemy, out var tone) ? tone : HostileTone.Bright;
}
