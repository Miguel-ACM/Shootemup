using System.Collections.Generic;
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
    static readonly Vector2[] berths = { new Vector2(-3.15f,-2.65f),new Vector2(3.15f,-2.65f),
        new Vector2(-3.15f,2.65f),new Vector2(3.15f,2.65f),new Vector2(2.9f,6.25f) };
    static readonly string[] signs = { "FLEET", "ARMORY", "SIMULATOR", "RECORDER", "FLIGHT DESK" };
    static readonly Color[] lamps = { DockInstrument.Sea,DockInstrument.Brass,new Color(.70f,.65f,.98f),
        new Color(.55f,.77f,.94f),new Color(.96f,.59f,.39f) };
    static readonly RunDifficulty[] modes = { RunDifficulty.Easy, RunDifficulty.Normal, RunDifficulty.Hard };
    static readonly string[] factors = { "0.5×", "1×", "2×" };
    static readonly Color Metal = new Color(.19f, .28f, .30f), Deck = new Color(.07f, .13f, .15f);
    readonly List<Transform> movingParts = new List<Transform>();
    TextMeshPro dockHint;
    Transform gateGauge, trolley;
    float gateHold, repeatAt;
    int room = -1, selection, band;
    GameObject console;
    DockInstrument instrument;

    Transform Strip(string title, Vector2 pos, Vector2 size, Color tint, int order = -7, float angle = 0)
    {
        var t = Quad(title, tint, order);
        t.SetParent(transform, false);
        t.position = Spot(pos, 4 - (order + 10) * .035f);
        t.localScale = new Vector3(size.x * 4, size.y * 4, 1);
        t.localRotation = Quaternion.Euler(0, 0, angle);
        return t;
    }
    TextMeshPro Stencil(string text, Vector2 pos, float size, Color color)
    {
        var t = new GameObject(text).AddComponent<TextMeshPro>();
        t.transform.SetParent(transform, false);
        t.transform.position = Spot(pos, 2);
        if (dialog != null)
            t.font = dialog.font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.rectTransform.sizeDelta = new Vector2(8, 1);
        t.textWrappingMode = TextWrappingModes.NoWrap;
        var renderer = t.GetComponent<MeshRenderer>();
        renderer.sortingLayerName = "Player";
        renderer.sortingOrder = 0;
        return t;
    }
    void BuildConcourse()
    {
        for (int side = -1; side <= 1; side += 2)
        {
            Strip("Runway channel", new Vector2(side * 1.38f, 0), new Vector2(.18f, 16), Deck, -12);
            for (int i = -7; i <= 7; i++)
                Strip("Inset runway light", new Vector2(side * 1.38f, i), new Vector2(.065f, i % 2 == 0 ? .47f : .19f), i % 2 == 0 ? DockInstrument.Sea : Metal, -11);
        }
        for (int i = 0; i < berths.Length; i++)
        {
            Vector2 b = berths[i];
            Color light = lamps[i];
            var facade = new GameObject(signs[i] + " workshop").AddComponent<SpriteRenderer>();
            facade.transform.SetParent(transform, false);
            facade.transform.position = Spot(b, 3.6f);
            facade.sprite = Resources.Load<Sprite>("DockArt/" + (Dock)i);
            facade.sortingLayerName = "Player";
            facade.sortingOrder = -8;
            Stencil(signs[i], b + Vector2.down * .67f, 2.3f, light);
        }
        var hull = player == null ? null : ShipFrames.BuildDisplay(player, ShipFrames.Get(ShipFrame.Lancer));
        if (hull != null)
        {
            hull.transform.SetParent(transform, true);
            hull.transform.position = Spot(berths[0] + Vector2.up * .17f, 2);
            hull.transform.localScale *= .68f;
            foreach (var r in hull.GetComponentsInChildren<MeshRenderer>())
            {
                r.sortingLayerName = "Player";
                r.sortingOrder = -3;
            }
        }
        // A ready lamp breathes inside each of the simulator and recorder consoles.
        for (int i = 2; i <= 3; i++)
            movingParts.Add(Strip("Status diode", berths[i] + new Vector2(.82f, -.40f), new Vector2(.075f, .075f), lamps[i], -5));
        Strip("Gantry track", new Vector2(-2.9f, 5.15f), new Vector2(2.8f, .08f), Metal);
        trolley = Strip("Overhead hoist", new Vector2(-2.9f, 5.1f), new Vector2(.58f, .27f), DockInstrument.Brass, -6);
        Strip("Departure latch", new Vector2(0, 6.65f), new Vector2(2.1f, .12f), Metal);
        gateGauge = Strip("Launch charge", new Vector2(0, 6.65f), new Vector2(.001f, .12f), DockInstrument.Brass, -5);
        Stencil("LAUNCH", new Vector2(0, 7.05f), 2.8f, DockInstrument.Paper);
        dockHint = Stencil("", new Vector2(0, -6.15f), 2.7f, DockInstrument.Paper);
    }

    int NearbyDock(Vector3 pos)
    {
        for (int i = 0; i < berths.Length; i++)
            if (Vector2.Distance(pos, Spot(berths[i], 0)) < 1.4f)
                return i;
        return -1;
    }
    bool UpdateDock(Vector3 p)
    {
        foreach (var part in movingParts)
            part.localScale = Vector3.one * (.21f + .09f * Mathf.Sin(3 * Time.time));
        trolley.position = Spot(new Vector2(-2.9f + Mathf.Sin(Time.time * .5f) * 1.03f, 5.1f), 3);
        if (room >= 0)
        {
            if (Menu.GameplayPaused)
                return true;
            instrument.group.alpha = Mathf.MoveTowards(instrument.group.alpha, 1, Time.unscaledDeltaTime * 7);
            var move = GameInput.Snapshot.Move;
            if (move.sqrMagnitude < .24f)
                repeatAt = 0;
            else if (Time.unscaledTime >= repeatAt)
            {
                bool horizontal = Mathf.Abs(move.x) >= Mathf.Abs(move.y);
                int direction = horizontal ? (move.x > 0 ? 1 : -1) : (move.y < 0 ? 1 : -1);
                Navigate(horizontal, direction);
                repeatAt = Time.unscaledTime + (repeatAt == 0 ? .34f : .15f);
            }
            return true;
        }
        bool gate = p.y >= GateY - .42f && Mathf.Abs(p.x - center.x) < GateHalfWidth;
        int near = NearbyDock(p), stock = NearestPlinth(p);
        string text = gate ? "HOLD  " + DockInstrument.FireKey() + "   /   LAUNCH" : near >= 0 ? DockInstrument.FireKey() + "   /   " + signs[near] :
            stock >= 0 ? DockInstrument.FireKey() + "  BUY     " + DockInstrument.FireKey(true) + "  PIN" : "";
        dockHint.text = text;
        gateHold = gate && !Arriving && !CloudTransition.Busy && !Menu.GameplayPaused && GameInput.Snapshot.Primary ?
            Mathf.Min(1, gateHold + Time.deltaTime) : 0;
        gateGauge.localScale = new Vector3(Mathf.Max(.001f, gateHold) * 8.4f, .48f, 1);
        if (gateHold >= 1)
            StartCoroutine(Launch());
        return false;
    }
    bool UseDock(InputAction.CallbackContext ctx, Vector3 p)
    {
        if (room < 0)
        {
            int near = NearbyDock(p);
            if (near < 0)
                return false;
            OpenDock(near);
            return true;
        }
        if (ctx.action == input.PlayerControls.FireSecondary)
            CloseDock();
        else if (ctx.action == input.PlayerControls.FirePrimary)
        {
            if (room == (int)Dock.Fleet && MetaProgress.CanFly((ShipFrame)selection))
            {
                Switch((ShipFrame)selection);
                RenderPage();
            }
            if (room == (int)Dock.Flight)
            {
                difficulty = modes[selection];
                MetaProgress.FlightSetting = difficulty;
                CloseDock();
            }
            if (room == (int)Dock.Simulator)
            {
                FlightTrials.Active = FlightTrials.All[selection];
                CloseDock();
                StartCoroutine(Launch());
            }
        }
        return true;
    }
    void OpenDock(int index)
    {
        room = index;
        selection = index == (int)Dock.Fleet ? (int)MetaProgress.Ship : index == (int)Dock.Flight ? System.Array.IndexOf(modes, difficulty) : 0;
        band = index == (int)Dock.Fleet ? System.Array.IndexOf(modes, difficulty) : 0;
        HoldingShip = true;
        repeatAt = Time.unscaledTime + .27f;
        dialog?.Hide();
        described = -2;
        console = new GameObject(signs[room] + " console", typeof(RectTransform));
        console.transform.SetParent(transform, false);
        instrument = new DockInstrument(console, dialog?.font, lamps[room], 31950);
        instrument.group.alpha = 0;
        if (room == (int)Dock.Armory)
            PrepareCatalog();
        RenderPage();
        ItemSound.Play("ready", .09f);
    }
    void CloseDock()
    {
        ReleaseExhibit();
        room = -1;
        HoldingShip = false;
        if (console != null)
            Destroy(console);
        pageNodes.Clear();
        instrument = null;
    }
}
