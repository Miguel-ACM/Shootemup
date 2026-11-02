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
    // Where the title's tunnel came to rest, so the hangar continues from exactly that view.
    static float tunnelTravel;
    static readonly Vector2[] plinthSpots = { new Vector2(2.7f, -3), new Vector2(2.7f, 0), new Vector2(2.7f, 3) };
    public const float DeckExtension = 48f;
    const float PlinthReach = .95f, GateHalfWidth = 1.55f;

    sealed class Plinth
    {
        public string kind;
        public AItem item;
        public Vector3 spot;
        public Transform disc, shadow;
        public bool sent;
        public TextMeshProUGUI label;
    }

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
        tunnelTravel = LevelEnvironment.TitleTravel;
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
    const float ArrivalDepth = 2.6f;

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

    static Vector3 dockedAt;

    readonly List<Plinth> plinths = new List<Plinth>();
    Shipcontroller player;
    Inputs input;
    Vector3 center;
    UnityEngine.Camera view;
    EZCameraShake.CameraShaker shaker;
    float cameraTravel;

    Material scenery;
    Transform stockScreen;
    TextMeshProUGUI stockInfo, walletText;
    Dialog dialog;
    readonly List<Mesh> quads = new List<Mesh>();
    bool launching;

    void Start()
    {
        current = this;
        var go = GameObject.FindGameObjectWithTag("Player");
        player = go != null ? go.GetComponent<Shipcontroller>() : null;
        view = UnityEngine.Camera.main;
        center = view != null ? view.transform.position : Vector3.zero;
        shaker = view != null ? view.GetComponent<EZCameraShake.CameraShaker>() : null;
        scenery = new Material(Resources.Load<Shader>("LevelScenery"));
        BuildHall();
        BuildShop();
        BuildConcourse();
        BuildGate();
        if (player != null)
        {
            parkedAt = dockedAt;
            arrival = 0;
        }
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
            // The title's camera sits at the origin; keep each section at the same place on screen.
            float y = center.y + LevelEnvironment.SectionY(i, tunnelTravel);
            if (!LevelEnvironment.CarriesTitle)
                HallSection(i, y);
            // Continue the same four-section tunnel beyond the carried title scenery.
            float next = center.y + LevelEnvironment.SectionY(i, tunnelTravel + DeckExtension) + DeckExtension;
            if (next > y + 1f)
                HallSection(i, next);
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
        var stock = MetaProgress.Stock;
        for (int i = 0; i < stock.Count && i < plinthSpots.Length; i++)
        {
            ItemCatalog.TryGet(stock[i], out var definition);
            if (definition == null)
                continue;
            var plinth = new Plinth { kind = stock[i], spot = Spot(plinthSpots[i], 1) };
            // Waits off the right edge, then glides onto its plinth during the arrival.
            var go = Instantiate(Resources.Load<GameObject>("Items/Item"), plinth.spot, Quaternion.identity);
            go.transform.SetParent(transform, true);
            plinth.item = definition.Attach(go);
            var visual = go.GetComponent<PSXObjectVisual>();
            if (visual != null)
                visual.itemKindOverride = definition.Key;
            plinth.item.SetRarity(ItemPool.RarityOf(stock[i]));
            // Hidden only after Awake has given the item its name and description.
            go.SetActive(false);
            plinth.disc = Hover("HangarShopPlinth", plinth.spot + new Vector3(0, -.3f, .5f), -4);
            plinth.shadow = Shadow(plinth.spot + new Vector3(0, -.3f, 0), .7f);
            plinths.Add(plinth);
            var plaque = MountedPanel("Stock placard", new Vector2(2.7f, plinthSpots[i].y + 1.12f), new Vector2(2.7f, 1.35f));
            plinth.label = WorldLabel(plaque, "", Vector2.zero, new Vector2(254, 121), 31, DockInstrument.Paper);

        }
    }

    void UpdateWallet()
    {
        bool near = player != null && player.transform.position.x > center.x + 1 &&
            Mathf.Abs(player.transform.position.y - center.y) < 5.8f && room < 0;
        walletText.transform.parent.gameObject.SetActive(near);
        walletText.text = "●  " + MetaProgress.Coins;
        int closest = NearestPlinth(player.transform.position);
        stockScreen.gameObject.SetActive(closest >= 0 && room < 0);
        if (closest >= 0)
        {
            var data = plinths[closest].item.GetData();
            stockInfo.text = "<b>" + data[0] + "</b>\n\n" + data[2] + "\n\n<size=75%>" +
                DockInstrument.FireKey() + "  BUY    " + DockInstrument.FireKey(true) + "  PIN</size>";
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
    float GateY => Camera.UpLimit + DeckExtension - .1f;
    Vector3 Spot(Vector2 local, float z) => new Vector3(center.x + local.x, center.y + local.y, z);

    // Arrival: the ship rises from below, the docks and plinths slide in from the sides and the light
    // at the top grows. Applied after the controller's own update, which would otherwise clamp the ship.
    const float ArrivalSeconds = 1.8f, SlideSeconds = .9f, LightSeconds = 2.6f;
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
        float below = center.y - UnityEngine.Camera.main.orthographicSize - ArrivalDepth;
        float t = 1 - Mathf.Pow(1 - Mathf.Clamp01(arrival), 3);
        player.transform.position = new Vector3(parkedAt.x, Mathf.LerpUnclamped(below, parkedAt.y, t), parkedAt.z);
        if (arrival >= 1)
        {
            arrival = -1;
            HoldingShip = false;
        }
    }

    void Update()
    {
        if (player == null || launching)
            return;
        // Debug: K unlocks every item.
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            MetaProgress.UnlockAll();
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
            if (offer.item.enabled && offer.sent && offer.item.Settled)
                offer.item.enabled = false;
            int price = MetaProgress.Price(offer.item.rarity);
            string tint = MetaProgress.Coins >= price ? "#F4C16C" : "#EB8A7C";
            offer.label.text = offer.item.GetData()[0] + "\n<size=85%><color=" + tint + ">" +
                (MetaProgress.Pin == offer.kind ? "PIN  " : "") + price + "  ●</color></size>";
            offer.label.transform.parent.gameObject.SetActive(!offer.item.IsDismissing);
        }
        UpdateWallet();
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
        if (player == null || launching || Arriving || HangarShutter.Active || Menu.GameplayPaused || CloudTransition.Busy)
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
            p.x = Mathf.Lerp(p.x, center.x, 1 - Mathf.Exp(-.8f * Time.deltaTime));
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
        if (current == this)
            current = null;
        ReleaseExhibit();
        if (scenery != null)
            Destroy(scenery);
        foreach (var mesh in quads)
            Destroy(mesh);
    }
}
