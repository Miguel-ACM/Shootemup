using UnityEngine;
using System;
using System.Collections.Generic;

public static class DistrictWaves
{
    public static readonly Type[] EnemyTypes = EnemyCatalog.WaveTypes;
    public static Type EnemyType(string name) => EnemyCatalog.Resolve(name);
    // Indexed by LevelId. Fixed simulator contracts use the original boss in slot zero.
    static readonly Type[][] bosses =
    {
        new[] { typeof(FieldsBomberBoss), typeof(PlainsRedKiteBoss), typeof(PlainsThreshingDrumBoss) },
        new[] { typeof(TunnelSawBoss) },
        new[] { typeof(SnowSiegeBoss), typeof(SnowIcebreakerBoss) },
        new[] { typeof(ThunderLanceBoss) },
        new[] { typeof(CityDuoBoss), typeof(CitySwitchyardBoss) },
        new[] { typeof(DesertWormBoss) },
        new[] { typeof(CandyRegentBoss) },
        new[] { typeof(SpaceCitadelBoss) },
        new[] { typeof(AbyssSubmarineBoss) },
        new[] { typeof(HellCrabBoss) },
        new[] { typeof(SeraphJudicatorBoss) },
        new[] { typeof(VoidSovereignBoss) },
        new[] { typeof(CisternHeart) },
        new[] { typeof(CoreGantry) },
        new[] { typeof(MushroomWarden) },
        new[] { typeof(DreamPainter) }
    };
    public static IReadOnlyList<Type> Bosses(LevelId district) => bosses[(int)district];
    public static Type BossType => Bosses(LevelRun.Current)[0];

    // A complete district: opening item, ten ordinary waves, two hexagon offers, the miniboss and its earned choice.
    public static readonly string[] CampaignSequence = { "reward", "2", "3", "4", "encounterChoice", "6", "8", "10", "miniboss", "12", "14", "minibossChoice", "encounterChoice", "16", "20", "boss" };
    public static string[] Sequence() => LevelRun.Current == LevelId.Void ? new[] { "boss" } : (string[])CampaignSequence.Clone();
    public static Wave Miniboss()
    {
        string[] types = { nameof(FieldsHeavyWing), nameof(TunnelDefensePlatform), nameof(SnowSiegeCraft), nameof(StormReactor), nameof(CityCommandCarrier), nameof(ScarabDredger), nameof(TaffyDuelist), nameof(OrbitalWarden), nameof(ReefLeviathan), nameof(InfernalFerryman), nameof(DawnGatekeeper), null, nameof(GrateKeeper), nameof(PressWarden), nameof(MushroomChampion), nameof(DreamChampion) };
        return new Wave(6, 23, 1, ConflictGroup.cg.center, new List<WaveUnit> { new WaveUnit(types[(int)LevelRun.Current], 0, new Vector3(0, 1.2f, 1)) { miniboss = true } }) { parts = new[] { types[(int)LevelRun.Current] } };
    }

    public static Wave Create(int budget)
    {
        return WaveComposer.Create(budget);
    }
}
