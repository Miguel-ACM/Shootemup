using System;
using System.Collections.Generic;
using UnityEngine;

// Explicit references preserve new actors in builds and make their model, ports,
// footprint and wave parts one reviewable piece of content.
public static class EnemyExpansion
{
    public sealed class Entry
    {
        public LevelId level;
        public Type type;
        public int hull;
        public float scale;
        public bool nose;
        public Func<Mesh> model;
        public Vector3[] ports, engines;
        public WavePart[] parts;
        public Entry(LevelId level, Type type, int hull, float scale, bool nose, Func<Mesh> model, Vector3[] ports, Vector3[] engines, WavePart[] parts)
        {
            this.level = level;
            this.type = type;
            this.hull = hull;
            this.scale = scale;
            this.nose = nose;
            this.model = model;
            this.ports = ports;
            this.engines = engines;
            this.parts = parts;
        }
    }

    static Entry DeepEntry(LevelId level, Type type, CombatHullId id, float scale, bool nose, WavePart[] parts, params Vector3[] ports)
        => new Entry(level, type, (int)id, scale, nose, () => IndustrialMeshFactory.DeepHull(id), ports, Array.Empty<Vector3>(), parts);

    public static readonly Entry[] All = {
        new Entry(LevelId.Mushroom, typeof(PuffballDrifter), (int)CombatHullId.PuffballDrifter, .53f, false, IndustrialMeshFactory.BuildPuffballDrifter,
            new[] { new Vector3(0, .28f, -.40f) }, Array.Empty<Vector3>(), WavePartLibrary.PuffballDrifterParts),
        new Entry(LevelId.Mushroom, typeof(BracketFan), (int)CombatHullId.BracketFan, .53f, false, IndustrialMeshFactory.BuildBracketFan,
            new[] { new Vector3(-.18f, -.60f, -.16f), new Vector3(.20f, -.49f, -.17f) }, Array.Empty<Vector3>(), WavePartLibrary.BracketFanParts),
        new Entry(LevelId.Mushroom, typeof(SporeBell), (int)CombatHullId.SporeBell, .61f, false, IndustrialMeshFactory.SporeBellMesh,
            new[] { new Vector3(0f, -.59f, -.26f) }, Array.Empty<Vector3>(), WavePartLibrary.SporeBellParts),
        new Entry(LevelId.Mushroom, typeof(CapRicochet), (int)CombatHullId.CapRicochet, .44f, false, IndustrialMeshFactory.CapRicochetMesh,
            new[] { new Vector3(0f, -.72f, -.24f) }, Array.Empty<Vector3>(), WavePartLibrary.CapRicochetParts),
        new Entry(LevelId.Mushroom, typeof(MyceliumLancer), (int)CombatHullId.MyceliumLancer, .56f, false, IndustrialMeshFactory.MyceliumLancerMesh,
            new[] { new Vector3(0f, -.86f, -.24f) }, Array.Empty<Vector3>(), WavePartLibrary.MyceliumLancerParts),
        new Entry(LevelId.Mushroom, typeof(MushroomChampion), (int)CombatHullId.MushroomChampion, 1.04f, false, IndustrialMeshFactory.MushroomChampionMesh,
            new[] { new Vector3(-.48f, -.47f, -.28f), new Vector3(.48f, -.47f, -.28f) }, Array.Empty<Vector3>(), Array.Empty<WavePart>()),
        new Entry(LevelId.Mushroom, typeof(MushroomWarden), (int)CombatHullId.MushroomWarden, .92f, false, IndustrialMeshFactory.MushroomWardenMesh,
            new[] { IndustrialMeshFactory.WardenLeftMuzzle, IndustrialMeshFactory.WardenRightMuzzle, IndustrialMeshFactory.WardenNoseMuzzle }, Array.Empty<Vector3>(), Array.Empty<WavePart>()),
        new Entry(LevelId.Dream, typeof(HourglassWeaver), (int)CombatHullId.HourglassWeaver, .61f, false, IndustrialMeshFactory.BuildHourglassWeaver,
            new[] { new Vector3(-.33f, -.68f, -.16f), new Vector3(.33f, -.68f, -.16f) }, Array.Empty<Vector3>(), WavePartLibrary.HourglassWeaverParts),
        new Entry(LevelId.Dream, typeof(InkQuill), (int)CombatHullId.InkQuill, .54f, false, IndustrialMeshFactory.BuildInkQuill,
            new[] { new Vector3(-.26f, -.88f, -.16f) }, Array.Empty<Vector3>(), WavePartLibrary.InkQuillParts),
        new Entry(LevelId.Dream, typeof(PaperKite), (int)CombatHullId.PaperKite, .48f, false, IndustrialMeshFactory.PaperKiteMesh,
            new[] { new Vector3(0f, -.85f, -.22f) }, Array.Empty<Vector3>(), WavePartLibrary.PaperKiteParts),
        new Entry(LevelId.Dream, typeof(CrescentDancer), (int)CombatHullId.CrescentDancer, .62f, false, IndustrialMeshFactory.CrescentDancerMesh,
            new[] { new Vector3(-.5f, -.53f, -.22f), new Vector3(.5f, -.53f, -.22f) }, Array.Empty<Vector3>(), WavePartLibrary.CrescentDancerParts),
        new Entry(LevelId.Dream, typeof(PaintedMask), (int)CombatHullId.PaintedMask, .76f, false, IndustrialMeshFactory.PaintedMaskMesh,
            new[] { new Vector3(.12f, -.77f, -.24f) }, Array.Empty<Vector3>(), WavePartLibrary.PaintedMaskParts),
        new Entry(LevelId.Dream, typeof(DreamChampion), (int)CombatHullId.DreamChampion, .96f, false, IndustrialMeshFactory.DreamChampionMesh,
            new[] { new Vector3(-.76f, -.45f, -.25f), new Vector3(.76f, -.45f, -.25f) }, Array.Empty<Vector3>(), Array.Empty<WavePart>()),
        new Entry(LevelId.Dream, typeof(DreamPainter), (int)CombatHullId.DreamPainter, .97f, false, IndustrialMeshFactory.DreamPainterMesh,
            Array.Empty<Vector3>(), Array.Empty<Vector3>(), Array.Empty<WavePart>()),
        DeepEntry(LevelId.Sewers, typeof(PlagueMote), CombatHullId.PlagueMote, .24f, false, WavePartLibrary.PlagueMoteParts),
        DeepEntry(LevelId.Sewers, typeof(SiltLeech), CombatHullId.SiltLeech, .51f, false, WavePartLibrary.SiltLeechParts),
        DeepEntry(LevelId.Sewers, typeof(SluiceBarge), CombatHullId.SluiceBarge, .81f, false, WavePartLibrary.SluiceBargeParts, new Vector3(-.46f, -.84f, -.43f), new Vector3(0f, -.84f, -.43f), new Vector3(.46f, -.84f, -.43f)),
        DeepEntry(LevelId.Sewers, typeof(OverflowBell), CombatHullId.OverflowBell, .64f, false, WavePartLibrary.OverflowBellParts, new Vector3(-.77f, .21f, -.35f), new Vector3(.77f, .21f, -.35f)),
        DeepEntry(LevelId.Sewers, typeof(PipeSkater), CombatHullId.PipeSkater, .46f, true, WavePartLibrary.PipeSkaterParts, new Vector3(0f, -1.05f, -.23f)),
        DeepEntry(LevelId.Sewers, typeof(GrateKeeper), CombatHullId.GrateKeeper, .92f, false, Array.Empty<WavePart>(), new Vector3(-.64f, -.6f, -.56f), new Vector3(.64f, -.6f, -.56f)),
        DeepEntry(LevelId.Sewers, typeof(CisternHeart), CombatHullId.CisternHeart, 1.02f, false, Array.Empty<WavePart>(), new Vector3(-1.4f, -.47f, -.5f), new Vector3(-1.61f, .19f, -.46f), new Vector3(-1.13f, .8f, -.41f), new Vector3(1.4f, -.47f, -.5f), new Vector3(1.61f, .19f, -.46f), new Vector3(1.13f, .8f, -.41f), new Vector3(0f, 1.11f, -.49f)),
        DeepEntry(LevelId.Core, typeof(TransitCube), CombatHullId.TransitCube, .59f, false, WavePartLibrary.TransitCubeParts),
        DeepEntry(LevelId.Core, typeof(FuseCube), CombatHullId.FuseCube, .59f, false, WavePartLibrary.FuseCubeParts),
        DeepEntry(LevelId.Core, typeof(SentryCube), CombatHullId.SentryCube, .58f, false, WavePartLibrary.SentryCubeParts, new Vector3(0f, -.43f, -.64f)),
        DeepEntry(LevelId.Core, typeof(RamCube), CombatHullId.RamCube, .76f, false, WavePartLibrary.RamCubeParts, new Vector3(-.57f, -.13f, -.62f), new Vector3(.57f, -.13f, -.62f)),
        DeepEntry(LevelId.Core, typeof(RelayCarrier), CombatHullId.RelayCarrier, .78f, false, WavePartLibrary.RelayCarrierParts, new Vector3(-.69f, -.81f, -.24f), new Vector3(.69f, -.81f, -.24f)),
        DeepEntry(LevelId.Core, typeof(RivetCutter), CombatHullId.RivetCutter, .51f, true, WavePartLibrary.RivetCutterParts, new Vector3(0f, -1.03f, -.25f)),
        DeepEntry(LevelId.Core, typeof(PressWarden), CombatHullId.PressWarden, .98f, false, Array.Empty<WavePart>(), new Vector3(-.77f, -.65f, -.5f), new Vector3(.77f, -.65f, -.5f)),
        DeepEntry(LevelId.Core, typeof(CoreGantry), CombatHullId.CoreGantry, 1f, false, Array.Empty<WavePart>(), new Vector3(-3.78f, -.53f, -.55f), new Vector3(3.78f, -.53f, -.55f)),
        new Entry(LevelId.Underwater, typeof(DepthMine), (int)CombatHullId.DepthMine, .62f, false, IndustrialMeshFactory.DepthMineMesh, new[] { new Vector3(0, 0, -.5f) }, Array.Empty<Vector3>(), WavePartLibrary.DepthMineParts),
        new Entry(LevelId.Heaven, typeof(ThroneBarge), 89, .79f, false, IndustrialMeshFactory.ThroneBargeMesh, new[] { new Vector3(0,-1.03f,-.23f), new Vector3(-.9f,-.87f,-.27f), new Vector3(.9f,-.87f,-.27f) }, new[] { new Vector3(-.7f,.8f,0), new Vector3(.7f,.8f,0) }, WavePartLibrary.ThroneBargeParts),
        new Entry(LevelId.Heaven, typeof(VotiveSpear), 88, .43f, true, IndustrialMeshFactory.VotiveSpearMesh, new[] { new Vector3(-.24f,-.835f,-.17f), new Vector3(.24f,-.835f,-.17f) }, new[] { new Vector3(0,.84f,0) }, WavePartLibrary.VotiveSpearParts),
        new Entry(LevelId.Heaven, typeof(PrismCantor), 87, .56f, false, IndustrialMeshFactory.PrismCantorMesh, new[] { new Vector3(-.68f,-.6f,-.3f), new Vector3(.68f,-.6f,-.3f), new Vector3(0,1.2f,-.1f) }, System.Array.Empty<Vector3>(), WavePartLibrary.PrismCantorParts),
        new Entry(LevelId.Heaven, typeof(AegisReliquary), 86, .66f, false, IndustrialMeshFactory.AegisReliquaryMesh, new[] { new Vector3(-.72f,-.96f,-.25f), new Vector3(.72f,-.96f,-.25f) }, new[] { new Vector3(-.5f,.85f,0), new Vector3(.5f,.85f,0) }, WavePartLibrary.AegisReliquaryParts),
        new Entry(LevelId.Heaven, typeof(ChoirLancer), 85, .52f, false, IndustrialMeshFactory.ChoirLancerMesh, new[] { new Vector3(0,-1.0f,-.23f) }, new[] { new Vector3(0,.85f,0) }, WavePartLibrary.ChoirLancerParts),
        // Individual enemies are registered here with their own content commits.
        new Entry(LevelId.Hell, typeof(BoneHarrier), (int)CombatHullId.BoneHarrier, 0.49f, true, IndustrialMeshFactory.BoneHarrierMesh, new[] { new Vector3(0,-.96f,-.23f) }, System.Array.Empty<Vector3>(), WavePartLibrary.BoneHarrierParts),
        new Entry(LevelId.Hell, typeof(AshCenser), (int)CombatHullId.AshCenser, 0.58f, false, IndustrialMeshFactory.AshCenserMesh, new[] { new Vector3(-.41f,-.52f,-.24f), new Vector3(.41f,-.52f,-.24f) }, System.Array.Empty<Vector3>(), WavePartLibrary.AshCenserParts),
        new Entry(LevelId.Underwater, typeof(JetSquid), (int)CombatHullId.JetSquid, 0.51f, true, IndustrialMeshFactory.JetSquidMesh, new[] { new Vector3(0,-.92f,-.22f) }, System.Array.Empty<Vector3>(), WavePartLibrary.JetSquidParts),
        new Entry(LevelId.Underwater, typeof(NautilusBell), (int)CombatHullId.NautilusBell, 0.57f, false, IndustrialMeshFactory.NautilusBellMesh, new[] { new Vector3(-.44f,-.51f,-.23f), new Vector3(.44f,-.51f,-.23f) }, System.Array.Empty<Vector3>(), WavePartLibrary.NautilusBellParts),
        new Entry(LevelId.Space, typeof(CometTug), (int)CombatHullId.CometTug, 0.55f, false, IndustrialMeshFactory.CometTugMesh, new[] { new Vector3(-.45f,-.98f,-.2f), new Vector3(.68f,-.71f,-.2f) }, System.Array.Empty<Vector3>(), WavePartLibrary.CometTugParts),
        new Entry(LevelId.Space, typeof(EclipseSail), (int)CombatHullId.EclipseSail, 0.57f, false, IndustrialMeshFactory.EclipseSailMesh, new[] { new Vector3(-.52f,-.61f,-.3f), new Vector3(.52f,-.61f,-.3f) }, System.Array.Empty<Vector3>(), WavePartLibrary.EclipseSailParts),
        new Entry(LevelId.Candyland, typeof(GumdropMortar), (int)CombatHullId.GumdropMortar, 0.53f, false, IndustrialMeshFactory.GumdropMortarMesh, new[] { new Vector3(-.46f,-.58f,-.21f), new Vector3(.46f,-.58f,-.21f) }, new[] { new Vector3(-0.29f,0.55f,0), new Vector3(0.29f,0.55f,0) }, WavePartLibrary.GumdropMortarParts),
        new Entry(LevelId.Candyland, typeof(RibbonGlider), (int)CombatHullId.RibbonGlider, 0.46f, false, IndustrialMeshFactory.RibbonGliderMesh, new[] { new Vector3(-.49f,-.44f,-.16f), new Vector3(.49f,-.44f,-.16f) }, new[] { new Vector3(-0.22f,0.65f,0), new Vector3(0.22f,0.65f,0) }, WavePartLibrary.RibbonGliderParts),
        new Entry(LevelId.Desert, typeof(MirageObelisk), (int)CombatHullId.MirageObelisk, 0.56f, false, IndustrialMeshFactory.MirageObeliskMesh, new[] { new Vector3(-.43f,-.87f,-.2f), new Vector3(.43f,-.87f,-.2f) }, new[] { new Vector3(-0.43f,0.79f,0), new Vector3(0.43f,0.79f,0) }, WavePartLibrary.MirageObeliskParts),
        new Entry(LevelId.Desert, typeof(DuneMoth), (int)CombatHullId.DuneMoth, 0.53f, false, IndustrialMeshFactory.DuneMothMesh, new[] { new Vector3(-.72f,-.27f,-.16f), new Vector3(.72f,-.27f,-.16f) }, new[] { new Vector3(-0.29f,0.68f,0), new Vector3(0.29f,0.68f,0) }, WavePartLibrary.DuneMothParts),
        new Entry(LevelId.Thunder, typeof(StormOrgan), (int)CombatHullId.StormOrgan, 0.57f, false, IndustrialMeshFactory.StormOrganMesh, new[] { new Vector3(-.38f,-1.02f,-.23f), new Vector3(.38f,-1.02f,-.23f) }, new[] { new Vector3(-0.38f,0.82f,0), new Vector3(0.38f,0.82f,0) }, WavePartLibrary.StormOrganParts),
        new Entry(LevelId.Thunder, typeof(FaradayPylon), (int)CombatHullId.FaradayPylon, .55f, false, IndustrialMeshFactory.FaradayPylonMesh, new[] { new Vector3(-.98f,0,-.2f), new Vector3(.98f,0,-.2f) }, System.Array.Empty<Vector3>(), WavePartLibrary.FaradayPylonParts),
        new Entry(LevelId.Thunder, typeof(CapacitorKite), (int)CombatHullId.CapacitorKite, 0.53f, false, IndustrialMeshFactory.CapacitorKiteMesh, new[] { new Vector3(-.52f,-.48f,-.26f), new Vector3(.52f,-.48f,-.26f) }, new[] { new Vector3(-0.28f,0.61f,0), new Vector3(0.28f,0.61f,0) }, WavePartLibrary.CapacitorKiteParts),
        new Entry(LevelId.Tunnel, typeof(RivetMantis), (int)CombatHullId.RivetMantis, 0.55f, false, IndustrialMeshFactory.RivetMantisMesh, new[] { new Vector3(-.78f,-.77f,-.16f), new Vector3(.78f,-.77f,-.16f) }, new[] { new Vector3(-0.3f,0.69f,0), new Vector3(0.3f,0.69f,0) }, WavePartLibrary.RivetMantisParts),
        new Entry(LevelId.Tunnel, typeof(BaffleSpinner), (int)CombatHullId.BaffleSpinner, 0.48f, false, IndustrialMeshFactory.BaffleSpinnerMesh, new[] { new Vector3(-.64f,-.3f,-.16f), new Vector3(.64f,-.3f,-.16f) }, new[] { new Vector3(-0.3f,0.55f,0), new Vector3(0.3f,0.55f,0) }, WavePartLibrary.BaffleSpinnerParts),
        new Entry(LevelId.City, typeof(TerraceGunboat), (int)CombatHullId.TerraceGunboat, 0.53f, false, IndustrialMeshFactory.TerraceGunboatMesh, new[] { new Vector3(-.82f,-.48f,-.18f), new Vector3(.82f,-.48f,-.18f) }, new[] { new Vector3(-0.65f,0.52f,0), new Vector3(0.65f,0.52f,0) }, WavePartLibrary.TerraceGunboatParts),
        new Entry(LevelId.City, typeof(MetroLancer), (int)CombatHullId.MetroLancer, 0.41f, true, IndustrialMeshFactory.MetroLancerMesh, new[] { new Vector3(-.2f,-1.02f,-.15f), new Vector3(.2f,-1.02f,-.15f) }, new[] { new Vector3(-0.24f,0.74f,0), new Vector3(0.24f,0.74f,0) }, WavePartLibrary.MetroLancerParts),
        new Entry(LevelId.Snow, typeof(HailLantern), (int)CombatHullId.HailLantern, 0.58f, false, IndustrialMeshFactory.HailLanternMesh, new[] { new Vector3(0,-.76f,-.32f) }, new[] { new Vector3(-0.33f,0.66f,0), new Vector3(0.33f,0.66f,0) }, WavePartLibrary.HailLanternParts),
        new Entry(LevelId.Snow, typeof(IcicleWeaver), (int)CombatHullId.IcicleWeaver, 0.49f, false, IndustrialMeshFactory.IcicleWeaverMesh, new[] { new Vector3(-.55f,-.78f,-.18f), new Vector3(.55f,-.78f,-.18f) }, new[] { new Vector3(-0.37f,0.59f,0), new Vector3(0.37f,0.59f,0) }, WavePartLibrary.IcicleWeaverParts),
        new Entry(LevelId.Fields, typeof(HarrowBomber), (int)CombatHullId.HarrowBomber, 0.61f, false, IndustrialMeshFactory.HarrowBomberMesh, new[] { new Vector3(-.72f,-.58f,-.23f), new Vector3(.72f,-.58f,-.23f) }, new[] { new Vector3(-0.7f,0.64f,0), new Vector3(0.7f,0.64f,0) }, WavePartLibrary.HarrowBomberParts),
        new Entry(LevelId.Fields, typeof(ReedSkimmer), (int)CombatHullId.ReedSkimmer, 0.49f, false, IndustrialMeshFactory.ReedSkimmerMesh, new[] { new Vector3(-.58f,-.62f,-.16f), new Vector3(.58f,-.62f,-.16f) }, new[] { new Vector3(0f,0.64f,0) }, WavePartLibrary.ReedSkimmerParts),
    };
    static readonly Dictionary<int, Entry> byHull = new Dictionary<int, Entry>();
    static readonly Dictionary<string, Entry> byName = new Dictionary<string, Entry>();
    static EnemyExpansion()
    {
        foreach (var entry in All)
        {
            byHull.Add(entry.hull, entry);
            byName.Add(entry.type.Name, entry);
        }
    }

