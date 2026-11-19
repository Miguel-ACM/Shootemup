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
        MetaProgress.EndRun(victory ? (FlightTrials.Active == null ? "Victory" : "Certified") : "Lost");
        instance.won = victory && MetaProgress.LastFlight?.outcome != "Lost";
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
    TextMeshProUGUI score, coins, footer;
    FlightLog log;
    bool won;
    bool tallied;
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
            new Vector2(405, 720), new Color(.013f, .025f, .035f, .95f));
        playfield.gameObject.AddComponent<RectMask2D>();
        string title = won ? (log.contract == null ? "VICTORY" : "CERTIFIED") : "GAME OVER";
        var headline = Label(title, 251, 60, 43,
            won ? accent : new Color(1, .76f, .58f));
        headline.textWrappingMode = TextWrappingModes.NoWrap;
        lettering = headline.rectTransform;
        if (won)
        {
            wings = new GameObject("Pilot wings", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            wings.transform.SetParent(playfield, false);
            wings.sprite = Resources.Load<Sprite>("DockArt/Wings");
            wings.rectTransform.sizeDelta = new Vector2(84, 42);
            wings.rectTransform.anchoredPosition = new Vector2(0, 310);
            wings.raycastTarget = false;
            ItemSound.Play("ready", .21f);
        }
        Label("SCORE", 193, 20, 12, DockInstrument.Muted, -77, 184);
        score = Label("", 161, 40, 27, DockInstrument.Paper, -77, 208);
        score.fontSizeMin = 14;
        score.textWrappingMode = TextWrappingModes.NoWrap;
        Label("COINS BANKED", 193, 20, 12, DockInstrument.Muted, 98, 145);
        coins = Label("", 161, 40, 25, DockInstrument.Brass, 98, 145);
        DockInstrument.Block(playfield, "Tally rule", Vector2.up * 124, new Vector2(334, 2), new Color(.22f, .31f, .32f));
        UnlockReceipt.Show(playfield, log, font);
        footer = Label("", -324, 32, 14, DockInstrument.Muted);
        group.alpha = 0;
    }

    TextMeshProUGUI Label(string text, float y, float height, float size, Color color, float x = 0, float width = 365)
    {
        var label = new GameObject("Result lettering", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(playfield, false);
        label.rectTransform.sizeDelta = new Vector2(width, height);
        label.rectTransform.anchoredPosition = new Vector2(x, y);
        if (font != null)
            label.font = font;
        label.spriteAsset = InputPrompts.InlineAsset;
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
        if (!tallied)
        {
            float t = Mathf.SmoothStep(0, 1, (age - .35f) / 1.1f);
            score.text = Mathf.RoundToInt(log.score * t).ToString("N0");
            coins.text = "+" + Mathf.RoundToInt(log.banked * t).ToString("N0");
            tallied = t >= 1;
        }
        bool ready = age > 1.6f;
        footer.text = ready ? DockInstrument.MenuKey() + "   HANGAR" : "";
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
