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
    static readonly Color Metal = new Color(.19f, .28f, .30f), Deck = new Color(.07f, .13f, .15f);
    static HangarHub current;
    readonly List<Transform> doorLights = new List<Transform>();
    readonly List<Transform> parkedShips = new List<Transform>();
    TextMeshProUGUI dockHint;
    Transform gateGauge;
    float gateHold, repeatAt, cameraAcross, baseViewSize;
    readonly GameObject[] interiors = new GameObject[5];
    int room = -1, selection, band;
    bool operating;
    GameObject console;
    DockInstrument instrument;
    TMP_FontAsset panelFont;

    public static Vector3 ConstrainTravel(Vector3 previous, Vector3 desired)
    {
        if (current == null)
            return previous;
        return current.ConstrainDeck(previous, desired);
    }

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
            float minX = crossing ? 0 : 6.5f;
            desired.x = center.x + side * Mathf.Clamp(next, minX, gallery ? GalleryRight : 15.5f);
            float height = next < 6.5f ? .95f : 6.5f;
            desired.y = center.y + (gallery && next >= 6.5f ?
                Mathf.Clamp(desired.y - center.y, GalleryBottom, GalleryTop) : Mathf.Clamp(desired.y - center.y, roomY - height, roomY + height));
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
        if (p.x - center.x >= 6.4f && p.y - center.y <= GalleryTop && p.y - center.y >= GalleryBottom)
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
        float zoom = operating ? 7.05f : baseViewSize;
        view.orthographicSize = Mathf.Lerp(view.orthographicSize, zoom, 1 - Mathf.Exp(-6 * Time.deltaTime));
        int portal = Portal(p);
        float across = 0;
        float along = Mathf.Clamp(p.y - center.y, 0, DeckExtension);
        if (portal >= 0 && Mathf.Abs(p.x - center.x) > 4 &&
            (Mathf.Abs(p.x - center.x) > Camera.RightLimit + .01f || Mathf.Abs(p.y - center.y - berths[portal].y) < 1))
        {
            float enter = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(4, 7.5f, Mathf.Abs(p.x - center.x)));
            Vector2 target = berths[portal];
            if (portal == (int)Dock.Armory)
                target = operating ? new Vector2(FilterDesk.x, Mathf.Max(GalleryBottom + view.orthographicSize, FilterDesk.y + 2.8f)) : new Vector2(Mathf.Clamp(p.x - center.x, 10, GalleryRight - 2), Mathf.Clamp(p.y - center.y, GalleryBottom + view.orthographicSize, GalleryTop - view.orthographicSize));
            else if (operating)
                target.y += 2.6f;
            across = target.x * enter;
            along = Mathf.Lerp(along, target.y, enter);
        }
        SetCameraTravel(Mathf.Lerp(cameraTravel, along, 1 - Mathf.Exp(-7 * Time.deltaTime)));
        float next = Mathf.Lerp(cameraAcross, across, 1 - Mathf.Exp(-7 * Time.deltaTime));
        MoveCamera(Vector3.right * (next - cameraAcross));
        cameraAcross = next;
        if (Mathf.Abs(p.x - center.x) >= 6.5f && portal != room)
            OpenDock(portal);
        else if (room >= 0 && Mathf.Abs(p.x - center.x) < 6.4f)
            CloseDock();
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
        if (view != null && baseViewSize > 0)
            view.orthographicSize = baseViewSize;
    }

    Transform Strip(string title, Vector2 pos, Vector2 size, Color tint, int order = -7, float angle = 0)
    {
        var t = Quad(title, tint, order);
        t.SetParent(transform, false);
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

    RectTransform MountedPanel(string title, Vector2 position, Vector2 size, bool floatSign = false)
    {
        var go = new GameObject(title, typeof(RectTransform), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size * 100;
        readouts.MountAt(rect, Spot(position, 1), .01f, floatSign);
        go.GetComponent<Image>().color = new Color(.018f, .034f, .043f);
        return rect;
    }

    TextMeshProUGUI WorldLabel(RectTransform face, string text, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        var t = new GameObject("Screen panelFont", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        t.transform.SetParent(face, false);
        t.rectTransform.sizeDelta = size;
        t.rectTransform.anchoredPosition = position;
        if (panelFont == null)
            panelFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        t.font = panelFont;
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
        for (int side = -1; side <= 1; side += 2)
        {
            Strip("Runway channel", new Vector2(side * 1.3f, DeckExtension * .5f), new Vector2(.11f, 16 + DeckExtension), Deck, -12);
            for (int y = -7; y <= 7 + DeckExtension; y += 2)
                Strip("Inset runway lamp", new Vector2(side * 1.3f, y), new Vector2(.055f, .32f), DockInstrument.Sea, -11);
        }
        for (int i = 0; i < berths.Length; i++)
        {
            int firstChild = transform.childCount;
            readouts.BuildingRoom = i;
            BuildRoom(i);
            readouts.BuildingRoom = -1;
            var interior = new GameObject(signs[i] + " interior");
            int end = transform.childCount;
            for (int part = end - 1; part >= firstChild; part--)
                transform.GetChild(part).SetParent(interior.transform, true);
            interior.transform.SetParent(transform, false);
            interiors[i] = interior;
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
            HangingSign(signs[i], new Vector2(side * 3.5f, y + 2), lamps[i], true);
            PortalLight(new Vector2(side * 4.55f, y), side);
        }
        var walletPanel = MountedPanel("Coin reader", new Vector2(2.7f, 5.3f), new Vector2(2.4f, .7f));
        walletText = WorldLabel(walletPanel, "", Vector2.zero, new Vector2(228, 60), 36, DockInstrument.Brass);
        stockScreen = MountedPanel("Outfitter display", new Vector2(-.75f, 0), new Vector2(3.8f, 4.6f));
        stockInfo = WorldLabel((RectTransform)stockScreen, "", Vector2.zero, new Vector2(350, 420), 30, DockInstrument.Paper);
        stockScreen.gameObject.SetActive(false);
        Strip("Departure latch", new Vector2(0, DeckExtension + 6.65f), new Vector2(2.1f, .12f), Metal);
        gateGauge = Strip("Launch charge", new Vector2(0, DeckExtension + 6.65f), new Vector2(.001f, .12f), DockInstrument.Brass, -5);
        Stencil("LAUNCH", new Vector2(0, DeckExtension + 7.05f), 2.8f, DockInstrument.Paper);
        dockHint = Stencil("", new Vector2(0, -6.15f), 2.2f, DockInstrument.Paper);
    }

    void BuildRoom(int index)
    {
        Vector2 b = berths[index];
        if (index == (int)Dock.Armory)
            return;
        // The workshop floor has its own walls, cable trenches and machinery apron.
        var floor = new GameObject(signs[index] + " deck").AddComponent<SpriteRenderer>();
        floor.transform.SetParent(transform, false);
        floor.transform.position = Spot(b, 80);
        floor.sprite = Resources.Load<Sprite>(index == (int)Dock.Fleet ? "DockArt/RoomDeckFleet" : "DockArt/RoomDeck");
        floor.drawMode = SpriteDrawMode.Simple;
        floor.transform.localScale = new Vector3(9.6f / floor.sprite.bounds.size.x, 13.8f / floor.sprite.bounds.size.y, 1);
        floor.sortingLayerName = "Background";
        floor.sortingOrder = -15;
        for (int side = -1; side <= 1; side += 2)
        {
            Strip("Recessed wall", b + Vector2.right * side * 4.7f, new Vector2(.28f, 13.8f), Metal, -10);
            for (int rib = -6; rib <= 6; rib += 2)
                Strip("Wall rib", b + new Vector2(side * 4.5f, rib), new Vector2(.42f, .18f), Metal, -8);
        }
        MountReceiver(b + Vector2.up * 2.8f);
        HangingSign(signs[index], b + Vector2.up * 6.25f, lamps[index], false);
        // A low physical control desk is below the wall screen.
        Strip("Console footing", b + Vector2.down * .5f, new Vector2(3, .7f), Metal, -6);
        Strip("Console keys", b + Vector2.down * .5f, new Vector2(2.6f, .29f), Deck, -5);
        for (int k = -2; k <= 2; k++)
            Strip("Console key", b + new Vector2(k * .39f, -.5f), new Vector2(.23f, .08f), lamps[index], -4);
        if (index != (int)Dock.Fleet)
        {
            BuildRoomEquipment(index, b);
            return;
        }
        for (int i = 0; i < ShipFrames.Count; i++)
        {
            Vector2 spot = b + new Vector2(i % 2 == 0 ? -2.15f : 2.15f, -2.3f - i / 2 * 2.65f);
            Hover("HangarDockPad", Spot(spot, 3), -4);
            var hull = ShipFrames.BuildDisplay(player, ShipFrames.Get((ShipFrame)i));
            hull.transform.SetParent(transform, true);
            hull.transform.position = Spot(spot, 1);
            hull.transform.localScale *= .95f;
            foreach (var renderer in hull.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.gameObject.layer = 0;
                renderer.sortingLayerName = "Background";
                renderer.sortingOrder = -2;
            }
            parkedShips.Add(hull.transform);
            Stencil(((ShipFrame)i).ToString().ToUpperInvariant(), spot + Vector2.down * 1.25f, 1.8f, DockInstrument.Paper);
            if (!MetaProgress.CanFly((ShipFrame)i))
                Stencil("LOCKED", spot + Vector2.up * 1.05f, 1.7f, DockInstrument.Brass);
        }
    }

    void DisplayEquipment(string key, Vector2 position)
    {
        var go = new GameObject(key + " rack display");
        go.transform.SetParent(transform, false);
        go.transform.position = Spot(position, 1);
        go.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.ExpansionItem(key);
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = EverydayItemMaterials.For(key);
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = -2;
        var mesh = go.GetComponent<MeshFilter>().sharedMesh;
        go.transform.localScale = Vector3.one * (1.05f / Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z));
        go.transform.rotation = Quaternion.Euler(-12, 20, -12);
    }

    void BuildRoomEquipment(int index, Vector2 b)
    {
        int kind = index == (int)Dock.Simulator ? 0 : index == (int)Dock.Recorder ? 1 : 2;
        var mesh = IndustrialMeshFactory.DockMachine(kind);
        var go = new GameObject(signs[index] + " machinery");
        go.transform.SetParent(transform, false);
        go.transform.position = Spot(b + Vector2.down * 4.6f, 2);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CombatMaterials.Fleet;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = -2;
        go.transform.localScale = Vector3.one * 1.25f;
        if (index == (int)Dock.Recorder)
        {
            DisplayEquipment("RibbonSpool", b + new Vector2(-2.6f, -6));
            DisplayEquipment("ClockworkSpring", b + new Vector2(2.6f, -6));
        }
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
            if (room == (int)Dock.Fleet)
            {
                for (int i = 0; i < parkedShips.Count; i++)
                {
                    var hull = parkedShips[i];
                    float distance = Vector2.Distance(p, hull.position);
                    hull.rotation = Quaternion.Euler(0, Mathf.Sin(Time.time * .7f + i) * 8, 0);
                    if (distance < 1.25f && selection != i)
                    {
                        selection = i;
                        RenderPage();
                    }
                }
            }
            if (operating && !Menu.GameplayPaused)
            {
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
        readouts.Aim((RectTransform)dockHint.transform.parent, Spot(new Vector2(cameraAcross, cameraTravel - 6.15f), 2));
        dockHint.text = gate ? "HOLD " + DockInstrument.FireKey() + "  /  LAUNCH" : "";
        gateHold = gate && !Arriving && !CloudTransition.Busy && !Menu.GameplayPaused && GameInput.Snapshot.Primary ?
            Mathf.Min(1, gateHold + Time.deltaTime) : 0;
        gateGauge.localScale = new Vector3(Mathf.Max(.001f, gateHold) * 8.4f, .48f, 1);
        if (gateHold >= 1)
            StartCoroutine(Launch());
        return room >= 0;
    }

    bool UseDock(InputAction.CallbackContext ctx, Vector3 p)
    {
        if (room < 0)
            return false;
        bool primary = ctx.action == input.PlayerControls.FirePrimary;
        bool secondary = ctx.action == input.PlayerControls.FireSecondary;
        if (secondary && operating)
        {
            operating = false;
            HoldingShip = false;
            RenderPage();
            return true;
        }
        if (room == (int)Dock.Fleet)
        {
            if (primary && Vector2.Distance(p, parkedShips[selection].position) < 1.3f && MetaProgress.CanFly((ShipFrame)selection))
            {
                Switch((ShipFrame)selection);
                RenderPage();
            }
            return true;
        }
        bool atDesk = Vector2.Distance(p, Spot(Desk(), 0)) < 1.6f;
        if (!operating)
        {
            if (primary && atDesk)
            {
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
            HangarShutter.Depart(() =>
            {
                CloseDock();
                ResetDeckCamera();
                player.transform.position = parkedAt;
                StartCoroutine(Launch());
            });
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
        readouts.ShowRoom(room);
        for (int i = 0; i < interiors.Length; i++)
            interiors[i].SetActive(i == room);
        selection = index == (int)Dock.Fleet ? (int)MetaProgress.Ship : index == (int)Dock.Flight ? System.Array.IndexOf(modes, difficulty) : 0;
        band = index == (int)Dock.Fleet ? System.Array.IndexOf(modes, difficulty) : index == (int)Dock.Armory ? galleryCategory : 0;
        if (index == (int)Dock.Armory)
            selection = galleryAvailability;
        operating = false;
        if (index == (int)Dock.Armory)
            return;
        console = new GameObject(signs[index] + " mounted screen", typeof(RectTransform));
        console.transform.SetParent(transform, false);
        instrument = new DockInstrument(console, dialog?.font, lamps[index]);
        console.transform.position = Spot(berths[index] + Vector2.up * 3.3f, 2);
        console.transform.localScale = Vector3.one * .012f;
        readouts.MountAt((RectTransform)console.transform, Spot(berths[index] + Vector2.up * 2.8f, 1), .012f);
        RenderPage();
        ItemSound.Play("ready", .08f);
    }

    void CloseDock()
    {
        ReleaseExhibit();
        hoverIndex = -1;
        if (cardPanel != null)
            cardPanel.gameObject.SetActive(false);
        if (room == (int)Dock.Armory)
            directoryControls.text = "";
        room = -1;
        readouts.ShowRoom(room);
        for (int i = 0; i < interiors.Length; i++)
            interiors[i].SetActive(true);
        operating = false;
        HoldingShip = false;
        if (console != null)
            Destroy(console);
        pageNodes.Clear();
        instrument = null;
    }
}
