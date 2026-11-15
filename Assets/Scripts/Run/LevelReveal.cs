using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LevelReveal : MonoBehaviour
{
    TMP_FontAsset font;
    RectTransform content;

    public static void Show()
    {
        if (GameOverState.Active)
            return;
        new GameObject("Level reveal title").AddComponent<LevelReveal>();
    }

    IEnumerator Start()
    {
        // Reuse the font already assigned to the game's item cards and menus.
        var dialog = FindAnyObjectByType<Dialog>();
        font = dialog != null ? dialog.font : null;
        if (font == null)
            foreach (var label in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
                if (label.font != null)
                {
                    font = label.font;
                    break;
                }

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Gameplay overlays sit below the pause menu's canvas at order 20.
        canvas.sortingOrder = 19;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        var group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        var body = new GameObject("Title and eyebrow", typeof(RectTransform));
        body.transform.SetParent(transform, false);
        content = body.GetComponent<RectTransform>();
        content.sizeDelta = new Vector2(405, 120);
        Label("Level " + LevelRun.Stage, 11, new Vector2(0, 40), new Color(.73f, .77f, .72f));
        Label(LevelRun.CurrentName.ToUpperInvariant(), 40, new Vector2(0, -3), new Color(.9f, .92f, .87f));
        // This run's district event and level modifiers sit in a row under the name, each its
        // icon over its name.
        var badges = new System.Collections.Generic.List<(Sprite icon, string name)>();
        var condition = DistrictConditions.Current;
        if (condition != DistrictCondition.Ordinary)
            badges.Add((DistrictConditions.Icon(condition), DistrictConditions.Label(condition)));
        var modifiers = LevelModifiers.Current;
        foreach (var modifier in LevelModifiers.All)
            if ((modifiers & modifier) != 0)
                badges.Add((LevelModifiers.Icon(modifier), LevelModifiers.Label(modifier)));
        float spacing = badges.Count > 0 ? Mathf.Min(132, 400f / badges.Count) : 0;
        for (int i = 0; i < badges.Count; i++)
        {
            float x = (i - (badges.Count - 1) * .5f) * spacing;
            var icon = new GameObject(badges[i].name + " icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(content, false);
            var rect = icon.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(32, 32);
            rect.anchoredPosition = new Vector2(x, -46);
            var image = icon.GetComponent<Image>();
            image.sprite = badges[i].icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var label = Label(badges[i].name, 13, new Vector2(x, -74), new Color(.86f, .8f, .64f));
            label.rectTransform.sizeDelta = new Vector2(spacing - 6, 24);
            label.enableAutoSizing = true;
            label.fontSizeMin = 8;
            label.fontSizeMax = 13;
        }
        float duration = CloudTransition.RevealSeconds + 1.3f;
        var instruments = FindAnyObjectByType<IndustrialHUD>();
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float enter = Mathf.SmoothStep(0, 1, t / .7f);
            float leave = Mathf.SmoothStep(0, 1, (t - (duration - 1.1f)) / 1.1f);
            float power = instruments != null ? FluorescentStartup.Sample(instruments.EntranceAge, .1f) : 1;
            group.alpha = enter * (1 - leave) * power;
            content.anchoredPosition = new Vector2(0, Mathf.Round(Mathf.Lerp(-12, 0, enter) + leave * 12));
            yield return null;
        }
        Destroy(gameObject);
    }

    TextMeshProUGUI Label(string text, float size, Vector2 position, Color color)
    {
        var go = new GameObject(text, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(content, false);
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta = new Vector2(390, 60);
        r.anchoredPosition = position;
        var label = go.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }
}
