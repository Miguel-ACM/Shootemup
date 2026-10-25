using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Available in builds too: debug runs are explicit, normal START always stays clean.
public sealed class DebugRunMenu : AMenuElement
{
    sealed class Entry
    {
        public Type type;
        public string name;
        public AItem.ItemType kind;
        public int count;
        public TextMeshProUGUI label;
    }

    static readonly List<(Type type, int slot)> pending = new List<(Type, int)>();
    readonly List<Entry> entries = new List<Entry>();
    readonly Entry[] weapons = new Entry[2], actives = new Entry[2];
    readonly List<Button> buttons = new List<Button>();
    GameObject overlay;
    ScrollRect scroll;
    RectTransform content;
    TextMeshProUGUI loadout;
    LevelId first = LevelId.Fields;
    int slot;
    bool starting;
    int openedFrame;
    GameObject previousSelection;
    static readonly Color Ink = new Color(.035f, .055f, .06f, .98f);
    static readonly Color Gold = new Color(.86f, .7f, .4f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPending()
    {
        pending.Clear();
        SceneManager.sceneLoaded -= SceneReady;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded += SceneReady;
    }

    static void SceneReady(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Title")
        {
            var start = FindAnyObjectByType<NewGame>();
            if (start == null)
                return;
            var menu = start.GetComponentInParent<Menu>();
            var row = new GameObject("Debug", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(menu.transform, false);
            var rect = row.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(726, 52);
            rect.anchoredPosition = new Vector2(0, -126);
            var debug = row.AddComponent<DebugRunMenu>();
            debug.Text(row.transform, "DEBUG", 24, Vector2.zero, new Vector2(726, 52));
            menu.RefreshElements();
        }
        else if (scene.name == "L1" && pending.Count > 0)
            new GameObject("Debug starting loadout").AddComponent<DebugLoadoutGrant>().Begin(pending.ToArray());
        if (scene.name != "Title")
            pending.Clear();
    }

    public override void OnAccept()
    {
        if (overlay != null)
            return;
        menu.Suspended = true;
        openedFrame = Time.frameCount;
        Build();
        EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
    }

    void Build()
    {
        overlay = new GameObject("Debug loadout", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        var panel = Box(overlay.transform, "Debug panel", new Vector2(0, 0), new Vector2(1040, 672), Ink);
        Text(panel, "DEBUG RUN", 30, new Vector2(-330, 295), new Vector2(320, 48));
        ButtonAt(panel, "BACK", new Vector2(418, 295), new Vector2(150, 40), Close);
        var levels = new[] { LevelId.Fields, LevelId.Snow, LevelId.City, LevelId.Thunder, LevelId.Tunnel, LevelId.Desert, LevelId.Candyland };
        for (int i = 0; i < levels.Length; i++)
        {
            var level = levels[i];
            ButtonAt(panel, level == LevelId.Fields ? "PLAINS" : level.ToString().ToUpperInvariant(), new Vector2(-420 + i * 140, 235), new Vector2(134, 38), () => { first = level; Refresh(); });
        }
        ButtonAt(panel, "SLOT 1", new Vector2(-415, 177), new Vector2(145, 38), () => { slot = 0; Refresh(); });
        ButtonAt(panel, "SLOT 2", new Vector2(-255, 177), new Vector2(145, 38), () => { slot = 1; Refresh(); });
        loadout = Text(panel, "", 18, new Vector2(170, 167), new Vector2(610, 66));
        var viewport = Box(panel, "Items", new Vector2(0, -49), new Vector2(980, 340), new Color(.06f, .09f, .09f, 1));
        viewport.gameObject.AddComponent<RectMask2D>();
        scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30;
        content = new GameObject("Catalogue", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(.5f, 1);
        scroll.content = content;
        foreach (var type in typeof(AItem).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(AItem).IsAssignableFrom(t)))
        {
            // Awake supplies the authored name/type. Disable before Start can seek a ship.
            var probe = new GameObject("Catalogue metadata");
            var item = (AItem)probe.AddComponent(type);
            item.enabled = false;
            entries.Add(new Entry { type = type, name = item.GetData()[0], kind = item.Kind });
            Destroy(probe);
        }
        entries.Sort((a, b) => { int c = a.kind.CompareTo(b.kind); return c != 0 ? c : string.Compare(a.name, b.name, StringComparison.Ordinal); });
        content.sizeDelta = new Vector2(0, Mathf.CeilToInt(entries.Count / 3f) * 46 + 12);
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            var button = ButtonAt(content, e.name, new Vector2(-324 + i % 3 * 324, -29 - i / 3 * 46), new Vector2(312, 38), () => Add(e));
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
            e.label = button.GetComponentInChildren<TextMeshProUGUI>();
        }
        Text(panel, "SCROLL  /  SELECT TO ADD", 16, new Vector2(-240, -246), new Vector2(500, 30));
        ButtonAt(panel, "CLEAR", new Vector2(-390, -294), new Vector2(190, 42), Clear);
        ButtonAt(panel, "START DEBUG RUN", new Vector2(240, -294), new Vector2(490, 42), StartRun);
        Refresh();
    }

    void Add(Entry entry)
    {
        if (entry.kind == AItem.ItemType.weapon)
            weapons[slot] = entry;
        else if (entry.kind == AItem.ItemType.active)
            actives[slot] = entry;
        else
            entry.count++;
        Refresh();
    }

    void Clear()
    {
        foreach (var entry in entries)
            entry.count = 0;
        Array.Clear(weapons, 0, 2);
        Array.Clear(actives, 0, 2);
        Refresh();
    }

    void Refresh()
    {
        loadout.text = $"{(first == LevelId.Fields ? "PLAINS" : first.ToString().ToUpperInvariant())}  /  SLOT {slot + 1}\n" +
            $"{weapons[0]?.name ?? "Base gun"}  |  {weapons[1]?.name ?? "Empty"}\n{actives[0]?.name ?? "Empty"}  |  {actives[1]?.name ?? "Empty"}";
        foreach (var entry in entries)
        {
            string suffix = entry.count > 0 ? "  x" + entry.count : "";
            if (weapons[0] == entry || actives[0] == entry)
                suffix += "  [1]";
            if (weapons[1] == entry || actives[1] == entry)
                suffix += "  [2]";
            entry.label.text = entry.name + suffix;
            entry.label.color = suffix.Length > 0 ? Gold : new Color(.76f, .84f, .8f);
        }
    }

    void StartRun()
    {
        if (starting || CloudTransition.Busy)
            return;
        starting = true;
        pending.Clear();
        foreach (var entry in entries)
            for (int i = 0; i < entry.count; i++)
                pending.Add((entry.type, 0));
        for (int i = 0; i < 2; i++)
        {
            if (weapons[i] != null)
                pending.Add((weapons[i].type, i));
            if (actives[i] != null)
                pending.Add((actives[i].type, i));
        }
        GameRules.StartNewRun();
        LevelRun.Reset(first);
        BGMManager.PlayLevelMusic(first);
        CloudTransition.Load("L1");
    }

    void Close()
    {
        Destroy(overlay);
        overlay = null;
        entries.Clear();
        buttons.Clear();
        Array.Clear(weapons, 0, 2);
        Array.Clear(actives, 0, 2);
        menu.Suspended = false;
        EventSystem.current.SetSelectedGameObject(null);
    }

    void Update()
    {
        if (overlay == null || starting)
            return;
        if (Time.frameCount > openedFrame && ((Keyboard.current?.escapeKey.wasPressedThisFrame ?? false) || (Gamepad.current?.buttonEast.wasPressedThisFrame ?? false)))
        {
            Close();
            return;
        }
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == previousSelection)
            return;
        previousSelection = selected;
        if (selected == null || !selected.transform.IsChildOf(content))
            return;
        // Keep keyboard/controller focus visible while navigating the catalogue.
        var rect = selected.GetComponent<RectTransform>();
        float y = -rect.anchoredPosition.y;
        float top = content.anchoredPosition.y;
        if (y - 22 < top)
            top = y - 22;
        else if (y + 22 > top + 340)
            top = y + 22 - 340;
        content.anchoredPosition = new Vector2(0, Mathf.Clamp(top, 0, Mathf.Max(0, content.sizeDelta.y - 340)));
    }

    protected override void LateUpdate()
    {
        if (overlay == null)
            base.LateUpdate();
    }

    void OnDestroy()
    {
        if (overlay != null)
            Destroy(overlay);
    }

    RectTransform Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        go.GetComponent<Image>().color = color;
        return rect;
    }

