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
