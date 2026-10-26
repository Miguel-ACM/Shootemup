using UnityEngine;
using System;
using System.Collections.Generic;

public static class DistrictWaves
{
    // Explicit types keep runtime factories safe under managed code stripping.
    public static readonly Type[] EnemyTypes = { typeof(ScoutInterceptor), typeof(PatrolGunship), typeof(HeavyBomber), typeof(WallTurret), typeof(MaintenanceDrone), typeof(ArmoredCarrier), typeof(ColdWeatherFighter), typeof(SiegeGunship), typeof(ShieldTransport), typeof(BeamDrone), typeof(RelayDrone), typeof(StormSkimmer), typeof(PursuitFighter), typeof(AttackDrone), typeof(CommandGunship), typeof(CourierDiver), typeof(AvalancheBomber), typeof(ArcLancer), typeof(FieldsHeavyWing), typeof(TunnelDefensePlatform), typeof(SnowSiegeCraft), typeof(StormReactor), typeof(CityCommandCarrier), typeof(SandEmitter), typeof(SandStriker), typeof(SandWorm), typeof(BonbonSkirmisher), typeof(WaferDart), typeof(MintSpinner), typeof(MeadowTank), typeof(FrostAuger), typeof(OrbitalScout), typeof(LaserSatellite), typeof(OrbitalCarrier), typeof(OrbitalMoonlet) };
    static DistrictWaves()
    {
        var all = new List<Type>(EnemyTypes);
        all.AddRange(new[] { typeof(SkullRevenant), typeof(GhostGalleon), typeof(CinderImp), typeof(ReefDart), typeof(MantaRay), typeof(Bathysphere), typeof(ScarabDredger), typeof(TaffyDuelist), typeof(OrbitalWarden), typeof(ReefLeviathan), typeof(InfernalFerryman) });
        EnemyTypes = all.ToArray();
    }

    public static Type EnemyType(string name) => Array.Find(EnemyTypes, t => t.Name == name);
    public static Type BossType
    {
        get
        {
            switch (LevelRun.Current)
            {
                case LevelId.Hell:
                    return typeof(HellCrabBoss);
                case LevelId.Underwater:
                    return typeof(AbyssSubmarineBoss);
                case LevelId.Space:
                    return typeof(SpaceCitadelBoss);
                case LevelId.Candyland:
                    return typeof(CandyRegentBoss);
                case LevelId.Desert:
                    return typeof(DesertWormBoss);
                case LevelId.Tunnel:
                    return typeof(TunnelSawBoss);
                case LevelId.Snow:
                    return typeof(SnowSiegeBoss);
                case LevelId.Thunder:
                    return typeof(ThunderLanceBoss);
                case LevelId.City:
                    return typeof(CityDuoBoss);
                default:
                    return typeof(FieldsBomberBoss);
            }
        }
    }

    public static string[] Sequence() => new[] { "reward", "2", "3", "4", "encounterChoice", "6", "8", "10", "miniboss", "12", "14", "minibossChoice", "16", "20", "boss" };
    public static Wave Miniboss()
    {
        string[] types = { nameof(FieldsHeavyWing), nameof(TunnelDefensePlatform), nameof(SnowSiegeCraft), nameof(StormReactor), nameof(CityCommandCarrier), nameof(ScarabDredger), nameof(TaffyDuelist), nameof(OrbitalWarden), nameof(ReefLeviathan), nameof(InfernalFerryman) };
        return new Wave(6, 23, 1, ConflictGroup.cg.center, new List<WaveUnit> { new WaveUnit(types[(int)LevelRun.Current], 0, new Vector3(0, 1.2f, 1)) { miniboss = true } }) { parts = new[] { types[(int)LevelRun.Current] } };
    }

    public static Wave Create(int budget)
    {
        return WaveComposer.Create(budget);
    }
}
