using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class HangarHub
{
    const float GalleryLeft = 6.5f, GalleryRight = 41, GalleryBottom = -24, GalleryTop = 11;
    static readonly Vector2 FilterDesk = new Vector2(23, -22);
    const float FirstX = 10.5f, FirstY = -10, ColumnStep = 2.85f, RowStep = 2.48f;
    static readonly string[] availability = { "EVERY ITEM", "RELEASED", "LOCKED" };
    GalleryShelf.Exhibit[] exhibits;
    MeshRenderer[] pieces;
    Material silhouette, guidePaint;
    Mesh guideMesh;
    RectTransform cardPanel;
    TextMeshProUGUI cardText, filterStatus, categoryReadout, availabilityReadout, directoryControls;
    int hoverIndex = -1, galleryCategory, galleryAvailability;
    bool galleryReady;
    bool loadingShelves = true;
    int loadedItems;
    static Vector2 Case(int i) => new Vector2(FirstX + (i % GalleryShelf.Columns) * ColumnStep, FirstY + (i / GalleryShelf.Columns) * RowStep);
    bool Released(GalleryShelf.Exhibit e) => !ItemCatalog.Get(e.key).InRewardPool || MetaProgress.IsUnlocked(e.key);

    void BuildGallery()
    {
        exhibits = new GalleryShelf.Exhibit[ItemCatalog.All.Count];
        pieces = new MeshRenderer[exhibits.Length];
        silhouette = new Material(Resources.Load<Shader>("GallerySilhouette"));
        var floor = new List<Vector3>();
        var colors = new List<Color>();
        var indices = new List<int>();
        void Bar(Vector2 pos, Vector2 size, Color color) => GalleryQuad(floor, colors, indices, pos, size, color);
        Bar(new Vector2((GalleryLeft + GalleryRight) / 2, (GalleryBottom + GalleryTop) / 2), new Vector2(GalleryRight - GalleryLeft, GalleryTop - GalleryBottom), new Color(.075f, .11f, .13f));
        for (int i = 0; i < exhibits.Length; i++)
        {
            var at = Case(i);
            Bar(at, new Vector2(1.85f, 1.48f), new Color(.20f, .27f, .29f));
            Bar(at + Vector2.up * .04f, new Vector2(1.69f, 1.3f), new Color(.30f, .34f, .34f));
            Bar(at + Vector2.down * .78f, new Vector2(.53f, .055f), DockInstrument.Brass);
        }
        GallerySurface("Museum cases", floor, colors, indices, -14);
        guidePaint = new Material(Resources.Load<Shader>("GalleryGuide"));
        guidePaint.SetVector("_Origin", new Vector4(FilterDesk.x, FilterDesk.y, 0, 0));
        guideMesh = GallerySurface("Red inlaid guide lamps", new List<Vector3>(), new List<Color>(), new List<int>(), -10);
        Strip("Catalog console base", FilterDesk, new Vector2(3.5f, 1), Metal, -5);
        Strip("Catalog input rail", FilterDesk + Vector2.down * .28f, new Vector2(2.9f, .18f), DockInstrument.Brass, -4);
        MountReceiver(FilterDesk + Vector2.up * 3.7f);
        HangingSign("GALLERY", new Vector2(9.5f, 8.8f), DockInstrument.Brass, true);
        HangingSign("FILTER  ↓", new Vector2(10, 3.4f), DockInstrument.Sea, false);
        var shelfPanel = MountedPanel("Filter console display", FilterDesk + Vector2.up * .42f, new Vector2(3.6f, .9f));
        filterStatus = WorldLabel(shelfPanel, "FILTER", Vector2.zero, new Vector2(342, 80), 31, DockInstrument.Brass);
        cardPanel = MountedPanel("Gallery object label", Vector2.zero, new Vector2(5, 2.4f));
        cardText = WorldLabel(cardPanel, "", Vector2.zero, new Vector2(475, 225), 32, DockInstrument.Paper);
        cardPanel.gameObject.SetActive(false);
        BuildDirectory();
        StartCoroutine(InstallShelves());
    }
    static void GalleryQuad(List<Vector3> vertices, List<Color> colors, List<int> indices, Vector2 at, Vector2 size, Color color)
    {
        int n = vertices.Count;
        vertices.Add(at + new Vector2(-size.x, -size.y) * .5f);
        vertices.Add(at + new Vector2(-size.x, size.y) * .5f);
        vertices.Add(at + size * .5f);
        vertices.Add(at + new Vector2(size.x, -size.y) * .5f);
        for (int j = 0; j < 4; j++)
            colors.Add(color);
        indices.Add(n);
        indices.Add(n + 1);
        indices.Add(n + 2);
        indices.Add(n);
        indices.Add(n + 2);
        indices.Add(n + 3);
    }
    Mesh GallerySurface(string name, List<Vector3> vertices, List<Color> colors, List<int> triangles, int order)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        quads.Add(mesh);
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = Spot(Vector2.zero, order == -14 ? 65 : 60);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = order == -10 ? guidePaint : scenery;
        r.sortingLayerName = "Background";
        r.sortingOrder = order;
        return mesh;
    }
    IEnumerator InstallShelves()
    {
        yield return null;
        for (int first = 0; first < exhibits.Length; first += GalleryShelf.Columns)
        {
            var request = GalleryShelf.Request(first / GalleryShelf.Columns);
            yield return request;
            var shelf = request.asset as GalleryShelf;
            if (shelf == null)
            {
                Debug.LogError("Missing baked gallery shelf " + first);
                loadingShelves = false;
                yield break;
            }
            shelf.Register();
            double start = Time.realtimeSinceStartupAsDouble;
            for (int j = 0; j < shelf.exhibits.Length; j++)
            {
                int i = first + j;
                var e = exhibits[i] = shelf.exhibits[j];
                var go = new GameObject("Museum exhibit " + i);
                go.transform.SetParent(transform, false);
                var bounds = e.mesh.bounds;
                var rotation = Quaternion.Euler(-18, 28, -9);
                float scale = 1.24f / Mathf.Max(.01f, bounds.size.x, bounds.size.y, bounds.size.z);
                go.transform.SetPositionAndRotation(Spot(Case(i), 1) - rotation * (bounds.center * scale), rotation);
                go.transform.localScale = Vector3.one * scale;
                go.AddComponent<MeshFilter>().sharedMesh = e.mesh;
                var r = pieces[i] = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = Released(e) ? e.paint : silhouette;
                r.sortingLayerName = "Background";
                r.sortingOrder = -2;
                loadedItems++;
                if (Time.realtimeSinceStartupAsDouble - start > .0015)
                {
                    yield return null;
                    start = Time.realtimeSinceStartupAsDouble;
                }
            }
            yield return null;
        }
        galleryReady = true;
        loadingShelves = false;
        ApplyGalleryFilter();
    }
    void RefreshGalleryAccess()
    {
        for (int i = 0; i < loadedItems; i++)
            pieces[i].sharedMaterial = Released(exhibits[i]) ? exhibits[i].paint : silhouette;
        hoverIndex = -1;
        ApplyGalleryFilter();
    }
    bool Matches(GalleryShelf.Exhibit e)
    {
        bool released = Released(e);
        return (galleryCategory == 0 || (int)e.category == galleryCategory - 1) && (galleryAvailability == 0 || (galleryAvailability == 1) == released);
    }
    void ApplyGalleryFilter()
    {
        if (!galleryReady)
            return;
        int band = galleryCategory;
        var v = new List<Vector3>();
        var c = new List<Color>();
        var t = new List<int>();
        var red = new Color(1, .14f, .085f);
        int count = 0;
        for (int column = 0; column < GalleryShelf.Columns; column++)
        {
            float highest = FilterDesk.y;
            for (int i = column; i < exhibits.Length; i += GalleryShelf.Columns)
            {
                if (!Matches(exhibits[i]))
                    continue;
                count++;
                var at = Case(i);
                if (band == 0 && galleryAvailability == 0)
                    continue;
                highest = at.y;
                GalleryQuad(v, c, t, at + Vector2.left * .94f, new Vector2(.45f, .055f), red);
                GalleryQuad(v, c, t, at + Vector2.down * .78f, new Vector2(.55f, .10f), red);
            }
            if (highest <= FilterDesk.y)
                continue;
            float x = Case(column).x - 1.14f;
            GalleryQuad(v, c, t, new Vector2(x, (highest + FilterDesk.y) / 2), new Vector2(.055f, highest - FilterDesk.y), red);
            GalleryQuad(v, c, t, new Vector2((x + FilterDesk.x) / 2, FilterDesk.y), new Vector2(Mathf.Abs(x - FilterDesk.x), .055f), red);
        }
        guideMesh.Clear();
        guideMesh.SetVertices(v);
        guideMesh.SetColors(c);
        guideMesh.SetTriangles(t, 0);
        filterStatus.text = categories[band] + "   ·   " + count;
        if (categoryReadout != null)
            categoryReadout.text = categories[band];
        if (availabilityReadout != null)
            availabilityReadout.text = availability[galleryAvailability] + "    ·    " + count + " CASES";
    }
    void GalleryHover(Vector3 p)
    {
        int col = Mathf.RoundToInt((p.x - center.x - FirstX) / ColumnStep);
        int row = Mathf.RoundToInt((p.y - center.y - FirstY) / RowStep);
        int i = row * GalleryShelf.Columns + col;
        if (operating || col < 0 || col >= GalleryShelf.Columns || i < 0 || i >= loadedItems ||
           Vector2.Distance(p, Spot(Case(i), 0)) > 1.05f)
            i = -1;
        if (i == hoverIndex)
            return;
        hoverIndex = i;
        cardPanel.gameObject.SetActive(i >= 0);
        if (i < 0)
            return;
        var e = exhibits[i];
        bool known = Released(e);
        var trial = FlightTrials.Blueprint(e.key);
        string source = known ? e.category.ToString().ToUpperInvariant() : trial != null ? "Certify " + trial.title : "Outfitter  ·  " + MetaProgress.Price(ItemPool.RarityOf(e.key)) + " coins";
        cardText.text = "<b>" + (known ? e.title : "???") + "</b>\n" + (known ? e.description : "???") + "\n<size=78%><color=#E9BC74>" + source + "</color></size>";
        readouts.Aim(cardPanel, Spot(Case(i) + Vector2.up * 1.92f, 0));
    }
    void BuildDirectory()
    {
        var root = new GameObject("Gallery directory tube", typeof(RectTransform));
        var display = new DockInstrument(root, dialog?.font, DockInstrument.Brass);
        readouts.MountAt((RectTransform)root.transform, Spot(FilterDesk + Vector2.up * 3.7f, 1), .012f);
        display.Label("02   /   GALLERY", -41, 255, 476, 34, 19, DockInstrument.Brass, true);
        display.Label("Gallery directory", 0, 167, 550, 74, 38, DockInstrument.Paper, true);
        categoryReadout = display.Label(categories[0], 0, 68, 550, 55, 31, DockInstrument.Brass, true);
        availabilityReadout = display.Label(availability[0], 0, 1, 550, 55, 28, DockInstrument.Paper);
        display.Label("← →   ITEM TYPE       ↑ ↓   RELEASE", 0, -121, 550, 55, 22, DockInstrument.Muted);
        directoryControls = display.Label("", 0, -254, 550, 36, 18, DockInstrument.Paper);
    }
}
