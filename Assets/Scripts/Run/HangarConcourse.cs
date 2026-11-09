using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed partial class HangarHub
{
    enum Dock
    {
        Fleet, Armory, Simulator, Recorder, Flight
    }
    // Opposite doors share a cross passage. The room centres sit beyond the side guards.
    static readonly Vector2[] berths = {
        new Vector2(-11, 6), new Vector2(11, 6), new Vector2(-11, 20),
        new Vector2(11, 20), new Vector2(11, 34)
    };
    static readonly string[] signs = { "FLEET", "ARMORY", "SIMULATOR", "RECORDER", "FLIGHT CONTROL" };
    static readonly Color[] lamps = { DockInstrument.Sea, DockInstrument.Brass, new Color(.70f,.65f,.98f),
        new Color(.55f,.77f,.94f), new Color(.96f,.59f,.39f) };
    static readonly RunDifficulty[] modes = { RunDifficulty.Easy, RunDifficulty.Normal, RunDifficulty.Hard };
    static readonly string[] factors = { "0.5×", "1×", "2×" };
    static readonly Color Metal = new Color(.19f, .28f, .30f);
    // The lane's own floor tones from the Tunnel level kit: two plate shades over dark seams and gutters.
    static readonly Color Deck = new Color(.075f, .100f, .109f), DeckPlate = new Color(.087f, .112f, .120f),
        DeckSeam = new Color(.043f, .060f, .069f), DeckGutter = new Color(.025f, .040f, .049f);
    static HangarHub current;
    readonly List<Transform> doorLights = new List<Transform>();
    readonly List<Transform> parkedShips = new List<Transform>();
    readonly DockInstrument[] screens = new DockInstrument[5];
    readonly List<GameObject>[] pages = new List<GameObject>[5];
    readonly int[] savedSelection = new int[5], savedBand = new int[5];
    readonly List<RectTransform> berthNames = new List<RectTransform>();
    TextMeshProUGUI berthNotice;
    int inspectedBerth = -1;
    sealed class Interior
    {
        public GameObject fixtures;
        public CanvasGroup lettering;

        public void Show(bool visible)
        {
            fixtures.SetActive(visible);
            lettering.alpha = visible ? 1 : 0;
        }
    }
    readonly Interior[] interiors = new Interior[berths.Length];
    Interior installing;
    Transform FixtureParent => installing != null ? installing.fixtures.transform : transform;
    float repeatAt, cameraAcross, baseViewSize;
    float roomFocusX, panProgress, panVelocity;
    int focusPortal = -1;
    int room = -1, selection, band;
    bool operating;
    DockInstrument instrument;

    public static Vector3 ConstrainTravel(Vector3 previous, Vector3 desired)
    {
        if (current == null)
            return previous;
        return current.ConstrainDeck(previous, desired);
    }

    const float RoomThreshold = 6.5f;
    const float PassageEnd = 12.2f;
    static float FarWall(int dock) => dock == (int)Dock.Fleet ? 28.5f : 20.5f;
    // How much of the closing bulkhead the camera shows past the far wall.
    const float EndWallView = 1.6f;
    static float DoorAlignment(int portal, float y) => portal < 0 ? 0 :
        1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.05f, 2.25f, Mathf.Abs(y - berths[portal].y)));

    Vector3 ConstrainDeck(Vector3 previous, Vector3 desired)
    {
        float x = previous.x - center.x, y = previous.y - center.y;
        int portal = Portal(previous);
        float halfWidth = Camera.RightLimit;
        if (Mathf.Abs(x) > halfWidth + .01f && portal >= 0)
        {
            float side = Mathf.Sign(berths[portal].x);
            float next = (desired.x - center.x) * side;
            float roomY = berths[portal].y;
            bool crossing = Mathf.Abs(y - roomY) < 1.05f;
            bool gallery = portal == (int)Dock.Armory;
            float minX = crossing ? 0 : gallery && y > GalleryVestibuleTop ? GalleryWing : RoomThreshold;
            desired.x = center.x + side * Mathf.Clamp(next, minX, gallery ? GalleryRight : FarWall(portal));
            float height = next < RoomThreshold ? .95f : 6.5f;
            desired.y = center.y + (gallery && next >= RoomThreshold ?
                Mathf.Clamp(desired.y - center.y, GalleryBottom, desired.x - center.x < GalleryWing ? GalleryVestibuleTop : GalleryReach) : Mathf.Clamp(desired.y - center.y, roomY - height, roomY + height));
        }
        else
        {
            bool opening = portal >= 0 && Mathf.Abs(y - berths[portal].y) < 1;
            desired.x = Mathf.Clamp(desired.x, center.x - (opening && berths[portal].x < 0 ? 15.5f : halfWidth),
                center.x + (opening && berths[portal].x > 0 ? 15.5f : halfWidth));
            desired.y = Mathf.Clamp(desired.y, Camera.DownLimit, Camera.UpLimit + DeckExtension);
        }
        return desired;
    }

    int Portal(Vector3 p)
    {
        float x = p.x - center.x, y = p.y - center.y;
        if (x >= 6.4f && y >= GalleryBottom && y <= (x >= GalleryWing ? GalleryTop : GalleryVestibuleTop))
            return (int)Dock.Armory;
        int nearest = -1;
        float distance = 8;
        for (int i = 0; i < berths.Length; i++)
        {
            if ((p.x - center.x) * berths[i].x < 0)
                continue;
            float delta = Mathf.Abs(p.y - center.y - berths[i].y);
            if (delta < distance)
            {
                distance = delta;
                nearest = i;
            }
        }
        return nearest;
    }

    void FollowDeck(Vector3 p)
    {
        if (baseViewSize <= 0)
            baseViewSize = view.orthographicSize;
        float zoom = operating ? 7.5f * MonitorScale(room) + .15f : baseViewSize;
        view.orthographicSize = Mathf.Lerp(view.orthographicSize, zoom, 1 - Mathf.Exp(-6 * Time.deltaTime));
        float lateral = Mathf.Abs(p.x - center.x);
        float halfView = view.orthographicSize * (405f / 720f);
        int portal = Portal(p);
        int entered = lateral > Camera.RightLimit ? portal : -1;
        if (entered != room)
        {
            if (entered >= 0)
                OpenDock(entered);
            else
                CloseDock();
        }
        float across = 0;
        float along = Mathf.Clamp(p.y - center.y, 0, DeckExtension);
        float enter = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(Camera.RightLimit - 1.5f, PassageEnd, lateral));
        float alignment = entered >= 0 ? 1 : DoorAlignment(portal, p.y - center.y);
        enter *= alignment;
        panProgress = Mathf.SmoothDamp(panProgress, enter, ref panVelocity, .65f);
        if (portal >= 0)
        {
            Vector2 target = berths[portal];
            if (operating)
            {
                target = MonitorAt(portal);
                target.y = (target.y + 3.8f * MonitorScale(portal) + StationAt(portal).y - 3.1f) * .5f;
            }
            else if (portal == (int)Dock.Armory)
                target = new Vector2(Mathf.Clamp(p.x - center.x, 11.6f, GalleryRight - halfView), Mathf.Clamp(p.y - center.y, FilterDesk.y + 1.7f, Mathf.Max(FilterDesk.y + 1.7f, GalleryReach - halfView)));
            else
            {
                // Pan along the room, including the separate berthing apron beyond the fleet desk.
                target.x = Mathf.Sign(target.x) * Mathf.Clamp(lateral, Mathf.Max(RoomThreshold + halfView, Mathf.Abs(MonitorAt(portal).x) - .65f), FarWall(portal) + EndWallView - halfView);
                target.y += Mathf.Clamp((p.y - center.y - target.y) * .18f, -.5f, .5f);
            }
            roomFocusX = portal == focusPortal
                ? Mathf.Lerp(roomFocusX, target.x, 1 - Mathf.Exp(-7 * Time.deltaTime)) : target.x;
            float roomDistance = Mathf.Abs(roomFocusX);
            // Bound the returning camera before the fixtures disappear at the lane edge.
            // The last unit of pan eases out as the pilot clears the door.
            float sill = Mathf.InverseLerp(Camera.RightLimit - 1.15f, Camera.RightLimit, lateral);
            float returnLimit = sill * sill + 1.75f * Mathf.Max(0, lateral - Camera.RightLimit);
            panProgress = Mathf.Min(panProgress, returnLimit / roomDistance);
            if (!operating)
            {
                // A boosting pilot can reach the edge before the pan catches up.
                float minPan = Mathf.Clamp01((lateral - halfView + .65f) / roomDistance);
                panProgress = Mathf.Max(panProgress, minPan * alignment);
            }
            across = roomFocusX * panProgress;
            along = Mathf.Lerp(along, target.y, enter);
        }
        SetCameraTravel(Mathf.Lerp(cameraTravel, along, 1 - Mathf.Exp(-7 * Time.deltaTime)));
        focusPortal = portal;
        MoveCamera(Vector3.right * (across - cameraAcross));
        cameraAcross = across;
    }

    void MoveCamera(Vector3 delta)
    {
        if (view == null)
            return;
        view.transform.position += delta;
        if (shaker != null)
            shaker.RestPositionOffset += view.transform.parent != null ? view.transform.parent.InverseTransformVector(delta) : delta;
    }

    void ResetDeckCamera()
    {
        SetCameraTravel(0);
        MoveCamera(Vector3.left * cameraAcross);
        cameraAcross = 0;
        panProgress = panVelocity = 0;
        focusPortal = -1;
        if (view != null && baseViewSize > 0)
            view.orthographicSize = baseViewSize;
    }

    Transform Strip(string title, Vector2 pos, Vector2 size, Color tint, int order = -7, float angle = 0)
    {
        var t = Quad(title, tint, order);
        t.SetParent(FixtureParent, false);
        t.position = Spot(pos, 4 - (order + 10) * .035f);
        t.localScale = new Vector3(size.x * 4, size.y * 4, 1);
        t.localRotation = Quaternion.Euler(0, 0, angle);
        return t;
    }

    TextMeshProUGUI Stencil(string text, Vector2 pos, float size, Color color)
    {
        var panel = MountedPanel("Deck stencil", pos, new Vector2(5, .58f));
        panel.GetComponent<Image>().enabled = false;
        return WorldLabel(panel, text, Vector2.zero, new Vector2(490, 58), Mathf.Max(27, size * 12), color);
    }

    RectTransform MountedPanel(string title, Vector2 position, Vector2 size, Transform follow = null, bool fadeOnReveal = false)
    {
        var go = new GameObject(title, typeof(RectTransform), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size * 100;
        readouts.MountAt(rect, Spot(position, 1), .01f, installing?.lettering.transform, follow, fadeOnReveal);
        go.GetComponent<Image>().color = new Color(.018f, .034f, .043f);
        return rect;
    }

    TextMeshProUGUI WorldLabel(RectTransform face, string text, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        var t = new GameObject("Screen panelFont", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        t.transform.SetParent(face, false);
        t.rectTransform.sizeDelta = size;
        t.rectTransform.anchoredPosition = position;
        if (dialog != null && dialog.font != null)
            t.font = dialog.font;
        t.spriteAsset = InputPrompts.InlineAsset;
        t.text = text;
        t.fontSize = fontSize;
        t.fontSizeMin = fontSize * .8f;
        t.fontSizeMax = fontSize;
        t.enableAutoSizing = true;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return t;
    }

    IEnumerator BuildConcourse()
    {
        for (int i = 0; i < berths.Length; i++)
        {
            installing = interiors[i] = new Interior
            {
                fixtures = new GameObject(signs[i] + " interior"),
                lettering = readouts.CreateRoomLayer(signs[i] + " displays")
            };
            installing.fixtures.transform.SetParent(transform, false);
            if (i == (int)Dock.Armory)
                BuildGallery();
            else
            {
                BuildRoom(i);
                installing.Show(false);
                yield return null;
                room = i;
                selection = i == (int)Dock.Fleet ? (int)MetaProgress.Ship : i == (int)Dock.Flight ? System.Array.IndexOf(modes, difficulty) : 0;
                band = i == (int)Dock.Fleet ? System.Array.IndexOf(modes, difficulty) : 0;
                var screen = new GameObject(signs[i] + " mounted screen", typeof(RectTransform));
                instrument = screens[i] = new DockInstrument(screen, dialog?.font, lamps[i]);
                readouts.MountAt((RectTransform)screen.transform, Spot(MonitorAt(i), 1), .012f * MonitorScale(i), layer: installing.lettering.transform);
                pages[i] = new List<GameObject>();
                RenderPage();
                savedSelection[i] = selection;
                savedBand[i] = band;
                room = -1;
            }
            installing.Show(false);
            installing = null;
            yield return null;
            float side = Mathf.Sign(berths[i].x), y = berths[i].y;
            Strip("Cross-passage floor", new Vector2(side * 5.7f, y), new Vector2(2.8f, 2.5f), Deck, -14);
            for (int edge = -1; edge <= 1; edge += 2)
                Strip("Passage sill", new Vector2(side * 5.7f, y + edge * 1.25f), new Vector2(2.8f, .17f), Metal, -9);
            Strip("Door jamb", new Vector2(side * 4.4f, y), new Vector2(.46f, 3.4f), Metal, -8);
            Strip("Open doorway", new Vector2(side * 4.4f, y), new Vector2(.49f, 2.1f), Deck, -7);
            doorLights.Add(Strip("Entry lamp", new Vector2(side * 4.35f, y + 1.45f), new Vector2(.50f, .13f), lamps[i], -5));
            for (int step = 0; step < 4; step++)
                Strip("Threshold guide", new Vector2(side * (4.75f + step * .55f), y), new Vector2(.25f, .07f), lamps[i], -6, side < 0 ? 180 : 0);
            HoverSign(signs[i], new Vector2(side * 2.95f, y + 2.25f), lamps[i]);
            PortalLight(new Vector2(side * 5.25f, y));
        }
        walletPanel = MountedPanel("Coin reader", new Vector2(-2.55f, 14.45f), new Vector2(3.1f, .88f));
        walletFade = walletPanel.gameObject.AddComponent<CanvasGroup>();
        walletFade.alpha = 0;
        walletText = CoinRow(walletPanel, 0, 296);
        walletPanel.gameObject.SetActive(false);
        stockScreen = MountedPanel("Outfitter display", Vector2.zero, new Vector2(4.35f, 3.08f), fadeOnReveal: true);
        stockInfo = WorldLabel(stockScreen, "", new Vector2(0, 35), new Vector2(395, 195), 28, DockInstrument.Paper);
        stockScreen.gameObject.SetActive(false);
    }

    // Workshop floors continue the hangar lane's plates, so every room shares its colour.
    void RoomDeck(string title, Vector2 mid, Vector2 size, float tramline)
    {
        var v = new List<Vector3>();
        var c = new List<Color>();
        var t = new List<int>();
        GalleryQuad(v, c, t, mid, size, DeckSeam);
        const float pitchX = 2, pitchY = 2.5f;
        int columns = Mathf.CeilToInt(size.x / pitchX), rows = Mathf.CeilToInt(size.y / pitchY);
        for (int row = 0; row < rows; row++)
            for (int col = 0; col < columns; col++)
            {
                // Clip the outer plates to the room instead of letting them spill past the walls.
                float x0 = mid.x - size.x / 2 + col * pitchX + .03f, x1 = Mathf.Min(x0 + pitchX - .06f, mid.x + size.x / 2 - .03f);
                float y0 = mid.y - size.y / 2 + row * pitchY + .04f, y1 = Mathf.Min(y0 + pitchY - .08f, mid.y + size.y / 2 - .04f);
                if (x1 - x0 < .1f || y1 - y0 < .1f)
                    continue;
                GalleryQuad(v, c, t, new Vector2(x0 + x1, y0 + y1) / 2, new Vector2(x1 - x0, y1 - y0), (row + col) % 3 == 0 ? DeckPlate : Deck);
            }
        for (int edge = -1; edge <= 1; edge += 2)
        {
            GalleryQuad(v, c, t, mid + Vector2.up * edge * (size.y / 2 - .12f), new Vector2(size.x, .24f), DeckGutter);
            GalleryQuad(v, c, t, mid + Vector2.right * edge * (size.x / 2 - .12f), new Vector2(.24f, size.y), DeckGutter);
        }
        // Fleet's yellow tramline joins the parking apron to the flight desk.
        if (tramline != 0)
            GalleryQuad(v, c, t, mid + Vector2.right * tramline * size.x * .23f, new Vector2(size.x * .46f, .07f), new Color(.42f, .40f, .32f));
        GallerySurface(title, v, c, t, -15);
    }

    static readonly Color WallFace = new Color(.050f, .066f, .074f), WallRib = new Color(.034f, .046f, .054f),
        WallLip = new Color(.17f, .22f, .23f), WallShadow = new Color(.018f, .028f, .034f);

    // A room ends in a solid bulkhead: a thick far wall with a sealed blast door and
    // walls along the top and bottom, each with a lit lip, ribs and a hazard band, so
    // the floor visibly stops instead of running off the edge of the screen.
    void RoomBulkheads(int index)
    {
        Vector2 b = berths[index];
        float side = Mathf.Sign(b.x);
        float far = FarWall(index), start = RoomThreshold - .45f;
        const float Reach = 10.8f, Face = 6.75f, Thick = 4.6f;
        var v = new List<Vector3>();
        var c = new List<Color>();
        var t = new List<int>();
        // Shadows the walls cast onto the deck.
        GalleryQuad(v, c, t, new Vector2(side * (far + .1f), b.y), new Vector2(.5f, Face * 2), WallShadow);
        for (int edge = -1; edge <= 1; edge += 2)
            GalleryQuad(v, c, t, new Vector2(side * (start + far) * .5f, b.y + edge * (Face - .2f)), new Vector2(far - start, .4f), WallShadow);
        // Far wall body and top/bottom walls.
        GalleryQuad(v, c, t, new Vector2(side * (far + .35f + Thick * .5f), b.y), new Vector2(Thick, Reach * 2), WallFace);
        for (int edge = -1; edge <= 1; edge += 2)
            GalleryQuad(v, c, t, new Vector2(side * (start + far + .35f) * .5f, b.y + edge * (Face + Reach) * .5f),
                new Vector2(far + .35f - start, Reach - Face), WallFace);
        // Ribs: vertical on the far wall, horizontal pitch along the side walls.
        for (float y = -Reach + .8f; y < Reach; y += 1.6f)
            GalleryQuad(v, c, t, new Vector2(side * (far + .35f + Thick * .5f), b.y + y), new Vector2(Thick, .16f), WallRib);
        for (float x = start + 1; x < far; x += 2)
            for (int edge = -1; edge <= 1; edge += 2)
                GalleryQuad(v, c, t, new Vector2(side * x, b.y + edge * (Face + Reach) * .5f), new Vector2(.16f, Reach - Face), WallRib);
        // Lit lips along every inner face, then a hazard band just behind them.
        GalleryQuad(v, c, t, new Vector2(side * (far + .41f), b.y), new Vector2(.12f, Face * 2 + .12f), WallLip);
        for (int edge = -1; edge <= 1; edge += 2)
            GalleryQuad(v, c, t, new Vector2(side * (start + far + .47f) * .5f, b.y + edge * (Face + .06f)), new Vector2(far + .47f - start, .12f), WallLip);
        var hazard = Color.Lerp(DockInstrument.Brass, WallFace, .45f);
        for (float y = -Face + .2f; y < Face - .2f; y += .5f)
            GalleryQuad(v, c, t, new Vector2(side * (far + .72f), b.y + y), new Vector2(.32f, .25f), hazard);
        for (float x = start + .3f; x < far + .2f; x += .5f)
            for (int edge = -1; edge <= 1; edge += 2)
                GalleryQuad(v, c, t, new Vector2(side * x, b.y + edge * (Face + .38f)), new Vector2(.25f, .32f), hazard);
        // A sealed blast door in the far wall, lit with the room's own lamp colour.
        float door = far + 1.35f;
        GalleryQuad(v, c, t, new Vector2(side * door, b.y), new Vector2(1.1f, 4.2f), Metal * .55f);
        for (int leaf = -1; leaf <= 1; leaf += 2)
            GalleryQuad(v, c, t, new Vector2(side * door, b.y + leaf * 1.02f), new Vector2(.82f, 1.9f), Metal * .8f);
        GalleryQuad(v, c, t, new Vector2(side * door, b.y), new Vector2(.86f, .08f), WallShadow);
        GalleryQuad(v, c, t, new Vector2(side * door, b.y + 2.35f), new Vector2(.5f, .14f), lamps[index]);
        GallerySurface(signs[index] + " bulkheads", v, c, t, -12);
    }

    void BuildRoom(int index)
    {
        Vector2 b = berths[index];
        float width = FarWall(index) - RoomThreshold + .9f;
        float side = Mathf.Sign(b.x);
        var mid = new Vector2(side * (RoomThreshold + FarWall(index)) * .5f, b.y);
        RoomDeck(signs[index] + " deck", mid, new Vector2(width, 13.8f), index == (int)Dock.Fleet ? side : 0);
        RoomBulkheads(index);
        MountReceiver(index);
        BuildControlDesk(index);
        if (index != (int)Dock.Fleet)
            return;

        HoverSign("< BERTHS", new Vector2(-15.7f, b.y - 1.35f), lamps[index]);
        var plaque = MountedPanel("Berth service plate", b, new Vector2(4.35f, .8f));
        berthNotice = WorldLabel(plaque, "", Vector2.zero, new Vector2(422, 73), 29, DockInstrument.Brass);
        plaque.gameObject.SetActive(false);
        var washVertices = new List<Vector3>();
        var washColors = new List<Color>();
        var washTriangles = new List<int>();
        for (int i = 0; i < ShipFrames.Count; i++)
        {
            bool locked = !MetaProgress.CanFly((ShipFrame)i);
            Vector2 spot = new Vector2(-21.25f - (i % 2) * 4.8f, b.y + (i < 2 ? 2.85f : -2.75f));
            MountModel("DockSaddle", Spot(spot, 3), -4, "Background");
            var hull = ShipFrames.BuildDisplay(player, ShipFrames.Get((ShipFrame)i));
            hull.transform.SetParent(FixtureParent, true);
            hull.transform.position = Spot(spot, 1);
            hull.transform.localScale *= .95f;
            foreach (var renderer in hull.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.gameObject.layer = 0;
                renderer.sortingLayerName = "Background";
                renderer.sortingOrder = -2;
                if (locked)
                    renderer.sharedMaterial = silhouette;
            }
            parkedShips.Add(hull.transform);
            berthNames.Add((RectTransform)Stencil(((ShipFrame)i).ToString().ToUpperInvariant(), spot + Vector2.down * 1.2f, 1.8f, DockInstrument.Paper).transform.parent);
            hull.SetActive(i != (int)MetaProgress.Ship);
            berthNames[i].gameObject.SetActive(hull.activeSelf);
            if (locked)
            {
                GalleryWell(washVertices, washColors, washTriangles, spot, 1.42f,
                    new Color(.40f, .46f, .44f), Color.clear);
                Strip("Berth interlock", spot + Vector2.down * 1.68f, new Vector2(.32f, .075f), DockInstrument.Brass, -5);
            }
        }
        if (washVertices.Count > 0)
            GallerySurface("Locked berth backlights", washVertices, washColors, washTriangles, -8);
    }

    bool UpdateDock(Vector3 p)
    {
        for (int i = 0; i < doorLights.Count; i++)
        {
            float near = Mathf.Clamp01(1 - Vector2.Distance(p, Spot(new Vector2(Mathf.Sign(berths[i].x) * 4.4f, berths[i].y), 0)) / 5);
            doorLights[i].localScale = new Vector3(2f, .52f + near * .45f * (.8f + .2f * Mathf.Sin(Time.time * 4)), 1);
        }
        if (room >= 0)
        {
            if (room == (int)Dock.Armory)
                GalleryHover(p);
            if (room == (int)Dock.Fleet && !operating)
            {
                int atBerth = -1;
                for (int i = 0; i < parkedShips.Count; i++)
                {
                    var hull = parkedShips[i];
                    if (!hull.gameObject.activeSelf)
                        continue;
                    float distance = Vector2.Distance(p, hull.position);
                    hull.rotation = Quaternion.Euler(0, Mathf.Sin(Time.time * .7f + i) * 8, 0);
                    if (distance < 1.25f)
                        atBerth = i;
                }
                if (atBerth != inspectedBerth)
                {
                    inspectedBerth = atBerth;
                    var plaque = (RectTransform)berthNotice.transform.parent;
                    bool locked = atBerth >= 0 && !MetaProgress.CanFly((ShipFrame)atBerth);
                    plaque.gameObject.SetActive(locked);
                    if (locked)
                    {
                        berthNotice.text = "Clear Void in " + (ShipFrame)(atBerth - 1);
                        readouts.Aim(plaque, parkedShips[atBerth].position + Vector3.up * 1.65f);
                    }
                    if (atBerth >= 0 && selection != atBerth)
                    {
                        selection = atBerth;
                        RenderPage();
                    }
                }
            }
            if (operating && !Menu.GameplayPaused)
            {
                RefreshConsoleFooter();
                var seat = Spot(Desk() + Vector2.down * 2.17f, p.z);
                player.transform.position = Vector3.Lerp(p, seat, 1 - Mathf.Exp(-9 * Time.deltaTime));
                var move = GameInput.Snapshot.Move;
                if (move.sqrMagnitude < .24f)
                    repeatAt = 0;
                else if (Time.unscaledTime >= repeatAt)
                {
                    bool horizontal = Mathf.Abs(move.x) >= Mathf.Abs(move.y);
                    Navigate(horizontal, horizontal ? (move.x > 0 ? 1 : -1) : (move.y < 0 ? 1 : -1));
                    repeatAt = Time.unscaledTime + (repeatAt == 0 ? .34f : .15f);
                }
            }
        }
        UpdatePrompt(p);
        bool gate = room < 0 && p.y >= GateY - .42f && Mathf.Abs(p.x - center.x) < GateHalfWidth;
        if (gate && !Arriving && !CloudTransition.Busy && !Menu.GameplayPaused)
            StartCoroutine(Launch());
        return room >= 0;
    }

    bool UseDock(InputAction.CallbackContext ctx, Vector3 p)
    {
        if (room < 0)
            return false;
        bool primary = ctx.action == input.PlayerControls.Accept;
        bool secondary = ctx.action == input.PlayerControls.Back;
        if (secondary && operating)
        {
            operating = false;
            HoldingShip = false;
            RenderPage();
            return true;
        }
        bool atDesk = Vector2.Distance(p, Spot(Desk(), 0)) < DeskReach;
        if (room == (int)Dock.Fleet && !operating && !atDesk)
        {
            if (primary && (ShipFrame)selection != MetaProgress.Ship && Vector2.Distance(p, parkedShips[selection].position) < 1.3f && MetaProgress.CanFly((ShipFrame)selection))
            {
                Switch((ShipFrame)selection);
                RenderPage();
            }
            return true;
        }
        if (!operating)
        {
            if (primary && atDesk)
            {
                ItemSound.Play("ready", .12f);
                operating = true;
                HoldingShip = true;
                repeatAt = Time.unscaledTime + .3f;
                RenderPage();
            }
            return true;
        }
        if (primary && room == (int)Dock.Flight)
        {
            difficulty = modes[selection];
            MetaProgress.FlightSetting = difficulty;
            operating = false;
            HoldingShip = false;
            RenderPage();
        }
        if (primary && room == (int)Dock.Simulator)
        {
            FlightTrials.Active = FlightTrials.All[selection];
            operating = false;
            HoldingShip = true;
            StartCoroutine(Launch());
        }
        return true;
    }

    void OpenDock(int index)
    {
        if (index < 0 || index == room)
            return;
        if (room >= 0)
            CloseDock();
        room = index;
        interiors[index].Show(true);
        operating = false;
        selection = index == (int)Dock.Armory ? galleryAvailability : savedSelection[index];
        band = index == (int)Dock.Armory ? galleryCategory : savedBand[index];
        instrument = screens[index];
        // A manifest can change while the pilot is shopping or switching hulls.
        if (index == (int)Dock.Flight)
            RenderPage();
    }

    void CloseDock()
    {
        if (room < 0)
            return;
        savedSelection[room] = selection;
        savedBand[room] = band;
        hoverIndex = -1;
        if (cardPanel != null)
            cardPanel.gameObject.SetActive(false);
        if (operating)
        {
            operating = false;
            RenderPage();
        }
        interiors[room].Show(false);
        room = -1;
        HoldingShip = false;
        instrument = null;
    }
}
