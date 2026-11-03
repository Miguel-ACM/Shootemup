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
            requests = new ResourceRequest[(ItemCatalog.All.Count + Columns - 1) / Columns];
        if (requests[row] == null)
        {
            requests[row] = Resources.LoadAsync<GalleryShelf>("Gallery/Shelf" + row);
            requests[row].priority = -1;
        }
        return requests[row];
    }
    public static IEnumerator Warm()
    {
        for (int first = 0; first < ItemCatalog.All.Count; first += Columns)
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
    public Exhibit[] exhibits;
}
