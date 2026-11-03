using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Dockside workshops flank a central outfitting lane. The persistent L1 core carries
/// the player and instruments across the handoff from the hangar to the additive run.
/// </summary>
[DefaultExecutionOrder(50)]
public sealed partial class HangarHub : MonoBehaviour
{
    public static bool Active
    {
        get; private set;
    }

    static RunDifficulty difficulty = RunDifficulty.Normal;
    // The tunnel phase on the frame its motion transfers to the incoming dock camera.
    static float tunnelTravel;
    static readonly Vector2[] plinthSpots = { new Vector2(-2.55f, 10.9f), new Vector2(2.55f, 14.25f), new Vector2(-2.55f, 17.3f) };
    const float LoadingBayX = -8f;
    const float PlacardHeight = 1.84f, ClassDrop = 1.54f;
    public const float DeckExtension = 32f;
    const float PlinthReach = .95f, GateHalfWidth = 1.55f;

    sealed class Plinth
    {
        public string kind;
        public AItem item;
        public Vector3 spot;
        public Transform carrier, disc, shadow;
        public float arrivalProgress = -1;
        public TextMeshProUGUI price;
        public RectTransform plaque, category;
        public int lastBank = -1;
        public bool pinned;
    }

    // The original outfitter's staggered lift, with enough travel to read at arcade resolution.
    static float PlatformLift(int index) => .13f * Mathf.Sin(Time.time * 1.1f + index * 1.7f);
    static readonly Quaternion PlatformTilt = Quaternion.Euler(32, 0, 0);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        Active = false;
        HoldingShip = false;
    }

    /// <summary>A new game opens the hangar first; the run itself starts from its doors.</summary>
    public static void Enter(RunDifficulty chosen)
    {
        FlightTrials.Active = null;
        difficulty = chosen;
        Active = true;
        GameRules.StartNewRun(chosen);
        LevelRun.Reset();
        CloudTransition.Load("L1", null, false);
    }

    /// <summary>After a lost run the ship returns to the hangar, with the same difficulty.</summary>
    public static void Return()
    {
        FlightTrials.Active = null;
        Active = true;
        GameRules.StartNewRun(difficulty);
        LevelRun.Reset();
    }

    public static void Leave()
    {
        FlightTrials.Active = null;
        Active = false;
        HoldingShip = false;
        LevelEnvironment.ReleaseTitle();
    }

    public const string HangarScene = "Hangar", RunScene = "Run";

    // Called when the core scene has loaded in hangar mode, before its first frame: prepare the core,
    // then bring in the hangar's own scene.
    public static void PrepareCore()
    {
        // The core's legacy shaft scenery never shows behind the hangar.
        foreach (var industrial in FindObjectsByType<PSXIndustrialField>())
            industrial.steelMaterial = null;
        // The ship waits below the screen from the very first frame, then flies up into place.
        var ship = GameObject.FindGameObjectWithTag("Player");
        var camera = UnityEngine.Camera.main;
        inlet = LevelEnvironment.CarriesTitle ? ApproachDepth : 0;
        tunnelTravel = LevelEnvironment.MoorTitle(inlet);
        if (camera != null && inlet > 0)
        {
            Vector3 delta = Vector3.down * inlet;
            camera.transform.position += delta;
            var shake = camera.GetComponent<EZCameraShake.CameraShaker>();
            if (shake != null)
                shake.RestPositionOffset += camera.transform.parent == null ? delta : camera.transform.parent.InverseTransformVector(delta);
        }
        if (ship != null && camera != null)
        {
            dockedAt = ship.transform.position;
            ship.transform.position = new Vector3(dockedAt.x, camera.transform.position.y - camera.orthographicSize - ArrivalDepth, dockedAt.z);
            // Until the hangar scene arrives and flies it in, nothing may clamp the ship back into view.
            HoldingShip = true;
        }
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(HangarScene, UnityEngine.SceneManagement.LoadSceneMode.Additive);
    }

    // Far enough below the screen that the ship and its thrusters are fully hidden before the entrance.
    const float ArrivalDepth = 2.6f, ApproachDepth = 18;
    static float inlet;

    /// <summary>The hangar is placing the ship itself (its entrance); the controller leaves its position alone.</summary>
    public static bool HoldingShip
    {
        get; private set;
    }

    /// <summary>True from the launch until the run's content has started, so the core skips its own setup.</summary>
    public static bool Launching
    {
        get; private set;
    }

    public static bool Approaching => Active && HoldingShip && (current == null || current.Arriving);
    static Vector3 dockedAt;

    readonly List<Plinth> plinths = new List<Plinth>();
    Shipcontroller player;
    Inputs input;
    Vector3 center;
    UnityEngine.Camera view;
    EZCameraShake.CameraShaker shaker;
    float cameraTravel;

    DockReadouts readouts;
    Material scenery;
    RectTransform stockScreen;
    TextMeshProUGUI stockInfo, walletText;
    int inspectedStock = -1;
    RectTransform walletPanel;
    Sprite coinStamp;
    Dialog dialog;
    readonly List<Mesh> quads = new List<Mesh>();
    bool launching, deckReady;

    public static IEnumerator WarmDock()
    {
        yield return GalleryShelf.Warm();
        // Terra palettes and hull geometry are shared with the actual parked craft.
        foreach (string model in new[] { "HangarShopPlinth", "ShipLancer", "ShipEmber", "ShipWarden" })
        {
            IndustrialMeshFactory.TerraMaterial(model);
            yield return null;
        }
        foreach (var model in stationModels)
        {
            IndustrialMeshFactory.TerraMaterial(model);
            yield return null;
        }
        IndustrialMeshFactory.TerraMaterial("DockReceiver");
        yield return null;
        IndustrialMeshFactory.TerraMaterial("DockSaddle");
        yield return null;
        IndustrialMeshFactory.TerraMaterial("DockHoverSign");
    }

    IEnumerator Start()
    {
        current = this;
        var go = GameObject.FindGameObjectWithTag("Player");
        player = go != null ? go.GetComponent<Shipcontroller>() : null;
        view = UnityEngine.Camera.main;
        center = view != null ? view.transform.position : Vector3.zero;
        center.y += inlet;
        cameraTravel = -inlet;
        shaker = view != null ? view.GetComponent<EZCameraShake.CameraShaker>() : null;
        scenery = new Material(Resources.Load<Shader>("LevelScenery"));
        silhouette = new Material(Resources.Load<Shader>("GallerySilhouette"));
        var display = new GameObject("Native dock displays", typeof(RectTransform));
        display.transform.SetParent(transform, false);
        readouts = display.AddComponent<DockReadouts>();
        readouts.Initialize(view);
        parkedAt = dockedAt;
        arrival = 0;
        BuildHall();
        yield return null;
        BuildShop();
        yield return null;
        yield return BuildConcourse();
        BuildPrompt();
        BuildGate();
        deckReady = true;
        input = GameInput.Actions;
        input.PlayerControls.FirePrimary.started += Interact;
        input.PlayerControls.FireSecondary.started += Interact;
        input.PlayerControls.ActiveLeft.started += Interact;
        input.PlayerControls.ActiveRight.started += Interact;

    }

    // The hangar is a bay inside the Tunnel: one still Tunnel section, with the docks, shop and doors built into it.
    void BuildHall()
    {
        // Normally the title's own tunnel came along; build a matching one only when it did not.
        for (int i = 0; i < 4; i++)
        {
            // Anchor the carried section to the beginning of the approach.
            float y = center.y - inlet + LevelEnvironment.SectionY(i, tunnelTravel);
            if (!LevelEnvironment.CarriesTitle)
                HallSection(i, y);
            // Extend the same sequence of twenty-unit sections past the far gate.
            if (y + 80 <= center.y + DeckExtension + 20)
                HallSection(i, y + 80);
        }
    }

    void HallSection(int i, float y)
    {
        Scenery(LevelEnvironment.KitMesh("TunnelGround" + i), y, 90, -50, .8f);
        Scenery(LevelEnvironment.KitMesh("TunnelDetail" + i), y, 90, -40, .8f);
        Scenery(LevelEnvironment.KitMesh("TunnelNear" + i), y, 76, -25, .8f);
    }

    void Scenery(Mesh mesh, float y, float z, int order, float width)
    {
        if (mesh == null)
            return;
        var part = new GameObject(mesh.name);
        part.transform.SetParent(transform, false);
        part.transform.position = new Vector3(center.x, y, z);
        part.transform.localScale = new Vector3(width, 1, 1);
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = part.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = scenery;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = order;
    }

    void BuildShop()
    {
        dialog = GameObject.FindGameObjectWithTag("Dialog")?.GetComponent<Dialog>();
        coinStamp = Resources.Load<Sprite>("DockArt/DockCoin");
        var stock = MetaProgress.Stock;
        for (int i = 0; i < stock.Count && i < plinthSpots.Length; i++)
        {
            ItemCatalog.TryGet(stock[i], out var definition);
            if (definition == null)
                continue;
            var plinth = new Plinth { kind = stock[i], spot = Spot(plinthSpots[i], 1) };
            plinth.carrier = new GameObject("Loaded outfitter platform").transform;
            plinth.carrier.SetParent(transform, false);
            plinth.carrier.position = plinth.spot;
            var go = Instantiate(Resources.Load<GameObject>("Items/Item"), plinth.spot, Quaternion.identity, plinth.carrier);
            plinth.item = definition.Attach(go);

            plinth.item.SetRarity(ItemPool.RarityOf(stock[i]));
            plinth.disc = MountModel("HangarShopPlinth", plinth.spot + new Vector3(0, -.27f, 1.3f), -4, parent: plinth.carrier);
            plinth.disc.localScale = Vector3.one * 1.72f;
            plinth.disc.localRotation = PlatformTilt;
            plinth.shadow = Shadow(new Vector3(plinth.spot.x + .35f, plinth.spot.y - .85f, 87), .7f, -20);
            plinth.shadow.SetParent(plinth.carrier, true);
            plinths.Add(plinth);
            var glowVertices = new List<Vector3>();
            var glowColors = new List<Color>();
            var glowTriangles = new List<int>();
            GalleryWell(glowVertices, glowColors, glowTriangles, plinthSpots[i], 1.22f,
                new Color(1, .07f, .035f, .88f), new Color(.85f, .025f, .016f, 0));
            GallerySurface("Outfitter red footlight", glowVertices, glowColors, glowTriangles, -6, plinth.carrier);
            var plaque = plinth.plaque = MountedPanel("Stock placard", plinthSpots[i] + Vector2.up * PlacardHeight, new Vector2(3.62f, 1.94f), fadeOnReveal: true);
            plaque.GetComponent<Image>().color = Color.black;
            DockInstrument.Block(plaque, "Rarity edge", new Vector2(0, 94), new Vector2(362, 5), AItem.GetRarityColor(plinth.item.rarity));
            WorldLabel(plaque, plinth.item.GetData()[0], new Vector2(0, 38), new Vector2(338, 91), 34, DockInstrument.Paper);
            plinth.price = CoinRow(plaque, -49, 330);
            plinth.category = MountedPanel("Equipment class", plinthSpots[i] + Vector2.down * ClassDrop, new Vector2(3.4f, .50f), fadeOnReveal: true);
            plinth.category.GetComponent<Image>().enabled = false;
            WorldLabel(plinth.category, DecisionOverlay.Category(plinth.item), Vector2.zero, new Vector2(334, 50), 32, DockInstrument.Paper);
            plaque.gameObject.SetActive(false);
            plinth.category.gameObject.SetActive(false);
            plinth.carrier.position = new Vector3(center.x + LoadingBayX, plinth.spot.y, plinth.spot.z);
            plinth.carrier.gameObject.SetActive(false);
        }
    }

    TextMeshProUGUI CoinRow(RectTransform face, float y, float width)
    {
        var row = new GameObject("Coin amount", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        row.SetParent(face, false);
        row.anchoredPosition = new Vector2(0, y);
        row.sizeDelta = new Vector2(width, 62);
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 21;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        layout.childControlWidth = layout.childControlHeight = true;
        var icon = new GameObject("Stamped coin", typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
        icon.transform.SetParent(row, false);
        icon.sprite = coinStamp;
        icon.raycastTarget = false;
        var dimensions = icon.GetComponent<LayoutElement>();
        dimensions.preferredWidth = dimensions.preferredHeight = 56;
        var amount = WorldLabel(row, "0", Vector2.zero, row.sizeDelta, 34, DockInstrument.Brass);
        amount.enableAutoSizing = false;
        amount.textWrappingMode = TextWrappingModes.NoWrap;
        return amount;
    }

    void UpdateShopReadouts(Vector3 p)
    {
        int near = room < 0 ? NearestPlinth(p, 2.45f) : -1;
        walletPanel.gameObject.SetActive(near >= 0);
        if (near >= 0)
            walletText.text = MetaProgress.Coins.ToString();
        int hovered = near >= 0 ? NearestPlinth(p) : -1;
        if (hovered != inspectedStock)
        {
            if (inspectedStock >= 0)
            {
                plinths[inspectedStock].plaque.gameObject.SetActive(false);
                plinths[inspectedStock].category.gameObject.SetActive(false);
            }
            inspectedStock = hovered;
            if (hovered >= 0)
            {
                var offer = plinths[hovered];
                stockInfo.text = offer.item.GetData()[2];
                float displayY = offer.spot.y > center.y + plinthSpots[1].y ? 18.95f : 10.55f;
                readouts.Aim(stockScreen, Spot(new Vector2(2.35f, displayY), 1));
                readouts.Reveal(offer.plaque);
                readouts.Reveal(offer.category);
                readouts.Reveal(stockScreen);
            }
            else
                stockScreen.gameObject.SetActive(false);
        }
        if (hovered >= 0)
        {
            UpdateButtonIcon(buyIcon);
            UpdateButtonIcon(pinIcon, true);
        }
    }

    void BuildGate()
    {
        // A faint mist across the open doors, fading downward; flying into it launches the run.
        var bottom = new Color(.62f, .74f, .8f, 0);
        var top = new Color(.7f, .8f, .86f, 0);
        float width = GateHalfWidth + .9f, low = GateY - 1.6f, high = GateY + 2f;
        var mesh = new Mesh { name = "Launch mist" };
        mesh.vertices = new[] { new Vector3(-width, low), new Vector3(-width, high), new Vector3(width, high), new Vector3(width, low) };
        mesh.colors = new[] { bottom, top, top, bottom };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        quads.Add(mesh);
        mist = mesh;
        var go = new GameObject("Launch mist");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(center.x, 0, 2);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = scenery;
        renderer.sortingLayerName = "Player";
        renderer.sortingOrder = 6;
    }

    // Docks and plinths hover over the Tunnel floor, with a soft shadow below and behind them.
    Transform MountModel(string model, Vector3 position, int order, string layer = "Player", Transform parent = null)
    {
        var mesh = IndustrialMeshFactory.TerraModel(model);
        if (mesh == null)
            return null;
        var go = new GameObject(model);
        go.transform.SetParent(parent != null ? parent : FixtureParent, false);
        go.transform.position = position;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = IndustrialMeshFactory.TerraMaterial(model) ?? CombatMaterials.Fleet;
        renderer.sortingLayerName = layer;
        renderer.sortingOrder = order;
        IndustrialMeshFactory.AttachTerraParts(model, go.transform, renderer.sharedMaterial);
        return go.transform;
    }

    Transform Shadow(Vector3 spot, float radius, int order)
    {
        const int Sides = 20;
        var vertices = new Vector3[Sides + 1];
        var colors = new Color[Sides + 1];
        var triangles = new int[Sides * 3];
        colors[0] = new Color(0, 0, 0, .8f);
        for (int i = 0; i < Sides; i++)
        {
            float angle = i * Mathf.PI * 2 / Sides;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * .8f) * radius;
            colors[i + 1] = new Color(0, 0, 0, 0);
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = 1 + (i + 1) % Sides;
            triangles[i * 3 + 2] = 1 + i;
        }
        var mesh = new Mesh { name = "Hover shadow", vertices = vertices, colors = colors, triangles = triangles };
        quads.Add(mesh);
        var go = new GameObject("Hover shadow");
        go.transform.SetParent(FixtureParent, false);
        go.transform.position = spot;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = scenery;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = order;
        return go.transform;
    }

    // A flat, unlit quad a quarter unit across, drawn among the ships.
    Transform Quad(string title, Color color, int order)
    {
        var mesh = new Mesh { name = title };
        mesh.vertices = new[] { new Vector3(-.125f, -.125f), new Vector3(-.125f, .125f), new Vector3(.125f, .125f), new Vector3(.125f, -.125f) };
        mesh.colors = new[] { color, color, color, color };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        quads.Add(mesh);
        var go = new GameObject(title);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = scenery;
        renderer.sortingLayerName = "Player";
        renderer.sortingOrder = order;
        return go.transform;
    }

    // The very top of the reachable arena.
    float GateY => Camera.UpLimit + DeckExtension - .1f;
    Vector3 Spot(Vector2 local, float z) => new Vector3(center.x + local.x, center.y + local.y, z);

    // Arrival: the ship rises from below, the docks and plinths slide in from the sides and the light
    // at the top grows. Applied after the controller's own update, which would otherwise clamp the ship.
    const float ArrivalSeconds = 1.8f, LightSeconds = 2.6f;
    Vector3 parkedAt;
    float arrival = -1, introTime;
    bool Arriving => arrival >= 0;
    Mesh mist;

    void LateUpdate()
    {
        AnimateSigns();
        // Fly through the title handoff. A return from combat waits for the cloud curtain.
        bool running = inlet > 0 || !CloudTransition.Busy;
        if (running)
            introTime += Mathf.Min(Time.deltaTime, .08f);
        if (inlet > 0 && Arriving)
        {
            float distance = Mathf.Clamp01(1 - introTime / (2 * inlet / 10.2f));
            SetCameraTravel(-inlet * distance * distance);
        }
        if (mist != null && introTime < LightSeconds + .5f)
        {
            float glow = Mathf.SmoothStep(0, 1, (introTime - .2f) / LightSeconds) * .85f;
            var bottom = new Color(.62f, .74f, .8f, 0);
            var top = new Color(.7f, .8f, .86f, glow);
            mist.colors = new[] { bottom, top, top, bottom };
        }
        if (room == (int)Dock.Armory)
            FloatGalleryPlatforms();
        for (int i = 0; i < plinths.Count; i++)
        {
            var offer = plinths[i];
            if (offer.arrivalProgress < 0)
            {
                if (Arriving || launching || room >= 0 || Mathf.Abs(offer.spot.y - view.transform.position.y) > view.orthographicSize - 1.5f)
                    continue;
                offer.arrivalProgress = 0;
                offer.carrier.gameObject.SetActive(true);
            }
            if (offer.arrivalProgress < 1)
            {
                offer.arrivalProgress = Mathf.Min(1, offer.arrivalProgress + Time.deltaTime / 1.5f);
                var at = offer.spot;
                at.x = Mathf.SmoothStep(center.x + LoadingBayX, at.x, offer.arrivalProgress);
                offer.carrier.position = at;
            }
            float lift = PlatformLift(i);
            offer.disc.localPosition = new Vector3(0, -.27f + lift, 1.3f);
            offer.shadow.localScale = Vector3.one * (1 - lift * .6f);
            if (offer.item != null && !offer.item.IsDismissing)
                offer.item.transform.localPosition = Vector3.up * lift;
        }
        if (!Arriving || player == null)
            return;
        if (running)
            arrival += Time.deltaTime / ArrivalSeconds;
        float below = center.y + cameraTravel - view.orthographicSize - ArrivalDepth;
        float t = 1 - Mathf.Pow(1 - Mathf.Clamp01(arrival), 3);
        player.transform.position = new Vector3(parkedAt.x, Mathf.LerpUnclamped(below, parkedAt.y, t), parkedAt.z);
        if (arrival >= 1 && deckReady && Mathf.Abs(cameraTravel) < .001f)
        {
            arrival = -1;
            HoldingShip = false;
        }
    }

    void Update()
    {
        if (!deckReady || player == null || launching)
            return;
        // Debug: K unlocks every item.
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            MetaProgress.UnlockAll();
            RefreshGalleryAccess();
            ItemSound.Play("itemSalvage", .16f);
        }
        var p = player.transform.position;
        if (!Arriving)
            FollowDeck(p);
        UpdateDock(p);
        for (int i = 0; i < plinths.Count; i++)
        {
            var offer = plinths[i];
            if (offer.item == null)
                continue;
            if (offer.item.enabled && offer.item.Settled)
                offer.item.enabled = false;
            bool pinned = MetaProgress.Pin == offer.kind;
            if (offer.lastBank == MetaProgress.Coins && offer.pinned == pinned)
                continue;
            offer.lastBank = MetaProgress.Coins;
            offer.pinned = pinned;
            int price = MetaProgress.Price(offer.item.rarity);
            offer.price.color = MetaProgress.Coins >= price ? DockInstrument.Brass : new Color(.92f, .54f, .49f);
            offer.price.text = price + (pinned ? "  <size=65%>PIN</size>" : "");
        }
        UpdateShopReadouts(p);
    }

    void SetCameraTravel(float travel)
    {
        if (view != null)
        {
            var delta = Vector3.up * (travel - cameraTravel);
            view.transform.position += delta;
            if (shaker != null)
                shaker.RestPositionOffset += view.transform.parent != null ? view.transform.parent.InverseTransformVector(delta) : delta;
        }
        cameraTravel = travel;
    }

    int NearestPlinth(Vector3 p, float reach = PlinthReach)
    {
        int best = -1;
        float closest = reach;
        for (int i = 0; i < plinths.Count; i++)
        {
            if (plinths[i].arrivalProgress < 1 || plinths[i].item == null || plinths[i].item.IsDismissing)
                continue;
            float d = Vector2.Distance(p, plinths[i].spot);
            if (d < closest)
            {
                closest = d;
                best = i;
            }
        }
        return best;
    }

    void Interact(InputAction.CallbackContext context)
    {
        if (!deckReady || player == null || launching || Arriving || Menu.GameplayPaused || CloudTransition.Busy)
            return;
        var p = player.transform.position;
        if (UseDock(context, p))
            return;
        int shop = NearestPlinth(p);
        if (shop < 0)
            return;
        if (context.action == input.PlayerControls.FireSecondary)
            MetaProgress.TogglePin(plinths[shop].kind);
        else
            Buy(plinths[shop]);
    }

    void Switch(ShipFrame frame)
    {
        if (!MetaProgress.CanFly(frame))
            return;
        MetaProgress.Ship = frame;
        for (int i = 0; i < parkedShips.Count; i++)
        {
            parkedShips[i].gameObject.SetActive(i != (int)frame);
            berthNames[i].gameObject.SetActive(i != (int)frame);
        }
        var definition = ShipFrames.Get(frame);
        ShipFrames.ApplyStats(definition);
        player.SetMaxHits(definition.hits);
        player.Repair(definition.hits);
        ShipFrames.ApplyHull(player, definition);
        var loadout = player.GetComponent<ShipLoadout>();
        if (loadout != null)
            ShipFrames.ApplyWeapons(loadout, definition);
        PSXImpactEffects.ItemGet(player.transform.position, new Color(.55f, .85f, 1));
    }

    void Buy(Plinth plinth)
    {
        if (!MetaProgress.TryBuy(plinth.kind))
        {
            ItemSound.Play("powerdown", .1f);
            return;
        }
        PSXImpactEffects.ItemGet(plinth.item.transform.position, AItem.GetRarityColor(plinth.item.rarity));
        ItemSound.Play("itemSalvage", .16f);
        plinth.item.Dismiss();
        RefreshGalleryAccess();
    }

    // The mist takes the ship: controls stop, it is drawn up out of view, and the clouds follow a second later.
    IEnumerator Launch()
    {
        launching = true;
        // The hangar track fades away as the ship is drawn up; Plains' music starts behind the clouds.
        StartCoroutine(BGMManager.FadeOutSong(2.5f));
        // The run's content starts loading now, at low priority, and waits ready behind the clouds.
        Application.backgroundLoadingPriority = ThreadPriority.Low;
        var runLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(RunScene, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        runLoad.allowSceneActivation = false;
        if (dialog != null)
            dialog.Hide();
        stockScreen.gameObject.SetActive(false);
        foreach (var weapon in player.GetComponentsInChildren<AWeapon>())
            weapon.StopFiring();
        // The controller clamps the ship inside the arena, so it lets go first.
        player.enabled = false;
        var ship = player.transform;
        float top = view.transform.position.y + view.orthographicSize + 1.5f, speed = 1f;
        while (ship.position.y < top)
        {
            speed += Time.deltaTime * 6;
            var p = ship.position;
            p.x = Mathf.Lerp(p.x, view.transform.position.x, 1 - Mathf.Exp(-.8f * Time.deltaTime));
            p.y += speed * Time.deltaTime;
            ship.position = p;
            yield return null;
        }
        yield return new WaitForSeconds(1);
        // Run by the cloud transition, which outlives this hub when the hangar scene unloads.
        CloudTransition.Change(StartRun(player, parkedAt, dialog, runLoad));
    }

    // The run begins behind flowing clouds: the small Run scene activates additively (no Single load and no
    // automatic asset unload), the hangar scene unloads a few frames later, and the core's heavier parts start
    // one per frame, so no frame has to absorb the whole start.
    IEnumerator StartRun(Shipcontroller player, Vector3 parkedAt, Dialog dialog, AsyncOperation runLoad)
    {
        Launching = true;
        HoldingShip = false;
        while (runLoad.progress < .9f)
            yield return null;
        ResetDeckCamera();
        Active = false;
        GameRules.StartNewRun(FlightTrials.Active?.difficulty ?? difficulty);
        LevelRun.Reset(FlightTrials.Active?.district ?? LevelId.Fields);
        MetaProgress.BeginRun();
        BGMManager.PlayLevelMusic(LevelRun.Current);
        runLoad.allowSceneActivation = true;
        while (!runLoad.isDone)
            yield return null;
        Application.backgroundLoadingPriority = ThreadPriority.Normal;
        yield return null;
        LevelEnvironment.ReleaseTitle();
        yield return null;
        var unload = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(HangarScene);
        yield return null;
        LevelEnvironment.Apply();
        yield return null;
        var frame = ShipFrames.Current;
        ShipFrames.ApplyStats(frame);
        player.transform.position = parkedAt;
        ShipFrames.ApplyHull(player, frame);
        var loadout = player.GetComponent<ShipLoadout>();
        ShipFrames.ApplyWeapons(loadout, frame);
        if (FlightTrials.SecondaryFireOnly)
            loadout.SetPrimary(null);
        player.SetMaxHits(frame.hits);
        player.Repair(frame.hits);
        player.enabled = true;
        if (dialog != null && dialog.Panel != null)
            dialog.Panel.anchoredPosition = new Vector2(0, DecisionOverlay.SkipHeight + 12);
        yield return null;
        // The instruments were built while the hangar idled; they light up on their own once the run is under way.
        player.StartCoroutine(ExplodingShield.GrantStarter(player));
        yield return null;
        yield return null;
        player.StartCoroutine(ShipFrames.GrantStarters(player, frame));
        while (unload != null && !unload.isDone)
            yield return null;
        Launching = false;
    }


    void OnDestroy()
    {
        if (input != null)
        {
            input.PlayerControls.FirePrimary.started -= Interact;
            input.PlayerControls.FireSecondary.started -= Interact;
            input.PlayerControls.ActiveLeft.started -= Interact;
            input.PlayerControls.ActiveRight.started -= Interact;
        }
        ResetDeckCamera();
        if (silhouette != null)
            Destroy(silhouette);
        if (guidePaint != null)
            Destroy(guidePaint);
        if (current == this)
            current = null;

        if (scenery != null)
            Destroy(scenery);
        foreach (var mesh in quads)
            Destroy(mesh);
    }
}
