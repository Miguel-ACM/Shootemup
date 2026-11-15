using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        var blackout = CreateBlackout();
        var menu = FindAnyObjectByType<NewGame>()?.GetComponentInParent<Menu>();
        CanvasGroup rows = null;
        if (menu != null)
        {
            menu.Suspended = true;
            rows = PrepareMenuRows(menu);
            var panel = menu.transform.parent.GetComponent<Image>();
            if (panel != null)
                panel.color = new Color(.025f, .04f, .045f, .38f);
        }

        // The title shares the gameplay scenes' PS1 dithering and colour depth.
        var camera = UnityEngine.Camera.main;
        if (camera != null)
        {
            var presentation = camera.GetComponent<PSXPresentation>();
            if (presentation == null)
                presentation = camera.gameObject.AddComponent<PSXPresentation>();
            presentation.Configure(Resources.Load<Material>("PSXScreenComposite"));
        }
        yield return LevelEnvironment.CreateTitleBackdrop();
        while (CloudTransition.Busy)
            yield return null;
        yield return LevelEnvironment.LightTitleTunnel(() => Destroy(blackout));

        if (menu == null)
            yield break;
        var title = menu.GetComponent<SkimfireTitle>();
        if (title != null)
        {
            title.Reveal();
            while (!title.EntranceComplete)
                yield return null;
        }
        yield return RevealMenu(rows);
        menu.Suspended = false;
    }

    GameObject CreateBlackout()
    {
        var cover = new GameObject("Title blackout", typeof(RectTransform), typeof(Canvas));
        cover.transform.SetParent(transform, false);
        var canvas = cover.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32750; // Below the returning cloud curtain, above the title and scene.
        var shade = new GameObject("Black", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        shade.rectTransform.SetParent(cover.transform, false);
        shade.rectTransform.anchorMin = Vector2.zero;
        shade.rectTransform.anchorMax = Vector2.one;
        shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
        shade.color = Color.black;
        shade.raycastTarget = false;
        return cover;
    }

    static readonly Vector2 MenuEntryOffset = Vector2.down * 28;

    static CanvasGroup PrepareMenuRows(Menu menu)
    {
        var group = new GameObject("Title menu rows", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<CanvasGroup>();
        var rect = (RectTransform)group.transform;
        rect.SetParent(menu.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        foreach (var row in menu.GetComponentsInChildren<AMenuElement>(true))
            row.transform.SetParent(rect, false);
        rect.anchoredPosition = MenuEntryOffset;
        group.alpha = 0;
        group.blocksRaycasts = false;
        return group;
    }

    static IEnumerator RevealMenu(CanvasGroup rows)
    {
        var rect = (RectTransform)rows.transform;
        const float Seconds = .38f;
        for (float age = 0; age < Seconds; age += Time.unscaledDeltaTime)
        {
            float reveal = Mathf.SmoothStep(0, 1, age / Seconds);
            rows.alpha = reveal;
            rect.anchoredPosition = MenuEntryOffset * (1 - reveal);
            yield return null;
        }
        rows.alpha = 1;
        rect.anchoredPosition = Vector2.zero;
        rows.blocksRaycasts = true;
    }
}
