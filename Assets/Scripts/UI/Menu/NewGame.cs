using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NewGame : AMenuElement
{
    bool starting;
    AMenuElement[] original;
    DifficultyOption[] options;
    public override void OnAccept()
    {
        if (starting || options != null)
            return;
        original = menu.GetComponentsInChildren<AMenuElement>();
        options = new DifficultyOption[3];
        string[] labels = { "EASY", "HARD", "BACK" };
        for (int i = 0; i < options.Length; i++)
        {
            var go = new GameObject(labels[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(transform.parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rectTransform.anchorMin;
            rect.anchorMax = rectTransform.anchorMax;
            rect.pivot = rectTransform.pivot;
            rect.sizeDelta = rectTransform.sizeDelta;
            rect.anchoredPosition = rectTransform.anchoredPosition + Vector2.down * i * (rectTransform.rect.height + 10);
            var layout = go.GetComponent<LayoutElement>();
            layout.preferredHeight = rectTransform.rect.height;
            layout.preferredWidth = rectTransform.rect.width;
            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(go.transform, false);
            var text = label.GetComponent<TextMeshProUGUI>();
            text.font = textElement.font;
            text.fontSize = Mathf.Max(36, textElement.fontSize);
            text.text = labels[i];
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            var textRect = (RectTransform)label.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var option = go.AddComponent<DifficultyOption>();
            option.Configure(this, i);
            options[i] = option;
        }
        foreach (var element in original)
            element.gameObject.SetActive(false);
        menu.RefreshElements();
        menu.setCurrentElement(options[0]);
    }

    public void Choose(int option)
    {
        if (starting)
            return;
        if (option == 2)
        {
            foreach (var element in original)
                element.gameObject.SetActive(true);
            foreach (var entry in options)
            {
                entry.gameObject.SetActive(false);
                Destroy(entry.gameObject);
            }
            options = null;
            menu.RefreshElements();
            menu.setCurrentElement(this);
            return;
        }
        starting = true;
        HangarHub.Enter(option == 0 ? RunDifficulty.Easy : RunDifficulty.Hard);
    }
}

