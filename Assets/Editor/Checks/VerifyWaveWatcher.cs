#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class VerifyWaveWatcher
{
    const string Key = "WaveWatcherReview";
    static double deadline;
    static VerifyWaveWatcher()
    {
        Application.logMessageReceived += (message, trace, type) =>
          {
              if (SessionState.GetBool("WaveWatcherReloadTest", false) && (type == LogType.Error || type == LogType.Exception) && !trace.Contains("UnityEditor.Search"))
                  SessionState.SetString("WaveWatcherReloadErrors", SessionState.GetString("WaveWatcherReloadErrors", "") + message + "\n");
          };
        WatcherReload.Restored += watcher =>
          {
              if (!SessionState.GetBool("WaveWatcherReloadTest", false))
                  return;
              SessionState.SetBool("WaveWatcherReloadTest", false);
              bool pass = watcher.Level == LevelId.Snow && LevelRun.Seed == 7119 && watcher.ReviewLoop == 2 && watcher.View == WatcherView.Formations &&
                  watcher.Selection == 7 && watcher.Variant == 5 && watcher.Mirror == -1 && watcher.AimSide == 1 && watcher.Paused && !watcher.Looping && watcher.PlaybackSpeed == .5f && SessionState.GetString("WaveWatcherReloadErrors", "") == "";
              File.WriteAllText("ArtSource/WaveWatcherReview/ReloadVerification.txt", pass ? "PASS: an actual script compilation exits without runtime errors and restores the district, seed, loop, catalog entry, variant, mirror, aim, pause, repeat and playback speed in a fresh session." : "FAIL: corrected preview state changed\n" + SessionState.GetString("WaveWatcherReloadErrors", ""));
              CheckHarness.Exit(pass ? 0 : 1);
          };
        EditorApplication.playModeStateChanged += state =>
          {
              if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
              {
                  new GameObject("Wave watcher review").AddComponent<WaveWatcherReview>();
                  deadline = EditorApplication.timeSinceStartup + 420;
              }
          };
        EditorApplication.update += () =>
          {
              if (deadline == 0 || !SessionState.GetBool(Key, false))
                  return;
              const string result = "ArtSource/WaveWatcherReview/Verification.txt";
              if (File.Exists(result))
              {
                  SessionState.SetBool(Key, false);
                  CheckHarness.Exit(File.ReadAllText(result).StartsWith("PASS") ? 0 : 1);
              }
              else if (EditorApplication.timeSinceStartup > deadline)
              {
                  File.WriteAllText(result, "FAIL: timed out");
                  CheckHarness.Exit(1);
              }
          };
    }
    // Command-line entry point for a disposable project copy; exits that editor when finished.
    public static void RunFinal()
    {
        SessionState.SetBool("WaveWatcherQuickReview", true);
        Run();
    }

    public static void RunCatalog()
    {
        SessionState.SetBool("WaveWatcherCatalogReview", true);
        Run();
    }

    public static void RunReload()
    {
        SessionState.SetBool("WaveWatcherReloadSetup", true);
        Run();
    }

    public static void Run()
    {
        Directory.CreateDirectory("ArtSource/WaveWatcherReview");
        if (File.Exists("ArtSource/WaveWatcherReview/Verification.txt"))
            File.Delete("ArtSource/WaveWatcherReview/Verification.txt");
        if (!Application.isBatchMode)
        {
            var game = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            game.position = new Rect(40, 40, 1280, 760);
            game.Show();
        }
        SessionState.SetBool(Key, true);
        CheckHarness.EnterPlayMode();
    }
}

