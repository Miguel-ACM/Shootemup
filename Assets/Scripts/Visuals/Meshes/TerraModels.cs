using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Models authored in Blender (ArtSource/Terra) and baked by obj_to_hull.py into Resources/TerraModels.
/// Each line is one polygon: its palette tile, then x y z per corner, already in hull space.
/// A baked model replaces the procedural hull of the same name; muzzles, sockets and colliders stay in code.
/// </summary>
public static partial class IndustrialMeshFactory
{
    static readonly Dictionary<string, Mesh> terraModels = new Dictionary<string, Mesh>();
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

    static float Float(string text) => float.Parse(text, CultureInfo.InvariantCulture);
}
