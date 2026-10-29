using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    static readonly Vector2[] padSpots = { new Vector2(-3.4f, 4.2f), new Vector2(-3.4f, 1.4f), new Vector2(-3.4f, -1.4f), new Vector2(-3.4f, -4.2f) };
    static readonly Vector2[] plinthSpots = { new Vector2(3.4f, 3.4f), new Vector2(3.4f, .6f), new Vector2(3.4f, -2.2f) };
    const float PadReach = 1f, PlinthReach = .85f, GateHalfWidth = 2f;

    sealed class Bay
    {
        public ShipFrame frame;
        public Transform display;
        public Vector3 spot;
        public Transform pips;
        public float lift;
    }

    sealed class Plinth
    {
        public string kind;
        public AItem item;
        public Vector3 spot;
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
        Active = true;
        GameRules.StartNewRun(chosen);
        LevelRun.Reset();
        CloudTransition.Load("L1");
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
    Transform gateFill;
    readonly List<Mesh> quads = new List<Mesh>();
    int described = -1;
    float launchHold;
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
        input = new Inputs();
        input.PlayerControls.FirePrimary.started += Interact;
        input.PlayerControls.FireSecondary.started += Interact;
        input.PlayerControls.ActiveLeft.started += Interact;
        input.PlayerControls.ActiveRight.started += Interact;
        input.Enable();
    }

    void BuildHall()
    {
        hall = IndustrialMeshFactory.TerraScenery("Hangar");
        if (hall == null)
            return;
        var floor = new GameObject("Hangar bay");
        floor.transform.SetParent(transform, false);
        floor.transform.position = new Vector3(center.x, center.y, 90);
        floor.AddComponent<MeshFilter>().sharedMesh = hall;
        var renderer = floor.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = scenery;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = -50;
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
                display.transform.position = bay.spot;
                foreach (var renderer in display.GetComponentsInChildren<MeshRenderer>())
                {
                    renderer.sortingLayerName = "Player";
                    renderer.sortingOrder = -2;
                }
                bay.display = display.transform;
            }
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
            var go = Instantiate(Resources.Load<GameObject>("Items/Item"), plinth.spot, Quaternion.identity);
            go.transform.SetParent(transform, true);
            plinth.item = (AItem)go.AddComponent(type);
            plinth.item.SetRarity(ItemPool.RarityOf(stock[i]));
            plinths.Add(plinth);
            overlay.Add(plinth.item);
        }
    }

    // A thin amber line across the open doors fills while the ship holds there.
    void BuildGate()
    {
        gateFill = Quad("Launch charge", new Color(1, .7f, .25f), 3);
        gateFill.SetParent(transform, false);
        gateFill.position = new Vector3(center.x, GateY + .35f, 2);
        gateFill.localScale = new Vector3(0, .5f, 1);
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

    float GateY => Mathf.Min(center.y + 6.4f, Camera.UpLimit - .3f);
    Vector3 Spot(Vector2 local, float z) => new Vector3(center.x + local.x, center.y + local.y, z);

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
            if (bay.display != null)
            {
                float t = Time.time * 1.3f + i;
                bay.display.position = bay.spot + new Vector3(0, Mathf.Sin(t) * .04f + bay.lift * .12f, 0);
                bay.display.rotation = Quaternion.AngleAxis(Mathf.Sin(t * .7f) * (4 + bay.lift * 10), Vector3.up);
                bay.display.localScale = player.transform.lossyScale * (1 + bay.lift * .1f);
            }
            bay.pips.gameObject.SetActive(i == pad);
        }
        int shop = NearestPlinth(p);
        for (int i = 0; i < plinths.Count; i++)
        {
            var item = plinths[i].item;
            if (item == null)
                continue;
            // Shop items are displays: keep their spin but never let them be attracted or equipped.
            if (item.enabled && item.Settled)
                item.enabled = false;
            int price = MetaProgress.Price(item.rarity);
            overlay.SetOffer(i, price.ToString(), MetaProgress.Coins >= price);
        }
        if (dialog != null && shop != described)
        {
            described = shop;
            if (shop >= 0)
            {
                var data = plinths[shop].item.GetData();
                dialog.ShowItemDesc(data[0], data[1], data[2], plinths[shop].item.rarity);
            }
            else
                dialog.Hide();
        }
        bool atGate = p.y >= GateY && Mathf.Abs(p.x - center.x) <= GateHalfWidth;
        launchHold = atGate ? launchHold + Time.deltaTime : 0;
        float progress = Mathf.Clamp01(launchHold / RouteCharge.HoldSeconds);
        gateFill.localScale = new Vector3(progress * GateHalfWidth * 2 * 16 / 4, .5f, 1);
        if (progress >= 1)
            Launch();
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
        if (player == null || launching || Menu.GameplayPaused || CloudTransition.Busy)
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

    void Launch()
    {
        launching = true;
        gateFill.localScale = new Vector3(GateHalfWidth * 2 * 4, .5f, 1);
        if (dialog != null)
            dialog.Hide();
        overlay.Dismiss();
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