    public static Entry Find(int hull) => byHull.TryGetValue(hull, out var entry) ? entry : null;
    public static Entry Find(string name) => byName.TryGetValue(name, out var entry) ? entry : null;
    public static Type[] Types => Array.ConvertAll(All, e => e.type);
    public static Mesh Mesh(int hull) => Find(hull)?.model() ?? throw new ArgumentException("Missing authored hull " + hull);
    public static Material Material(int hull)
    {
        if (hull == (int)CombatHullId.DreamPainter) return IndustrialMeshFactory.DreamPorcelain;
        if (hull == (int)CombatHullId.MushroomWarden) return IndustrialMeshFactory.MothPaint;
        if(hull==91)return HeavenAngelVisual.Surface;
        var entry = Find(hull);
        return entry != null && entry.level == LevelId.Heaven ? HeavenMaterials.Ivory : entry != null && entry.level == LevelId.Space ? SpaceMaterials.Fleet : entry != null && entry.level == LevelId.Candyland ? CandyMaterials.Glaze : CombatMaterials.Fleet;
    }

    public static Type[] Include(Type[] types, LevelId level)
    {
        var result = new List<Type>(types);
        foreach (var entry in All)
            if (entry.level == level && !typeof(DistrictBoss).IsAssignableFrom(entry.type))
                result.Add(entry.type);
        return result.ToArray();
    }
}

public static partial class WavePartLibrary
{
    static readonly Dictionary<LevelId, WavePart[]> expanded = new Dictionary<LevelId, WavePart[]>();
    static readonly Dictionary<(LevelId, DistrictCondition), WavePart[]> conditioned = new Dictionary<(LevelId, DistrictCondition), WavePart[]>();

    // Every authored part of a district, including those flown only under a condition.
    public static WavePart[] All(LevelId level)
    {
        if (expanded.TryGetValue(level, out var cached))
            return cached;
        var parts = new List<WavePart>(BaseFor(level));
        foreach (var entry in EnemyExpansion.All)
            if (entry.level == level)
                parts.AddRange(entry.parts);
        return expanded[level] = parts.ToArray();
    }

    // The parts this run flies in the district, under the condition its seed drew.
    public static WavePart[] For(LevelId level) => For(level, DistrictConditions.For(level, LevelRun.Seed));

    public static WavePart[] For(LevelId level, DistrictCondition condition)
    {
        if (conditioned.TryGetValue((level, condition), out var cached))
            return cached;
        return conditioned[(level, condition)] = Array.FindAll(All(level), part => part.Flies(condition));
    }
}
