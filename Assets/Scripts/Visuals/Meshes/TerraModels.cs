using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Models authored in Blender (ArtSource/Terra) and baked by obj_to_hull.py into Resources/TerraModels.
/// Each line is one polygon: its palette tile, then x y z per corner, already in hull space.
/// An optional first "palette" line gives the model its own sixteen tile colours (items use this).
/// A "part" line starts an animated part (rotor, fin, impeller): its polygons follow until the next part line.
/// A baked model replaces the procedural model of the same name; muzzles, sockets and colliders stay in code.
/// </summary>
public static partial class IndustrialMeshFactory
{
    public sealed class TerraPart
    {
        public string name;
        public bool spin;
        public Vector3 axis, pivot;
        public float speed, amplitude;
        public Mesh mesh;
    }

    static readonly Dictionary<string, Mesh> terraModels = new Dictionary<string, Mesh>();
    static readonly Dictionary<string, List<TerraPart>> terraParts = new Dictionary<string, List<TerraPart>>();
    static readonly Dictionary<string, Color[]> terraPalettes = new Dictionary<string, Color[]>();
    static readonly Dictionary<string, Material> terraMaterials = new Dictionary<string, Material>();
    static readonly HashSet<string> missingTerraModels = new HashSet<string>();

    public static Mesh TerraModel(string name)
    {
        if (terraModels.TryGetValue(name, out var cached) && cached != null)
            return cached;
        if (missingTerraModels.Contains(name))
            return null;
        var asset = Resources.Load<TextAsset>("TerraModels/" + name);
        if (asset == null)
        {
            missingTerraModels.Add(name);
            return null;
        }
        var body = new Builder();
        var b = body;
        var parts = new List<TerraPart>();
        var builders = new List<Builder>();
        TerraPart part = null;
        foreach (var line in asset.text.Split('\n'))
        {
            var fields = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length == 49 && fields[0] == "palette")
            {
                var palette = new Color[16];
                for (int i = 0; i < 16; i++)
                    palette[i] = new Color(Float(fields[1 + i * 3]), Float(fields[2 + i * 3]), Float(fields[3 + i * 3]));
                terraPalettes[name] = palette;
                continue;
            }
            // part <name> <spin|swing> ax ay az px py pz <speed> <amplitude>
            if (fields.Length == 11 && fields[0] == "part")
            {
                part = new TerraPart
                {
                    name = fields[1],
                    spin = fields[2] == "spin",
                    axis = new Vector3(Float(fields[3]), Float(fields[4]), Float(fields[5])).normalized,
                    pivot = new Vector3(Float(fields[6]), Float(fields[7]), Float(fields[8])),
                    speed = Float(fields[9]),
                    amplitude = Float(fields[10]),
                };
                parts.Add(part);
                b = new Builder();
                builders.Add(b);
                continue;
            }
            if (fields.Length < 10)
                continue;
            int tile = int.Parse(fields[0], CultureInfo.InvariantCulture);
            var points = new Vector3[(fields.Length - 1) / 3];
            var origin = part != null ? part.pivot : Vector3.zero;
            for (int i = 0; i < points.Length; i++)
                points[i] = new Vector3(Float(fields[1 + i * 3]), Float(fields[2 + i * 3]), Float(fields[3 + i * 3])) - origin;
            b.Face(tile, points);
        }
        for (int i = 0; i < parts.Count; i++)
            parts[i].mesh = builders[i].Finish("Terra " + name + " " + parts[i].name);
        terraParts[name] = parts;
        return terraModels[name] = body.Finish("Terra " + name);
    }

    /// <summary>Adds a Terra model's animated parts under the transform that shows its body.</summary>
    public static void AttachTerraParts(string name, Transform body, Material material)
    {
        if (TerraModel(name) == null || !terraParts.TryGetValue(name, out var parts))
            return;
        var bodyRenderer = body.GetComponent<MeshRenderer>();
        foreach (var part in parts)
        {
            var go = new GameObject("Terra " + part.name);
            go.transform.SetParent(body, false);
            go.transform.localPosition = part.pivot;
            go.AddComponent<MeshFilter>().sharedMesh = part.mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            if (bodyRenderer != null)
            {
                renderer.sortingLayerID = bodyRenderer.sortingLayerID;
                renderer.sortingOrder = bodyRenderer.sortingOrder;
            }
            go.AddComponent<TerraPartMotion>().part = part;
        }
    }

    /// <summary>The material for a Terra model with its own palette, or null when it uses a district atlas.</summary>
    public static Material TerraMaterial(string name)
    {
        if (terraMaterials.TryGetValue(name, out var cached) && cached != null)
            return cached;
        if (TerraModel(name) == null || !terraPalettes.TryGetValue(name, out var palette))
            return null;
        var material = new Material(CombatMaterials.Fleet) { name = "Terra " + name + " paint" };
        var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Terra " + name + " atlas" };
        var pixels = new Color[256 * 256];
        for (int tile = 0; tile < 16; tile++)
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    // The fleet atlas's bevel: dark rim, lit upper-left edge, shaded lower-right edge.
                    bool edge = x < 5 || x > 58 || y < 5 || y > 58;
                    float shade = edge ? .62f : x < 7 || y > 56 ? 1.18f : x > 56 || y < 7 ? .8f : 1;
                    var c = palette[tile] * shade;
                    c.a = 1;
                    pixels[(tile / 4 * 64 + y) * 256 + tile % 4 * 64 + x] = c;
                }
        texture.SetPixels(pixels);
        texture.Apply();
        material.mainTexture = texture;
        return terraMaterials[name] = material;
    }

    static float Float(string text) => float.Parse(text, CultureInfo.InvariantCulture);
}

/// <summary>Turns a rotor continuously or swings a fin back and forth around its pivot.</summary>
public sealed class TerraPartMotion : MonoBehaviour
{
    public IndustrialMeshFactory.TerraPart part;
    float phase;

    // Mirrored fins on one craft share a phase; separate craft do not move in lockstep.
    void Start() => phase = Mathf.Abs(transform.parent.GetHashCode() % 997) * .013f;

    void Update()
    {
        float t = Time.time + phase;
        float angle = part.spin ? t * part.speed : part.amplitude * Mathf.Sin(t * part.speed * Mathf.PI * 2);
        transform.localRotation = Quaternion.AngleAxis(angle, part.axis);
    }
}
