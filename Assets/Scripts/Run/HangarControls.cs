using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class HangarHub
{
    RectTransform hoverPrompt;
    RawImage inputPicture, buyIcon, pinIcon;
    Texture keyPicture, bumperPicture;
    const float DeskReach = 1.9f;
    static readonly string[] stationModels = { "DockQuadrant", "DockReader", "DockTrainer", "DockTransport", "DockScope" };
    readonly Transform[] pilotLamps = new Transform[5];

    static Vector2 StationAt(int index)
    {
        if (index == (int)Dock.Armory) return FilterDesk;
        Vector2 door = berths[index];
        return new Vector2(Mathf.Sign(door.x) * 12.4f, door.y - 2.65f);
    }

    static Vector2 MonitorAt(int index)
    {
        if (index == (int)Dock.Armory) return FilterDesk + new Vector2(1.1f, 5.65f);
        Vector2 door = berths[index];
        return new Vector2(Mathf.Sign(door.x) * 13.8f, door.y + 3.25f);
    }

    static float MonitorScale(int index) => index == (int)Dock.Fleet ? .94f : .82f;
    Vector2 Desk() => StationAt(room);

    void BuildControlDesk(int index)
    {
        Vector2 at = StationAt(index);
        var station = MountModel(stationModels[index], Spot(at, 1), -2, "Background");
        station.localRotation = Quaternion.Euler(19, -9, 0);
        Shadow(Spot(at + new Vector2(.16f, -.25f), 57), 1.93f, -9);
        // The status lamp is set into the approach sill, clear of the instrument itself.
        var sill = at + new Vector2(0, -1.7f);
        Strip("Docking sill", sill, new Vector2(1.35f, .12f), Metal, -7);
        pilotLamps[index] = Strip("Console ready lamp", sill, new Vector2(.64f, .06f), lamps[index], -6);
    }

    sealed class LiftSign
    {
        public Transform body;
        public Vector3 rest;
        public readonly Transform[] exhaust = new Transform[2];
    }
    readonly List<LiftSign> liftSigns = new List<LiftSign>();

    void HoverSign(string text, Vector2 at, Color color)
    {
        var sign = new LiftSign { rest = Spot(at, 1.5f) };
        sign.body = MountModel("DockHoverSign", sign.rest, 2, "Background");
        sign.body.name = text + " lift sign";
        var shipEffects = player.GetComponent<PSXShipEffects>();
        for (int i = 0; i < sign.exhaust.Length; i++)
        {
            var jet = new GameObject(i == 0 ? "Port lift exhaust" : "Starboard lift exhaust");
            jet.transform.SetParent(sign.body, false);
            jet.transform.localPosition = new Vector3(i == 0 ? -1.55f : 1.55f, -.55f, -.24f);
            jet.AddComponent<MeshFilter>().sharedMesh = shipEffects.flameMesh;
            var renderer = jet.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = shipEffects.exhaustMaterial;
            renderer.sortingLayerName = "Background";
            renderer.sortingOrder = 1;
            sign.exhaust[i] = jet.transform;
        }
        liftSigns.Add(sign);
        var panel = MountedPanel("Lettering " + text, at, new Vector2(2.45f, .76f), sign.body);
        panel.GetComponent<Image>().enabled = false;
        var label = WorldLabel(panel, text == "FLIGHT CONTROL" ? "FLIGHT\nCONTROL" : text,
            Vector2.zero, panel.sizeDelta, 30, color);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.fontSizeMin = 19;
        var shadow = Shadow(Spot(at + new Vector2(.12f, -.42f), 58), 1.9f, -7);
        shadow.localScale = new Vector3(1, .42f, 1);
    }

    void AnimateSigns()
    {
        for (int i = 0; i < liftSigns.Count; i++)
        {
            var sign = liftSigns[i];
            if (!sign.body.gameObject.activeInHierarchy)
                continue;
            float phase = Time.time * 1.65f + i * 1.9f;
            sign.body.position = sign.rest + Vector3.up * (.11f * Mathf.Sin(phase));
            float thrust = .76f + .14f * Mathf.Cos(phase);
            for (int side = 0; side < sign.exhaust.Length; side++)
            {
                float flicker = Mathf.Sin(Mathf.Floor(Time.time * 15) * 2.39f + i + side * 1.7f);
                sign.exhaust[side].localScale = new Vector3(1.4f, thrust + .13f * flicker, 1);
            }
        }
    }
    void PortalLight(Vector2 at)
    {
        // One continuous pool of light through the sill, feathered against both jambs.
        const int columns = 13, rows = 5;
        var vertices = new Vector3[columns * rows];
        var colors = new Color[vertices.Length];
        var triangles = new int[(columns - 1) * (rows - 1) * 6];
        int t = 0;
        for (int x = 0; x < columns; x++)
        {
            float u = 2f * x / (columns - 1) - 1;
            float glow = Mathf.SmoothStep(0, .82f, 1 - Mathf.Abs(u));
            for (int y = 0; y < rows; y++)
            {
                int i = x * rows + y;
                float v = (y - 2) * .5f;
                vertices[i] = new Vector3(u * 2.35f, Mathf.Sign(v) * Mathf.Sqrt(Mathf.Abs(v)) * 1.06f);
                colors[i] = new Color(.7f, .8f, .86f, y == 0 || y == rows - 1 ? 0 : glow);
                if (x == 0 || y == 0) continue;
                triangles[t++] = i - rows - 1; triangles[t++] = i - rows; triangles[t++] = i;
                triangles[t++] = i - rows - 1; triangles[t++] = i; triangles[t++] = i - 1;
            }
        }
        var mesh = new Mesh { name = "Threshold light", vertices = vertices, colors = colors, triangles = triangles };
        quads.Add(mesh);
        var go = new GameObject("Portal illumination");
        go.transform.SetParent(transform, false);
        go.transform.position = Spot(at, 3);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = scenery;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = 1;
    }
    void BuildPrompt()
    {
        hoverPrompt = MountedPanel("Control glyph", Vector2.zero, new Vector2(1.25f, .85f));
        hoverPrompt.GetComponent<Image>().enabled = false;
        keyPicture = Resources.Load<Texture2D>("DockArt/KeyPrompt");
        bumperPicture = Resources.Load<Texture2D>("DockArt/PadPrompt");
        inputPicture = ButtonIcon(hoverPrompt, Vector2.zero, 1);
        buyIcon = ButtonIcon(stockScreen, new Vector2(-157, -100), .72f);
        pinIcon = ButtonIcon(stockScreen, new Vector2(48, -100), .72f);
        WorldLabel(stockScreen, "BUY", new Vector2(-55, -100), new Vector2(103, 55), 32, DockInstrument.Paper);
        WorldLabel(stockScreen, "PIN", new Vector2(150, -100), new Vector2(103, 55), 32, DockInstrument.Paper);
        hoverPrompt.gameObject.SetActive(false);
    }
    RawImage ButtonIcon(RectTransform parent, Vector2 at, float scale)
    {
        var icon = new GameObject("Control image", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        icon.rectTransform.SetParent(parent, false);
        icon.rectTransform.sizeDelta = new Vector2(118, 82);
        icon.rectTransform.anchoredPosition = at;
        icon.rectTransform.localScale = Vector3.one * scale;
        icon.raycastTarget = false;
        var legend = WorldLabel(icon.rectTransform, "", Vector2.up * 4, new Vector2(94, 61), 45, DockInstrument.Ink);
        legend.fontStyle = FontStyles.Bold;
        return icon;
    }
    void UpdateButtonIcon(RawImage icon, bool secondary = false)
    {
        bool pad = DockInstrument.UsingGamepad;
        var texture = pad ? bumperPicture : keyPicture;
        if (icon.texture == texture) return;
        icon.texture = texture;
        string key = DockInstrument.FireKey(secondary);
        bool leftBumper = pad && key == "LEFT SHOULDER";
        icon.uvRect = leftBumper ? new Rect(1, 0, -1, 1) : new Rect(0, 0, 1, 1);
        var legend = icon.GetComponentInChildren<TextMeshProUGUI>();
        legend.fontSizeMax = pad ? 31 : 45;
        legend.fontSizeMin = legend.fontSizeMax * .75f;
        legend.text = leftBumper ? "LB" : pad && key == "RIGHT SHOULDER" ? "RB" : key;
    }
    void UpdatePrompt(Vector3 p)
    {
        Vector3 point = p;
        bool show = false;
        if (room >= 0)
        {
            point = Spot(Desk(), 0);
            show = !operating && Vector2.Distance(p, point) < DeskReach;
            var lamp = pilotLamps[room];
            lamp.localScale = new Vector3(operating ? 3.4f : 2.55f, .25f + .09f * Mathf.Sin(Time.time * (operating ? 7 : 3)), 1);
            if (!show && !operating && room == (int)Dock.Fleet)
            {
                point = parkedShips[selection].position;
                show = (ShipFrame)selection != MetaProgress.Ship && MetaProgress.CanFly((ShipFrame)selection) && Vector2.Distance(point, p) < 1.3f;
            }
        }
        hoverPrompt.gameObject.SetActive(show);
        if (!show)
            return;
        if (hoverPrompt.GetSiblingIndex() != hoverPrompt.parent.childCount - 1)
            hoverPrompt.SetAsLastSibling();
        UpdateButtonIcon(inputPicture);
        readouts.Aim(hoverPrompt, point + Vector3.up * 1.63f);
    }

    void MountReceiver(int index)
    {
        var shell = MountModel("DockReceiver", Spot(MonitorAt(index), 2), -4, "Background");
        shell.localScale = Vector3.one * MonitorScale(index);
    }
}
