using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Models authored in Blender (ArtSource/Terra) and baked by obj_to_hull.py into Resources/TerraModels.
/// Each line is one polygon: its palette tile, then x y z per corner, already in hull space.
/// An optional first "palette" line gives the model its own sixteen tile colours (items use this).
/// A baked model replaces the procedural model of the same name; muzzles, sockets and colliders stay in code.
/// </summary>
public static partial class IndustrialMeshFactory
{
    static readonly Dictionary<string, Mesh> terraModels = new Dictionary<string, Mesh>();
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
        var b = new Builder();
        foreach (var line in asset.text.Split('\n'))
        {
            var parts = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 49 && parts[0] == "palette")
            {
                var palette = new Color[16];
                for (int i = 0; i < 16; i++)
                    palette[i] = new Color(Float(parts[1 + i * 3]), Float(parts[2 + i * 3]), Float(parts[3 + i * 3]));
                terraPalettes[name] = palette;
                continue;
            }
            if (parts.Length < 10)
                continue;
            int tile = int.Parse(parts[0], CultureInfo.InvariantCulture);
            var points = new Vector3[(parts.Length - 1) / 3];
            for (int i = 0; i < points.Length; i++)
                points[i] = new Vector3(Float(parts[1 + i * 3]), Float(parts[2 + i * 3]), Float(parts[3 + i * 3]));
            b.Face(tile, points);
        }
        return terraModels[name] = b.Finish("Terra " + name);
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
