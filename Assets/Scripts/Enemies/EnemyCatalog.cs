using System;
using System.Collections.Generic;

public enum EnemyId { ScoutInterceptor, PatrolGunship, HeavyBomber, WallTurret, MaintenanceDrone, ArmoredCarrier, ColdWeatherFighter, SiegeGunship, ShieldTransport, BeamDrone, RelayDrone, StormSkimmer, PursuitFighter, AttackDrone, CommandGunship, CourierDiver, AvalancheBomber, ArcLancer, FieldsHeavyWing, TunnelDefensePlatform, SnowSiegeCraft, StormReactor, CityCommandCarrier, SandEmitter, SandStriker, SandWorm, BonbonSkirmisher, WaferDart, MintSpinner, MeadowTank, FrostAuger, OrbitalScout, LaserSatellite, OrbitalCarrier, OrbitalMoonlet, SkullRevenant, GhostGalleon, CinderImp, ReefDart, MantaRay, Bathysphere, ScarabDredger, TaffyDuelist, OrbitalWarden, ReefLeviathan, InfernalFerryman, BoneHarrier, AshCenser, JetSquid, NautilusBell, CometTug, EclipseSail, GumdropMortar, RibbonGlider, MirageObelisk, DuneMoth, StormOrgan, CapacitorKite, RivetMantis, BaffleSpinner, TerraceGunboat, MetroLancer, HailLantern, IcicleWeaver, HarrowBomber, ReedSkimmer, HellCrabBoss, AbyssSubmarineBoss, SpaceCitadelBoss, CandyRegentBoss, DesertWormBoss, TunnelSawBoss, SnowSiegeBoss, ThunderLanceBoss, CityDuoBoss, FieldsBomberBoss }
public enum CombatHullId
{
    ScoutInterceptor = 0,
    PatrolGunship = 1,
    HeavyBomber = 2,
    WallTurret = 3,
    MaintenanceDrone = 4,
    ArmoredCarrier = 5,
    ColdWeatherFighter = 6,
    SiegeGunship = 7,
    ShieldTransport = 8,
    BeamDrone = 9,
    RelayDrone = 10,
    StormSkimmer = 11,
    PursuitFighter = 12,
    AttackDrone = 13,
    CommandGunship = 14,
    CourierDiver = 15,
    AvalancheBomber = 17,
    ArcLancer = 18,
    FieldsHeavyWing = 24,
    TunnelDefensePlatform = 25,
    SnowSiegeCraft = 26,
    StormReactor = 27,
    CityCommandCarrier = 28,
    SandEmitter = 30,
    SandStriker = 31,
    SandWorm = 32,
    BonbonSkirmisher = 35,
    WaferDart = 36,
    MintSpinner = 37,
    MeadowTank = 39,
    FrostAuger = 40,
    OrbitalScout = 41,
    LaserSatellite = 42,
    OrbitalCarrier = 43,
    OrbitalMoonlet = 44,
    SkullRevenant = 46,
    GhostGalleon = 47,
    CinderImp = 48,
    ReefDart = 50,
    MantaRay = 51,
    Bathysphere = 52,
    ScarabDredger = 56,
    TaffyDuelist = 57,
    OrbitalWarden = 60,
    ReefLeviathan = 58,
    InfernalFerryman = 59,
    BoneHarrier = 80,
    AshCenser = 79,
    JetSquid = 78,
    NautilusBell = 77,
    CometTug = 76,
    EclipseSail = 75,
    GumdropMortar = 74,
    RibbonGlider = 73,
    MirageObelisk = 72,
    DuneMoth = 71,
    StormOrgan = 70,
    CapacitorKite = 69,
    RivetMantis = 68,
    BaffleSpinner = 67,
    TerraceGunboat = 66,
    MetroLancer = 65,
    HailLantern = 64,
    IcicleWeaver = 63,
    HarrowBomber = 62,
    ReedSkimmer = 61,
    HellCrabBoss = 49,
    AbyssSubmarineBoss = 53,
    SpaceCitadelBoss = 45,
    CandyRegentBoss = 38,
    DesertWormBoss = 33,
    TunnelSawBoss = 20,
    SnowSiegeBoss = 21,
    ThunderLanceBoss = 22,
    CityDuoBoss = 12,
    FieldsBomberBoss = 23,
}
public sealed class EnemyDefinition
{
    public readonly EnemyId Id;
    public readonly Type ComponentType;
    public readonly CombatHullId Hull;
    public readonly bool Boss;
    public string Key => Id.ToString();
    public EnemyDefinition(EnemyId id, Type type, CombatHullId hull, bool boss = false) { Id = id; ComponentType = type; Hull = hull; Boss = boss; }
}
public static class EnemyCatalog
{
    public static readonly EnemyDefinition[] All = {
        new EnemyDefinition(EnemyId.ScoutInterceptor, typeof(ScoutInterceptor), CombatHullId.ScoutInterceptor),
        new EnemyDefinition(EnemyId.PatrolGunship, typeof(PatrolGunship), CombatHullId.PatrolGunship),
        new EnemyDefinition(EnemyId.HeavyBomber, typeof(HeavyBomber), CombatHullId.HeavyBomber),
        new EnemyDefinition(EnemyId.WallTurret, typeof(WallTurret), CombatHullId.WallTurret),
        new EnemyDefinition(EnemyId.MaintenanceDrone, typeof(MaintenanceDrone), CombatHullId.MaintenanceDrone),
        new EnemyDefinition(EnemyId.ArmoredCarrier, typeof(ArmoredCarrier), CombatHullId.ArmoredCarrier),
        new EnemyDefinition(EnemyId.ColdWeatherFighter, typeof(ColdWeatherFighter), CombatHullId.ColdWeatherFighter),
        new EnemyDefinition(EnemyId.SiegeGunship, typeof(SiegeGunship), CombatHullId.SiegeGunship),
        new EnemyDefinition(EnemyId.ShieldTransport, typeof(ShieldTransport), CombatHullId.ShieldTransport),
        new EnemyDefinition(EnemyId.BeamDrone, typeof(BeamDrone), CombatHullId.BeamDrone),
        new EnemyDefinition(EnemyId.RelayDrone, typeof(RelayDrone), CombatHullId.RelayDrone),
        new EnemyDefinition(EnemyId.StormSkimmer, typeof(StormSkimmer), CombatHullId.StormSkimmer),
        new EnemyDefinition(EnemyId.PursuitFighter, typeof(PursuitFighter), CombatHullId.PursuitFighter),
        new EnemyDefinition(EnemyId.AttackDrone, typeof(AttackDrone), CombatHullId.AttackDrone),
        new EnemyDefinition(EnemyId.CommandGunship, typeof(CommandGunship), CombatHullId.CommandGunship),
        new EnemyDefinition(EnemyId.CourierDiver, typeof(CourierDiver), CombatHullId.CourierDiver),
        new EnemyDefinition(EnemyId.AvalancheBomber, typeof(AvalancheBomber), CombatHullId.AvalancheBomber),
        new EnemyDefinition(EnemyId.ArcLancer, typeof(ArcLancer), CombatHullId.ArcLancer),
        new EnemyDefinition(EnemyId.FieldsHeavyWing, typeof(FieldsHeavyWing), CombatHullId.FieldsHeavyWing),
        new EnemyDefinition(EnemyId.TunnelDefensePlatform, typeof(TunnelDefensePlatform), CombatHullId.TunnelDefensePlatform),
        new EnemyDefinition(EnemyId.SnowSiegeCraft, typeof(SnowSiegeCraft), CombatHullId.SnowSiegeCraft),
        new EnemyDefinition(EnemyId.StormReactor, typeof(StormReactor), CombatHullId.StormReactor),
        new EnemyDefinition(EnemyId.CityCommandCarrier, typeof(CityCommandCarrier), CombatHullId.CityCommandCarrier),
        new EnemyDefinition(EnemyId.SandEmitter, typeof(SandEmitter), CombatHullId.SandEmitter),
        new EnemyDefinition(EnemyId.SandStriker, typeof(SandStriker), CombatHullId.SandStriker),
        new EnemyDefinition(EnemyId.SandWorm, typeof(SandWorm), CombatHullId.SandWorm),
        new EnemyDefinition(EnemyId.BonbonSkirmisher, typeof(BonbonSkirmisher), CombatHullId.BonbonSkirmisher),
        new EnemyDefinition(EnemyId.WaferDart, typeof(WaferDart), CombatHullId.WaferDart),
        new EnemyDefinition(EnemyId.MintSpinner, typeof(MintSpinner), CombatHullId.MintSpinner),
        new EnemyDefinition(EnemyId.MeadowTank, typeof(MeadowTank), CombatHullId.MeadowTank),
        new EnemyDefinition(EnemyId.FrostAuger, typeof(FrostAuger), CombatHullId.FrostAuger),
        new EnemyDefinition(EnemyId.OrbitalScout, typeof(OrbitalScout), CombatHullId.OrbitalScout),
        new EnemyDefinition(EnemyId.LaserSatellite, typeof(LaserSatellite), CombatHullId.LaserSatellite),
        new EnemyDefinition(EnemyId.OrbitalCarrier, typeof(OrbitalCarrier), CombatHullId.OrbitalCarrier),
        new EnemyDefinition(EnemyId.OrbitalMoonlet, typeof(OrbitalMoonlet), CombatHullId.OrbitalMoonlet),
        new EnemyDefinition(EnemyId.SkullRevenant, typeof(SkullRevenant), CombatHullId.SkullRevenant),
        new EnemyDefinition(EnemyId.GhostGalleon, typeof(GhostGalleon), CombatHullId.GhostGalleon),
        new EnemyDefinition(EnemyId.CinderImp, typeof(CinderImp), CombatHullId.CinderImp),
        new EnemyDefinition(EnemyId.ReefDart, typeof(ReefDart), CombatHullId.ReefDart),
        new EnemyDefinition(EnemyId.MantaRay, typeof(MantaRay), CombatHullId.MantaRay),
        new EnemyDefinition(EnemyId.Bathysphere, typeof(Bathysphere), CombatHullId.Bathysphere),
        new EnemyDefinition(EnemyId.ScarabDredger, typeof(ScarabDredger), CombatHullId.ScarabDredger),
        new EnemyDefinition(EnemyId.TaffyDuelist, typeof(TaffyDuelist), CombatHullId.TaffyDuelist),
        new EnemyDefinition(EnemyId.OrbitalWarden, typeof(OrbitalWarden), CombatHullId.OrbitalWarden),
        new EnemyDefinition(EnemyId.ReefLeviathan, typeof(ReefLeviathan), CombatHullId.ReefLeviathan),
        new EnemyDefinition(EnemyId.InfernalFerryman, typeof(InfernalFerryman), CombatHullId.InfernalFerryman),
        new EnemyDefinition(EnemyId.BoneHarrier, typeof(BoneHarrier), CombatHullId.BoneHarrier),
        new EnemyDefinition(EnemyId.AshCenser, typeof(AshCenser), CombatHullId.AshCenser),
        new EnemyDefinition(EnemyId.JetSquid, typeof(JetSquid), CombatHullId.JetSquid),
        new EnemyDefinition(EnemyId.NautilusBell, typeof(NautilusBell), CombatHullId.NautilusBell),
        new EnemyDefinition(EnemyId.CometTug, typeof(CometTug), CombatHullId.CometTug),
        new EnemyDefinition(EnemyId.EclipseSail, typeof(EclipseSail), CombatHullId.EclipseSail),
        new EnemyDefinition(EnemyId.GumdropMortar, typeof(GumdropMortar), CombatHullId.GumdropMortar),
        new EnemyDefinition(EnemyId.RibbonGlider, typeof(RibbonGlider), CombatHullId.RibbonGlider),
        new EnemyDefinition(EnemyId.MirageObelisk, typeof(MirageObelisk), CombatHullId.MirageObelisk),
        new EnemyDefinition(EnemyId.DuneMoth, typeof(DuneMoth), CombatHullId.DuneMoth),
        new EnemyDefinition(EnemyId.StormOrgan, typeof(StormOrgan), CombatHullId.StormOrgan),
        new EnemyDefinition(EnemyId.CapacitorKite, typeof(CapacitorKite), CombatHullId.CapacitorKite),
        new EnemyDefinition(EnemyId.RivetMantis, typeof(RivetMantis), CombatHullId.RivetMantis),
        new EnemyDefinition(EnemyId.BaffleSpinner, typeof(BaffleSpinner), CombatHullId.BaffleSpinner),
        new EnemyDefinition(EnemyId.TerraceGunboat, typeof(TerraceGunboat), CombatHullId.TerraceGunboat),
        new EnemyDefinition(EnemyId.MetroLancer, typeof(MetroLancer), CombatHullId.MetroLancer),
        new EnemyDefinition(EnemyId.HailLantern, typeof(HailLantern), CombatHullId.HailLantern),
        new EnemyDefinition(EnemyId.IcicleWeaver, typeof(IcicleWeaver), CombatHullId.IcicleWeaver),
        new EnemyDefinition(EnemyId.HarrowBomber, typeof(HarrowBomber), CombatHullId.HarrowBomber),
        new EnemyDefinition(EnemyId.ReedSkimmer, typeof(ReedSkimmer), CombatHullId.ReedSkimmer),
        new EnemyDefinition(EnemyId.HellCrabBoss, typeof(HellCrabBoss), CombatHullId.HellCrabBoss, true),
        new EnemyDefinition(EnemyId.AbyssSubmarineBoss, typeof(AbyssSubmarineBoss), CombatHullId.AbyssSubmarineBoss, true),
        new EnemyDefinition(EnemyId.SpaceCitadelBoss, typeof(SpaceCitadelBoss), CombatHullId.SpaceCitadelBoss, true),
        new EnemyDefinition(EnemyId.CandyRegentBoss, typeof(CandyRegentBoss), CombatHullId.CandyRegentBoss, true),
        new EnemyDefinition(EnemyId.DesertWormBoss, typeof(DesertWormBoss), CombatHullId.DesertWormBoss, true),
        new EnemyDefinition(EnemyId.TunnelSawBoss, typeof(TunnelSawBoss), CombatHullId.TunnelSawBoss, true),
        new EnemyDefinition(EnemyId.SnowSiegeBoss, typeof(SnowSiegeBoss), CombatHullId.SnowSiegeBoss, true),
        new EnemyDefinition(EnemyId.ThunderLanceBoss, typeof(ThunderLanceBoss), CombatHullId.ThunderLanceBoss, true),
        new EnemyDefinition(EnemyId.CityDuoBoss, typeof(CityDuoBoss), CombatHullId.CityDuoBoss, true),
        new EnemyDefinition(EnemyId.FieldsBomberBoss, typeof(FieldsBomberBoss), CombatHullId.FieldsBomberBoss, true),
    };
    static readonly Dictionary<string, EnemyDefinition> byKey = new Dictionary<string, EnemyDefinition>(StringComparer.Ordinal);
    static readonly Dictionary<Type, EnemyDefinition> byType = new Dictionary<Type, EnemyDefinition>();
    public static readonly Type[] WaveTypes;
    static EnemyCatalog()
    {
        foreach (var actor in All)
        {
            if (!typeof(DistrictEnemy).IsAssignableFrom(actor.ComponentType) || actor.ComponentType.IsAbstract)
                throw new InvalidOperationException("Invalid actor: " + actor.Key);
            byKey.Add(actor.Key, actor);
            byType.Add(actor.ComponentType, actor);

        }
        WaveTypes = new[] { typeof(ScoutInterceptor), typeof(PatrolGunship), typeof(HeavyBomber), typeof(WallTurret), typeof(MaintenanceDrone), typeof(ArmoredCarrier), typeof(ColdWeatherFighter), typeof(SiegeGunship), typeof(ShieldTransport), typeof(BeamDrone), typeof(RelayDrone), typeof(StormSkimmer), typeof(PursuitFighter), typeof(AttackDrone), typeof(CommandGunship), typeof(CourierDiver), typeof(AvalancheBomber), typeof(ArcLancer), typeof(FieldsHeavyWing), typeof(TunnelDefensePlatform), typeof(SnowSiegeCraft), typeof(StormReactor), typeof(CityCommandCarrier), typeof(SandEmitter), typeof(SandStriker), typeof(SandWorm), typeof(BonbonSkirmisher), typeof(WaferDart), typeof(MintSpinner), typeof(MeadowTank), typeof(FrostAuger), typeof(OrbitalScout), typeof(LaserSatellite), typeof(OrbitalCarrier), typeof(OrbitalMoonlet), typeof(BoneHarrier), typeof(AshCenser), typeof(JetSquid), typeof(NautilusBell), typeof(CometTug), typeof(EclipseSail), typeof(GumdropMortar), typeof(RibbonGlider), typeof(MirageObelisk), typeof(DuneMoth), typeof(StormOrgan), typeof(CapacitorKite), typeof(RivetMantis), typeof(BaffleSpinner), typeof(TerraceGunboat), typeof(MetroLancer), typeof(HailLantern), typeof(IcicleWeaver), typeof(HarrowBomber), typeof(ReedSkimmer), typeof(SkullRevenant), typeof(GhostGalleon), typeof(CinderImp), typeof(ReefDart), typeof(MantaRay), typeof(Bathysphere), typeof(ScarabDredger), typeof(TaffyDuelist), typeof(OrbitalWarden), typeof(ReefLeviathan), typeof(InfernalFerryman) };
    }
    public static EnemyDefinition Get(Type type) => byType.TryGetValue(type, out var actor) ? actor : throw new ArgumentException("Unregistered actor: " + type);
    public static EnemyDefinition Get(EnemyId id) => byKey[id.ToString()];
    public static Type Resolve(string key) => byKey.TryGetValue(key, out var actor) ? actor.ComponentType : null;
}
