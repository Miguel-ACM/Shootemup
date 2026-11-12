using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Brass-edged enamel instruments shared by the dock consoles and the flight receipt.
public sealed class DockInstrument
{
    public readonly RectTransform face;
    public readonly CanvasGroup group;
    readonly TMP_FontAsset font;
    internal static readonly Vector2 TubeSize = new Vector2(607, 557);
    public static readonly Color Ink = new Color(.035f, .073f, .09f), Sea = new Color(.50f, .88f, .83f),
        Paper = new Color(.90f, .94f, .89f), Brass = new Color(.98f, .74f, .38f), Muted = new Color(.59f, .70f, .72f);
    public DockInstrument(GameObject root, TMP_FontAsset typeface, Color accent)
    {
        font = typeface;
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(622, 574);
        group = root.AddComponent<CanvasGroup>();
        face = Block(root.transform, "Cast metal frame", Vector2.zero, new Vector2(622, 574), new Color(.18f, .26f, .27f));
        Block(face, "Recessed gasket", Vector2.zero, new Vector2(614, 565), new Color(.01f, .014f, .016f));
        var glass = new GameObject("Convex phosphor tube", typeof(RectTransform), typeof(CanvasRenderer), typeof(DockTubeGlass)).GetComponent<DockTubeGlass>();
        glass.transform.SetParent(face, false);
        glass.rectTransform.sizeDelta = TubeSize;
        glass.raycastTarget = false;
        Block(face, "Top rail", new Vector2(0, 229), new Vector2(560, 2), accent);
        Block(face, "Foot rail", new Vector2(0, -219), new Vector2(560, 2), new Color(.20f, .32f, .34f));
        for (int i = 0; i < 4; i++)
        {
            var pos = new Vector2(i % 2 == 0 ? -300 : 300, i < 2 ? 276 : -276);
            Block(face, "Rivet", pos, new Vector2(5, 5), Muted).localRotation = Quaternion.Euler(0, 0, 45);
        }
        for (int i = 0; i < 7; i++)
            Block(face, "Registration notch", new Vector2(203 + i * 11, 255), new Vector2(5, i == 6 ? 15 : 7), accent);
    }
    public static RectTransform Block(Transform parent, string name, Vector2 pos, Vector2 size, Color tint)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = tint;
        image.raycastTarget = false;
        var r = image.rectTransform;
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        return r;
    }
    public TextMeshProUGUI Label(string value, float x, float y, float w, float h, float size, Color tint, bool bold = false)
    {
        var go = new GameObject("Legend", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(face, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        if (font != null)
            t.font = font;
        t.spriteAsset = InputPrompts.InlineAsset;
        t.text = value;
        t.fontSize = size;
        t.enableAutoSizing = true;
        t.fontSizeMin = size * .74f;
        t.fontSizeMax = size;
        t.lineSpacing = 3;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.color = tint;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.raycastTarget = false;
        t.rectTransform.sizeDelta = new Vector2(w, h);
        t.rectTransform.anchoredPosition = new Vector2(x, y);
        return t;
    }
    public static bool UsingGamepad => InputPrompts.Device != PromptDevice.Keyboard;

    public static string FireKey(bool secondary = false)
    {
        var actions = GameInput.Actions.PlayerControls;
        var action = secondary ? actions.FireSecondary : actions.FirePrimary;
        return InputPrompts.Inline(action);
    }

    public static string MenuKey(bool back = false)
    {
        var controls = GameInput.Actions.PlayerControls;
        return InputPrompts.Inline(back ? controls.Back : controls.Accept);
    }
}