    TextMeshProUGUI Text(Transform parent, string value, float size, Vector2 position, Vector2 dimensions)
    {
        var go = new GameObject(value, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = GetComponentInParent<Menu>().GetComponentInChildren<TextMeshProUGUI>().font;
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(.76f, .84f, .8f);
        text.raycastTarget = false;
        return text;
    }

    Button ButtonAt(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var rect = Box(parent, label, position, size, new Color(.12f, .17f, .17f, 1));
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        var colors = button.colors;
        colors.highlightedColor = Gold;
        colors.selectedColor = Gold;
        colors.pressedColor = new Color(.6f, .48f, .25f);
        button.colors = colors;
        Text(rect, label, 20, Vector2.zero, size);
        // The submit that opens DEBUG must not also activate its first button.
        button.onClick.AddListener(() => { if (Time.frameCount > openedFrame) action(); });
        buttons.Add(button);
        return button;
    }
}

public sealed class DebugLoadoutGrant : MonoBehaviour
{
    public void Begin((Type type, int slot)[] items) => StartCoroutine(Grant(items));
    IEnumerator Grant((Type type, int slot)[] items)
    {
        yield return null; // Ship, base weapons, HUD and combat upgrades finish Start first.
        var ship = FindAnyObjectByType<Shipcontroller>();
        if (ship == null)
        {
            Debug.LogError("Debug loadout: missing ship");
            Destroy(gameObject);
            yield break;
        }
        var grants = new List<(AItem item, int slot)>();
        foreach (var choice in items)
        {
            var go = Instantiate(Resources.Load<GameObject>("Items/Item"), new Vector3(100, 100, 1), Quaternion.identity);
            grants.Add(((AItem)go.AddComponent(choice.type), choice.slot));
        }
        yield return null; // Initialize item art and active-item state before normal pickup effects.
        foreach (var grant in grants)
            grant.item.GrantTo(ship, grant.slot);
        Destroy(gameObject);
    }
}
