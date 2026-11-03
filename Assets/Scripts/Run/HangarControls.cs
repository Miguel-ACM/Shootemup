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
    Vector2 Desk() => room == (int)Dock.Armory ? FilterDesk : berths[room] + Vector2.down * 1.25f;

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
        if (room >= 0 && !operating)
        {
            if (room == (int)Dock.Fleet)
            {
                point = parkedShips[selection].position;
                show = MetaProgress.CanFly((ShipFrame)selection) && Vector2.Distance(point, p) < 1.3f;
            }
            else
            {
                point = Spot(Desk(), 0);
                show = Vector2.Distance(p, point) < 1.6f;
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
        readouts.Aim(hoverPrompt, point + Vector3.up * (room < 0 ? 1.92f : 1.12f));
    }

    void MountReceiver(Vector2 at)
    {
        var go = new GameObject("Cast monitor housing");
        go.transform.SetParent(transform, false);
        go.transform.position = Spot(at, 2);
        go.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.DockMonitor();
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = CombatMaterials.Fleet;
        r.sortingLayerName = "Background";
        r.sortingOrder = -4;
    }
}
