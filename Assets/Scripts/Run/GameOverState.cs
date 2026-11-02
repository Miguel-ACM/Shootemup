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
        var dialog = FindAnyObjectByType<Dialog>();
        if (dialog != null)
            dialog.Hide();
        instance.Build(dialog != null ? dialog.font : null);
    }

    DockInstrument meter;
    TextMeshProUGUI tally, stamps, footer;
    FlightLog log;
    bool won;
    int sounded;
    Image wings;
    void Build(TMP_FontAsset font)
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
        meter = new DockInstrument(gameObject, font, accent, 32010);
        group = meter.group;
        meter.Label("FLIGHT DEBRIEF", -48, 255, 462, 32, 15, accent, true);
        string title = won ? (log.contract == null ? "VICTORY" : "CERTIFIED") : "GAME OVER";
        var headline = meter.Label(title, 0, 173, 558, 94, 74, won ? DockInstrument.Brass : DockInstrument.Paper, true);
        lettering = headline.rectTransform;
        if (won)
        {
            headline.rectTransform.anchoredPosition = new Vector2(70, 173);
            headline.rectTransform.sizeDelta = new Vector2(412, 88);
            wings = new GameObject("Pilot wings", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            wings.transform.SetParent(meter.face, false);
            wings.sprite = Resources.Load<Sprite>("DockArt/Wings");
            wings.rectTransform.sizeDelta = new Vector2(135, 68);
            wings.rectTransform.anchoredPosition = new Vector2(-217, 173);
            ItemSound.Play("ready", .21f);
        }
        meter.Label((log.contract ?? log.frame.ToString()).ToUpperInvariant() + "    /    " + log.difficulty.ToString().ToUpperInvariant()
            + "    /    " + log.Duration, 0, 105, 558, 36, 19, DockInstrument.Muted);
        meter.Label(log.route, 0, 65, 558, 48, 19, DockInstrument.Paper);
        DockInstrument.Block(meter.face, "Tally rule", new Vector2(0, 35), new Vector2(558, 2), accent);
        tally = meter.Label("", 0, -16, 558, 85, 23, DockInstrument.Paper);
        stamps = meter.Label("", 0, -138, 558, 142, 19, accent, true);
        footer = meter.Label("", 0, -252, 558, 36, 17, DockInstrument.Muted);
        group.alpha = 0;
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
        tally.text = "SCORE   " + Mathf.RoundToInt(log.score * t).ToString("N0") + "      COINS   +" + Mathf.RoundToInt(log.earned * t)
            + "   <size=65%>(" + log.banked + " banked)</size>\n" + log.bosses + (log.bosses == 1 ? " boss     " : " bosses     ") + log.clean + " no-hit     " + log.hits + (log.hits == 1 ? " hit     " : " hits     ") + log.peak + "× peak";
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
        footer.text = ready ? DockInstrument.FireKey() + "   RETURN TO HANGAR" : "";
        if (ready && !leaving && !CloudTransition.Busy && (GameInput.Actions.PlayerControls.FirePrimary.WasPressedThisFrame()
            || age > Mathf.Max(12, 5 + log.awards.Count * 1.4f)))
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
