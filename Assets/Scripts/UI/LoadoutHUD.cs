using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using System.Text.RegularExpressions;

// Instruments live entirely outside the fixed portrait playfield.
public sealed class LoadoutHUD : MonoBehaviour
{
    ShipLoadout loadout;
    Canvas canvas;
    Font font;
    RenderTexture texture;
    Transform studio;
    RectTransform[] panels = new RectTransform[4];
    OctagonGraphic[] rims = new OctagonGraphic[4];
    Text[] names = new Text[4], keys = new Text[4], states = new Text[4];
    Transform[] models = new Transform[4];
    string[] displayed = new string[4];
    RectTransform badge;
    Text[] badgeKeys = new Text[2];
    static Sprite keycap;
    static readonly Color Amber = new Color(1, .71f, .29f), Cyan = new Color(.4f, .82f, .86f), Steel = new Color(.48f, .59f, .61f);
    public RectTransform[] Panels => panels;
    void Start()
    {
        loadout = GetComponent<ShipLoadout>();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var root = new GameObject("Octagonal loadout instruments", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        BuildStudio();
        for (int i = 0; i < 4; i++)
        {
            bool active = i >= 2;
            float size = active ? 74 : 124;
            var p = Plate(root.transform, (active ? "Active " : "Weapon ") + (i % 2 + 1), size, new Color(.035f, .055f, .065f, .98f));
            panels[i] = p;
            FluorescentStartup.Attach(p.gameObject, .16f + (i % 2) * .1f + (active ? .08f : 0));
            p.anchorMin = p.anchorMax = new Vector2(.5f, 0);
            rims[i] = Plate(p, "Machined rim", size - 4, Steel, 2).GetComponent<OctagonGraphic>();
            Plate(p, "Inset seam", size - 13, new Color(.19f, .27f, .29f), 1);
            var picture = new GameObject("Equipment model", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            picture.transform.SetParent(p, false);
            var image = picture.GetComponent<RawImage>();
            image.raycastTarget = false;
            image.texture = texture;
            image.uvRect = new Rect(i * .25f, 0, .25f, 1);
            picture.GetComponent<RectTransform>().sizeDelta = Vector2.one * (active ? 52 : 84);
            picture.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, active ? 2 : 7);
            keys[i] = Key(p, active ? new Vector2(0, -32) : new Vector2(0, -55));
            names[i] = Label(p, "EMPTY", active ? 9 : 10, new Vector2(0, active ? 62 : -84), new Vector2(active ? 114 : 170, 30), Steel);
            states[i] = Label(p, "", 8, new Vector2(0, active ? 44 : 43), new Vector2(100, 12), Cyan);
            if (!active)
            {
                for (int s = -1; s <= 1; s += 2)
                    Plate(p, "Fastener", 4, Steel).anchoredPosition = new Vector2(s * 49, 0);
            }
        }
        badge = new GameObject("Replacement controls above ship", typeof(RectTransform)).GetComponent<RectTransform>();
        badge.SetParent(root.transform, false);
        badge.sizeDelta = new Vector2(80, 32);
        badgeKeys[0] = Key(badge, new Vector2(-22, 0));
        badgeKeys[1] = Key(badge, new Vector2(22, 0));
        badge.gameObject.SetActive(false);
    }

    void BuildStudio()
    {
        // Keep camera and mesh units independent of the player root's gameplay scale.
        studio = new GameObject("Loadout model studio").transform;
        studio.position = new Vector3(650, 0, 0);
        texture = new RenderTexture(512, 128, 16, RenderTextureFormat.ARGB32) { name = "Loadout equipment portraits", filterMode = FilterMode.Point };
        var go = new GameObject("Loadout portrait camera");
        go.transform.SetParent(studio, false);
        go.transform.localPosition = new Vector3(0, 0, -10);
        var camera = go.AddComponent<UnityEngine.Camera>();
        camera.orthographic = true;
        camera.orthographicSize = .65f;
        camera.aspect = 4;
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 20;
        camera.backgroundColor = Color.clear;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.targetTexture = texture;
        camera.depth = -49;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
    }

    void Update()
    {
        if (loadout == null || panels[0] == null)
            return;
        bool pad = Gamepad.current != null && (Keyboard.current == null || Gamepad.current.lastUpdateTime > Keyboard.current.lastUpdateTime);
        string[] inputs = pad ? new[] { "RB", "LB", "LT", "RT" } : new[] { "Z", "X", "A", "S" };
        float room = Mathf.Max(0, (Screen.width * 720f / Screen.height - 405) * .5f);
        float scale = Mathf.Min(.64f, Mathf.Max(0, room - 52) / 175f);
        for (int i = 0; i < 4; i++)
        {
            bool active = i >= 2;
            int side = i % 2 == 0 ? -1 : 1;
            panels[i].localScale = Vector3.one * scale;
            // Matching 45-degree chamfers meet along a shared edge, without a floating gap.
            float join = .77f * (124 + 74) * .5f;
            panels[i].anchoredPosition = new Vector2(side * (202.5f + 52 + (62 + (active ? join : 0)) * scale), (90 + (active ? join : 0)) * scale);
            string type = active ? (loadout.Actives[i - 2] != null ? loadout.Actives[i - 2].GetType().Name : "") : (loadout.Weapons[i] != null ? loadout.Weapons[i].GetType().Name : "");
            if (displayed[i] != type)
                SetModel(i, type);
            keys[i].text = inputs[i];
            bool selected = loadout.HoveredItem != null && active == (loadout.HoveredItem is AActiveItem);
            rims[i].color = selected ? Color.Lerp(Steel, Amber, .5f + .5f * Mathf.Sin(Time.unscaledTime * 12)) : active ? Cyan * .8f : Steel;
            if (!selected && active && loadout.Actives[i - 2] is Overdrive && GetComponent<OverdriveBoost>() != null)
                rims[i].color = Color.Lerp(Amber, Steel, .2f + .2f * Mathf.Sin(Time.time * 10));
            if (!selected && !active && loadout.Weapons[i] is WindUpCannon cannon && cannon.Charging)
                rims[i].color = Color.Lerp(Steel, Cyan, Mathf.Clamp01(cannon.Charge / 1.2f));
            names[i].enabled = false;
            states[i].text = "";
            if (active && !selected && loadout.Actives[i - 2] != null)
            {
                var item = loadout.Actives[i - 2];
                float remaining = Mathf.Max(0, item.cd - item.lastCD);
                states[i].text = remaining > .05f ? Mathf.CeilToInt(remaining).ToString() : "";
                states[i].color = remaining > .05f ? Steel : Cyan;
            }
            if (models[i] != null)
                models[i].localRotation = Quaternion.Euler(-16, Mathf.Sin(Time.unscaledTime * .65f + i) * 24, i < 2 ? -30 : 0);
        }
        bool show = loadout.HoveredItem != null;
        badge.gameObject.SetActive(show);
        if (show)
        {
            var viewport = UnityEngine.Camera.main.WorldToViewportPoint(transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, new Vector2(viewport.x * Screen.width, viewport.y * Screen.height), null, out var point);
            badge.anchoredPosition = point + new Vector2(0, 42);
            int offset = loadout.HoveredItem is AActiveItem ? 2 : 0;
            badgeKeys[0].text = inputs[offset];
            badgeKeys[1].text = inputs[offset + 1];
        }
    }

    void SetModel(int slot, string type)
    {
        displayed[slot] = type;
        if (models[slot] != null)
            Destroy(models[slot].gameObject);
        names[slot].text = type.Length == 0 ? "EMPTY" : Regex.Replace(type.Replace("Weapon", ""), "(?<=[a-z])(?=[A-Z])", " ").ToUpperInvariant();
        if (type.Length == 0)
            return;
        var mesh = IndustrialMeshFactory.ExpansionItem(slot < 2 ? type + "Item" : type);
        var go = new GameObject(type + " portrait");
        go.transform.SetParent(studio, false);
        go.transform.localPosition = new Vector3((slot - 1.5f) * 1.3f, 0, 0);
        go.transform.localScale = Vector3.one * (.87f / Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z));
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = CombatMaterials.Fleet;
        models[slot] = go.transform;
    }

