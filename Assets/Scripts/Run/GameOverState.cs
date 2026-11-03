using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The final flight tally occupies the same portrait aperture as the district reveals.
public sealed class GameOverState : MonoBehaviour
{
    static GameOverState instance;
    public static bool Active => instance != null;
    CanvasGroup group;
    RectTransform lettering;
    TMP_FontAsset font;
    float age;
    bool leaving;
    public static void Show(bool victory = false)
    {
        if (Active)
            return;
        instance = new GameObject("Game over", typeof(RectTransform)).AddComponent<GameOverState>();
        instance.won = victory;
        MetaProgress.EndRun(victory ? (FlightTrials.Active == null ? "Victory" : "Certified") : "Lost");
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
        var dialog = FindAnyObjectByType<Dialog>(FindObjectsInactive.Include);
        if (dialog != null)
        {
            dialog.Hide();
            instance.font = dialog.font;
        }
        instance.Build();
    }

    RectTransform playfield;
    TextMeshProUGUI score, coins, stamps, footer;
    FlightLog log;
    bool won;
    int sounded;
    Image wings;
    void Build()
    {
        log = MetaProgress.LastFlight ?? new FlightLog
        {
            score = Score.CurrentScore,
            earned = RunCoins.Earned,
            route = LevelRun.CurrentName,
            frame = ShipFrames.Current.frame,
            difficulty = Difficulty.Current
        };
        var accent = won ? DockInstrument.Brass : DockInstrument.Sea;
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32010;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 1;
        group = gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        playfield = DockInstrument.Block(transform, "Flight area", Vector2.zero,
            new Vector2(405, 720), new Color(.013f, .025f, .035f, .86f));
        playfield.gameObject.AddComponent<RectMask2D>();
        string title = won ? (log.contract == null ? "VICTORY" : "CERTIFIED") : "GAME OVER";
        var headline = Label(title, won ? 220 : 245, 82, 44,
            won ? accent : new Color(1, .76f, .58f));
        headline.textWrappingMode = TextWrappingModes.NoWrap;
        lettering = headline.rectTransform;
        if (!won)
            DockInstrument.Block(playfield, "Debrief rule", new Vector2(0, 193), new Vector2(330, 3), new Color(.75f, .29f, .17f));
        if (won)
        {
            wings = new GameObject("Pilot wings", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            wings.transform.SetParent(playfield, false);
            wings.sprite = Resources.Load<Sprite>("DockArt/Wings");
            wings.rectTransform.sizeDelta = new Vector2(124, 62);
            wings.rectTransform.anchoredPosition = new Vector2(0, 291);
            wings.raycastTarget = false;
            ItemSound.Play("ready", .21f);
        }
        Label((log.contract ?? log.frame.ToString()).ToUpperInvariant() + " / " + log.difficulty.ToString().ToUpperInvariant()
            + " / " + log.Duration, 151, 35, 15, DockInstrument.Paper).textWrappingMode = TextWrappingModes.NoWrap;
        Label(log.route, 111, 48, 15, DockInstrument.Muted);
        Label("SCORE", 48, 22, 13, accent);
        score = Label("", 9, 52, 34, DockInstrument.Paper);
        coins = Label("", -39, 38, 16, DockInstrument.Brass);
        coins.textWrappingMode = TextWrappingModes.NoWrap;
        Label(log.bosses + (log.bosses == 1 ? " boss" : " bosses") + "   " + log.clean + " no-hit\n"
            + log.hits + (log.hits == 1 ? " hit" : " hits") + "   " + log.peak + "x peak", -86, 44, 16, DockInstrument.Paper);
        stamps = Label("", -192, 151, 16, accent);
        stamps.textWrappingMode = TextWrappingModes.NoWrap;
        stamps.lineSpacing = 14;
        footer = Label("", -321, 36, 14, DockInstrument.Muted);
        group.alpha = 0;
    }

    TextMeshProUGUI Label(string text, float y, float height, float size, Color color)
    {
        var label = new GameObject("Result lettering", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(playfield, false);
        label.rectTransform.sizeDelta = new Vector2(365, height);
        label.rectTransform.anchoredPosition = new Vector2(0, y);
        if (font != null)
            label.font = font;
        label.fontSizeMax = size;
        label.fontSizeMin = size * .65f;
        label.enableAutoSizing = true;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
        label.text = text;
        return label;
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        group.alpha = Mathf.Clamp01(age / .35f);
        lettering.localScale = Vector3.one * Mathf.Lerp(1.08f, 1, Mathf.SmoothStep(0, 1, age / .55f));
        if (wings != null)
        {
            float strike = Mathf.SmoothStep(0, 1, (age - .15f) / .5f);
            wings.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1, strike);
            wings.color = new Color(1, 1, 1, strike);
        }
        float t = Mathf.SmoothStep(0, 1, (age - .35f) / 1.1f);
        score.text = Mathf.RoundToInt(log.score * t).ToString("N0");
        coins.text = "+" + Mathf.RoundToInt(log.earned * t) + " coins  <size=80%>" + log.banked + " banked</size>";
        int count = Mathf.Clamp(Mathf.FloorToInt((age - 1.05f) * 3), 0, log.awards.Count);
        if (count > sounded)
        {
            sounded = count;
            ItemSound.Play("ready", .13f);
        }
        if (count > 0)
        {
            // The complete list stays available on the recorder; cycle four stamps at a time on long receipts.
            int start = count < log.awards.Count ? (count - 1) / 4 * 4 : (Mathf.FloorToInt(Mathf.Max(0, age - 2 - log.awards.Count / 3f) / 3) % ((count + 3) / 4)) * 4;
            stamps.text = "NEW RELEASES\n" + string.Join("\n", log.awards.GetRange(start, Mathf.Min(4, count - start)));
        }
        else
            stamps.text = age > 1.2f && log.awards.Count == 0 ? "FLIGHT LOG FILED" : "";
        bool ready = age > 1.6f + log.awards.Count / 3f;
        footer.text = ready ? DockInstrument.MenuKey() + "   RETURN TO HANGAR" : "";
        var controls = GameInput.Actions.PlayerControls;
        if (ready && !leaving && !CloudTransition.Busy && (controls.Accept.WasPressedThisFrame() || controls.Back.WasPressedThisFrame()))
        {
            leaving = true;
            CloudTransition.Load("L1", HangarHub.Return);
        }
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
