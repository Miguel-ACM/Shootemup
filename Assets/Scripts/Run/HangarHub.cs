using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The hub between runs, inside the gameplay scene: parked ships on the left (hover and press a button to switch),
/// the item shop on the right (hover and press to buy into the unlock pool) and the launch doors at the top.
/// </summary>
public sealed class HangarHub : MonoBehaviour
{
    public static bool Active
    {
        get; private set;
    }

    static RunDifficulty difficulty = RunDifficulty.Hard;
    // Where the title's tunnel came to rest, so the hangar continues from exactly that view.
    static float tunnelTravel;
    static readonly Vector2[] padSpots = { new Vector2(-2.25f, 4.2f), new Vector2(-2.25f, 1.4f), new Vector2(-2.25f, -1.4f), new Vector2(-2.25f, -4.2f) };
    static readonly Vector2[] plinthSpots = { new Vector2(2.25f, 3.5f), new Vector2(2.25f, 0), new Vector2(2.25f, -3.5f) };
    const float PadReach = 1f, PlinthReach = .85f, GateHalfWidth = 2f;

    sealed class Bay
    {
        public ShipFrame frame;
        public Transform display;
        public Vector3 spot;
        public Transform pips, pad, shadow;
        public float lift;
    }

    sealed class Plinth
    {
        public string kind;
        public AItem item;
        public Vector3 spot;
        public Transform disc, shadow;
        public bool sent;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        Active = false;
    }

    /// <summary>A new game opens the hangar first; the run itself starts from its doors.</summary>
    public static void Enter(RunDifficulty chosen)
    {
        difficulty = chosen;
        tunnelTravel = LevelEnvironment.TitleTravel;
        Active = true;
        GameRules.StartNewRun(chosen);
        LevelRun.Reset();
        CloudTransition.Load("L1", null, false);
    }

    public static void Leave()
    {
        Active = false;
    }

    // Called once the gameplay scene has loaded in hangar mode.
    public static void Build()
    {
        new GameObject("Hangar hub").AddComponent<HangarHub>();
    }

