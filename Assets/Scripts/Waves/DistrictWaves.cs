using UnityEngine;
using System;
using System.Collections.Generic;

public static class DistrictWaves
{
    public static readonly Type[] EnemyTypes = EnemyCatalog.WaveTypes;
    public static Type EnemyType(string name) => EnemyCatalog.Resolve(name);
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
