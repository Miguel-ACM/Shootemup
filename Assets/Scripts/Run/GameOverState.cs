using UnityEngine;
using UnityEngine.UI;
using TMPro;

// A short, unmistakable end to the run, followed by the existing title transition.
public sealed class GameOverState : MonoBehaviour
{
    static GameOverState instance;
    public static bool Active => instance != null;
    CanvasGroup group;
    RectTransform lettering;
    float age;
    bool leaving;
    public static void Show()
    {
        if (Active)
            return;
        instance = new GameObject("Game over", typeof(RectTransform)).AddComponent<GameOverState>();
        MetaProgress.EndRun();
        IndustrialHUD.ShutDown();
        CombatDecision.End();
        GameRules.playerDisabled = true;
        foreach (var wave in FindObjectsByType<WaveOrchestrer>())
        {
            wave.CancelInvoke();
            wave.StopAllCoroutines();
            wave.enabled = false;
        }
        foreach (var weapon in FindObjectsByType<AWeapon>())
            weapon.StopFiring();
        foreach (var reveal in FindObjectsByType<LevelReveal>())
            Destroy(reveal.gameObject);
        foreach (var choice in FindObjectsByType<LevelChoice>())
            Destroy(choice.gameObject);
        foreach (var shop in FindObjectsByType<DecisionShop>())
            shop.Abort();
        var dialog = FindAnyObjectByType<Dialog>();
        if (dialog != null)
            dialog.Hide();
        instance.Build(dialog != null ? dialog.font : null);
    }

    void Build(TMP_FontAsset font)
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32010;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0;
        group.blocksRaycasts = false;
        var panel = new GameObject("Dark playfield", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.5f, 0);
        rect.anchorMax = new Vector2(.5f, 1);
        rect.sizeDelta = new Vector2(405, 0);
        panel.GetComponent<Image>().color = new Color(.012f, .018f, .025f, .92f);
        var title = new GameObject("GAME OVER", typeof(RectTransform), typeof(TextMeshProUGUI));
        title.transform.SetParent(rect, false);
        var text = title.GetComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.text = "GAME\nOVER";
        text.fontSize = 100;
        text.enableAutoSizing = true;
        text.fontSizeMin = 68;
        text.fontSizeMax = 100;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1, .75f, .38f);
        text.lineSpacing = -12;
        text.raycastTarget = false;
        lettering = text.rectTransform;
        lettering.sizeDelta = new Vector2(380, 280);
        lettering.anchoredPosition = new Vector2(0, 20);
        // The coins this run banked for the hangar: a coin and its count under the lettering.
        var haul = new GameObject("Coins gathered", typeof(RectTransform), typeof(TextMeshProUGUI));
        haul.transform.SetParent(rect, false);
        var count = haul.GetComponent<TextMeshProUGUI>();
        if (font != null)
            count.font = font;
        count.text = "+" + RunCoins.Count;
        count.fontSize = 34;
        count.alignment = TextAlignmentOptions.MidlineLeft;
        count.color = new Color(1, .82f, .4f);
        count.raycastTarget = false;
        // Coin and number centred together beneath the lettering.
        float numberWidth = count.GetPreferredValues(count.text).x;
        float total = 24 + 8 + numberWidth;
        count.rectTransform.sizeDelta = new Vector2(numberWidth + 4, 44);
        count.rectTransform.anchoredPosition = new Vector2(-total * .5f + 32 + numberWidth * .5f + 2, -150);
        var coin = new GameObject("Coin", typeof(RectTransform), typeof(Image));
        coin.transform.SetParent(rect, false);
        var disc = coin.GetComponent<Image>();
        disc.sprite = CombatVisuals.Disc;
        disc.color = new Color(1, .76f, .25f);
        disc.raycastTarget = false;
        disc.rectTransform.sizeDelta = new Vector2(24, 24);
        disc.rectTransform.anchoredPosition = new Vector2(-total * .5f + 12, -150);
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        group.alpha = Mathf.Clamp01(age / .35f);
        lettering.localScale = Vector3.one * Mathf.Lerp(1.12f, 1, Mathf.SmoothStep(0, 1, age / .45f));
        if (age >= 4.2f && !leaving && !CloudTransition.Busy)
        {
            leaving = true;
            // The roguelite loop: a lost run goes back to the hangar, where the banked coins can be spent.
            CloudTransition.Load("L1", HangarHub.Return);
        }
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
