using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public sealed class TitleTunnelBackdrop : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
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
            new GameObject("Title scenery").AddComponent<TitleTunnelBackdrop>();
    }

    IEnumerator Start()
    {
        var menu = FindAnyObjectByType<NewGame>()?.GetComponentInParent<Menu>();
        if (menu != null)
        {
            var panel = menu.transform.parent.GetComponent<Image>();
            if (panel != null)
                panel.color = new Color(.025f, .04f, .045f, .38f);
            Arrange(menu);
        }
        yield return LevelEnvironment.CreateTitleBackdrop();
    }

    // Scene rows and the rows added by the dev tools share one size, spacing and type style.
    static void Arrange(Menu menu)
    {
        var frame = menu.GetComponent<Image>();
        if (frame != null)
            frame.enabled = false;
        var rows = menu.GetComponentsInChildren<AMenuElement>();
        for (int i = 0; i < rows.Length; i++)
        {
            var rect = (RectTransform)rows[i].transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(620, 78);
            rect.anchoredPosition = new Vector2(0, -60 + ((rows.Length - 1) * .5f - i) * 96);
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
