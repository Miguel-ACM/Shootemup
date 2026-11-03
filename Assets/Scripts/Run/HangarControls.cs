using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed partial class HangarHub
{
    RectTransform hoverPrompt;
    RawImage inputPicture;
    TextMeshProUGUI binding;
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
    void PortalLight(Vector2 at, float side)
    {
        // The same pale, continuous mist as the departure aperture, turned into the side corridor.
        var mesh = new Mesh { name = "Threshold light" };
        mesh.vertices = new[] { new Vector3(-1.5f * side, -1), new Vector3(-1.5f * side, 1), new Vector3(.8f * side, 1), new Vector3(.8f * side, -1) };
        var clear = new Color(.62f, .74f, .8f, 0);
        var lit = new Color(.7f, .8f, .86f, .82f);
        mesh.colors = new[] { clear, clear, lit, lit };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
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
        inputPicture = new GameObject("Control image", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        inputPicture.transform.SetParent(hoverPrompt, false);
        inputPicture.rectTransform.sizeDelta = new Vector2(118, 82);
        binding = WorldLabel(hoverPrompt, "", Vector2.up * 4, new Vector2(94, 61), 45, DockInstrument.Ink);
        binding.fontStyle = FontStyles.Bold;
        keyPicture = Resources.Load<Texture2D>("DockArt/KeyPrompt");
        bumperPicture = Resources.Load<Texture2D>("DockArt/PadPrompt");
        hoverPrompt.gameObject.SetActive(false);
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
        if (room < 0 && NearestPlinth(p) >= 0)
            show = true;
        hoverPrompt.gameObject.SetActive(show);
        if (!show)
            return;
        if (hoverPrompt.GetSiblingIndex() != hoverPrompt.parent.childCount - 1)
            hoverPrompt.SetAsLastSibling();
        bool pad = Gamepad.current != null && (Keyboard.current == null || Gamepad.current.lastUpdateTime > Keyboard.current.lastUpdateTime);
        inputPicture.texture = pad ? bumperPicture : keyPicture;
        string key = DockInstrument.FireKey();
        binding.text = pad && key.Contains("SHOULDER") ? (key.Contains("RIGHT") ? "RB" : "LB") : key;
        Vector3 offset = room < 0 ? new Vector3(-1.25f, .32f) : Vector3.up * 1.63f;
        readouts.Aim(hoverPrompt, point + offset);
    }

    void MountReceiver(int index)
    {
        var shell = MountModel("DockReceiver", Spot(MonitorAt(index), 2), -4, "Background");
        shell.localScale = Vector3.one * MonitorScale(index);
    }
}
