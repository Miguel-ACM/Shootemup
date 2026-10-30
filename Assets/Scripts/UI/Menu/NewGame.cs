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
        // This row is hidden while the difficulty rows show, so the menu, which stays active, runs the exit.
        menu.StartCoroutine(IntoHangar(option == 0 ? RunDifficulty.Easy : RunDifficulty.Hard));
    }

    // The menu fades away while the title's tunnel slows to a stop; the hangar then continues from that same view.
    System.Collections.IEnumerator IntoHangar(RunDifficulty difficulty)
    {
        const float Seconds = 1.1f;
        menu.Suspended = true;
        var group = menu.GetComponent<CanvasGroup>();
        var panel = menu.transform.parent.GetComponent<Image>();
        var shade = panel != null ? panel.color : Color.clear;
        var halt = StartCoroutine(LevelEnvironment.HaltTitle(Seconds));
        for (float t = 0; t < Seconds; t += Time.unscaledDeltaTime)
        {
            float fade = Mathf.Clamp01(t / .4f);
            if (group != null)
                group.alpha = 1 - fade;
            if (panel != null)
                panel.color = new Color(shade.r, shade.g, shade.b, shade.a * (1 - Mathf.Clamp01(t / Seconds)));
            yield return null;
        }
        yield return halt;
        // A short jolt as the tunnel locks in place, then the hangar takes over the very same view.
        var camera = UnityEngine.Camera.main;
        if (camera != null)
        {
            var rest = camera.transform.localPosition;
            for (float t = 0; t < .35f; t += Time.unscaledDeltaTime)
            {
                float strength = .14f * (1 - t / .35f);
                camera.transform.localPosition = rest + (Vector3)(Random.insideUnitCircle * strength);
                yield return null;
            }
            camera.transform.localPosition = rest;
        }
        LevelEnvironment.CarryTitle();
        HangarHub.Enter(difficulty);
    }
}

