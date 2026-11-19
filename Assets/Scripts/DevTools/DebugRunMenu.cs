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
public sealed partial class DebugRunMenu : AMenuElement
{
    sealed class Entry
    {
        public Type type;
        public string name;
        public AItem.ItemType kind;
        public int count;
        public int portrait;
        public Image card;
        public TextMeshProUGUI badge;
        public string description, weaponName;
        public ItemRarity rarity;
    }

    sealed class SlotCard
    {
        public Image card;
        public RawImage weapon, active;
        public TextMeshProUGUI weaponName, activeName;
    }

    static readonly List<(Type type, int slot)> pending = new List<(Type, int)>();
    // Opens a pact offer before the run's first wave.
    static bool forcePact;
    static int debugRun = -1;
    internal static bool IsDebugRun => debugRun == GameRules.RunSerial;
    int routeIndex, damageIndex;
    static readonly int[] damageSteps = { 1, 2, 4, 8, 16 };
    TextMeshProUGUI routeLabel, damageLabel;
    Image pactButton;
    TextMeshProUGUI weatherLabel;
    // Null follows the run seed; otherwise the debug run flies this district condition.
    DistrictCondition? weather;
    TextMeshProUGUI modifierLabel;
    // Null follows the seed; otherwise every district of the debug run takes this set.
    LevelModifier? modifiers;
    static readonly LevelModifier?[] ModifierCycle = { null, LevelModifier.None, LevelModifier.Elites, LevelModifier.Coins, LevelModifier.FastFire, LevelModifier.Elites | LevelModifier.Coins | LevelModifier.FastFire };
    readonly List<Entry> entries = new List<Entry>();
    readonly Entry[] weapons = new Entry[2], actives = new Entry[2];
    readonly List<Button> buttons = new List<Button>();
    readonly Dictionary<LevelId, Image> levelButtons = new Dictionary<LevelId, Image>();
    readonly SlotCard[] slots = new SlotCard[2];
    ItemPortraitStudio studio;
    GameObject overlay;
    ScrollRect scroll;
    RectTransform content;
    LevelId first = LevelId.Fields;
    int slot;
    bool starting;
    int openedFrame;
    GameObject previousSelection;
    static readonly Color Ink = new Color(.035f, .055f, .06f, .98f);
    static readonly Color Gold = new Color(.86f, .7f, .4f);
    static readonly Color CardColor = new Color(.085f, .12f, .125f, 1), ChosenCard = new Color(.32f, .25f, .12f, 1);
    static readonly Color Ivory = new Color(.76f, .84f, .8f);
    const int Columns = 6;
    const float CardWidth = 152, CardHeight = 104, CardStep = 112, ViewHeight = 330;
    const int BaseGun = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPending()
    {
        pending.Clear();
        browserMemory = null;
        debugRun = -1;
        forcePact = false;
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
            debug.Label(row.transform, "DEBUG", 24, Vector2.zero, new Vector2(726, 52));
            menu.RefreshElements();
        }
        else if (scene.name == "L1" && pending.Count > 0)
            new GameObject("Debug starting loadout").AddComponent<DebugLoadoutGrant>().Begin(pending.ToArray());
        if (scene.name == "L1" && forcePact)
            new GameObject("Debug pact").AddComponent<DebugPactOffer>();
        if (scene.name != "Title")
        {
            pending.Clear();
            forcePact = false;
        }
    }

    public override void OnAccept()
    {
        if (overlay != null)
            return;
        if (InRun && (FindAnyObjectByType<Shipcontroller>() == null || !Menu.GameplayPaused))
            return;
        menu.Suspended = true;
        openedFrame = Time.frameCount;
        Build();
        if (InRun) RestoreBrowser();
        else EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
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
        if (InRun) scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var panel = Box(overlay.transform, "Debug panel", new Vector2(0, 0), new Vector2(1040, 672), Ink);
        Box(panel, "Header rule", new Vector2(0, 268), new Vector2(980, 2), new Color(.2f, .27f, .27f));
        Label(panel, InRun ? "DEBUG ITEMS" : "DEBUG RUN", 30, new Vector2(-330, 300), new Vector2(320, 48)).color = Gold;
        ButtonAt(panel, "BACK", new Vector2(418, 300), new Vector2(150, 38), Close);
        if (!InRun)
        {
            pactButton = ButtonAt(panel, "PACT", new Vector2(258, 300), new Vector2(150, 38), () => { forcePact = !forcePact; Refresh(); }).GetComponent<Image>();
            weatherLabel = ButtonAt(panel, "SEEDED SKY", new Vector2(100, 300), new Vector2(160, 38), CycleWeather, 16).GetComponentInChildren<TextMeshProUGUI>();
            modifierLabel = ButtonAt(panel, "SEEDED MODS", new Vector2(-80, 300), new Vector2(160, 38), CycleModifiers, 16).GetComponentInChildren<TextMeshProUGUI>();
            var levels = new[] { LevelId.Fields, LevelId.Snow, LevelId.City, LevelId.Thunder, LevelId.Tunnel, LevelId.Desert, LevelId.Mushroom, LevelId.Candyland, LevelId.Space, LevelId.Underwater, LevelId.Sewers, LevelId.Hell, LevelId.Heaven, LevelId.Core, LevelId.Dream, LevelId.Void };
            for (int i = 0; i < levels.Length; i++)
            {
                var level = levels[i];
                var button = ButtonAt(panel, level == LevelId.Fields ? "PLAINS" : level.ToString().ToUpperInvariant(),
                    new Vector2(-427 + i % 8 * 122, 246 - i / 8 * 28), new Vector2(115, 26), () => { first = level; if (weather.HasValue && !DistrictConditions.Allowed(level, weather.Value)) weather = null; Refresh(); }, 14);
                levelButtons[level] = button.GetComponent<Image>();
            }
        }
        foreach (var type in typeof(AItem).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(AItem).IsAssignableFrom(t)))
        {
            // Awake supplies the authored name/type. Disable before Start can seek a ship.
            var probe = new GameObject("Catalogue metadata");
            var item = (AItem)probe.AddComponent(type);
            item.enabled = false;
            entries.Add(new Entry { type = type, name = item.GetData()[0] ?? type.Name, kind = item.Kind,
                description = item.GetData()[2] ?? "", weaponName = (item as AWeaponItem)?.weaponName,
                rarity = ItemCatalog.TryGet(type.Name, out var definition) ? definition.Rarity : ItemRarity.Special });
            Destroy(probe);
        }
        entries.Sort((a, b) => { int c = a.kind.CompareTo(b.kind); return c != 0 ? c : string.Compare(a.name, b.name, StringComparison.Ordinal); });
        var kinds = entries.Select(e => e.type.Name).ToList();
        kinds.Add("BaseWeaponItem");
        studio = ItemPortraitStudio.Create(kinds);
        for (int i = 0; i < entries.Count; i++)
            entries[i].portrait = i;
        for (int i = 0; i < 2; i++)
            slots[i] = SlotCardAt(panel, i);
        var viewport = Box(panel, "Items", new Vector2(0, InRun ? -88 : -52), new Vector2(980, ViewHeight), new Color(.05f, .075f, .08f, 1));
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
        content.sizeDelta = new Vector2(0, Mathf.CeilToInt(entries.Count / (float)Columns) * CardStep + 8);
        for (int i = 0; i < entries.Count; i++)
            ItemCard(entries[i], new Vector2((i % Columns - (Columns - 1) * .5f) * 160, -8 - CardHeight * .5f - i / Columns * CardStep));
        if (InRun)
            BuildBrowser(panel);
        else
        {
            damageLabel = ButtonAt(panel, "", new Vector2(-376,-250), new Vector2(226,32),
                () => { damageIndex = (damageIndex+1)%damageSteps.Length; Refresh(); },16).GetComponentInChildren<TextMeshProUGUI>();
            routeLabel = ButtonAt(panel, "", new Vector2(-108,-250), new Vector2(290,32),
                () => { routeIndex = (routeIndex+1)%LevelRun.VoidOrigins.Length; Refresh(); },16).GetComponentInChildren<TextMeshProUGUI>();
            ButtonAt(panel, "CLEAR", new Vector2(-390, -294), new Vector2(190, 42), Clear);
            ButtonAt(panel, "START DEBUG RUN", new Vector2(240, -294), new Vector2(490, 42), StartRun);
        }
        Refresh();
    }

    SlotCard SlotCardAt(Transform panel, int index)
    {
        var card = new SlotCard();
        var button = ButtonAt(panel, "SLOT " + (index + 1), new Vector2(index == 0 ? -245 : 245, InRun ? 224 : 166), new Vector2(470, 76), () => { slot = index; Refresh(); }, 0);
        card.card = button.GetComponent<Image>();
        Label(button.transform, (index + 1).ToString(), 30, new Vector2(-200, 0), new Vector2(50, 60)).color = Gold;
        card.weapon = Portrait(button.transform, new Vector2(-140, 0), 64);
        card.weaponName = Label(button.transform, "", 14, new Vector2(-40, 0), new Vector2(136, 60));
        card.active = Portrait(button.transform, new Vector2(50, 0), 64);
        card.activeName = Label(button.transform, "", 14, new Vector2(160, 0), new Vector2(136, 60));
        card.weaponName.alignment = card.activeName.alignment = TextAlignmentOptions.Left;
        card.weaponName.textWrappingMode = card.activeName.textWrappingMode = TextWrappingModes.Normal;
        return card;
    }

    void ItemCard(Entry entry, Vector2 position)
    {
        var button = ButtonAt(content, entry.name, position, new Vector2(CardWidth, CardHeight), () => Add(entry), 0, InRun);
        if (button is DebugItemButton itemButton)
            itemButton.Remove = () => { if (Time.frameCount > openedFrame && !granting) StartCoroutine(RemoveItem(entry)); };
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
        entry.card = button.GetComponent<Image>();
        var stripe = Box(button.transform, "Kind", new Vector2(0, CardHeight * .5f - 2), new Vector2(CardWidth, 4), KindColor(entry.kind));
        stripe.GetComponent<Image>().raycastTarget = false;
        Portrait(button.transform, new Vector2(0, 14), 70).uvRect = studio.Cell(entry.portrait);
        var name = Label(button.transform, entry.name, 13, new Vector2(0, -34), new Vector2(CardWidth - 8, 30));
        name.textWrappingMode = TextWrappingModes.Normal;
        entry.badge = Label(button.transform, "", 15, new Vector2(CardWidth * .5f - 22, CardHeight * .5f - 18), new Vector2(40, 22));
        entry.badge.color = Gold;
    }

    static Color KindColor(AItem.ItemType kind) => kind == AItem.ItemType.weapon ? Gold : kind == AItem.ItemType.active ? new Color(.35f, .6f, 1f) : new Color(.45f, .62f, .58f);

    RawImage Portrait(Transform parent, Vector2 position, float size)
    {
        var go = new GameObject("Model", typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = Vector2.one * size;
        var image = go.GetComponent<RawImage>();
        image.texture = studio.Atlas;
        image.raycastTarget = false;
        return image;
    }

    void Add(Entry entry)
    {
        if (InRun)
        {
            if (!granting) StartCoroutine(GrantItem(entry, slot));
            return;
        }
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
        if (InRun) SyncInventory();
        foreach (var level in levelButtons)
            level.Value.color = level.Key == first ? ChosenCard : CardColor;
        if (pactButton != null) pactButton.color = forcePact ? ChosenCard : CardColor;
        if (modifierLabel != null) modifierLabel.text = !modifiers.HasValue ? "SEEDED MODS" : modifiers.Value == LevelModifier.None ? "NO MODS" :
            modifiers.Value == (LevelModifier.Elites | LevelModifier.Coins | LevelModifier.FastFire) ? "ALL MODS" : modifiers.Value.ToString().ToUpperInvariant();
        if (weatherLabel != null) weatherLabel.text = weather.HasValue ? DistrictConditions.Label(weather.Value) : "SEEDED EVENT";
        if (damageLabel != null) damageLabel.text = "DAMAGE ×" + damageSteps[damageIndex];
        if (routeLabel != null)
        {
            routeLabel.GetComponentInParent<Button>(true).gameObject.SetActive(first == LevelId.Void);
            routeLabel.text = "VOID FROM " + LevelRun.VoidOrigins[routeIndex].ToString().ToUpperInvariant();
        }
        for (int i = 0; i < 2; i++)
        {
            slots[i].card.color = i == slot ? ChosenCard : CardColor;
            bool baseGun = InRun ? HasBaseGun(i) : i == 0;
            Show(slots[i].weapon, slots[i].weaponName, weapons[i], baseGun ? "Base gun" : "", baseGun);
            Show(slots[i].active, slots[i].activeName, actives[i], "", false);
        }
        foreach (var entry in entries)
        {
            string badge = entry.count > 0 ? "x" + entry.count : "";
            if (weapons[0] == entry || actives[0] == entry)
                badge = "1";
            if (weapons[1] == entry || actives[1] == entry)
                badge = badge == "1" ? "1+2" : "2";
            entry.badge.text = badge;
            entry.card.color = badge.Length > 0 ? ChosenCard : CardColor;
        }
        if (InRun) FilterBrowser(false);
    }

    // An empty first weapon slot shows the ship's base gun.
    void Show(RawImage portrait, TextMeshProUGUI label, Entry entry, string fallback, bool baseGun)
    {
        int index = entry != null ? entry.portrait : baseGun ? entries.Count : -1;
        portrait.enabled = index >= 0;
        if (index >= 0)
            portrait.uvRect = studio.Cell(index);
        label.text = entry?.name ?? fallback;
    }

    // Seeded sky, then each condition in turn; conditions outside their district fly ordinary.
    void CycleWeather()
    {
        int next = weather.HasValue ? (int)weather.Value + 1 : 0;
        int total = Enum.GetValues(typeof(DistrictCondition)).Length;
        while (next < total && !DistrictConditions.Allowed(first, (DistrictCondition)next)) next++;
        weather = next < total ? (DistrictCondition?)next : null;
        Refresh();
    }

    void CycleModifiers()
    {
        int index = Array.IndexOf(ModifierCycle, modifiers);
        modifiers = ModifierCycle[(index + 1) % ModifierCycle.Length];
        Refresh();
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
        debugRun = GameRules.RunSerial;
        GameRules.enemyDamageReceived *= damageSteps[damageIndex];
        LevelRun.Reset(first);
        if (first == LevelId.Void) LevelRun.PreviewVoidOrigin(LevelRun.VoidOrigins[routeIndex]);
        DistrictConditions.Forced = weather;
        LevelModifiers.Forced = modifiers;
        BGMManager.PlayLevelMusic(first);
        CloudTransition.Load("L1");
    }

    void Close()
    {
        if (granting) return;
        RememberBrowser();
        search = null;
        status = null;
        previousSelection = null;
        visibleEntries.Clear();
        Destroy(overlay);
        overlay = null;
        if (studio != null)
            Destroy(studio.gameObject);
        levelButtons.Clear();
        entries.Clear();
        buttons.Clear();
        Array.Clear(weapons, 0, 2);
        Array.Clear(actives, 0, 2);
        menu.Suspended = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        menu.setCurrentElement(this);
    }

    void Update()
    {
        if (overlay == null || starting)
            return;
        if (InRun && !menu.IsOpen) { Close(); return; }
        if (BrowserHandlesBack()) return;
        if (Time.frameCount > openedFrame && ((Keyboard.current?.escapeKey.wasPressedThisFrame ?? false) || (Gamepad.current?.buttonEast.wasPressedThisFrame ?? false)))
        {
            Close();
            return;
        }
        var selected = EventSystem.current.currentSelectedGameObject;
        if (InRun && !search.isFocused && ((Keyboard.current?.deleteKey.wasPressedThisFrame ?? false) ||
            (Gamepad.current?.buttonWest.wasPressedThisFrame ?? false)))
            selected?.GetComponent<DebugItemButton>()?.Remove?.Invoke();
        if (selected == previousSelection)
            return;
        previousSelection = selected;
        if (selected == null || !selected.transform.IsChildOf(content))
            return;
        // Keep keyboard/controller focus visible while navigating the catalogue.
        var rect = selected.GetComponent<RectTransform>();
        float y = -rect.anchoredPosition.y;
        float top = content.anchoredPosition.y;
        float half = CardHeight * .5f + 6;
        if (y - half < top)
            top = y - half;
        else if (y + half > top + ViewHeight)
            top = y + half - ViewHeight;
        content.anchoredPosition = new Vector2(0, Mathf.Clamp(top, 0, Mathf.Max(0, content.sizeDelta.y - ViewHeight)));
    }

    protected override void LateUpdate()
    {
        if (overlay == null)
            base.LateUpdate();
    }

    void OnDestroy()
    {
        RememberBrowser();
        if (overlay != null)
            Destroy(overlay);
        if (studio != null)
            Destroy(studio.gameObject);
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

    TextMeshProUGUI Label(Transform parent, string value, float size, Vector2 position, Vector2 dimensions)
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
        text.enableAutoSizing = true;
        text.fontSizeMin = size * .6f;
        text.fontSizeMax = size;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Ivory;
        text.raycastTarget = false;
        return text;
    }

    // A text size of 0 leaves the label out, for cards that lay out their own contents.
    Button ButtonAt(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, float textSize = 20, bool itemCard = false)
    {
        var rect = Box(parent, label, position, size, CardColor);
        Button button = itemCard ? rect.gameObject.AddComponent<DebugItemButton>() : rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        var colors = button.colors;
        // Focus lights the card's own colour, so chosen cards stay distinct while focused.
        colors.colorMultiplier = 2.4f;
        colors.normalColor = new Color(1 / 2.4f, 1 / 2.4f, 1 / 2.4f);
        colors.highlightedColor = colors.selectedColor = new Color(1, .82f, .5f);
        colors.pressedColor = new Color(.8f, .65f, .4f);
        button.colors = colors;
        if (textSize > 0)
            Label(rect, label, textSize, Vector2.zero, size);
        // The submit that opens DEBUG must not also activate its first button.
        button.onClick.AddListener(() => { if (Time.frameCount > openedFrame) action(); });
        buttons.Add(button);
        return button;
    }
}

public sealed class DebugPactOffer : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null; // The orchestrer has built its wave sequence.
        var orchestrer = FindAnyObjectByType<WaveOrchestrer>();
        if (orchestrer != null)
            orchestrer.ForcePactNext();
        Destroy(gameObject);
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

