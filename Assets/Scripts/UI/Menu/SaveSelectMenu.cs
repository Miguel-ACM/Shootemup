using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The title owns flight input while this page is open. Pointer and controller actions
// use the same targets, including the separate bin in each occupied card.
public sealed class SaveSelectMenu : MonoBehaviour
{
    static readonly Color Danger = new Color(.95f, .42f, .35f);
    readonly SaveSlotSummary[] slots = new SaveSlotSummary[MetaProgress.SlotCount];
    readonly Image[] cards = new Image[MetaProgress.SlotCount], bins = new Image[MetaProgress.SlotCount];
    Menu menu;
    NewGame entry;
    TMP_FontAsset font;
    CanvasGroup titleGroup;
    RectTransform page, dialog;
    TextMeshProUGUI status;
    Image back, cancel, erase;
    int selected, deleteSlot, openedFrame;
    bool binSelected, confirmErase, heldMove, closing;
    float age;
    CanvasGroup fade;

    public static void Show(NewGame entry, Menu menu, TMP_FontAsset font)
    {
        if (menu.Suspended) return;
        var selector = new GameObject("Save files", typeof(RectTransform)).AddComponent<SaveSelectMenu>();
        selector.entry = entry;
        selector.menu = menu;
        selector.font = font;
        selector.selected = MetaProgress.CurrentSlot - 1;
        menu.Suspended = true;
        selector.titleGroup = menu.GetComponent<CanvasGroup>();
        selector.titleGroup.alpha = 0;
        selector.titleGroup.blocksRaycasts = false;
        selector.Build();
    }

    void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(900, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        gameObject.AddComponent<GraphicRaycaster>();
        fade = gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 0;
        var shade = Block(transform, "Shade", Vector2.zero, Vector2.zero, new Color(.015f, .025f, .03f, .92f));
        shade.anchorMin = Vector2.zero;
        shade.anchorMax = Vector2.one;
        shade.offsetMin = shade.offsetMax = Vector2.zero;
        shade.GetComponent<Image>().raycastTarget = true;
        page = new GameObject("Save list", typeof(RectTransform)).GetComponent<RectTransform>();
        page.SetParent(transform, false);
        page.sizeDelta = new Vector2(720, 720);
        Label(page, "PLAY", new Vector2(0, 313), new Vector2(500, 40), 30, DockInstrument.Paper);
        status = Label(page, "", new Vector2(0, -277), new Vector2(680, 24), 12, Danger);
        back = Target(page, "Back", new Vector2(0, -320), new Vector2(220, 44), 3, false);
        Label(back.transform, "BACK", Vector2.zero, new Vector2(210, 34), 17, DockInstrument.Paper);
        RefreshCards();
        openedFrame = Time.frameCount;
        heldMove = true;
    }

