using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One format for the title and pause menus: SETTINGS follows the first row, and every
/// row shares one size, spacing and type style.</summary>
public static class MenuLayout
{
    const float RowWidth = 680, RowHeight = 78, Spacing = 96;

    public static void Dress(Menu menu, bool title)
    {
        var frame = menu.GetComponent<Image>();
        if (frame != null)
            frame.enabled = false;
        var rows = menu.GetComponentsInChildren<AMenuElement>(true);
        if (rows.Length == 0)
            return;
        var first = rows[0].transform;
        var font = first.GetComponentInChildren<TextMeshProUGUI>(true).font;
        float centre = title ? -60 : 0;
        SettingsEntry.Create(first.parent, font, centre).transform.SetSiblingIndex(first.GetSiblingIndex() + 1);
        Arrange(menu, centre);
    }

    // A plain row in the menu's style; the caller adds its menu element.
    public static RectTransform Row(Transform parent, string label, TMP_FontAsset font)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var text = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(go.transform, false);
        text.font = font;
        text.text = label;
        text.raycastTarget = false;
        return (RectTransform)go.transform;
    }

    public static void Arrange(Menu menu, float centre)
    {
        var rows = menu.GetComponentsInChildren<AMenuElement>();
        for (int i = 0; i < rows.Length; i++)
        {
            var rect = (RectTransform)rows[i].transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(RowWidth, RowHeight);
            rect.anchoredPosition = new Vector2(0, centre + ((rows.Length - 1) * .5f - i) * Spacing);
            if (rows[i] is OptionRow)
                continue;
            var text = rows[i].GetComponentInChildren<TextMeshProUGUI>();
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            text.text = text.text.Trim().ToUpperInvariant();
            text.margin = Vector4.zero;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = false;
            text.fontSize = 36;
            text.characterSpacing = 6;
            text.alignment = TextAlignmentOptions.Center;
        }
    }
}
