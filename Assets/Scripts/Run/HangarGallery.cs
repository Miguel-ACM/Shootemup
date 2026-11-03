using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class HangarHub
{
    const float GalleryLeft = 6.5f, GalleryRight = 69, GalleryBottom = .2f, GalleryTop = 48, GalleryWing = 22, GalleryVestibuleTop = 12;
    static readonly Vector2 FilterDesk = new Vector2(11.4f, 4.5f);
    const float FirstX = 24.8f, FirstY = 14.8f, ColumnStep = 4.15f, RowStep = 3.8f;
    static readonly string[] availability = { "EVERY ITEM", "RELEASED", "LOCKED" };
    GalleryShelf.Exhibit[] exhibits;
    MeshRenderer[] pieces;
    Material silhouette, guidePaint;
    Mesh guideMesh;
    Mesh floorLights;
    MeshRenderer[] platforms;
    int[] caseVertices;
    Color[] litFloorColors;
    List<Color> floorColors;
    MaterialPropertyBlock dimItem;
    static readonly int ItemTint = Shader.PropertyToID("_Tint");
    static readonly Color GalleryDeck = new Color(.075f, .11f, .13f);
    RectTransform cardPanel;
    TextMeshProUGUI cardText, categoryReadout, availabilityReadout, directoryControls;
    GalleryLampMatrix caseLeds;
    int hoverIndex = -1, galleryCategory, galleryAvailability;
    bool galleryReady;
    int loadedItems;
    static Vector2 Case(int i) => new Vector2(FirstX + (i % GalleryShelf.Columns) * ColumnStep, FirstY + (i / GalleryShelf.Columns) * RowStep);
    bool Released(GalleryShelf.Exhibit e) => !ItemCatalog.Get(e.key).InRewardPool || MetaProgress.IsUnlocked(e.key);

    void BuildGallery()
    {
        exhibits = new GalleryShelf.Exhibit[ItemCatalog.All.Count];
        pieces = new MeshRenderer[exhibits.Length];
        platforms = new MeshRenderer[exhibits.Length];
        caseVertices = new int[exhibits.Length + 1];
        dimItem = new MaterialPropertyBlock();
        var floor = new List<Vector3>();
        var colors = new List<Color>();
        var indices = new List<int>();
        void Bar(Vector2 pos, Vector2 size, Color color) => GalleryQuad(floor, colors, indices, pos, size, color);
        var decking = GalleryDeck;
        Bar(new Vector2((GalleryLeft + GalleryRight) / 2, (GalleryBottom + GalleryVestibuleTop) / 2),
            new Vector2(GalleryRight - GalleryLeft, GalleryVestibuleTop - GalleryBottom), decking);
        Bar(new Vector2((GalleryWing + GalleryRight) / 2, (GalleryVestibuleTop + GalleryTop) / 2),
            new Vector2(GalleryRight - GalleryWing, GalleryTop - GalleryVestibuleTop), decking);
        // The locking seams and cable gutters continue around the corner into the display aisles.
        var seam = new Color(.052f, .079f, .089f);
        var edge = new Color(.20f, .26f, .26f);
        for (float x = GalleryLeft + 2; x < GalleryRight; x += 3)
            Bar(new Vector2(x, (GalleryBottom + GalleryVestibuleTop) / 2), new Vector2(.05f, GalleryVestibuleTop - GalleryBottom), seam);
        for (float y = GalleryBottom + 1; y < GalleryTop; y += RowStep)
        {
            float left = y < GalleryVestibuleTop ? GalleryLeft : GalleryWing;
            Bar(new Vector2((left + GalleryRight) / 2, y), new Vector2(GalleryRight - left, .055f), seam);
        }
        Bar(new Vector2((GalleryLeft + GalleryWing) / 2, GalleryVestibuleTop), new Vector2(GalleryWing - GalleryLeft, .21f), edge);
        Bar(new Vector2(GalleryWing, (GalleryTop + GalleryVestibuleTop) / 2), new Vector2(.18f, GalleryTop - GalleryVestibuleTop), edge);
        Bar(new Vector2((GalleryLeft + GalleryRight) / 2, GalleryBottom), new Vector2(GalleryRight - GalleryLeft, .25f), edge);
        // Paired marker lights lead around the directory to the north exhibition hall.
        for (float x = FilterDesk.x + 2; x < FirstX + 1; x += .8f)
            Bar(new Vector2(x, FilterDesk.y), new Vector2(.23f, .075f), DockInstrument.Brass);
        for (float y = FilterDesk.y + .8f; y < FirstY - 1; y += .8f)
            Bar(new Vector2(FirstX, y), new Vector2(.075f, .23f), DockInstrument.Brass);
        for (int i = 0; i < exhibits.Length; i++)
        {
            caseVertices[i] = colors.Count;
            var at = Case(i);
            // A fixed pool of red light and an offset contact shadow beneath the levitating base.
            GalleryWell(floor, colors, indices, at + new Vector2(.17f, -.35f), 1.39f, new Color(.48f, .065f, .045f), GalleryDeck);
            GalleryWell(floor, colors, indices, at + new Vector2(.23f, -.76f), .93f, new Color(.012f, .016f, .021f, .86f), Color.clear);
            Bar(at + Vector2.down * 1.12f, new Vector2(.47f, .055f), GalleryLampMatrix.RouteBlue);
        }
        caseVertices[exhibits.Length] = colors.Count;
        floorLights = GallerySurface("Gallery floor and footlights", floor, colors, indices, -14);
        floorColors = colors;
        litFloorColors = colors.ToArray();
        guidePaint = new Material(Resources.Load<Shader>("GalleryGuide"));
        guidePaint.SetVector("_Origin", new Vector4(FilterDesk.x, FilterDesk.y, 0, 0));
        guideMesh = GallerySurface("Blue inlaid guide lamps", new List<Vector3>(), new List<Color>(), new List<int>(), -10);
        BuildControlDesk((int)Dock.Armory);
        MountReceiver((int)Dock.Armory);
        HoverSign("CASES >", FilterDesk + new Vector2(2.6f, -2.5f), DockInstrument.Brass);
        cardPanel = MountedPanel("Gallery object label", Vector2.zero, new Vector2(6.6f, 2.4f));
        cardText = WorldLabel(cardPanel, "", Vector2.zero, new Vector2(624, 225), 27, DockInstrument.Paper);
        cardPanel.gameObject.SetActive(false);
        BuildDirectory();
        StartCoroutine(InstallShelves());
    }
    static void GalleryWell(List<Vector3> vertices, List<Color> colors, List<int> indices, Vector2 at, float radius, Color core, Color edge)
    {
        int first = vertices.Count;
        vertices.Add(at);
        colors.Add(core);
        const int sides = 16;
        for (int i = 0; i < sides; i++)
        {
            float angle = i * 2 * Mathf.PI / sides;
            vertices.Add(at + new Vector2(Mathf.Cos(angle), .9f * Mathf.Sin(angle)) * radius);
            colors.Add(edge);
            indices.Add(first);
            indices.Add(first + 1 + (i + 1) % sides);
            indices.Add(first + 1 + i);
        }
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
        go.transform.SetParent(FixtureParent, false);
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
                yield break;
            }
            shelf.Register();
            double start = Time.realtimeSinceStartupAsDouble;
            for (int j = 0; j < shelf.exhibits.Length; j++)
            {
                int i = first + j;
                var e = exhibits[i] = shelf.exhibits[j];
                var rig = new GameObject("Floating exhibit " + i).transform;
                rig.SetParent(interiors[(int)Dock.Armory].fixtures.transform, false);
                rig.position = Spot(Case(i), 0);
                var platform = MountModel("HangarShopPlinth", rig.position + new Vector3(0, -.24f, 2.4f), -4, "Background", rig);
                platform.localScale = Vector3.one * 2.18f;
                platform.localRotation = PlatformTilt;
                platforms[i] = platform.GetComponent<MeshRenderer>();
                var go = new GameObject("Museum exhibit " + i);
                go.transform.SetParent(rig, false);
                var bounds = e.mesh.bounds;
                var rotation = Quaternion.Euler(-18, 28, -9);
                float scale = 1.24f / Mathf.Max(.01f, bounds.size.x, bounds.size.y, bounds.size.z);
                go.transform.SetPositionAndRotation(rig.position + new Vector3(0, .15f, 1) - rotation * (bounds.center * scale), rotation);
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
        ApplyGalleryFilter();
    }
    void FloatGalleryPlatforms()
    {
        Vector2 focus = view.transform.position - center;
        float halfHeight = view.orthographicSize;
        float halfWidth = halfHeight * (405f / 720f);
        for (int i = 0; i < loadedItems; i++)
        {
            Vector2 at = Case(i);
            if (Mathf.Abs(at.x - focus.x) > halfWidth + 1.5f || Mathf.Abs(at.y - focus.y) > halfHeight + 2)
                continue;
            // Move the hull and its cargo together. The selection point, lights and labels stay steady.
            platforms[i].transform.parent.position = Spot(at + Vector2.up * PlatformLift(i), 0);
        }
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
    void LightCase(int i, bool match)
    {
        const float dim = .22f;
        for (int v = caseVertices[i]; v < caseVertices[i + 1]; v++)
        {
            Color lit = litFloorColors[v];
            Color faded = Color.Lerp(GalleryDeck, lit, dim);
            faded.a = lit.a;
            floorColors[v] = match ? lit : faded;
        }
        TintExhibit(platforms[i], match, dim);
        TintExhibit(pieces[i], match, dim);
    }
    void TintExhibit(MeshRenderer model, bool match, float dim)
    {
        var paint = model.sharedMaterial;
        if (match || paint == silhouette)
        {
            model.SetPropertyBlock(null);
            return;
        }
        var tint = paint.GetColor(ItemTint);
        dimItem.SetColor(ItemTint, new Color(tint.r * dim, tint.g * dim, tint.b * dim, tint.a));
        model.SetPropertyBlock(dimItem);
    }
    void ApplyGalleryFilter()
    {
        if (!galleryReady)
            return;
        int band = galleryCategory;
        var v = new List<Vector3>();
        var c = new List<Color>();
        var t = new List<int>();
        var blue = GalleryLampMatrix.RouteBlue;
        int count = 0;
        for (int column = 0; column < GalleryShelf.Columns; column++)
        {
            float highest = FilterDesk.y;
            for (int i = column; i < exhibits.Length; i += GalleryShelf.Columns)
            {
                bool match = Matches(exhibits[i]);
                caseLeds.matches[i] = match;
                LightCase(i, match);
                if (!match)
                    continue;
                count++;
                var at = Case(i);
                if (band == 0 && galleryAvailability == 0)
                    continue;
                highest = at.y;
                GalleryQuad(v, c, t, at + Vector2.left * .94f, new Vector2(.45f, .055f), blue);
                GalleryQuad(v, c, t, at + Vector2.down * 1.12f, new Vector2(.55f, .10f), blue);
            }
            if (highest <= FilterDesk.y)
                continue;
            float x = Case(column).x - 1.14f;
            GalleryQuad(v, c, t, new Vector2(x, (highest + FilterDesk.y) / 2), new Vector2(.055f, highest - FilterDesk.y), blue);
            GalleryQuad(v, c, t, new Vector2((x + FilterDesk.x) / 2, FilterDesk.y), new Vector2(Mathf.Abs(x - FilterDesk.x), .055f), blue);
        }
        caseLeds.SetVerticesDirty();
        floorLights.SetColors(floorColors);
        guideMesh.Clear();
        guideMesh.SetVertices(v);
        guideMesh.SetColors(c);
        guideMesh.SetTriangles(t, 0);
        if (categoryReadout != null)
            categoryReadout.text = categories[band];
        if (availabilityReadout != null)
            availabilityReadout.text = availability[galleryAvailability] + "  /  " + count.ToString("00");
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
        string source = known ? DecisionOverlay.Category(e.category).ToUpperInvariant() : trial != null ? "Certify " + trial.title : "Outfitter  ·  " + MetaProgress.Price(ItemPool.RarityOf(e.key)) + " coins";
        cardText.text = "<b>" + (known ? e.title : "???") + "</b>\n" + (known ? e.description : "???") + "\n<size=78%><color=#E9BC74>" + source + "</color></size>";
        float height = cardText.GetPreferredValues(cardText.text, 624, Mathf.Infinity).y;
        cardPanel.sizeDelta = new Vector2(660, height + 36);
        cardText.rectTransform.sizeDelta = new Vector2(624, height + 4);
        readouts.Aim(cardPanel, Spot(Case(i) + Vector2.up * (1.03f + .005f * cardPanel.sizeDelta.y), 0));
    }
    void BuildDirectory()
    {
        var root = new GameObject("Gallery directory tube", typeof(RectTransform));
        var display = new DockInstrument(root, dialog?.font, DockInstrument.Brass);
        readouts.MountAt((RectTransform)root.transform, Spot(MonitorAt((int)Dock.Armory), 1), .012f * MonitorScale((int)Dock.Armory), layer: installing.lettering.transform);
        display.Label("02   /   GALLERY", -41, 255, 476, 34, 19, DockInstrument.Brass, true);
        display.Label("Directory", 0, 167, 550, 74, 38, DockInstrument.Paper, true);
        categoryReadout = display.Label(categories[0], 0, 86, 550, 48, 27, DockInstrument.Brass, true);
        availabilityReadout = display.Label(availability[0], 0, 38, 550, 38, 21, DockInstrument.Paper);
        caseLeds = new GameObject("Case index", typeof(RectTransform), typeof(CanvasRenderer), typeof(GalleryLampMatrix)).GetComponent<GalleryLampMatrix>();
        caseLeds.transform.SetParent(display.face, false);
        caseLeds.raycastTarget = false;
        caseLeds.matches = new bool[exhibits.Length];
        caseLeds.rectTransform.sizeDelta = new Vector2(202, 160);
        caseLeds.rectTransform.anchoredPosition = new Vector2(-153, -88);
        display.Label("< > TYPE", 119, -59, 269, 42, 22, DockInstrument.Muted);
        display.Label("^ v ACCESS", 119, -107, 269, 42, 22, DockInstrument.Muted);
        directoryControls = display.Label("", 0, -254, 550, 36, 18, DockInstrument.Paper);
    }
}
