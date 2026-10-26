#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>End-to-end play verification, excluded from player builds.</summary>
public sealed class LevelFlowProbe : MonoBehaviour
{
    const string Output = "FlowReview/LevelFlowResult.txt";
    const string Images = "FlowReview/Captures";
    readonly List<string> errors = new List<string>();
    readonly Queue<string> captures = new Queue<string>();
    bool recordingClouds;
    float nextCloudCapture;
    float previousCloudTime, previousCloudY;
    int cloudFrames;
    void Awake()
    {
        Application.runInBackground = true;
        Application.logMessageReceived += Log;
    }

    void Log(string message, string trace, LogType type)
    {
        // Unity 6's editor search index can throw on startup in a freshly imported batch project.
        if (trace.Contains("UnityEditor.Search.SearchDatabase"))
            return;
        if (type == LogType.Exception || type == LogType.Error)
        {
            var error = message + "\n" + trace;
            if (!errors.Contains(error))
                errors.Add(error);
        }
    }

    IEnumerator Start()
    {
        StartCoroutine(CaptureFrames());
        var run = Run();
        while (true)
        {
            object current = null;
            bool more = false;
            Exception failure = null;
            try
            {
                more = run.MoveNext();
                if (more)
                    current = run.Current;
            }
            catch (Exception e) { failure = e; }
            if (failure != null)
            {
                File.WriteAllText(Output, "FAIL\n" + failure + "\n" + string.Join("\n", errors));
                QuitUnlessTested(1);
                yield break;
            }
            if (!more)
                break;
            yield return current;
        }
        File.WriteAllText(Output, "PASS: gameplay preloaded without activating the paused menu; same preload reused on new game; all offered environments prepared invisibly and reused, unused routes released; live ship/background movement during title and cloud reveal; two-label title with the game font and Level x eyebrow; main menu; ordered routes and harder loops (400 loop trials); neutral/switch/frame-rate charge checks; five environments; cloud cover/reveal; actual boss death; both spatial route selections; pause; player/stats/equipment preservation; next-stage waves; return to menu and new-run reset.\n" + string.Join("\n", errors));
        QuitUnlessTested(errors.Count == 0 ? 0 : 1);
    }