    RectTransform Plate(Transform parent, string name, float size, Color color, float border = 0)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(OctagonGraphic));
        go.transform.SetParent(parent, false);
        var shape = go.GetComponent<OctagonGraphic>();
        shape.color = color;
        shape.thickness = border;
        shape.raycastTarget = false;
        var r = (RectTransform)go.transform;
        r.sizeDelta = Vector2.one * size;
        return r;
    }

    Text Label(Transform parent, string value, int size, Vector2 at, Vector2 span, Color color)
    {
        var go = new GameObject(value, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.text = value;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        var r = (RectTransform)go.transform;
        r.anchoredPosition = at;
        r.sizeDelta = span;
        return t;
    }

    Text Key(Transform parent, Vector2 at)
    {
        if (keycap == null)
        {
            var texture = new Texture2D(32, 24, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color[32 * 24];
            for (int y = 0; y < 24; y++)
                for (int x = 0; x < 32; x++)
                {
                    bool corner = (x < 3 || x > 28) && (y < 3 || y > 20);
                    pixels[y * 32 + x] = corner ? Color.clear : x < 2 || x > 29 || y < 2 ? new Color(.12f, .19f, .2f) : y > 21 ? new Color(.83f, .9f, .86f) : y < 5 ? new Color(.29f, .39f, .39f) : new Color(.65f, .74f, .71f);
                }
            texture.SetPixels(pixels);
            texture.Apply();
            keycap = Sprite.Create(texture, new Rect(0, 0, 32, 24), new Vector2(.5f, .5f), 32);
        }
        var go = new GameObject("Control keycap sprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().sprite = keycap;
        go.GetComponent<Image>().raycastTarget = false;
        var rect = (RectTransform)go.transform;
        rect.sizeDelta = new Vector2(32, 24);
        rect.anchoredPosition = at;
        return Label(rect, "", 12, new Vector2(0, 1), new Vector2(32, 24), new Color(.04f, .09f, .1f));
    }

    void OnDestroy()
    {
        if (canvas != null)
            Destroy(canvas.gameObject);
        if (studio != null)
            Destroy(studio.gameObject);
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
    }
}
