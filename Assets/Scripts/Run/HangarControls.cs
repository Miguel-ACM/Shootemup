using TMPro;
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

    void HangingSign(string text, Vector2 at, Color color, bool suspended)
    {
        if (suspended)
        {
            for (int side = -1; side <= 1; side += 2)
                Strip("Sign hanger", at + new Vector2(side * .91f, .49f), new Vector2(.045f, .56f), Metal, -7);
        }
        var sign = MountedPanel("Floating " + text, at, new Vector2(2.75f, .74f), suspended);
        DockInstrument.Block(sign, "Lit lip", new Vector2(0, -34), new Vector2(257, 4), color);
        var label = WorldLabel(sign, text, Vector2.zero, new Vector2(262, 62), 32, color);
        label.fontStyle = FontStyles.Bold;
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
