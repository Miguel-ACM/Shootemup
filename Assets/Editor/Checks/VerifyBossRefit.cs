#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class VerifyBossRefit
{
    public const string Folder = "ArtSource/BossRefitReview";
    const string Key = "BossRefitReview";
    static double deadline;
    static VerifyBossRefit()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            deadline = EditorApplication.timeSinceStartup + 300;
            new GameObject("Boss refit review").AddComponent<BossRefitReview>();
        };
        EditorApplication.update += () => {
            if (deadline == 0 || !SessionState.GetBool(Key, false)) return;
            string path = Folder + "/Verification.txt";
            if (!File.Exists(path) && EditorApplication.timeSinceStartup < deadline) return;
            SessionState.SetBool(Key, false);
            if (!File.Exists(path)) File.WriteAllText(path, "FAIL timeout");
            CheckHarness.Exit(File.ReadAllText(path).StartsWith("PASS") ? 0 : 1);
        };
    }
    static void Guard()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/Temp/DecisionHoverReview/"))
            throw new Exception("Use the isolated review project");
    }
    public static void Run()
    {
        Guard(); Directory.CreateDirectory(Folder); File.Delete(Folder + "/Verification.txt");
        PlayerSettings.companyName = "ShootemupReview"; PlayerSettings.productName = "BossRefitReview";
        try
        {
            var baseline = JsonUtility.FromJson<CaptureDifficultyBaseline.Snapshot>(File.ReadAllText(Folder + "/FormerEasy.json"));
            var normal = CaptureDifficultyBaseline.Read(RunDifficulty.Normal);
            if (JsonUtility.ToJson(baseline) != JsonUtility.ToJson(normal)) throw new Exception("Normal differs from the recorded former Easy configuration");
            File.WriteAllText(Folder + "/NormalParity.txt", "PASS\nNormal exactly preserves former Easy: global factors, " + normal.actors.Count + " actor configurations and " + normal.waves.Count + " production wave signatures.\n");
            foreach (RunDifficulty mode in new[] { RunDifficulty.Easy, RunDifficulty.Normal, RunDifficulty.Hard })
            {
                GameRules.StartNewRun(mode);
                for (int i = 0; i < 100; i++) Score.IncreaseScore(1);
                if (Score.CurrentScore != (int)(100 * Difficulty.ScoreFactor)) throw new Exception("Small-score scaling failed: " + mode);
                if (Score.CurrentMultiplier != 1) throw new Exception("Difficulty scaled combo growth");
                RunCoins.Add(7); Score.IncreaseScore(100);
                if (RunCoins.Count != 7) throw new Exception("Difficulty changed the run wallet");
            }
            File.WriteAllText(Folder + "/Score.txt", "PASS\n100 one-point awards produce Easy=50, Normal=100, Hard=200. Combo growth and the coin wallet stay independent.\n");
            var regressions = new ReviewRegressionTests();
            foreach (var mode in new[] { RunDifficulty.Easy, RunDifficulty.Normal, RunDifficulty.Hard })
                regressions.MultipartMetersTrackPhaseObjectives(mode);
            regressions.PhaseInvulnerabilityRejectsDirectHitsBeforeEffectsAndScore();
            regressions.HomingSkipsProtectedHullAndCore();
            File.WriteAllText(Folder + "/Regression.txt", "PASS\nUpdated multipart meter cases on all three tiers, phase damage eligibility and detached-target homing regressions.\n");
            RenderStations();
            GameRules.StartNewRun(); LevelRun.Reset(LevelId.Fields, 17381);
            typeof(VerifySecondaryItems).GetMethod("NativeGameView", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            SessionState.SetBool(Key, true);
            SessionState.SetString(Key + "Focus", VerifyCombatRework.Argument("--refit-focus", "all"));
            EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/L1.unity");
            EditorApplication.isPlaying = true;
        }
        catch (Exception e) { File.WriteAllText(Folder + "/Verification.txt", "FAIL\n" + e); EditorApplication.Exit(1); }
    }
    public static string Focus => SessionState.GetString(Key + "Focus", "all");
    public static void Menu()
    {
        Guard(); Directory.CreateDirectory(Folder); File.Delete(Folder + "/Verification.txt");
        PlayerSettings.companyName = "ShootemupReview"; PlayerSettings.productName = "BossRefitReview";
        typeof(VerifySecondaryItems).GetMethod("NativeGameView", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        SessionState.SetBool(Key, true); SessionState.SetString(Key + "Focus", "menu");
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Title.unity");
        EditorApplication.isPlaying = true;
    }
    static void RenderStations()
    {
        var camera = new GameObject("Station model camera").AddComponent<UnityEngine.Camera>();
        camera.transform.position = new Vector3(1000, 0, -12);
        camera.orthographic = true; camera.orthographicSize = 2.8f;
        camera.cullingMask = 1 << 31; camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.025f, .04f, .06f);
        var target = new RenderTexture(800, 600, 24); camera.targetTexture = target;
        for (int version = 0; version < 2; version++)
        {
            var root = new GameObject("Station model").transform; root.position = new Vector3(1000, 0, 0);
            if (version == 0)
            {
                var model = Model(root, IndustrialMeshFactory.TerraModel("Hull45"), Vector3.zero);
                IndustrialMeshFactory.AttachTerraParts("Hull45", model, SpaceMaterials.Fleet);
                for (int i = 0; i < 5; i++)
                    Model(root, IndustrialMeshFactory.SpaceModule(i < 2 ? 0 : i < 4 ? 1 : 2), i < 2 ? new Vector3(i == 0 ? -1.95f : 1.95f, .25f, -.08f)
                        : i < 4 ? new Vector3(i == 2 ? -1.13f : 1.13f, -1, -.12f) : new Vector3(0, 0, -.08f));
            }
            else
            {
                Model(root, IndustrialMeshFactory.CitadelRefit(), Vector3.zero);
                Model(root, IndustrialMeshFactory.CitadelReactor(), new Vector3(0, 0, -.38f));
            }
            foreach (var child in root.GetComponentsInChildren<Transform>()) child.gameObject.layer = 31;
            for (int view = 0; view < 2; view++)
            {
                root.rotation = Quaternion.Euler(view == 0 ? 0 : -18, view == 0 ? 0 : 25, 0);
                camera.Render(); var previous = RenderTexture.active; RenderTexture.active = target;
                var png = new Texture2D(800, 600, TextureFormat.RGB24, false);
                png.ReadPixels(new Rect(0, 0, 800, 600), 0, 0); png.Apply();
                File.WriteAllBytes(Folder + (version == 0 ? "/SpaceBefore" : "/SpaceAfter") + view + ".png", png.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(png); RenderTexture.active = previous;
            }
            UnityEngine.Object.DestroyImmediate(root.gameObject);
        }
        UnityEngine.Object.DestroyImmediate(camera.gameObject); target.Release(); UnityEngine.Object.DestroyImmediate(target);
    }
    static Transform Model(Transform parent, Mesh mesh, Vector3 position)
    {
        var go = new GameObject(mesh.name); go.transform.SetParent(parent, false); go.transform.localPosition = position;
        go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = SpaceMaterials.Fleet;
        return go.transform;
    }
    public static void Build()
    {
        Guard();
        if (!File.ReadAllText(Folder + "/Verification.txt").StartsWith("PASS")) throw new Exception("Review first");
        PlayerSettings.companyName = "DefaultCompany"; PlayerSettings.productName = "seup";
        string output = VerifyCombatRework.Argument("--refit-output", "");
        if (!Path.IsPathRooted(output) || Path.GetExtension(output) != ".exe") throw new Exception("Absolute exe path required");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = BuildTarget.StandaloneWindows64, locationPathName = output });
        bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        File.WriteAllText(Folder + "/Build.txt", (ok ? "PASS" : "FAIL") + "\nWindows x64: " + output + "\nErrors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings + "\n");
        EditorApplication.Exit(ok ? 0 : 1);
    }
}

