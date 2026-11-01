using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A menu row that changes a saved option: left/right adjusts it, accept or a click steps it.</summary>
public sealed class OptionRow : AMenuElement
{
    public enum Kind
    {
        Music,
        Sound,
        PsxFilter
    }

    const float RepeatDelay = .35f, RepeatRate = .1f;
    static readonly Color Lit = new Color(.86f, .9f, .82f), Unlit = new Color(.22f, .27f, .27f);
    static readonly Color FocusLit = new Color(.08f, .1f, .1f), FocusUnlit = new Color(.5f, .41f, .24f);
    Kind kind;
    Image[] cells;
    TextMeshProUGUI state;
    int held;
    float nextRepeat;

    public static OptionRow Create(Transform parent, Kind kind, TMP_FontAsset font)
    {
        var go = new GameObject(kind == Kind.PsxFilter ? "PSX FILTER" : kind.ToString().ToUpperInvariant(), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var label = Text(go.transform, go.name, font, TextAlignmentOptions.Left);
        label.rectTransform.offsetMin = new Vector2(40, 0);
        var row = go.AddComponent<OptionRow>();
        row.kind = kind;
        if (kind == Kind.PsxFilter)
        {
            row.state = Text(go.transform, "", font, TextAlignmentOptions.Right);
            row.state.rectTransform.offsetMax = new Vector2(-40, 0);
        }
        else
        {
            // One block per volume step, like the HUD's meter cells.
            row.cells = new Image[GameOptions.Steps];
            for (int i = 0; i < row.cells.Length; i++)
            {
                var cell = new GameObject("Step " + (i + 1), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                cell.transform.SetParent(go.transform, false);
                var rect = (RectTransform)cell.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(1, .5f);
                rect.pivot = new Vector2(1, .5f);
                rect.sizeDelta = new Vector2(18, 16 + i * 2.4f);
                rect.anchoredPosition = new Vector2(-40 - (GameOptions.Steps - 1 - i) * 25, 0);
                row.cells[i] = cell.GetComponent<Image>();
                row.cells[i].raycastTarget = false;
            }
        }
        return row;
    }

    static TextMeshProUGUI Text(Transform parent, string value, TMP_FontAsset font, TextAlignmentOptions alignment)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = 36;
        text.characterSpacing = 6;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        var rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return text;
    }

    public override void OnAccept()
    {
        Step(1, true);
    }

    void Update()
    {
        if (menu == null || !menu.IsOpen || menu.Suspended || menu.CurrentElement != this)
        {
            held = 0;
            return;
        }
        float x = GameInput.Actions.PlayerControls.Move.ReadValue<Vector2>().x;
        int direction = x > .5f ? 1 : x < -.5f ? -1 : 0;
        if (direction == 0)
            held = 0;
        else if (direction != held)
        {
            held = direction;
            nextRepeat = Time.unscaledTime + RepeatDelay;
            Step(direction, false);
        }
        else if (kind != Kind.PsxFilter && Time.unscaledTime >= nextRepeat)
        {
            nextRepeat = Time.unscaledTime + RepeatRate;
            Step(direction, false);
        }
    }

    // Accept wraps a full volume back to silence; left/right stop at the ends.
    void Step(int direction, bool wrap)
    {
        switch (kind)
        {
            case Kind.Music:
                GameOptions.SetMusic(Next(GameOptions.Music, direction, wrap));
                break;
            case Kind.Sound:
                GameOptions.SetSound(Next(GameOptions.Sound, direction, wrap));
                break;
            default:
                GameOptions.SetPsxFilter(!GameOptions.PsxFilter);
                break;
        }
        // Sound uses the new level, so the blip previews it.
        SoundManager.PlaySound("uiConfirm", .1f);
    }

    static int Next(int value, int direction, bool wrap)
    {
        int next = value + direction;
        if (wrap)
            return next > GameOptions.Steps ? 0 : next;
        return Mathf.Clamp(next, 0, GameOptions.Steps);
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        float focus = Focus;
        if (state != null)
        {
            state.text = GameOptions.PsxFilter ? "ON" : "OFF";
            state.color = textElement.color;
            return;
        }
        int level = kind == Kind.Music ? GameOptions.Music : GameOptions.Sound;
        for (int i = 0; i < cells.Length; i++)
            cells[i].color = i < level ? Color.Lerp(Lit, FocusLit, focus) : Color.Lerp(Unlit, FocusUnlit, focus);
    }
}