    IEnumerator Run()
    {
        UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
        UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        foreach (var device in new List<UnityEngine.InputSystem.InputDevice>(UnityEngine.InputSystem.InputSystem.devices))
            UnityEngine.InputSystem.InputSystem.DisableDevice(device);
        Directory.CreateDirectory(Images);
        Directory.CreateDirectory(Images + "/CloudMotion");
        yield return null;
        Check(SceneManager.GetActiveScene().name == "Title" && Time.timeScale == 0, "Play did not begin at the paused main menu");
        var transition = FindAnyObjectByType<CloudTransition>();
        float preloadDeadline = Time.realtimeSinceStartup + 30;
        while ((Get(transition, "preparedLoad") as AsyncOperation)?.progress < .9f || Get(transition, "preparedLoad") == null)
        {
            Check(Time.realtimeSinceStartup < preloadDeadline, "Gameplay was not preloaded while idling at the menu");
            yield return null;
        }
        var menuLoad = (AsyncOperation)Get(transition, "preparedLoad");
        Debug.Log("Preload verification: gameplay ready at paused menu");
        Check(!menuLoad.allowSceneActivation && !CloudTransition.Busy && Time.timeScale == 0 && SceneManager.GetActiveScene().name == "Title",
            "Speculative gameplay preload activated the scene or disturbed the paused menu");
        Capture("01-menu");
        recordingClouds = true;
        {
            var reviewStart = FindAnyObjectByType<NewGame>();
            reviewStart.OnAccept();
            reviewStart.Choose(1);
        }
        Check(ReferenceEquals(menuLoad, Get(transition, "preparedLoad")), "New game discarded the preloaded gameplay operation");
        LevelRun.Reset(LevelId.Fields); // A dark valley makes the cloud edge readable in the recording.
        Check(CloudTransition.Busy, "New game bypassed the clouds");
        yield return new WaitForSecondsRealtime(CloudTransition.RevealSeconds * .34f);
        Capture("02-cloud-billows");
        Check(CloudTransition.Busy && SceneManager.GetActiveScene().name == "Title", "Cloud cover completed too quickly");
        var cloud = FindAnyObjectByType<CloudTransition>();
        var clip = cloud.GetComponentInChildren<UnityEngine.UI.RectMask2D>();
        Check(clip != null && Mathf.Approximately(clip.rectTransform.rect.width, 405), "Clouds are not clipped to the playfield");
        Check(cloud.GetComponentsInChildren<CloudCurtainGraphic>().Length == 4, "Cloud layers missing");
        var flowing = cloud.GetComponentsInChildren<CloudCurtainGraphic>();
        float coverFlow = flowing[0].FlowDistance;
        var artwork = Resources.Load<Texture2D>("CloudBillow");
        Check(artwork != null && artwork.filterMode == FilterMode.Point && artwork.width <= 128, "Cloud artwork is not a point-filtered PSX sprite");
        bool transparent = false, opaque = false;
        foreach (var pixel in artwork.GetPixels32())
        {
            transparent |= pixel.a == 0;
            opaque |= pixel.a == 255;
        }
        Check(transparent && opaque, "Cloud sprite lost its transparent silhouette or opaque billows");
        yield return new WaitForSecondsRealtime(CloudTransition.RevealSeconds * .67f);
        Capture("02-white-cover");
        Check(flowing[0].FlowDistance > coverFlow + 40, "Cloud billows stopped during cover/loading");
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-verifySlowScenery") >= 0)
        {
            // The disposable verification project delays its background parse to simulate slow I/O.
            yield return new WaitForSecondsRealtime(.12f);
            var loadingBanks = (RectTransform[])Get(cloud, "banks");
            var parked = loadingBanks[0].anchoredPosition;
            float flow = flowing[0].FlowDistance;
            Capture("02-loading-flow-before");
            yield return new WaitForSecondsRealtime(.5f);
            Check(loadingBanks[0].anchoredPosition.y < parked.y - 100, "Cloud curtain stopped while waiting for the next scene");
            Check(flowing[0].FlowDistance >= flow + 20, "Cloud texture stopped while waiting for the next scene");
            Capture("02-loading-flow-after");
        }
        float until = Time.realtimeSinceStartup + 12;
        while (FindAnyObjectByType<LevelReveal>() == null && Time.realtimeSinceStartup < until)
            yield return null;
        yield return null;
        var reveal = FindAnyObjectByType<LevelReveal>();
        Check(reveal != null && Time.timeScale == 1, "Level title paused gameplay");
        var prepared = typeof(LevelEnvironment).GetField("kit", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null) as System.Collections.IDictionary;
        Check(prepared != null && prepared.Count == 38, "Scenery was not prepared before scene reveal");
        var instruments = FindAnyObjectByType<IndustrialHUD>();
        Check(instruments != null && !instruments.EntranceComplete, "HUD skipped its first entrance");
        Check(instruments.GetComponentsInChildren<FluorescentStartup>(true).Length >= 3, "Score, resources and boss instruments are missing their shared power-on");
        Check(LitCells(instruments, "healthCells") == 0 && LitCells(instruments, "manaCells") == 0, "HUD filled before its casing entered");
        while (instruments.EntranceAge < .24f)
            yield return null;
        float initialPower = ((CanvasGroup)Get(instruments, "sideGroup")).alpha;
        Check(initialPower > 0 && initialPower < 1, "HUD missed its dim initial fluorescent strike");
        Capture("02-hud-casing");
        var labels = reveal.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        Check(labels.Length == 2, "Level title contains extra text");
        Check(Array.Exists(labels, l => l.text == "Level " + LevelRun.Stage), "Missing small Level x eyebrow");
        foreach (var label in labels)
            Check(label.font == FindAnyObjectByType<Dialog>().font, "Level title uses a different font from the game");
        var liveShip = FindAnyObjectByType<Shipcontroller>();
        float beforeX = liveShip.transform.position.x;
        var liveBackground = (MonoBehaviour)FindAnyObjectByType<LevelEnvironment>() ?? FindAnyObjectByType<PSXGrassField>();
        float beforeTravel = (float)Get(liveBackground, "travel");
        var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.RightArrow));
        yield return new WaitForSecondsRealtime(.3f);
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
        yield return null;
        Check(liveShip.transform.position.x > beforeX + .4f, "Ship cannot move during the title/cloud reveal");
        Check(!Mathf.Approximately((float)Get(liveBackground, "travel"), beforeTravel), "Background stopped during the title");
        var banks = (RectTransform[])Get(cloud, "banks");
        var extensions = (float[])Get(cloud, "trailingExtension");
        Check(banks[3].anchoredPosition.y + extensions[3] < banks[0].anchoredPosition.y + extensions[0], "Cloud exit did not peel away the front layer first");
        while ((float)Get(instruments, "entranceAge") < 1.15f)
            yield return null;
        Check(LitCells(instruments, "healthCells") > 0 && LitCells(instruments, "healthCells") < 24 && LitCells(instruments, "manaCells") == 0, "Health did not charge segment by segment before energy");
        Capture("02-hud-health-charging");
        while ((float)Get(instruments, "entranceAge") < 2.1f)
            yield return null;
        Check(LitCells(instruments, "healthCells") == 24 && LitCells(instruments, "manaCells") > 0 && LitCells(instruments, "manaCells") < 24, "Energy did not charge after health");
        Capture("02-hud-energy-charging");
        while (CloudTransition.Busy && Time.realtimeSinceStartup < until)
            yield return null;
        recordingClouds = false;
        for (int i = 0; i < banks.Length; i++)
            Check(banks[i].anchoredPosition.y + extensions[i] + CloudCurtainGraphic.EdgeOuterY < -360, "Cloud canvas disappeared before its last edge left the screen");
        Check(!CloudTransition.Busy && Time.timeScale == 1, "Menu-to-game transition stalled");
        yield return new WaitForSecondsRealtime(.1f);
        Capture("02-live-title");
        Check(SceneManager.GetActiveScene().name == "L1", "New game did not load gameplay");
        Debug.Log("Preload verification: reused gameplay preload");
        var ship = FindAnyObjectByType<Shipcontroller>();
        Check(ship != null, "Player missing");
        var weapon = ship.GetComponentInChildren<AWeapon>();
        yield return new WaitForSecondsRealtime(1.2f);
        foreach (LevelId level in Enum.GetValues(typeof(LevelId)))
        {
            // Time cold construction steps, including nested resource/worker waits.
            yield return MeasurePreparation(level);
            // Prepare exactly as the route selector does, and verify the built object is reused.
            CloudTransition.PrepareRoutes(level, LevelId.Fields);
            while ((bool)Get(transition, "routesPreparing"))
                yield return null;
            var warmedMusic = (AudioClip[])Get(transition, "routeMusic");
            foreach (var routeClip in warmedMusic)
                Check(routeClip != null && routeClip.loadInBackground && routeClip.loadState == AudioDataLoadState.Loaded, "Offered route music was not loaded in the background before the transition");
            var routeCache = (Dictionary<LevelId, LevelEnvironment>)typeof(LevelEnvironment)
                .GetField("preparedRoutes", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            routeCache.TryGetValue(level, out var preparedRoute);
            if (level != LevelId.Fields)
                Check(preparedRoute != null && !preparedRoute.gameObject.activeSelf, "Offered route was not prepared invisibly");
            var movingBackground = (MonoBehaviour)FindAnyObjectByType<LevelEnvironment>() ?? FindAnyObjectByType<PSXGrassField>();
            float coverTravel = (float)Get(movingBackground, "travel");
            float shipX = ship.transform.position.x;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.LeftArrow));
            CloudTransition.Change(() => { LevelRun.Reset(level); LevelEnvironment.Apply(); });
            yield return new WaitForSecondsRealtime(.3f);
            Check(Time.timeScale == 1 && !Mathf.Approximately((float)Get(movingBackground, "travel"), coverTravel), "Cloud cover stopped gameplay");
            Check(ship.transform.position.x < shipX - .3f, "Cloud cover stopped ship controls: " + level + ", x=" + shipX + " -> " + ship.transform.position.x + ", input=" + Get(ship, "move") + ", enabled=" + ship.enabled + ", speed=" + GameRules.playerMoveSpeed);
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
            yield return null;
            ship.transform.position = new Vector3(0, ship.transform.position.y, ship.transform.position.z);
            yield return new WaitForSecondsRealtime(3.9f);
            Capture("03-reveal-" + level);
            until = Time.realtimeSinceStartup + 10;
            while (CloudTransition.Busy && Time.realtimeSinceStartup < until)
                yield return null;
            Check(!CloudTransition.Busy, "Environment transition stalled");
            if (level != LevelId.Fields)
                Check(preparedRoute == FindAnyObjectByType<LevelEnvironment>(), "Level change rebuilt an already prepared route");
            Check(routeCache.Count == 0, "Level change retained unused prepared routes");
            Debug.Log("Preload verification: reused prepared route " + level);
            Check(instruments == FindAnyObjectByType<IndustrialHUD>() && instruments.EntranceComplete, "HUD entrance replayed between levels");
            if (level == LevelId.Thunder)
            {
                yield return new WaitForSeconds(1.7f);
                Capture("04-thunder-lightning");
            }
            yield return new WaitForSecondsRealtime(2.5f);
            Capture("04-environment-" + level);
            Check(level == LevelId.Fields ? FindAnyObjectByType<PSXGrassField>() != null : FindAnyObjectByType<LevelEnvironment>() != null, "Environment missing: " + level);
        }
        LevelRun.Reset();
        LevelEnvironment.Apply();
        var waves = FindAnyObjectByType<WaveOrchestrer>();
        // Skip the opening shops only in this probe, so an actual instantiated boss exercises the death hook.
        foreach (var item in FindObjectsByType<AItem>())
            Destroy(item.gameObject);
        ship.health = 73;
        GameRules.playerShootDamage = 9;
        ship.transform.position = new Vector3(0, -4, ship.transform.position.z);
        Call(waves, "SpawnBoss");
        yield return new WaitForSeconds(.2f);
        Check(FindAnyObjectByType<DistrictBoss>() != null, "Boss did not spawn");
        foreach (var boss in FindObjectsByType<DistrictBoss>())
            boss.damage(100000);
        yield return new WaitForSeconds(1);
        var choice = FindAnyObjectByType<LevelChoice>();
        Check(choice != null && LevelChoice.Active, "Boss death did not open the route picker");
        var choiceLabels = choice.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        Check(choiceLabels.Length == 2, "Route picker contains more than the level names");
        foreach (var label in choiceLabels)
            Check(label.font == FindAnyObjectByType<Dialog>().font && (label.text == choice.Left.ToString().ToUpperInvariant() || label.text == choice.Right.ToString().ToUpperInvariant()), "Route picker name or font is wrong");
        Check(choice.Left != choice.Right && choice.Left != LevelRun.Current && choice.Right != LevelRun.Current, "Invalid boss route pair");
        yield return new WaitForSeconds(.7f);
        Check(choice.Charge.Progress == 0, "Centre charged a choice");
        Capture("05-neutral-choice");
        ship.transform.position = new Vector3(-3, -4, ship.transform.position.z);
        yield return new WaitForSeconds(.5f);
        Check(choice.Charge.Progress > .1f, "Left hold did not charge");
        var fill = ((RectTransform[])Get(choice, "fills"))[0];
        Check(fill.anchorMin == new Vector2(.5f, 0) && fill.pivot == new Vector2(.5f, 0) && Mathf.Approximately(fill.sizeDelta.x, 202.5f), "Route fill does not rise from the bottom at full half width");
        Check(fill.sizeDelta.y / 720 < choice.Charge.Progress * .8f, "Route fill is not easing in");
        float paused = choice.Charge.Progress;
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(.5f);
        Check(Mathf.Abs(choice.Charge.Progress - paused) < .001f, "Paused choice kept charging");
        Time.timeScale = 1;
        ship.transform.position = new Vector3(0, -4, ship.transform.position.z);
        yield return new WaitForSeconds(.1f);
        Check(choice.Charge.Progress == 0, "Centre did not cancel the choice");
        var expected = choice.Left;
        int stage = LevelRun.Stage;
        ship.transform.position = new Vector3(-3, -4, ship.transform.position.z);
        yield return new WaitForSeconds(1.35f);
        Capture("06-left-half-filled");
        until = Time.realtimeSinceStartup + 6;
        while (!CloudTransition.Busy && Time.realtimeSinceStartup < until)
            yield return null;
        Check(CloudTransition.Busy, "Left route failed to commit");
        ship.health = 73; // Freeze a known value after boss drops have finished falling.
        until = Time.realtimeSinceStartup + 10;
        while (CloudTransition.Busy && Time.realtimeSinceStartup < until)
            yield return null;
        Check(LevelRun.Current == expected && LevelRun.Stage == stage + 1, "Left route loaded the wrong level or advanced twice");
        Check(ship == FindAnyObjectByType<Shipcontroller>() && ship.health == 73 && GameRules.playerShootDamage == 9, "Stage change lost player state: health=" + ship.health + ", damage=" + GameRules.playerShootDamage);
        Check(weapon == ship.GetComponentInChildren<AWeapon>(), "Stage change lost equipment");
        Check(!LevelChoice.Active && !GameRules.playerDisabled && Time.timeScale == 1, "Stage change left choice or player disabled");
        Check((int)Get(waves, "currentWaveNumber") == 0 && AEnemy.Live.Count == 0, "Next stage spawned enemies before the cloud opening finished");
        yield return new WaitForSeconds(WaveOrchestrer.OpeningBreather + .1f);
        Check((int)Get(waves, "currentWaveNumber") == 1, "Next stage waves did not restart after the opening breather");
        foreach (var item in FindObjectsByType<AItem>())
            Destroy(item.gameObject);
        Call(waves, "SpawnBoss");
        yield return new WaitForSeconds(.2f);
        foreach (var boss in FindObjectsByType<DistrictBoss>())
            boss.damage(100000);
        yield return new WaitForSeconds(1.1f);
        choice = FindAnyObjectByType<LevelChoice>();
        Check(choice != null, "Second boss did not open another choice");
        expected = choice.Right;
        stage = LevelRun.Stage;
        ship.transform.position = new Vector3(3, -4, ship.transform.position.z);
        yield return new WaitForSeconds(1.35f);
        Capture("07-right-half-filled");
        until = Time.realtimeSinceStartup + 6;
        while (!CloudTransition.Busy && Time.realtimeSinceStartup < until)
            yield return null;
        Check(CloudTransition.Busy, "Right route failed to commit");
        until = Time.realtimeSinceStartup + 10;
        while (CloudTransition.Busy && Time.realtimeSinceStartup < until)
            yield return null;
        Check(LevelRun.Current == expected && LevelRun.Stage == stage + 1, "Right route selected the wrong level");
        Call(waves, "SpawnBoss");
        yield return new WaitForSeconds(.2f);
        foreach (var boss in FindObjectsByType<DistrictBoss>())
            boss.damage(100000);
        yield return new WaitForSeconds(1.1f);
        Check(!LevelChoice.Active && FindAnyObjectByType<LevelChoice>() == null, "Final boss opened a redundant choice");
        until = Time.realtimeSinceStartup + 12;
        while (CloudTransition.Busy && Time.realtimeSinceStartup < until)
            yield return null;
        Check(LevelRun.Current == LevelId.Fields && LevelRun.Stage == 4 && LevelRun.Loop == 2, "Final boss did not loop to harder Plains");
        Check(ship == FindAnyObjectByType<Shipcontroller>() && weapon == ship.GetComponentInChildren<AWeapon>() && GameRules.playerShootDamage == 9, "Loop discarded equipment or upgrades");
        Check(!GameRules.playerDisabled && !LevelChoice.Active, "Loop left controls disabled");
        CloudTransition.PrepareRoutes(LevelId.Snow, LevelId.Thunder);
        CloudTransition.Load("Title");
        Check(!(bool)Get(transition, "routesPreparing"), "Returning to the menu left route preparation running");
        until = Time.realtimeSinceStartup + 10;
        while (CloudTransition.Busy && Time.realtimeSinceStartup < until)
            yield return null;
        Check(SceneManager.GetActiveScene().name == "Title" && Time.timeScale == 0, "Return to menu failed");
        Check((bool)Get(FindAnyObjectByType<Menu>(), "menuOpen"), "Returned main menu cannot accept input");
        {
            var reviewStart = FindAnyObjectByType<NewGame>();
            reviewStart.OnAccept();
            reviewStart.Choose(1);
        }
        until = Time.realtimeSinceStartup + 10;
        while (CloudTransition.Busy && Time.realtimeSinceStartup < until)
            yield return null;
        Check(LevelRun.Current == LevelId.Fields && LevelRun.Loop == 1 && LevelRun.Stage == 1 && GameRules.playerShootDamage == 2 && !GameRules.playerDisabled, "New run retained previous run state");
        UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
        Check(errors.Count == 0, "Unity reported runtime errors: " + string.Join("\n", errors));
    }

    static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    IEnumerator MeasurePreparation(LevelId level)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(LevelEnvironment.PrepareRoute(level));
        int frames = 0;
        double longest = 0;
        try
        {
            while (stack.Count > 0)
            {
                var step = stack.Peek();
                var watch = System.Diagnostics.Stopwatch.StartNew();
                bool more = step.MoveNext();
                watch.Stop();
                longest = Math.Max(longest, watch.Elapsed.TotalMilliseconds);
                Check(watch.Elapsed.TotalMilliseconds < 100, "Route preparation blocked the main thread for " + watch.Elapsed.TotalMilliseconds + "ms: " + level);
                if (!more)
                {
                    (stack.Pop() as IDisposable)?.Dispose();
                    continue;
                }
                if (step.Current is IEnumerator nested)
                {
                    stack.Push(nested);
                    continue;
                }
                frames++;
                yield return step.Current;
            }
            if (level != LevelId.Fields)
                Check(frames >= 4, "Environment was built without yielding frames: " + level);
            Debug.Log("Preparation timing: " + level + ", " + frames + " yields, longest main-thread step " + longest.ToString("F2") + "ms");
        }
        finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); }
    }

    static int LitCells(IndustrialHUD hud, string field)
    {
        int count = 0;
        foreach (var cell in (UnityEngine.UI.Image[])Get(hud, field))
            if (cell.color.r > .3f)
                count++;
        return count;
    }

    static object Get(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    static void Field(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    static void Call(object target, string name) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    void Capture(string name)
    {
        captures.Enqueue(name);
    }

    IEnumerator CaptureFrames()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();
            bool record = recordingClouds && Time.unscaledTime >= nextCloudCapture;
            if (record && CloudTransition.Busy)
            {
                var cloud = FindAnyObjectByType<CloudTransition>();
                float y = ((RectTransform[])Get(cloud, "banks"))[0].anchoredPosition.y;
                if (previousCloudTime > 0 && Time.unscaledTime > previousCloudTime)
                    Check(y < previousCloudY, "Cloud curtain paused between cover and reveal");
                previousCloudTime = Time.unscaledTime;
                previousCloudY = y;
            }
            else if (!recordingClouds || !CloudTransition.Busy)
                previousCloudTime = 0;
            if (captures.Count == 0 && !record)
                continue;
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            if (image != null)
            {
                var bytes = image.EncodeToPNG();
                if (captures.Count > 0)
                    File.WriteAllBytes(Path.Combine(Images, captures.Dequeue() + ".png"), bytes);
                if (record)
                {
                    File.WriteAllBytes(Images + "/CloudMotion/" + (cloudFrames++).ToString("000") + ".png", bytes);
                    nextCloudCapture = Time.unscaledTime + .05f;
                }
                Destroy(image);
            }
        }
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= Log;
    }

    // When a Test Runner test drives this probe (CheckHarness), the test leaves Play mode itself.
    static void QuitUnlessTested(int code)
    {
        if (!UnityEditor.SessionState.GetBool("CheckHarness.UnderTest", false) && !UnityEditor.SessionState.GetBool("CheckHarness.TestSession", false))
            UnityEditor.EditorApplication.Exit(code);
    }
}
#endif
