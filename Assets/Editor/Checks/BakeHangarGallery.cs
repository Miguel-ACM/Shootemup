using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Bake the same pickup geometry and paint used by a run. Runtime requests one shelf
// asynchronously and installs it across frames, without parsing model text or building atlases.
public static class BakeHangarGallery
{
    // Balance/copy changes need fresh labels without rebuilding approved meshes and textures.
    public static void RefreshDescriptions()
    {
        foreach (var shelf in Resources.LoadAll<GalleryShelf>("Gallery"))
        {
            bool changed = false;
            foreach (var exhibit in shelf.exhibits)
            {
                if (!ItemCatalog.TryGet(exhibit.key, out var definition)) continue;
                var probe = new GameObject("Read gallery copy");
                probe.SetActive(false);
                try
                {
                    var item = definition.Attach(probe);
                    item.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(item, null);
                    var data = item.GetData();
                    if (string.IsNullOrEmpty(data[0]) || (exhibit.title == data[0] && exhibit.description == data[2])) continue;
                    exhibit.title = data[0];
                    exhibit.description = data[2];
                    changed = true;
                }
                finally { UnityEngine.Object.DestroyImmediate(probe); }
            }
            if (!changed) continue;
            EditorUtility.SetDirty(shelf);
            Debug.Log("Refreshed gallery copy: " + AssetDatabase.GetAssetPath(shelf));
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Shootemup/Bake hangar gallery")]
    public static void Run()
    {
        const string folder = "Assets/Resources/Gallery";
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        var definitions = ItemCatalog.Exhibits.OrderBy(e => e.Key, StringComparer.Ordinal).ToArray();
        for (int first = 0; first < definitions.Length; first += GalleryShelf.Columns)
        {
            var path = folder + "/Shelf" + (first / GalleryShelf.Columns) + ".asset";
            var shelf = AssetDatabase.LoadAssetAtPath<GalleryShelf>(path);
            if (shelf == null)
            {
                shelf = ScriptableObject.CreateInstance<GalleryShelf>();
                AssetDatabase.CreateAsset(shelf, path);
            }
            else
                foreach (var part in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (part != shelf && part != null)
                        UnityEngine.Object.DestroyImmediate(part, true);
            shelf.exhibits = new GalleryShelf.Exhibit[Math.Min(GalleryShelf.Columns, definitions.Length - first)];
            for (int i = 0; i < shelf.exhibits.Length; i++)
            {
                var definition = definitions[first + i];
                var probe = new GameObject("Read gallery metadata");
                var item = definition.Attach(probe);
                if (string.IsNullOrEmpty(item.GetData()[0]))
                    item.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(item, null);
                var data = item.GetData();
                var mesh = IndustrialMeshFactory.ExpansionItem(definition.Key);
                var paint = EverydayItemMaterials.For(definition.Key);
                if (mesh == null || paint == null || string.IsNullOrEmpty(data[0]))
                    throw new Exception("Incomplete exhibit " + definition.Key);
                if (!EditorUtility.IsPersistent(mesh))
                {
                    mesh = UnityEngine.Object.Instantiate(mesh);
                    mesh.name = definition.Key;
                    AssetDatabase.AddObjectToAsset(mesh, shelf);
                }
                if (!EditorUtility.IsPersistent(paint))
                {
                    paint = new Material(paint) { name = definition.Key + " paint" };
                    var texture = paint.mainTexture as Texture2D;
                    if (texture != null && !EditorUtility.IsPersistent(texture))
                    {
                        var copy = UnityEngine.Object.Instantiate(texture);
                        copy.name = definition.Key + " enamel";
                        copy.Compress(true);
                        copy.Apply(false, true);
                        AssetDatabase.AddObjectToAsset(copy, shelf);
                        paint.mainTexture = copy;
                    }
                    AssetDatabase.AddObjectToAsset(paint, shelf);
                }
                shelf.exhibits[i] = new GalleryShelf.Exhibit { key = definition.Key, title = data[0], description = data[2], category = item.Kind, mesh = mesh, paint = paint };
                UnityEngine.Object.DestroyImmediate(probe);
            }
            EditorUtility.SetDirty(shelf);
            AssetDatabase.SaveAssets();
        }
        Debug.Log("Baked " + definitions.Length + " gallery exhibits");
    }
}
