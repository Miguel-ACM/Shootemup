using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class DebugRunMenu
{
    public bool InRun { get; set; }
    TMP_InputField search;
    TMP_Dropdown typeFilter, rarityFilter, collectedFilter, ordering;
    TextMeshProUGUI status, resultCount;
    readonly List<Entry> visibleEntries = new List<Entry>();
    bool granting;
    const string BrowserControls = "Left click +    Right click −    Enter / Cross +    Delete / Square −";
    bool popupWasOpen;
    int browserRun;
    sealed class BrowserMemory
    {
        public int run, type, rarity, collection, order, slot;
        public float scroll;
        public string query, focus;
    }
    static BrowserMemory browserMemory;

    void RememberBrowser()
    {
        if (!InRun || search == null || content == null) return;
        browserMemory = new BrowserMemory { run = browserRun, query = search.text,
            type = typeFilter.value, rarity = rarityFilter.value, collection = collectedFilter.value,
            order = ordering.value, slot = slot, scroll = content.anchoredPosition.y,
            focus = EventSystem.current?.currentSelectedGameObject?.name };
    }

    void RestoreBrowser()
    {
        popupWasOpen = false;
        browserRun = GameRules.RunSerial;
        var memory = browserMemory != null && browserMemory.run == GameRules.RunSerial ? browserMemory : null;
        slot = memory?.slot ?? 0;
        search.SetTextWithoutNotify(memory?.query ?? "");
        typeFilter.SetValueWithoutNotify(memory?.type ?? 0);
        rarityFilter.SetValueWithoutNotify(memory?.rarity ?? 0);
        collectedFilter.SetValueWithoutNotify(memory?.collection ?? 0);
        ordering.SetValueWithoutNotify(memory?.order ?? 0);
        Refresh();
        var focus = overlay.GetComponentsInChildren<Selectable>().FirstOrDefault(control => control.name == memory?.focus);
        previousSelection = focus != null ? focus.gameObject : buttons[0].gameObject;
        EventSystem.current.SetSelectedGameObject(previousSelection);
        content.anchoredPosition = new Vector2(0, Mathf.Clamp(memory?.scroll ?? 0, 0, Mathf.Max(0, content.sizeDelta.y - ViewHeight)));
    }

    bool BrowserHandlesBack()
    {
        if (!InRun) return false;
        bool popupOpen = ordering.IsExpanded || typeFilter.IsExpanded || rarityFilter.IsExpanded || collectedFilter.IsExpanded;
        bool back = (UnityEngine.InputSystem.Keyboard.current?.escapeKey.wasPressedThisFrame ?? false) ||
            (UnityEngine.InputSystem.Gamepad.current?.buttonEast.wasPressedThisFrame ?? false);
        bool consumed = back && (popupWasOpen || popupOpen);
        if (consumed)
        {
            if (ordering.IsExpanded) ordering.Hide();
            if (typeFilter.IsExpanded) typeFilter.Hide();
            if (rarityFilter.IsExpanded) rarityFilter.Hide();
            if (collectedFilter.IsExpanded) collectedFilter.Hide();
        }
        popupWasOpen = !consumed && popupOpen;
        return consumed;
    }

    static readonly string[] TypeNames = { "ALL TYPES", "PASSIVE", "WEAPON", "ACTIVE", "COMPANION", "SECONDARY" };
    static readonly AItem.ItemType[] Types = { AItem.ItemType.passive, AItem.ItemType.weapon,
        AItem.ItemType.active, AItem.ItemType.partner, AItem.ItemType.passiveWeapon };

    void BuildBrowser(Transform panel)
    {
        var field = Box(panel, "Search items", new Vector2(-130, 156), new Vector2(720, 38), CardColor);
        search = field.gameObject.AddComponent<TMP_InputField>();
        var area = Box(field, "Text area", Vector2.zero, new Vector2(692, 34), Color.clear);
        area.gameObject.AddComponent<RectMask2D>();
        area.GetComponent<Image>().raycastTarget = false;
        var text = Label(area, "", 18, Vector2.zero, new Vector2(686, 34));
        text.alignment = TextAlignmentOptions.MidlineLeft;
        var placeholder = Label(area, "Search name or effect...", 18, Vector2.zero, new Vector2(686, 34));
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        placeholder.color = new Color(.48f, .58f, .56f);
        search.textViewport = area;
        search.textComponent = text;
        search.placeholder = placeholder;
        search.characterLimit = 100;
        search.richText = false;
        search.lineType = TMP_InputField.LineType.SingleLine;
        search.onValueChanged.AddListener(_ => FilterBrowser(true));
        ordering = Dropdown(panel, "Order", new Vector2(370, 156), 240,
            new[] { "NAME A-Z", "NAME Z-A", "RARITY", "TYPE" });
        typeFilter = Dropdown(panel, "Type", new Vector2(-380, 108), 220, TypeNames);
        rarityFilter = Dropdown(panel, "Rarity", new Vector2(-150, 108), 220,
            new[] { "ALL RARITIES", "COMMON", "RARE", "EPIC", "LEGENDARY", "PACT", "SPECIAL" });
        collectedFilter = Dropdown(panel, "Collection", new Vector2(80, 108), 220,
            new[] { "ALL ITEMS", "COLLECTED", "NOT COLLECTED" });
        ButtonAt(panel, "RESET FILTERS", new Vector2(285, 108), new Vector2(170, 36), ResetFilters, 15);
        resultCount = Label(panel, "", 15, new Vector2(435, 108), new Vector2(110, 36));
        status = Label(panel, BrowserControls, 16,
            new Vector2(0, -288), new Vector2(980, 44));
        status.textWrappingMode = TextWrappingModes.Normal;
        foreach (var entry in entries)
        {
            var hint = entry.card.gameObject.AddComponent<DebugItemHint>();
            hint.Show = () =>
            {
                if (!granting && status != null)
                    status.text = entry.name + " / " + entry.rarity + " — " + entry.description;
            };
        }
    }

    TMP_Dropdown Dropdown(Transform panel, string name, Vector2 position, float width, string[] options)
    {
        var root = Box(panel, name, position, new Vector2(width, 36), CardColor);
        var dropdown = root.gameObject.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = root.GetComponent<Image>();
        dropdown.captionText = Label(root, "", 16, new Vector2(-10, 0), new Vector2(width - 32, 32));
        Label(root, "v", 16, new Vector2(width * .5f - 16, 0), new Vector2(20, 30));
        // TMP clones this template into its own blocking popup, with built-in pad navigation.
        var template = Box(root, "Template", Vector2.zero, new Vector2(width, options.Length * 34 + 8), Ink);
        template.anchorMin = template.anchorMax = new Vector2(.5f, 0);
        template.pivot = new Vector2(.5f, 1);
        var viewport = Box(template, "Viewport", Vector2.zero, Vector2.zero, Color.clear);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(4, 4);
        viewport.offsetMax = new Vector2(-4, -4);
        viewport.gameObject.AddComponent<RectMask2D>();
        var list = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
        list.SetParent(viewport, false);
        list.anchorMin = new Vector2(0, 1);
        list.anchorMax = Vector2.one;
        list.pivot = new Vector2(.5f, 1);
        list.sizeDelta = new Vector2(0, 34);
        var row = Box(list, "Option", Vector2.zero, new Vector2(0, 34), CardColor);
        row.anchorMin = Vector2.zero;
        row.anchorMax = new Vector2(1, 0);
        row.pivot = new Vector2(.5f, 0);
        var toggle = row.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = row.GetComponent<Image>();
        var mark = Box(row, "Selected", new Vector2(-width * .5f + 12, 0), new Vector2(3, 24), Gold);
        mark.GetComponent<Image>().raycastTarget = false;
        toggle.graphic = mark.GetComponent<Image>();
        dropdown.itemText = Label(row, "", 16, new Vector2(4, 0), new Vector2(width - 32, 32));
        var scroller = template.gameObject.AddComponent<ScrollRect>();
        scroller.content = list;
        scroller.viewport = viewport;
        scroller.horizontal = false;
        scroller.movementType = ScrollRect.MovementType.Clamped;
        dropdown.template = template;
        template.gameObject.SetActive(false);
        dropdown.AddOptions(options.ToList());
        dropdown.onValueChanged.AddListener(_ => FilterBrowser(true));
        return dropdown;
    }

    void ResetFilters()
    {
        search.SetTextWithoutNotify("");
        typeFilter.SetValueWithoutNotify(0);
        rarityFilter.SetValueWithoutNotify(0);
        collectedFilter.SetValueWithoutNotify(0);
        ordering.SetValueWithoutNotify(0);
        FilterBrowser(true);
    }

    void FilterBrowser(bool resetScroll)
    {
        if (search == null) return;
        var terms = search.text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        visibleEntries.Clear();
        foreach (var entry in entries)
        {
            bool collected = entry.count > 0 || weapons.Contains(entry) || actives.Contains(entry);
            bool match = (typeFilter.value == 0 || entry.kind == Types[typeFilter.value - 1]) &&
                (rarityFilter.value == 0 || (int)entry.rarity == rarityFilter.value - 1) &&
                (collectedFilter.value == 0 || collected == (collectedFilter.value == 1));
            string haystack = entry.name + " " + entry.description + " " + entry.type.Name;
            foreach (var term in terms)
                match &= haystack.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
            entry.card.gameObject.SetActive(match);
            if (match) visibleEntries.Add(entry);
        }
        visibleEntries.Sort((a, b) =>
        {
            int byName = string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
            if (ordering.value == 1) return -byName;
            int primary = ordering.value == 2 ? a.rarity.CompareTo(b.rarity) :
                ordering.value == 3 ? a.kind.CompareTo(b.kind) : 0;
            return primary != 0 ? primary : byName;
        });
        for (int i = 0; i < visibleEntries.Count; i++)
        {
            var rect = visibleEntries[i].card.rectTransform;
            rect.anchoredPosition = new Vector2((i % Columns - (Columns - 1) * .5f) * 160,
                -8 - CardHeight * .5f - i / Columns * CardStep);
            rect.SetSiblingIndex(i);
        }
        content.sizeDelta = new Vector2(0, Mathf.CeilToInt(visibleEntries.Count / (float)Columns) * CardStep + 8);
        scroll.StopMovement();
        content.anchoredPosition = new Vector2(0, resetScroll ? 0 : Mathf.Clamp(content.anchoredPosition.y, 0, Mathf.Max(0, content.sizeDelta.y - ViewHeight)));
        resultCount.text = visibleEntries.Count + " / " + entries.Count;
        if (visibleEntries.Count == 0) status.text = "No matching items";
        else if (status.text == "No matching items") status.text = BrowserControls;
        var selected = EventSystem.current?.currentSelectedGameObject;
        if (selected != null && !selected.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(search.gameObject);
    }

    void SyncInventory()
    {
        var ship = FindAnyObjectByType<Shipcontroller>();
        if (ship == null) return;
        var inventory = RunInventory.For(ship);
        var loadout = ship.GetComponent<ShipLoadout>();
        foreach (var entry in entries)
            entry.count = inventory.Entries.Count(item => item.key == entry.type.Name);
        for (int i = 0; i < 2; i++)
        {
            var gun = loadout.Weapons[i];
            var active = loadout.Actives[i];
            weapons[i] = gun == null ? null : entries.Find(e => e.kind == AItem.ItemType.weapon && e.weaponName == gun.GetType().Name);
            actives[i] = active == null ? null : entries.Find(e => e.type == active.GetType());
        }
    }

    bool HasBaseGun(int index)
    {
        var ship = FindAnyObjectByType<Shipcontroller>();
        return ship != null && ship.GetComponent<ShipLoadout>().Weapons[index] is BaseWeapon;
    }

    IEnumerator RemoveItem(Entry entry)
    {
        var ship = FindAnyObjectByType<Shipcontroller>();
        if (ship == null || !Menu.GameplayPaused) yield break;
        granting = true;
        try
        {
            bool removed = RunInventory.For(ship).DebugRemove(entry.type.Name, slot);
            // Let removed components finish OnDestroy before accepting another grant.
            yield return null;
            Refresh();
            status.text = (removed ? "REMOVED: " : "NOT COLLECTED: ") + entry.name;
        }
        finally { granting = false; }
    }

    IEnumerator GrantItem(Entry entry, int targetSlot)
    {
        var ship = FindAnyObjectByType<Shipcontroller>();
        if (ship == null || ship.health <= 0 || !Menu.GameplayPaused) yield break;
        var prefab = Resources.Load<GameObject>("Items/Item");
        if (prefab == null) { status.text = "Item prefab unavailable"; yield break; }
        granting = true;
        var go = Instantiate(prefab, new Vector3(100, 100, 1), Quaternion.identity);
        var item = (AItem)go.AddComponent(entry.type);
        if (ItemCatalog.TryGet(entry.type.Name, out var definition)) item.SetRarity(definition.RarityKey);
        // Start must initialize art/active state. A frame yield still advances while paused.
        yield return null;
        try
        {
            if (ship != null && item != null)
            {
                item.GrantTo(ship, targetSlot);
                status.text = "GIVEN: " + entry.name;
                Refresh();
            }
            else if (go != null) Destroy(go);
        }
        finally { granting = false; }
    }
}

// Keep normal Submit for keyboard/pad grants; only the item cards change mouse semantics.
public sealed class DebugItemButton : Button
{
    public Action Remove;
    public override void OnPointerClick(PointerEventData eventData)
    {
        if (!IsActive() || !IsInteractable()) return;
        if (eventData.button == PointerEventData.InputButton.Right) Remove?.Invoke();
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            Select();
            onClick.Invoke();
        }
    }
}

// Only hover/select handlers: drag and wheel events continue to the catalogue ScrollRect.
public sealed class DebugItemHint : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    public Action Show;
    public void OnPointerEnter(PointerEventData eventData) => Show?.Invoke();
    public void OnSelect(BaseEventData eventData) => Show?.Invoke();
}
