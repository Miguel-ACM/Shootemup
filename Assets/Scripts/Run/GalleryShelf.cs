using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[PreferBinarySerialization]
public sealed class GalleryShelf : ScriptableObject
{
    public const int Columns = 11;
    [System.Serializable]
    public sealed class Exhibit
    {
        public string key, title, description;
        public AItem.ItemType category;
        public Mesh mesh;
        public Material paint;
    }
    static readonly Dictionary<string, Exhibit> prepared = new Dictionary<string, Exhibit>();
    static ResourceRequest[] requests;
    public static ResourceRequest Request(int row)
    {
        if (requests == null)
            requests = new ResourceRequest[(ItemCatalog.Exhibits.Count + Columns - 1) / Columns];
        if (requests[row] == null)
        {
            requests[row] = Resources.LoadAsync<GalleryShelf>("Gallery/Shelf" + row);
            requests[row].priority = -1;
        }
        return requests[row];
    }
    public static IEnumerator Warm()
    {
        for (int first = 0; first < ItemCatalog.Exhibits.Count; first += Columns)
        {
            var request = Request(first / Columns);
            yield return request;
            if (request.asset is GalleryShelf shelf)
                shelf.Register();
            yield return null;
        }
    }
    public void Register()
    {
        foreach (var exhibit in exhibits)
            prepared[exhibit.key] = exhibit;
    }
    public static bool TryGet(string key, out Exhibit exhibit) => prepared.TryGetValue(key, out exhibit);

    internal static bool Resolve(string key, out Exhibit exhibit)
    {
        if (TryGet(key, out exhibit)) return true;
        if (!ItemCatalog.TryGet(key, out var definition)) return false;
        var probe = new GameObject("Read gallery metadata");
        probe.SetActive(false);
        var item = definition.Attach(probe);
        if (string.IsNullOrEmpty(item.GetData()[0]))
            item.GetType().GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public)?.Invoke(item, null);
        var data = item.GetData();
        var mesh = IndustrialMeshFactory.ExpansionItem(key);
        var paint = EverydayItemMaterials.For(key);
        var kind = item.Kind;
        if (Application.isPlaying) Destroy(probe);
        else DestroyImmediate(probe);
        if (mesh == null || paint == null) return false;
        exhibit = new Exhibit { key = key, title = data[0], description = data[2], category = kind, mesh = mesh, paint = paint };
        prepared[key] = exhibit;
        return true;
    }

    public Exhibit[] exhibits;
}
