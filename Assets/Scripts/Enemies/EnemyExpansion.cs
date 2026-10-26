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
        { this.level = level; this.type = type; this.hull = hull; this.scale = scale; this.nose = nose; this.model = model; this.ports = ports; this.engines = engines; this.parts = parts; }
    }
    public static readonly Entry[] All = {
        // Individual enemies are registered here with their own content commits.
        new Entry(LevelId.Space, typeof(CometTug), 76, 0.55f, false, IndustrialMeshFactory.CometTugMesh, new[] { new Vector3(-.45f,-.98f,-.2f), new Vector3(.68f,-.71f,-.2f) }, System.Array.Empty<Vector3>(), WavePartLibrary.CometTugParts),
        new Entry(LevelId.Space, typeof(EclipseSail), 75, 0.57f, false, IndustrialMeshFactory.EclipseSailMesh, new[] { new Vector3(-.52f,-.61f,-.3f), new Vector3(.52f,-.61f,-.3f) }, System.Array.Empty<Vector3>(), WavePartLibrary.EclipseSailParts),
        new Entry(LevelId.Candyland, typeof(GumdropMortar), 74, 0.53f, false, IndustrialMeshFactory.GumdropMortarMesh, new[] { new Vector3(-.46f,-.58f,-.21f), new Vector3(.46f,-.58f,-.21f) }, new[] { new Vector3(-.28f,.68f,0), new Vector3(.28f,.68f,0) }, WavePartLibrary.GumdropMortarParts),
        new Entry(LevelId.Candyland, typeof(RibbonGlider), 73, 0.46f, false, IndustrialMeshFactory.RibbonGliderMesh, new[] { new Vector3(-.49f,-.44f,-.16f), new Vector3(.49f,-.44f,-.16f) }, new[] { new Vector3(-.28f,.68f,0), new Vector3(.28f,.68f,0) }, WavePartLibrary.RibbonGliderParts),
        new Entry(LevelId.Desert, typeof(MirageObelisk), 72, 0.56f, false, IndustrialMeshFactory.MirageObeliskMesh, new[] { new Vector3(-.43f,-.87f,-.2f), new Vector3(.43f,-.87f,-.2f) }, new[] { new Vector3(-.28f,.68f,0), new Vector3(.28f,.68f,0) }, WavePartLibrary.MirageObeliskParts),
        new Entry(LevelId.Desert, typeof(DuneMoth), 71, 0.53f, false, IndustrialMeshFactory.DuneMothMesh, new[] { new Vector3(-.72f,-.27f,-.16f), new Vector3(.72f,-.27f,-.16f) }, new[] { new Vector3(-.28f,.68f,0), new Vector3(.28f,.68f,0) }, WavePartLibrary.DuneMothParts),
        new Entry(LevelId.Thunder, typeof(StormOrgan), 70, 0.57f, false, IndustrialMeshFactory.StormOrganMesh, new[] { new Vector3(-.38f,-1.02f,-.23f), new Vector3(.38f,-1.02f,-.23f) }, new[] { new Vector3(-.28f,.68f,0), new Vector3(.28f,.68f,0) }, WavePartLibrary.StormOrganParts),
        new Entry(LevelId.Thunder, typeof(CapacitorKite), 69, 0.53f, false, IndustrialMeshFactory.CapacitorKiteMesh, new[] { new Vector3(-.52f,-.48f,-.26f), new Vector3(.52f,-.48f,-.26f) }, new[] { new Vector3(-.28f,.68f,0), new Vector3(.28f,.68f,0) }, WavePartLibrary.CapacitorKiteParts),
        new Entry(LevelId.Tunnel, typeof(RivetMantis), 68, 0.55f, false, IndustrialMeshFactory.RivetMantisMesh, new[] { new Vector3(-.78f,-.77f,-.16f), new Vector3(.78f,-.77f,-.16f) }, new[] { new Vector3(-.28f,.68f,0), new Vector3(.28f,.68f,0) }, WavePartLibrary.RivetMantisParts),
        new Entry(LevelId.Tunnel, typeof(BaffleSpinner), 67, 0.48f, false, IndustrialMeshFactory.BaffleSpinnerMesh, new[] { new Vector3(-.64f,-.3f,-.16f), new Vector3(.64f,-.3f,-.16f) }, new[] { new Vector3(-.28f,.68f,0), new Vector3(.28f,.68f,0) }, WavePartLibrary.BaffleSpinnerParts),
        new Entry(LevelId.City, typeof(TerraceGunboat), 66, 0.53f, false, IndustrialMeshFactory.TerraceGunboatMesh, new[] { new Vector3(-.82f,-.48f,-.18f), new Vector3(.82f,-.48f,-.18f) }, new[] { new Vector3(-.28f,.68f,0), new Vector3(.28f,.68f,0) }, WavePartLibrary.TerraceGunboatParts),
        new Entry(LevelId.City, typeof(MetroLancer), 65, 0.41f, true, IndustrialMeshFactory.MetroLancerMesh, new[] { new Vector3(-.2f,-1.02f,-.15f), new Vector3(.2f,-1.02f,-.15f) }, new[] { new Vector3(-.28f,.68f,0), new Vector3(.28f,.68f,0) }, WavePartLibrary.MetroLancerParts),
        new Entry(LevelId.Snow, typeof(HailLantern), 64, 0.58f, false, IndustrialMeshFactory.HailLanternMesh, new[] { new Vector3(0,-.76f,-.32f) }, new[] { new Vector3(-.28f,.68f,0), new Vector3(.28f,.68f,0) }, WavePartLibrary.HailLanternParts),
        new Entry(LevelId.Snow, typeof(IcicleWeaver), 63, 0.49f, false, IndustrialMeshFactory.IcicleWeaverMesh, new[] { new Vector3(-.55f,-.78f,-.18f), new Vector3(.55f,-.78f,-.18f) }, new[] { new Vector3(-.28f,.68f,0), new Vector3(.28f,.68f,0) }, WavePartLibrary.IcicleWeaverParts),
        new Entry(LevelId.Fields, typeof(HarrowBomber), 62, 0.61f, false, IndustrialMeshFactory.HarrowBomberMesh, new[] { new Vector3(-.72f,-.58f,-.23f), new Vector3(.72f,-.58f,-.23f) }, new[] { new Vector3(-.28f,.68f,0), new Vector3(.28f,.68f,0) }, WavePartLibrary.HarrowBomberParts),
        new Entry(LevelId.Fields, typeof(ReedSkimmer), 61, 0.49f, false, IndustrialMeshFactory.ReedSkimmerMesh, new[] { new Vector3(-.58f,-.62f,-.16f), new Vector3(.58f,-.62f,-.16f) }, new[] { new Vector3(-.28f,.68f,0), new Vector3(.28f,.68f,0) }, WavePartLibrary.ReedSkimmerParts),
    };
    public static Entry Find(int hull) => Array.Find(All, e => e.hull == hull);
    public static Entry Find(string name) => Array.Find(All, e => e.type.Name == name);
    public static Type[] Types => Array.ConvertAll(All, e => e.type);
    public static Mesh Mesh(int hull) => Find(hull)?.model() ?? throw new ArgumentException("Missing authored hull " + hull);
    public static Material Material(int hull)
    {
        var entry = Find(hull);
        return entry != null && entry.level == LevelId.Space ? SpaceMaterials.Fleet : entry != null && entry.level == LevelId.Candyland ? CandyMaterials.Glaze : CombatMaterials.Fleet;
    }
    public static Type[] Include(Type[] types, LevelId level)
    {
        var result = new List<Type>(types);
        foreach (var entry in All) if (entry.level == level) result.Add(entry.type);
        return result.ToArray();
    }
}

public static partial class WavePartLibrary
{
    static readonly Dictionary<LevelId, WavePart[]> expanded = new Dictionary<LevelId, WavePart[]>();
    public static WavePart[] For(LevelId level)
    {
        if (expanded.TryGetValue(level, out var cached)) return cached;
        var parts = new List<WavePart>(BaseFor(level));
        foreach (var entry in EnemyExpansion.All) if (entry.level == level) parts.AddRange(entry.parts);
        return expanded[level] = parts.ToArray();
    }
}
