using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One format for the title and pause menus: the options follow the first row, and every
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
        var kinds = new[] { OptionRow.Kind.Music, OptionRow.Kind.Sound, OptionRow.Kind.PsxFilter };
        for (int i = 0; i < kinds.Length; i++)
            OptionRow.Create(first.parent, kinds[i], font).transform.SetSiblingIndex(first.GetSiblingIndex() + 1 + i);
        Arrange(menu, title ? -60 : 0);
    }

    static void Arrange(Menu menu, float centre)
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