public sealed class BossRefitReview : MonoBehaviour
{
    readonly List<string> checks = new List<string>(), errors = new List<string>();
    Shipcontroller ship;
    WaveOrchestrer flow;
    void Awake() { DontDestroyOnLoad(gameObject); Application.runInBackground = true; Application.logMessageReceived += Log; }
    void OnDestroy() { Application.logMessageReceived -= Log; }
    void Log(string text, string trace, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception) && !trace.Contains("UnityEditor.Search")) errors.Add(text + "\n" + trace);
    }
    void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        checks.Add(message); File.WriteAllLines(VerifyBossRefit.Folder + "/Progress.txt", checks);
    }
    static void Set(object target, string field, object value) => target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    void Clear()
    {
        foreach (var shop in FindObjectsByType<DecisionShop>()) shop.Abort();
        foreach (var e in AEnemy.Live.ToArray()) if (e != null && e.GetComponentInParent<DistrictBoss>() == null) Destroy(e.gameObject);
        foreach (var e in FindObjectsByType<DistrictBoss>()) Destroy(e.gameObject);
        foreach (var item in AItem.Available.ToArray()) if (item != null) Destroy(item.gameObject);
        foreach (var p in FindObjectsByType<APickable>()) Destroy(p.gameObject);
        CombatRound.ClearAll(); foreach (var p in FindObjectsByType<PlayerRound>()) p.Despawn();
        CombatDecision.End();
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(VerifyBossRefit.Folder + "/" + name + ".png", image.EncodeToPNG()); Destroy(image);
    }
    IEnumerator Start()
    {
        var stack = new Stack<IEnumerator>(); stack.Push(Run());
        while (stack.Count > 0)
        {
            bool more = false; object step = null; Exception failure = null;
            try { more = stack.Peek().MoveNext(); if (more) step = stack.Peek().Current; } catch (Exception e) { failure = e; }
            if (failure != null) { errors.Add(failure.ToString()); break; }
            if (!more) { stack.Pop(); continue; }
            if (step is IEnumerator nested) { stack.Push(nested); continue; } yield return step;
        }
        Time.timeScale = 1;
        File.WriteAllText(VerifyBossRefit.Folder + "/Verification.txt", (errors.Count == 0 ? "PASS" : "FAIL") + "\n" + string.Join("\n", checks) + "\n" + string.Join("\n", errors));
    }
    IEnumerator Run()
    {
        if (VerifyBossRefit.Focus == "menu") { yield return Menu(); yield break; }
        float deadline = Time.realtimeSinceStartup + 60;
        while ((flow = FindAnyObjectByType<WaveOrchestrer>()) == null && Time.realtimeSinceStartup < deadline) yield return null;
        Check(flow != null, "isolated run controller loaded");
        flow.enabled = false; flow.CancelInvoke(); flow.StopAllCoroutines();
        ship = FindAnyObjectByType<Shipcontroller>(); ship.ObserverMode = true;
        while ((CloudTransition.Busy || !WavePreparation.Ready) && Time.realtimeSinceStartup < deadline) yield return null;
        Clear(); yield return null; GameRules.playerDisabled = false; Time.timeScale = 1;
        ship.transform.position = new Vector3(0, Camera.DownLimit + 1.7f, -1);
        foreach (var gun in ship.GetComponentsInChildren<AWeapon>()) gun.enabled = false;
        foreach (var mode in new[] { RunDifficulty.Easy, RunDifficulty.Normal, RunDifficulty.Hard })
        {
            if (VerifyBossRefit.Focus == "all" || VerifyBossRefit.Focus == "space") yield return Boss(LevelId.Space, mode);
            if (VerifyBossRefit.Focus == "all" || VerifyBossRefit.Focus == "head") yield return Boss(LevelId.Hell, mode);
        }
        Check(errors.Count == 0, "no runtime errors");
    }
    IEnumerator Menu()
    {
            yield return new WaitForSecondsRealtime(.8f);
            var menu = FindAnyObjectByType<NewGame>(); Check(menu != null, "title Continue row exists");
            menu.OnAccept(); yield return new WaitForSecondsRealtime(.3f);
            var labels = FindObjectsByType<DifficultyOption>().Select(o => o.GetComponentInChildren<TMPro.TextMeshProUGUI>().text).ToArray();
            Check(labels.Length == 4 && labels.Any(s => s.Contains("NORMAL")) && labels.Any(s => s.Contains("0.5×")) && labels.Any(s => s.Contains("2×")), "title offers all three difficulties and score factors");
            yield return Capture("DifficultyMenu"); menu.Choose(3); yield return null;
            Check(FindObjectsByType<DifficultyOption>().Length == 0, "Back returns from difficulty selection");
        Check(errors.Count == 0, "no runtime errors");
    }
    IEnumerator Boss(LevelId level, RunDifficulty mode)
    {
        Clear(); yield return null;
        GameRules.StartNewRun(mode); LevelRun.Reset(level, 17381);
        LevelEnvironment.Apply();
        while (!WavePreparation.Ready) yield return null;
        Set(flow, "choosingRoute", false);
        var boss = (DistrictBoss)DistrictEnemy.Spawn(level == LevelId.Space ? typeof(SpaceCitadelBoss) : typeof(HellCrabBoss), new Vector3(0, Camera.UpLimit - 3, 1));
        yield return null;
        AEnemy part;
        if (boss is SpaceCitadelBoss station)
        {
            part = station.Reactor;
            Check(station.Parts.Length == 1 && part != null && station.GetComponentsInChildren<AEnemy>().Length == 2, mode + " Space has exactly one central weakpoint");
            float hp = station.health; station.damage(100); Check(station.health == hp, "Space outer armor cannot bypass the reactor");
            PlayerRound.Spawn(station.Hull.TransformPoint(new Vector3(1.8f, -.7f, 0)), Vector2.up, 5, 8);
            yield return new WaitForSeconds(.15f); Check(station.health == hp, "native shots at exterior station armor do no damage");
        }
        else
        {
            var crab = (HellCrabBoss)boss; part = crab.Head;
            var hit = part.GetComponent<PolygonCollider2D>();
            foreach (float x in new[] { -1.1f, 0, 1.1f })
                Check(hit.OverlapPoint(crab.Hull.TransformPoint(new Vector3(x, .1f, 0))), mode + " crab head covers cheek/center x=" + x);
            Check(crab.GetComponentsInChildren<AEnemy>().Length == 2, "crab eyes and pincers have no separate weakpoints");
        }
        float before = part.health;
        for (int i = -1; i <= 1; i++)
            PlayerRound.Spawn(part.transform.position + new Vector3(i * (level == LevelId.Hell ? .95f : .2f), -.7f, 0), Vector2.up, 3, 10);
        yield return new WaitForSeconds(.25f);
        Check(part.health < before && Mathf.Abs(part.health - boss.health) < .01f, mode + " " + level + " native hits update the shared boss health");
        Check(CombatUpgrades.Nearest(part.transform.position, null, 1) == part, "homing targets the actual weakpoint");
        yield return new WaitForSeconds(3);
        Check(CombatRound.Live.Count > 0 && CombatRound.Live.Count <= CombatRound.Limit, mode + " " + level + " phase one fires within the existing pool");
        yield return Capture(level.ToString() + mode + "Phase1");
        part.damage(boss.health * .56f); yield return null;
        Check(boss.CurrentPhase == 2 && boss.Invulnerable, "shared health triggers a protected second phase");
        int score = Score.CurrentScore; before = part.health;
        Check(CombatHit.Apply(part, 50, part.transform.position, Vector2.up, fire: true) == 0 && part.health == before && Score.CurrentScore == score && part.GetComponent<BurningHull>() == null,
            "phase break rejects weakpoint damage, coatings and score");
        yield return new WaitForSeconds(1.8f);
        Check(!boss.Invulnerable && part.CanReceiveDamage && boss.PhaseCount == (mode == RunDifficulty.Hard ? 3 : 2), mode + " has the correct phase count and resumes taking hits");
        yield return new WaitForSeconds(3); yield return Capture(level.ToString() + mode + "Phase2");
        if (mode == RunDifficulty.Hard)
        {
            part.damage(boss.health * .51f); yield return null; yield return new WaitForSeconds(1.8f);
            Check(boss.CurrentPhase == 3 && part.CanReceiveDamage, level + " retains its Hard-only third phase");
            yield return new WaitForSeconds(2); yield return Capture(level + "HardPhase3");
        }
        if (mode == RunDifficulty.Normal)
        {
            boss.SetOrchestrer(flow, 900 + (int)level); part.damage(1000000); yield return null;
            Check(BossDestruction.Current != null && BossDestruction.Current.Wreckage != null, level + " keeps its intact-hull finale and prepared breakup");
            float until = Time.unscaledTime + 5;
            while (FindAnyObjectByType<DecisionShop>() == null && Time.unscaledTime < until) yield return null;
            Check(FindAnyObjectByType<DecisionShop>() != null, level + " victory still opens its mandatory item reward");
            yield return new WaitForSeconds(.8f);
        }
        Clear(); yield return null;
    }
}
#endif