    readonly List<Bay> bays = new List<Bay>();
    readonly List<Plinth> plinths = new List<Plinth>();
    Shipcontroller player;
    Inputs input;
    Vector3 center;
    Material scenery;
    Mesh hall;
    DecisionOverlay overlay;
    Dialog dialog;
    readonly List<Mesh> quads = new List<Mesh>();
    int described = -1;
    bool launching;

    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        player = go != null ? go.GetComponent<Shipcontroller>() : null;
        var camera = UnityEngine.Camera.main;
        center = camera != null ? camera.transform.position : Vector3.zero;
        foreach (var field in FindObjectsByType<PSXGrassField>(FindObjectsInactive.Include))
            field.gameObject.SetActive(false);
        scenery = new Material(Resources.Load<Shader>("LevelScenery"));
        BuildHall();
        BuildBays();
        BuildShop();
        BuildGate();
        if (player != null)
        {
            parkedAt = player.transform.position;
            arrival = 0;
        }
        input = new Inputs();
        input.PlayerControls.FirePrimary.started += Interact;
        input.PlayerControls.FireSecondary.started += Interact;
        input.PlayerControls.ActiveLeft.started += Interact;
        input.PlayerControls.ActiveRight.started += Interact;
        input.Enable();
    }

    // The hangar is a bay inside the Tunnel: one still Tunnel section, with the docks, shop and doors built into it.
    void BuildHall()
    {
        for (int i = 0; i < 4; i++)
        {
            // The title's camera sits at the origin; keep each section at the same place on screen.
            float y = center.y + LevelEnvironment.SectionY(i, tunnelTravel);
            Scenery(LevelEnvironment.KitMesh("TunnelGround" + i), y, 90, -50, .8f);
            Scenery(LevelEnvironment.KitMesh("TunnelDetail" + i), y, 90, -40, .8f);
            Scenery(LevelEnvironment.KitMesh("TunnelNear" + i), y, 76, -25, .8f);
        }
        hall = IndustrialMeshFactory.TerraScenery("HangarProps");
        // Raised above the Tunnel's wall detail, which writes depth up to 2.2 units toward the camera.
        Scenery(hall, center.y, 87.5f, -30, 1);
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

    void BuildBays()
    {
        if (player == null)
            return;
        for (int i = 0; i < ShipFrames.Count && i < padSpots.Length; i++)
        {
            var frame = (ShipFrame)i;
            var bay = new Bay { frame = frame, spot = Spot(padSpots[i], player.transform.position.z + 1) };
            var display = ShipFrames.BuildDisplay(player, ShipFrames.Get(frame));
            if (display != null)
            {
                display.transform.SetParent(transform, true);
                display.transform.position = bay.spot + Vector3.left * SlideDistance;
                foreach (var renderer in display.GetComponentsInChildren<MeshRenderer>())
                {
                    renderer.sortingLayerName = "Player";
                    renderer.sortingOrder = renderer.name == "Cockpit" ? -1 : -2;
                }
                bay.display = display.transform;
            }
            bay.pad = Hover("HangarDockPad", bay.spot + new Vector3(-SlideDistance, 0, .6f), -4);
            bay.shadow = Shadow(bay.spot + Vector3.left * SlideDistance, 1.15f);
            bay.pips = new GameObject("Hull pips").transform;
            bay.pips.SetParent(transform, false);
            bay.pips.position = bay.spot + new Vector3(0, -1.05f, 0);
            int hits = ShipFrames.Get(frame).hits;
            for (int h = 0; h < hits; h++)
            {
                var pip = Quad("Pip", new Color(1, .62f, .3f), -1);
                pip.SetParent(bay.pips, false);
                pip.localPosition = new Vector3((h - (hits - 1) * .5f) * .2f, 0, 0);
                pip.localScale = new Vector3(.6f, .9f, 1);
            }
            bay.pips.gameObject.SetActive(false);
            bays.Add(bay);
        }
        RefreshBays();
    }

    void BuildShop()
    {
        dialog = GameObject.FindGameObjectWithTag("Dialog")?.GetComponent<Dialog>();
        overlay = new GameObject("Hangar shop prices", typeof(RectTransform)).AddComponent<DecisionOverlay>();
        overlay.transform.SetParent(transform, false);
        overlay.Build(dialog != null ? dialog.font : null, false);
        var stock = MetaProgress.Stock;
        for (int i = 0; i < stock.Count && i < plinthSpots.Length; i++)
        {
            var type = System.Type.GetType(stock[i]);
            if (type == null)
                continue;
            var plinth = new Plinth { kind = stock[i], spot = Spot(plinthSpots[i], 1) };
            // Waits off the right edge, then glides onto its plinth during the arrival.
            var go = Instantiate(Resources.Load<GameObject>("Items/Item"), plinth.spot + Vector3.right * SlideDistance, Quaternion.identity);
            go.transform.SetParent(transform, true);
            go.SetActive(false);
            plinth.item = (AItem)go.AddComponent(type);
            plinth.item.SetRarity(ItemPool.RarityOf(stock[i]));
            plinth.disc = Hover("HangarShopPlinth", plinth.spot + new Vector3(SlideDistance, -.3f, .5f), -4);
            plinth.shadow = Shadow(plinth.spot + new Vector3(SlideDistance, -.3f, 0), .7f);
            plinths.Add(plinth);
            overlay.Add(plinth.item);
        }
    }

    // The banked coins, on the wall beside the shop: a coin and a number on an opaque plate.
    RectTransform wallet;
    TextMeshProUGUI walletCount;

    void UpdateWallet()
    {
        if (wallet == null)
        {
            var root = new GameObject("Hangar wallet", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 64;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1;
            var plate = new GameObject("Wallet plate", typeof(RectTransform), typeof(Image));
            plate.transform.SetParent(root.transform, false);
            plate.GetComponent<Image>().color = Color.black;
            wallet = plate.GetComponent<RectTransform>();
            wallet.sizeDelta = new Vector2(58, 24);
            var coin = new GameObject("Coin", typeof(RectTransform), typeof(Image));
            coin.transform.SetParent(wallet, false);
            var disc = coin.GetComponent<Image>();
            disc.sprite = CombatVisuals.Disc;
            disc.color = new Color(1, .76f, .25f);
            disc.raycastTarget = false;
            disc.rectTransform.sizeDelta = new Vector2(12, 12);
            disc.rectTransform.anchoredPosition = new Vector2(-18, 0);
            var text = new GameObject("Coins", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(wallet, false);
            walletCount = text.GetComponent<TextMeshProUGUI>();
            if (dialog != null)
                walletCount.font = dialog.font;
            walletCount.fontSize = 12;
            walletCount.alignment = TextAlignmentOptions.MidlineLeft;
            walletCount.color = new Color(1, .82f, .4f);
            walletCount.raycastTarget = false;
            walletCount.rectTransform.sizeDelta = new Vector2(36, 20);
            walletCount.rectTransform.anchoredPosition = new Vector2(8, 0);
        }
        walletCount.text = MetaProgress.Coins.ToString();
        var camera = UnityEngine.Camera.main;
        var canvasRect = (RectTransform)wallet.parent;
        var viewport = camera.WorldToViewportPoint(new Vector3(center.x + 4, center.y, 0));
        wallet.anchoredPosition = new Vector2((viewport.x - .5f) * canvasRect.rect.width, (viewport.y - .5f) * canvasRect.rect.height);
        wallet.gameObject.SetActive(Slide(plinths.Count) < .5f);
    }

    void BuildGate()
    {
        // A faint mist across the open doors, fading downward; flying into it launches the run.
        var bottom = new Color(.62f, .74f, .8f, 0);
        var top = new Color(.7f, .8f, .86f, 0);
        float width = GateHalfWidth + .9f, low = GateY - 1.6f, high = center.y + 9.5f;
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
    Transform Hover(string model, Vector3 position, int order)
    {
        var mesh = IndustrialMeshFactory.TerraModel(model);
        if (mesh == null)
            return null;
        var go = new GameObject(model);
        go.transform.SetParent(transform, false);
        go.transform.position = position;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = IndustrialMeshFactory.TerraMaterial(model) ?? CombatMaterials.Fleet;
        renderer.sortingLayerName = "Player";
        renderer.sortingOrder = order;
        return go.transform;
    }

    Transform Shadow(Vector3 spot, float radius)
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
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(spot.x + .35f, spot.y - .55f, 87);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = scenery;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = -20;
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
    float GateY => Camera.UpLimit - .1f;
    Vector3 Spot(Vector2 local, float z) => new Vector3(center.x + local.x, center.y + local.y, z);

    // Arrival: the ship rises from below, the docks and plinths slide in from the sides and the light
    // at the top grows. Applied after the controller's own update, which would otherwise clamp the ship.
    const float ArrivalSeconds = 1.4f, SlideDistance = 6, SlideSeconds = .9f, LightSeconds = 2.6f;
    Vector3 parkedAt;
    float arrival = -1, introTime;
    bool Arriving => arrival >= 0;
    Mesh mist;

    float Slide(int order) => 1 - Mathf.SmoothStep(0, 1, (introTime - .25f - order * .12f) / SlideSeconds);

    void LateUpdate()
    {
        // Everything holds its starting pose until the transition has finished.
        bool running = !CloudTransition.Busy;
        if (running)
            introTime += Time.deltaTime;
        if (mist != null && introTime < LightSeconds + .5f)
        {
            float glow = Mathf.SmoothStep(0, 1, (introTime - .2f) / LightSeconds) * .85f;
            var bottom = new Color(.62f, .74f, .8f, 0);
            var top = new Color(.7f, .8f, .86f, glow);
            mist.colors = new[] { bottom, top, top, bottom };
        }
        for (int i = 0; i < plinths.Count; i++)
            if (!plinths[i].sent && plinths[i].item != null && Slide(i) < .999f)
            {
                plinths[i].sent = true;
                plinths[i].item.gameObject.SetActive(true);
                plinths[i].item.GoToPosition(plinths[i].spot);
            }
        if (!Arriving || player == null)
            return;
        if (running)
            arrival += Time.deltaTime / ArrivalSeconds;
        float below = center.y - UnityEngine.Camera.main.orthographicSize - 1.2f;
        float t = 1 - Mathf.Pow(1 - Mathf.Clamp01(arrival), 3);
        player.transform.position = new Vector3(parkedAt.x, Mathf.LerpUnclamped(below, parkedAt.y, t), parkedAt.z);
        if (arrival >= 1)
            arrival = -1;
    }

    void Update()
    {
        if (player == null || launching)
            return;
        var p = player.transform.position;
        int pad = NearestBay(p);
        for (int i = 0; i < bays.Count; i++)
        {
            var bay = bays[i];
            bay.lift = Mathf.MoveTowards(bay.lift, i == pad ? 1 : 0, Time.deltaTime * 5);
            float t = Time.time * 1.3f + i;
            float hover = Mathf.Sin(t) * .06f + bay.lift * .12f;
            var slide = Vector3.left * SlideDistance * Slide(i);
            if (bay.pad != null)
                bay.pad.position = bay.spot + slide + new Vector3(0, hover, .6f);
            if (bay.shadow != null)
            {
                bay.shadow.position = new Vector3(bay.spot.x + .35f, bay.spot.y - .55f, 87) + slide;
                bay.shadow.localScale = Vector3.one * (1 - hover * .5f);
            }
            if (bay.display != null)
            {
                bay.display.position = bay.spot + slide + new Vector3(0, hover, 0);
                bay.display.rotation = Quaternion.AngleAxis(Mathf.Sin(t * .7f) * (4 + bay.lift * 10), Vector3.up);
                bay.display.localScale = player.transform.lossyScale * (1 + bay.lift * .1f);
            }
            bay.pips.gameObject.SetActive(i == pad);
        }
        int shop = NearestPlinth(p);
        for (int i = 0; i < plinths.Count; i++)
        {
            float bob = Mathf.Sin(Time.time * 1.1f + i * 1.7f) * .05f;
            var slide = Vector3.right * SlideDistance * Slide(i);
            if (plinths[i].disc != null)
                plinths[i].disc.position = plinths[i].spot + slide + new Vector3(0, -.3f + bob, .5f);
            if (plinths[i].shadow != null)
                plinths[i].shadow.position = new Vector3(plinths[i].spot.x + .35f, plinths[i].spot.y - .85f, 87) + slide;
            var item = plinths[i].item;
            if (item == null)
                continue;
            // Shop items are displays: keep their spin but never let them be attracted or equipped.
            if (item.enabled && plinths[i].sent && item.Settled)
                item.enabled = false;
            int price = MetaProgress.Price(item.rarity);
            overlay.SetOffer(i, price.ToString(), MetaProgress.Coins >= price);
        }
        UpdateWallet();
        if (dialog != null && shop != described)
        {
            described = shop;
            if (shop >= 0)
            {
                var data = plinths[shop].item.GetData();
                // In the hangar the card sits at the very bottom, clear of the shop.
                if (dialog.Panel != null)
                    dialog.Panel.anchoredPosition = new Vector2(0, 12);
                dialog.ShowItemDesc(data[0], data[1], data[2], plinths[shop].item.rarity);
            }
            else
                dialog.Hide();
        }
        if (!Arriving && p.y >= GateY && Mathf.Abs(p.x - center.x) <= GateHalfWidth)
            StartCoroutine(Launch());
    }

    int NearestBay(Vector3 p)
    {
        int best = -1;
        float closest = PadReach;
        for (int i = 0; i < bays.Count; i++)
        {
            if (bays[i].frame == MetaProgress.Ship)
                continue;
            float d = Vector2.Distance(p, bays[i].spot);
            if (d < closest)
            {
                closest = d;
                best = i;
            }
        }
        return best;
    }

    int NearestPlinth(Vector3 p)
    {
        int best = -1;
        float closest = PlinthReach;
        for (int i = 0; i < plinths.Count; i++)
        {
            if (plinths[i].item == null || plinths[i].item.IsDismissing)
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
        if (player == null || launching || Arriving || Menu.GameplayPaused || CloudTransition.Busy)
            return;
        var p = player.transform.position;
        int pad = NearestBay(p);
        if (pad >= 0)
        {
            Switch(bays[pad].frame);
            return;
        }
        int shop = NearestPlinth(p);
        if (shop >= 0)
            Buy(plinths[shop]);
    }

    void Switch(ShipFrame frame)
    {
        MetaProgress.Ship = frame;
        var definition = ShipFrames.Get(frame);
        ShipFrames.ApplyStats(definition);
        player.SetMaxHits(definition.hits);
        player.Repair(definition.hits);
        ShipFrames.ApplyHull(player, definition);
        var loadout = player.GetComponent<ShipLoadout>();
        if (loadout != null)
            ShipFrames.ApplyWeapons(loadout, definition);
        PSXImpactEffects.ItemGet(player.transform.position, new Color(.55f, .85f, 1));
        ItemSound.Play("ready", .16f);
        RefreshBays();
    }

    void RefreshBays()
    {
        foreach (var bay in bays)
            if (bay.display != null)
                bay.display.gameObject.SetActive(bay.frame != MetaProgress.Ship);
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
        described = -2;
    }

    // The mist takes the ship: controls stop, it is drawn up out of view, and the clouds follow a second later.
    IEnumerator Launch()
    {
        launching = true;
        if (dialog != null)
            dialog.Hide();
        overlay.Dismiss();
        foreach (var weapon in player.GetComponentsInChildren<AWeapon>())
            weapon.StopFiring();
        // The controller clamps the ship inside the arena, so it lets go first.
        player.enabled = false;
        var ship = player.transform;
        float top = center.y + UnityEngine.Camera.main.orthographicSize + 1.5f, speed = 1f;
        while (ship.position.y < top)
        {
            speed += Time.deltaTime * 6;
            var p = ship.position;
            p.x = Mathf.Lerp(p.x, center.x, 1 - Mathf.Exp(-.8f * Time.deltaTime));
            p.y += speed * Time.deltaTime;
            ship.position = p;
            yield return null;
        }
        yield return new WaitForSeconds(1);
        CloudTransition.Load("L1", () =>
        {
            Active = false;
            GameRules.StartNewRun(difficulty);
            LevelRun.Reset();
            MetaProgress.BeginRun();
            BGMManager.PlayLevelMusic(LevelRun.Current);
        });
    }

    void OnDestroy()
    {
        input?.Disable();
        input?.Dispose();
        if (scenery != null)
            Destroy(scenery);
        if (hall != null)
            Destroy(hall);
        foreach (var mesh in quads)
            Destroy(mesh);
    }
}