public sealed class WaveWatcherReview : MonoBehaviour
{
    const string Folder = "ArtSource/WaveWatcherReview/";
    readonly List<string> checks = new List<string>(), errors = new List<string>();
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
        Application.logMessageReceived += Log;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= Log;
    }

    void Log(string message, string trace, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception) && !trace.Contains("UnityEditor.Search"))
            errors.Add(message);
    }

    void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
        checks.Add("PASS " + message);
    }

    IEnumerator Start()
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(Run());
        while (stack.Count > 0)
        {
            bool more = false;
            object step = null;
            Exception error = null;
            try
            {
                more = stack.Peek().MoveNext();
                if (more)
                    step = stack.Peek().Current;
            }
            catch (Exception ex) { error = ex; }
            if (error != null)
            {
                errors.Add(error.ToString());
                break;
            }
            if (!more)
            {
                stack.Pop();
                continue;
            }
            if (step is IEnumerator nested)
            {
                stack.Push(nested);
                continue;
            }
            yield return step;
        }
        Time.timeScale = 1;
        File.WriteAllText(Folder + "Verification.txt", (errors.Count == 0 ? "PASS" : "FAIL") + "\n" + string.Join("\n", checks) + "\n" + string.Join("\n", errors));
    }

    static Button Button(string name) => UnityEngine.Object.FindObjectsByType<Button>().First(x => x.name == name);
    void LabelsFit(WaveWatcher watcher)
    {
        Canvas.ForceUpdateCanvases();
        foreach (var label in watcher.GetComponentsInChildren<TextMeshProUGUI>())
        {
            label.ForceMeshUpdate();
            Check(!label.isTextOverflowing, "catalog label fits: " + label.text);
        }
    }

    IEnumerator CatalogReview()
    {
        if (SceneManager.GetActiveScene().name != "Title")
            yield return SceneManager.LoadSceneAsync("Title");
        yield return null;
        FindAnyObjectByType<WaveWatcherMenu>().OnAccept();
        float deadline = Time.realtimeSinceStartup + 80;
        while ((WaveWatcher.Current == null || !WaveWatcher.Current.Ready) && Time.realtimeSinceStartup < deadline)
            yield return null;
        var watcher = WaveWatcher.Current;
        Check(watcher != null && watcher.Ready, "catalog launches through the title into real combat");
        watcher.SetSeed(4137);
        yield return null;
        yield return null;
        string original = WaveVarietyAudit.Signature(DistrictWaves.Create(2));
        watcher.SelectLevel(LevelId.Snow);
        Check(LevelRun.Seed == 4137, "district changes retain the reported seed");
        watcher.SelectLevel(LevelId.Fields);
        yield return null;
        yield return null;
        Check(WaveVarietyAudit.Signature(DistrictWaves.Create(2)) == original, "returning to a district reproduces its exact wave");
        watcher.NewSet();
        Check(LevelRun.Seed != 4137, "new set explicitly changes the seed");
        watcher.SetSeed(4137);
        watcher.SelectLoop(3);
        Check(LevelRun.Stage == 7, "loop preview uses real run difficulty");
        watcher.SelectLoop(1);
        while (!WavePreparation.Ready)
            yield return null;
        yield return null;
        yield return null;
        watcher.TogglePause();
        yield return null;
        yield return null;
        float stopped = watcher.Elapsed;
        yield return new WaitForSecondsRealtime(.1f);
        Check(watcher.Elapsed == stopped && Time.timeScale == 0, "pause freezes the timeline");
        watcher.StepOnce();
        yield return null;
        yield return null;
        yield return null;
        Check(Mathf.Abs(watcher.Elapsed - stopped - 1f / 60) < .001f && Time.timeScale == 0, $"single step advances exactly one sixtieth then freezes (delta {watcher.Elapsed - stopped:F6}, scale {Time.timeScale}, capture {Time.captureDeltaTime})");
        watcher.TogglePause();
        watcher.CycleSpeed();
        watcher.CycleSpeed();
        var seen = new HashSet<Type>();
        int valid = 0, unavailable = 0;
        foreach (var level in new[] { LevelId.Fields, LevelId.Snow, LevelId.City, LevelId.Thunder, LevelId.Tunnel })
        {
            watcher.SelectLevel(level);
            watcher.SelectView(WatcherView.Waves);
            Check(watcher.Entries.Count == 19, level + " retains access to all budgets, including extras outside the shortened run");
            Button("PAGE >").onClick.Invoke();
            yield return null;
            Check(watcher.GetComponentsInChildren<Button>().Any(x => x.name == "20"), level + " last catalog page exposes wave 20");
            watcher.SelectView(WatcherView.Formations);
            foreach (var part in WavePartLibrary.For(level))
            {
                foreach (int side in new[] { -1, 1 })
                {
                    int count = 0;
                    for (int variant = 0; variant < 13; variant++)
                    {
                        var wave = WaveComposer.PreviewPart(part, variant, side, out int resolved);
                        if (wave == null)
                        {
                            unavailable++;
                            continue;
                        }
                        if (!WaveGeometry.Clear(WaveVarietyAudit.Tracks(wave)))
                            throw new Exception("Unsafe preview " + level + " / " + part.Name);
                        if (wave.units.Any(x => x.side != side || x.part != part.Name))
                            throw new Exception("Preview changed its selected part or mirror");
                        valid++;
                        count++;
                    }
                    Check(count > 0, level + " / " + part.Name + " has selectable safe variants facing " + side);
                }
            }
            watcher.SelectEntry(watcher.Entries.Count - 1);
            watcher.SelectVariant(12);
            watcher.ToggleMirror();
            while (!WavePreparation.Ready)
                yield return null;
            yield return null;
            yield return null;
            yield return Clip(level + "CatalogPart", 5, false);
            yield return Capture(level + "Catalog");
            LabelsFit(watcher);
            Check(watcher.ReviewReference.Contains("variant=13/13") && watcher.ReviewReference.Contains("seed=4137"), "formation reference identifies variant, mirror and seed");
            watcher.SelectView(WatcherView.Enemies);
            for (int i = 0; i < watcher.Entries.Count; i++)
            {
                seen.Add(watcher.Entries[i].Enemy);
                watcher.SelectEntry(i);
                yield return null;
                yield return null;
                Check(watcher.Spawned == 1 && AEnemy.Live.Any(x => x.GetType() == watcher.SelectedEntry.Enemy), watcher.SelectedEntry.Id + " spawns its actual runtime model and behavior");
            }
            watcher.SelectView(WatcherView.Bosses);
            watcher.SelectEncounter(WaveWatcher.BossTwo);
            yield return Clip(level + "CatalogBoss", level == LevelId.Tunnel ? 11 : 5);
            LabelsFit(watcher);
            if (level == LevelId.City)
            {
                Check(AEnemy.Live.OfType<CityDuoBoss>().Count() == 1 && AEnemy.Live.OfType<CityDuoBoss>().Single().heavy, "City heavy survivor is available");
                watcher.SelectEncounter(WaveWatcher.BossOther);
                yield return new WaitForSeconds(1);
                Check(AEnemy.Live.OfType<CityDuoBoss>().Count() == 1 && !AEnemy.Live.OfType<CityDuoBoss>().Single().heavy, "City fast survivor is available");
                yield return Capture("CityFastSurvivor");
            }
        }
        Check(seen.SetEquals(DistrictWaves.EnemyTypes), "enemy browser covers every registered enemy type exactly");
        Check(valid > 0, "audited " + valid + " safe mirrored formation variants; " + unavailable + " unavailable layouts reported explicitly");
        watcher.SelectLevel(LevelId.Thunder);
        watcher.SelectView(WatcherView.Projectiles);
        while (!WavePreparation.Ready)
            yield return null;
        yield return null;
        yield return null;
        for (int i = 0; i < watcher.Entries.Count; i++)
        {
            watcher.SelectEntry(i);
            yield return null;
            yield return null;
            yield return null;
            Check(CombatRound.Live.Count > 0, watcher.SelectedEntry.Id + " uses real projectiles");
            LabelsFit(watcher);
            foreach (var beam in CombatRound.Live.OfType<PulseLaser>())
                Check(beam.IsAttachedTo(FindObjectsByType<DistrictEnemy>().Single().Hull), "projectile preview beams retain an attached emitter");
        }
        watcher.SelectEntry(0);
        watcher.ToggleLoop();
        yield return new WaitForSeconds(10);
        Check(CombatRound.Live.Count == 0, "repeat off plays a projectile sample once and lets its whole tail clear");
        watcher.ToggleLoop();
        yield return null;
        yield return null;
        yield return null;
        Check(CombatRound.Live.Count > 0, "repeat on resumes the projectile sample");
        watcher.SelectEntry(watcher.Entries.Count - 1);
        yield return null;
        yield return null;
        yield return null;
        yield return Clip("BeamCatalog", 3);
        var clipboard = GUIUtility.systemCopyBuffer;
        watcher.CopyForChat();
        yield return null;
        yield return null;
        yield return null;
        Check(watcher.Paused && GUIUtility.systemCopyBuffer.Contains("projectile/side-ports") && File.Exists("WaveWatcherFeedback/Latest.txt") && File.Exists("WaveWatcherFeedback/Latest.png"), "copy for chat freezes and saves a precise review reference and screenshot");
        GUIUtility.systemCopyBuffer = clipboard;
        watcher.TogglePause();
        watcher.SelectView(WatcherView.Formations);
        watcher.SelectEntry(watcher.Entries.Count - 1);
        watcher.ToggleTour();
        watcher.NextEntry(1);
        Check(watcher.View == WatcherView.Enemies && watcher.Selection == 0, "tour advances between full catalogs");
        watcher.SelectView(WatcherView.Projectiles);
        watcher.SelectEntry(watcher.Entries.Count - 1);
        watcher.NextEntry(1);
        Check(watcher.Level == LevelId.Tunnel && watcher.View == WatcherView.Waves && watcher.Selection == 0, "tour advances to the next district after its final catalog");
        watcher.ToggleTour();
        watcher.Exit();
        deadline = Time.realtimeSinceStartup + 30;
        while ((SceneManager.GetActiveScene().name != "Title" || CloudTransition.Busy) && Time.realtimeSinceStartup < deadline)
            yield return null;
        Check(WaveWatcher.Current == null && Time.timeScale == 0 && Time.captureDeltaTime == 0, "exit restores paused title timing and disposes the watcher");
    }

    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder + name + ".jpg", image.EncodeToJPG(88));
        Destroy(image);
    }

    IEnumerator Clip(string name, float seconds, bool requireFire = true)
    {
        Directory.CreateDirectory(Folder + name);
        float start = Time.time, next = 0;
        int frames = 0, peak = 0, visiblePeak = 0;
        bool moved = false;
        var positions = new Dictionary<AEnemy, Vector3>();
        while (Time.time - start < seconds)
        {
            peak = Mathf.Max(peak, CombatRound.Live.Count);
            int visible = 0;
            foreach (var enemy in AEnemy.Live)
            {
                if (!WaveGeometry.Visible(enemy.transform.position, enemy.size))
                    continue;
                visible++;
                if (positions.TryGetValue(enemy, out var prior) && Vector2.Distance(prior, enemy.transform.position) > .01f)
                    moved = true;
                positions[enemy] = enemy.transform.position;
            }
            visiblePeak = Mathf.Max(visiblePeak, visible);
            if (Time.time - start >= next)
            {
                yield return Capture(name + "/" + (frames++).ToString("D4"));
                next += .1f;
            }
            yield return null;
        }
        if (requireFire)
            Check(peak > 0, name + " shows actual live enemy shooting (peak " + peak + " bullets)");
        else
            Check(visiblePeak > 0 && moved, name + " shows actual moving models (peak " + visiblePeak + " ships, " + peak + " bullets)");
    }

    IEnumerator Run()
    {
        if (SessionState.GetBool("WaveWatcherReloadSetup", false))
        {
            SessionState.SetBool("WaveWatcherReloadSetup", false);
            if (SceneManager.GetActiveScene().name != "Title")
                yield return SceneManager.LoadSceneAsync("Title");
            yield return null;
            WaveWatcher.Launch();
            while (WaveWatcher.Current == null || !WaveWatcher.Current.Ready)
                yield return null;
            var preview = WaveWatcher.Current;
            preview.SelectLoop(2);
            preview.SetSeed(7119);
            preview.SelectLevel(LevelId.Snow);
            preview.SelectView(WatcherView.Formations);
            preview.SelectEntry(7);
            preview.SelectVariant(5);
            preview.ToggleMirror();
            preview.SetAim(1);
            preview.CycleSpeed();
            preview.ToggleLoop();
            preview.TogglePause();
            while (!WavePreparation.Ready)
                yield return null;
            SessionState.SetBool("WaveWatcherReloadTest", true);
            SessionState.SetBool("WaveWatcherReview", false);
            SessionState.SetString("WaveWatcherReloadErrors", "");
            File.WriteAllText("Assets/WatcherReloadTrigger.cs", "internal static class WatcherReloadTrigger { } // " + Guid.NewGuid());
            AssetDatabase.Refresh();
            while (true)
                yield return null;
        }
        if (SessionState.GetBool("WaveWatcherCatalogReview", false))
        {
            yield return CatalogReview();
            yield break;
        }
        if (SceneManager.GetActiveScene().name != "Title")
            yield return SceneManager.LoadSceneAsync("Title");
        yield return null;
        yield return new WaitForSecondsRealtime(.3f);
        var entry = FindAnyObjectByType<WaveWatcherMenu>();
        Check(entry != null, "title menu exposes Wave Watcher");
        yield return Capture("TitleEntry");
        entry.OnAccept();
        float deadline = Time.realtimeSinceStartup + 55;
        while ((WaveWatcher.Current == null || !WaveWatcher.Current.Ready) && Time.realtimeSinceStartup < deadline)
            yield return null;
        var watcher = WaveWatcher.Current;
        Check(watcher != null && watcher.Ready, "title entry loads a ready live viewer after the cloud curtain");
        Canvas.ForceUpdateCanvases();
        foreach (var label in watcher.GetComponentsInChildren<TextMeshProUGUI>())
        {
            label.ForceMeshUpdate();
            Check(!label.isTextOverflowing && (label.text.Contains("\n") || label.textInfo.lineCount <= 1), "watcher label fits: " + label.text);
        }
        var ship = FindAnyObjectByType<Shipcontroller>();
        Check(!FindAnyObjectByType<WaveOrchestrer>().enabled, "ordinary wave progression is stopped in the viewer");
        Check(ship.ObserverMode && !ship.enabled && ship.GetComponentsInChildren<Renderer>().All(x => x.forceRenderingOff), "player hull, equipment and thrusters are invisible");
        Check(ship.GetComponentsInChildren<Collider2D>().All(x => !x.enabled), "invisible player cannot erase pattern bullets");
        float health = ship.health;
        ship.ReceiveHazardDamage(1000000);
        Check(ship.health == health && !GameOverState.Active, "invisible aim target is invulnerable even to beams");
        Check(!GameRules.playerDisabled, "enemy shooting stays enabled in observer mode");
        var round = CombatRound.Launch<SimpleRound>(ship.transform.position, Vector2.down, 0);
        yield return new WaitForSeconds(.15f);
        Check(round != null && round.gameObject.activeSelf, "hostile bullets pass through the hidden player");
        watcher.SetAim(-1);
        Check(ship.transform.position.x < 0, "left aim changes the real target");
        watcher.SetAim(1);
        Check(ship.transform.position.x > 0, "right aim changes the real target");
        watcher.SetAim(0);
        Button("PAUSE").onClick.Invoke();
        yield return null;
        yield return null;
        float stopped = watcher.Elapsed;
        yield return new WaitForSecondsRealtime(.25f);
        Check(watcher.Paused && Time.timeScale == 0 && watcher.Elapsed == stopped, "pause freezes real movement and bullets while UI remains usable");
        Button("PAUSE").onClick.Invoke();
        watcher.CycleSpeed();
        yield return null;
        yield return null;
        Check(Time.timeScale == .5f, "half-speed playback controls the actual simulation");
        watcher.CycleSpeed();
        yield return null;
        Check(watcher.PlaybackSpeed == 2, "double-speed playback is available");
        watcher.CycleSpeed();
        foreach (var level in SessionState.GetBool("WaveWatcherQuickReview", false) ? new[] { LevelId.Fields } : new[] { LevelId.Fields, LevelId.Snow, LevelId.City, LevelId.Thunder, LevelId.Tunnel })
        {
            watcher.SelectLevel(level);
            Check(watcher.Entries.Count == 19, level + " exposes every authored wave from 2 through 20");
            for (int wave = WaveWatcher.FirstWave; wave <= WaveWatcher.LastWave; wave++)
            {
                watcher.SelectEncounter(wave);
                yield return null;
                Check(watcher.Encounter == wave, level + " wave " + wave + " selectable");
            }
            watcher.SelectEncounter(2);
            yield return Clip(level + "Wave", DistrictWaves.Create(2).duration + .2f);
            Check(watcher.Spawned == DistrictWaves.Create(2).units.Count, level + " uses the full real wave recipe and spawn timing");
            watcher.SelectEncounter(WaveWatcher.Miniboss);
            yield return Clip(level + "Mini", 9);
            watcher.SelectEncounter(WaveWatcher.BossTwo);
            yield return Clip(level + "BossTwo", 12);
            if (level == LevelId.City)
                Check(FindObjectsByType<CityDuoBoss>().Length == 1, "City phase two shows the actual enraged survivor");
            if (level == LevelId.Tunnel)
                Check(FindObjectsByType<TunnelSawBoss>().Length == 3, "Tunnel phase two deploys the two real smaller saws");
        }
        watcher.SelectLevel(LevelId.Fields);
        watcher.SelectEncounter(2);
        yield return new WaitForSeconds(.2f);
        float elapsed = watcher.Elapsed;
        Button("REPLAY").onClick.Invoke();
        yield return null;
        yield return null;
        Check(watcher.Elapsed < elapsed, "replay resets the real wave timeline");
        watcher.ToggleLoop();
        Check(!watcher.Looping, "looping can be disabled");
        watcher.ToggleLoop();
        watcher.CycleSpeed();
        watcher.CycleSpeed(); // Two-times simulation makes the full hazard-tail check faster.
        bool repeated = false;
        float previous = watcher.Elapsed;
        deadline = Time.realtimeSinceStartup + 25;
        while (!repeated && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
            repeated = previous > 1 && watcher.Elapsed < previous;
            previous = watcher.Elapsed;
        }
        Check(repeated, "loop replays only after ships and their real projectile tails have cleared");
        watcher.CycleSpeed();
        yield return Capture("Controls");
        Button("BACK").onClick.Invoke();
        deadline = Time.realtimeSinceStartup + 30;
        while ((SceneManager.GetActiveScene().name != "Title" || CloudTransition.Busy) && Time.realtimeSinceStartup < deadline)
            yield return null;
        Check(SceneManager.GetActiveScene().name == "Title" && WaveWatcher.Current == null, "Back returns to title and disposes the viewer");
        {
            var reviewStart = FindAnyObjectByType<NewGame>();
            reviewStart.OnAccept();
            reviewStart.Choose(1);
        }
        deadline = Time.realtimeSinceStartup + 30;
        while ((SceneManager.GetActiveScene().name != "L1" || CloudTransition.Busy) && Time.realtimeSinceStartup < deadline)
            yield return null;
        yield return null;
        ship = FindAnyObjectByType<Shipcontroller>();
        Check(WaveWatcher.Current == null && ship.enabled && !ship.ObserverMode, "normal START has a visible controllable vulnerable player");
        Check(FindAnyObjectByType<WaveOrchestrer>().enabled && Time.timeScale == 1, "normal START restores ordinary combat and time");
    }
}
#endif