    void RefreshCards()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (cards[i] != null)
            {
                cards[i].gameObject.SetActive(false);
                Destroy(cards[i].gameObject);
            }
            slots[i] = MetaProgress.InspectSlot(i + 1);
            var save = slots[i];
            cards[i] = Target(page, "File " + save.Slot, new Vector2(0, 180 - i * 180), new Vector2(680, 162), i, false);
            var face = Block(cards[i].transform, "Face", Vector2.zero, new Vector2(674, 156), DockInstrument.Ink);
            Label(face, "FILE " + save.Slot, new Vector2(-205, 53), new Vector2(220, 30), 21, DockInstrument.Paper);
            bins[i] = null;
            if (save.Exists)
            {
                bins[i] = Target(cards[i].transform, "Delete File " + save.Slot, new Vector2(306, 52), new Vector2(44, 44), i, true);
                DrawBin(bins[i].transform);
            }
            if (!save.Readable)
            {
                Label(face, save.Exists ? "UNREADABLE SAVE" : "NEW SAVE", new Vector2(0, -8),
                    new Vector2(580, 36), 22, save.Exists ? Danger : DockInstrument.Muted);
                continue;
            }
            Label(face, save.Completion + "%", new Vector2(211, 51), new Vector2(130, 34), 25, DockInstrument.Brass);
            var track = Block(face, "Completion", new Vector2(-13, 23), new Vector2(604, 4), new Color(.16f, .23f, .25f));
            var fill = Block(track, "Progress", Vector2.zero, new Vector2(604 * save.Completion / 100f, 4), DockInstrument.Brass);
            fill.anchorMin = fill.anchorMax = fill.pivot = new Vector2(0, .5f);
            Label(face, save.Ship + "  /  " + save.Difficulty.ToUpperInvariant(), new Vector2(-140, 0),
                new Vector2(350, 22), 14, DockInstrument.Paper);
            Label(face, save.Coins + " COINS", new Vector2(197, 0), new Vector2(230, 22), 14, DockInstrument.Brass);
            Label(face, "AREAS " + save.Districts + "/12   ITEMS " + save.Items + "/" + save.TotalItems +
                "   HULLS " + save.Hulls + "/" + ShipFrames.Count + "   SIM " + save.Trials + "/" + FlightTrials.All.Length,
                new Vector2(0, -27), new Vector2(630, 22), 11, DockInstrument.Muted);
            Label(face, save.Flights + " FLIGHTS   " + save.Victories + " WINS   BEST " + save.Best + "   " + save.PlayTime,
                new Vector2(0, -54), new Vector2(630, 22), 11, DockInstrument.Muted);
        }
        PaintFocus();
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        fade.alpha = Mathf.SmoothStep(0, 1, age / .18f);
        if (closing || Time.frameCount <= openedFrame || CloudTransition.Busy) return;
        var controls = GameInput.Actions.PlayerControls;
        var move = controls.Move.ReadValue<Vector2>();
        bool moving = move.sqrMagnitude > .3f;
        if (!moving) heldMove = false;
        else if (!heldMove)
        {
            heldMove = true;
            if (dialog != null)
                confirmErase = Mathf.Abs(move.x) > Mathf.Abs(move.y) ? move.x > 0 : move.y < 0;
            else if (Mathf.Abs(move.y) >= Mathf.Abs(move.x))
            {
                selected = (selected + (move.y > 0 ? 3 : 1)) % 4;
                binSelected = false;
            }
            else binSelected = move.x > 0 && selected < 3 && slots[selected].Exists;
            PaintFocus();
        }
        if (controls.Back.WasPressedThisFrame())
        {
            if (dialog != null) CancelDelete();
            else Close();
        }
        else if (controls.Accept.WasPressedThisFrame())
        {
            if (dialog != null) ChooseDelete(confirmErase);
            else Activate(selected, binSelected);
        }
    }

    public void FocusTarget(int index, bool bin)
    {
        if (closing) return;
        if (dialog != null)
        {
            if (index < 4) return;
            confirmErase = index == 5;
        }
        else
        {
            selected = index;
            binSelected = bin;
        }
        PaintFocus();
    }

    public void Activate(int index, bool bin)
    {
        if (closing || Time.frameCount <= openedFrame) return;
        if (dialog != null)
        {
            if (index >= 4) ChooseDelete(index == 5);
            return;
        }
        if (index < 0 || index > 3) return;
        if (index == 3) { Close(); return; }
        selected = index;
        binSelected = bin;
        if (bin) OpenDelete(index + 1);
        else if (MetaProgress.SelectSlot(index + 1))
        {
            entry.PlaySelectedSlot();
        }
        else status.text = "COULD NOT OPEN FILE " + (index + 1);
    }

    void OpenDelete(int slot)
    {
        if (!slots[slot - 1].Exists) return;
        deleteSlot = slot;
        confirmErase = false;
        page.gameObject.SetActive(false);
        dialog = Block(transform, "Delete confirmation", Vector2.zero, new Vector2(620, 252), Danger);
        var face = Block(dialog, "Face", Vector2.zero, new Vector2(614, 246), DockInstrument.Ink);
        Label(face, "DELETE FILE " + slot + "?", new Vector2(0, 76), new Vector2(570, 40), 25, DockInstrument.Paper);
        Label(face, "All progress in this file will be erased.", new Vector2(0, 18), new Vector2(560, 44), 15, DockInstrument.Muted);
        cancel = Target(face, "Cancel", new Vector2(-145, -69), new Vector2(250, 46), 4, false);
        erase = Target(face, "Delete", new Vector2(145, -69), new Vector2(250, 46), 5, false);
        Label(cancel.transform, "CANCEL", Vector2.zero, new Vector2(240, 36), 17, DockInstrument.Paper);
        Label(erase.transform, "DELETE", Vector2.zero, new Vector2(240, 36), 17, DockInstrument.Paper);
        openedFrame = Time.frameCount;
        PaintFocus();
    }

    void ChooseDelete(bool confirmed)
    {
        if (Time.frameCount <= openedFrame) return;
        if (confirmed)
        {
            bool deleted = MetaProgress.DeleteSlot(deleteSlot);
            status.text = deleted ? "" : "COULD NOT DELETE FILE " + deleteSlot;
        }
        CancelDelete();
        RefreshCards();
    }

    void CancelDelete()
    {
        dialog.gameObject.SetActive(false);
        Destroy(dialog.gameObject);
        dialog = null;
        page.gameObject.SetActive(true);
        binSelected = false;
        openedFrame = Time.frameCount;
        PaintFocus();
    }

    void PaintFocus()
    {
        if (dialog != null)
        {
            cancel.color = confirmErase ? DockInstrument.Ink : DockInstrument.Brass * .65f;
            erase.color = confirmErase ? Danger * .65f : DockInstrument.Ink;
            return;
        }
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].color = selected == i && !binSelected ? DockInstrument.Brass : new Color(.18f, .26f, .27f);
            if (bins[i] != null) bins[i].color = selected == i && binSelected ? Danger * .65f : DockInstrument.Ink;
        }
        back.color = selected == 3 ? DockInstrument.Brass * .65f : DockInstrument.Ink;
    }

    void Close()
    {
        closing = true;
        menu.Suspended = false;
        titleGroup.alpha = 1;
        titleGroup.blocksRaycasts = true;
        menu.setCurrentElement(entry);
        Destroy(gameObject);
    }

    public void DismissForDeparture()
    {
        closing = true;
        titleGroup.alpha = 1;
        Destroy(gameObject);
    }

    Image Target(Transform parent, string name, Vector2 at, Vector2 size, int index, bool bin)
    {
        var rect = Block(parent, name, at, size, DockInstrument.Ink);
        var target = rect.gameObject.AddComponent<SaveSlotButton>();
        target.Owner = this;
        target.Index = index;
        target.Bin = bin;
        var image = rect.GetComponent<Image>();
        image.raycastTarget = true;
        return image;
    }

    TextMeshProUGUI Label(Transform parent, string text, Vector2 at, Vector2 size, float points, Color color)
        => UnlockReceipt.Label(parent, text, at, size, points, color, font);

    static RectTransform Block(Transform parent, string name, Vector2 at, Vector2 size, Color color)
        => DockInstrument.Block(parent, name, at, size, color);

    static void DrawBin(Transform parent)
    {
        Block(parent, "Handle", new Vector2(0, 13), new Vector2(10, 3), DockInstrument.Paper);
        Block(parent, "Lid", new Vector2(0, 9), new Vector2(24, 3), DockInstrument.Paper);
        Block(parent, "Bin", new Vector2(0, -3), new Vector2(18, 19), DockInstrument.Paper);
        Block(parent, "Inset", new Vector2(0, -2), new Vector2(12, 15), DockInstrument.Ink);
        Block(parent, "Left slat", new Vector2(-3, -2), new Vector2(2, 11), DockInstrument.Paper);
        Block(parent, "Right slat", new Vector2(3, -2), new Vector2(2, 11), DockInstrument.Paper);
    }
}

public sealed class SaveSlotButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public SaveSelectMenu Owner;
    public int Index;
    public bool Bin;
    public void OnPointerEnter(PointerEventData eventData) => Owner.FocusTarget(Index, Bin);
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) Owner.Activate(Index, Bin);
    }
}
