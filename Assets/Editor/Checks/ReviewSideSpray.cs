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
using Unity.Profiling;

[InitializeOnLoad]
public static class ReviewSideSpray
{
    public static string Folder => Path.GetFullPath("ReviewOutput/RapidSideSpray");
    static ReviewSideSpray()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("ReviewSideSpray", false)) return;
            MetaProgress.ReviewSaveDirectory = Folder;
            new GameObject("Side spray review").AddComponent<SideSprayReviewRuntime>();
        };
    }
    static void Guard()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\', '/').EndsWith("/Temp/UnlockReview/Assets"))
            throw new Exception("Use the isolated command-line review checkout.");
        Directory.CreateDirectory(Folder);
    }
    public static void Run() => StartReview(false);
    public static void RunProbes() => StartReview(true);
    static void StartReview(bool probesOnly)
    {
        Guard();
        SessionState.SetBool("ReviewSideSpray.ProbesOnly", probesOnly);
        PlayerSettings.companyName = "ShootemupReview";
        PlayerSettings.productName = "RapidSideSprayReview";
        MetaProgress.ReviewSaveDirectory = Folder;
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("ReviewSideSpray", true);
        EditorApplication.isPlaying = true;
    }
    public static void Build()
    {
        Guard();
        PlayerSettings.companyName = "DefaultCompany";
        PlayerSettings.productName = "seup";
        // Reuse this checkout's existing player output instead of filling the drive.
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = BuildTarget.StandaloneWindows64, locationPathName = "Build/VoidBackdrop/Shootemup.exe", options = BuildOptions.None
        });
        File.WriteAllText(Folder + "/Build.txt", report.summary.result + "\nErrors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings);
        EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
    public static void RenderAndBuild()
    {
        Guard();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Difficulty.Select(RunDifficulty.Normal);
        LevelRun.Preview(LevelId.Snow, 20261106, 1);
        var camera = new GameObject("Offscreen projectile camera").AddComponent<UnityEngine.Camera>();
        camera.transform.position = new Vector3(0, 0, -20);
        camera.orthographic = true; camera.orthographicSize = 8.850766f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.035f, .045f, .075f);
        camera.cullingMask = 1 << 31; camera.allowHDR = false;
        var target = new RenderTexture(405, 720, 24); camera.targetTexture = target;
        var age = typeof(CombatRound).GetField("age", BindingFlags.Instance | BindingFlags.NonPublic);
        var giantTick = typeof(GiantOrb).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);
        var simpleTick = typeof(CombatRound).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);
        var program = typeof(CombatRound).GetMethod("Program", BindingFlags.Instance | BindingFlags.NonPublic);
        var shots = new List<CombatRound>();
        var pixels = new Texture2D(405, 720, TextureFormat.RGB24, false);
        Directory.CreateDirectory(Folder + "/Motion");
        UnityEngine.Random.InitState(20261106);
        CombatRound.Launch<GiantOrb>(new Vector3(0, 6.7f), Vector2.down, 1.65f, .66f).SideSpray(1.2f);
        for (int frame = 0; frame < 90; frame++)
        {
            for (int step = 0; step < 4; step++)
            {
                shots.Clear(); shots.AddRange(CombatRound.Live);
                foreach (var shot in shots)
                {
                    age.SetValue(shot, (float)age.GetValue(shot) + 1f / 60);
                    (shot is GiantOrb ? giantTick : simpleTick).Invoke(shot, new object[] { 1f / 60 });
                    program.Invoke(shot, null);
                }
            }
            foreach (var shot in CombatRound.Live) shot.gameObject.layer = 31;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 405, 720), 0, 0); pixels.Apply();
            File.WriteAllBytes(Folder + "/Motion/" + frame.ToString("D3") + ".png", pixels.EncodeToPNG());
        }
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(pixels);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Build();
    }
}

public sealed class SideSprayReviewRuntime : MonoBehaviour
{
    static readonly MethodInfo release = typeof(CombatRound).GetMethod("Release", BindingFlags.NonPublic | BindingFlags.Instance);
    readonly List<string> report = new();
    readonly List<string> errors = new();
    readonly List<double> frames = new(1000), physics = new(1000);
    readonly List<CombatRound> scratch = new(3600);
    void Check(bool okay, string message)
    {
        report.Add((okay ? "PASS " : "FAIL ") + message);
        if (!okay) errors.Add(message);
        File.WriteAllLines(ReviewSideSpray.Folder + "/Progress.txt", report);
    }
    void Log(string message, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error) errors.Add(message + "\n" + stack);
    }
    IEnumerator Ready()
    {
        yield return null;
        while (CloudTransition.Busy || !WavePreparation.Ready) yield return null;
    }
    void Clear()
    {
        scratch.Clear(); scratch.AddRange(CombatRound.Live);
        foreach (var round in scratch) release.Invoke(round, null);
    }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        Application.logMessageReceived += Log;
        bool probesOnly = SessionState.GetBool("ReviewSideSpray.ProbesOnly", false);
        if (probesOnly) report.AddRange(File.ReadAllLines(ReviewSideSpray.Folder + "/EncounterChecks.txt"));
        QualitySettings.renderPipeline = ScriptableObject.CreateInstance<CelestialNoRenderAsset>();
        AudioListener.pause = true;
        Time.captureDeltaTime = 1f / 60;
        while (CloudTransition.Busy || Time.frameCount < 5) yield return null;
        WaveWatcher.Launch();
        while (WaveWatcher.Current == null || !WaveWatcher.Current.Ready || CloudTransition.Busy || !WavePreparation.Ready) yield return null;
        var watcher = WaveWatcher.Current;
        if (watcher.Looping) watcher.ToggleLoop();
        watcher.SelectView(WatcherView.Bosses); yield return Ready();
        foreach (var level in probesOnly ? Array.Empty<LevelId>() : new[] { LevelId.Snow, LevelId.Candyland })
        {
            watcher.SelectLevel(level); yield return Ready();
            watcher.SelectEncounter(WaveWatcher.Miniboss); yield return Ready();
            foreach (var difficulty in new[] { RunDifficulty.Easy, RunDifficulty.Normal, RunDifficulty.Hard })
            {
                watcher.SelectDifficulty(difficulty); yield return Ready();
                DistrictMiniboss boss = null;
                for (int guard = 0; guard < 1200 && (boss == null || !boss.Engaged); guard++)
                { boss = FindAnyObjectByType<DistrictMiniboss>(); yield return null; }
                Check(boss != null && boss.Engaged, level + " " + difficulty + " miniboss engages.");
                int created = CombatRound.CreatedCount, giantPeak = 0, childPeak = 0;
                for (int frame = 0; frame < 720; frame++)
                {
                    yield return null;
                    int giants = 0, children = 0;
                    foreach (var round in CombatRound.Live)
                    {
                        if (round is GiantOrb) giants++;
                        if (round.generation == 1 && round is SimpleRound) children++;
                    }
                    giantPeak = Mathf.Max(giantPeak, giants); childPeak = Mathf.Max(childPeak, children);
                }
                Check(giantPeak >= 2 && childPeak > 0, $"{level} {difficulty}: giant peak {giantPeak}, side-round peak {childPeak}.");
                Check(CombatRound.CreatedCount == created, level + " " + difficulty + " uses warmed projectile objects only.");
            }
        }
        watcher.StopAllCoroutines(); watcher.enabled = false;
        foreach (var enemy in FindObjectsByType<DistrictEnemy>()) Destroy(enemy.gameObject);
        yield return null;
        Clear(); Difficulty.Select(RunDifficulty.Hard);

        GiantOrb orb = null;
        var rates = new Dictionary<RunDifficulty,int>();
        foreach (var difficulty in new[] { RunDifficulty.Hard, RunDifficulty.Normal, RunDifficulty.Easy })
        {
            Difficulty.Select(difficulty); UnityEngine.Random.InitState(6110603 + (int)difficulty);
            Vector2 heading = CombatVisuals.Rotate(Vector2.down, difficulty == RunDifficulty.Normal ? 16 : 0);
            Vector2 side = new Vector2(-heading.y, heading.x);
            orb = CombatRound.Launch<GiantOrb>(new Vector3(0, 2), heading, 0, .66f).SideSpray();
            orb.damage = 27; orb.generation = 2;
            bool warned = false, single = true, inherited = true, onRim = true, sameSide = false;
            int left = 0, right = 0, lastSide = 0;
            float lowAngle = 100, highAngle = -100, lowSpeed = 100, highSpeed = 0;
            var timings = new List<int>();
            for (int frame = 0; frame < 300; frame++)
            {
                yield return null;
                warned |= orb.GetComponent<SpriteRenderer>().color.r > 1.1f;
                scratch.Clear();
                foreach (var round in CombatRound.Live) if (round != orb) scratch.Add(round);
                if (scratch.Count == 0) continue;
                timings.Add(frame); single &= scratch.Count == 1;
                foreach (var round in scratch)
                {
                    int flank = Vector2.Dot(side, round.Heading) < 0 ? -1 : 1;
                    if (flank < 0) left++; else right++;
                    sameSide |= lastSide == flank; lastSide = flank;
                    float lean = Vector2.SignedAngle(side * flank, round.Heading);
                    float velocity = round.speed / (1.25f * GameRules.enemyBulletSpeed * LevelRun.EnemyShotSpeedScale);
                    lowAngle = Mathf.Min(lowAngle, lean); highAngle = Mathf.Max(highAngle, lean);
                    lowSpeed = Mathf.Min(lowSpeed, velocity); highSpeed = Mathf.Max(highSpeed, velocity);
                    inherited &= round.generation == 3 && round.damage == 27 && round.UsesPixelContact;
                    onRim &= Vector2.Distance(round.transform.position, orb.transform.position) > .5f;
                    release.Invoke(round, null);
                }
            }
            rates[difficulty] = timings.Count;
            int minimum = difficulty == RunDifficulty.Hard ? 40 : difficulty == RunDifficulty.Normal ? 20 : 7;
            Check(warned && timings.Count >= minimum && timings[0] >= 35, difficulty + ": initial warning, then " + timings.Count + " rapid individual shots in five seconds.");
            Check(single && left > 0 && right > 0 && sameSide, difficulty + ": random left/right choices, including repeated sides; left=" + left + ", right=" + right + ".");
            Check(lowAngle >= -9.01f && highAngle <= 9.01f && lowAngle < -1 && highAngle > 1 &&
                lowSpeed >= .899f && highSpeed <= 1.101f && highSpeed-lowSpeed > .04f,
                $"{difficulty}: skew {lowAngle:F2}..{highAngle:F2} degrees; speed {lowSpeed:F3}..{highSpeed:F3} of the authored velocity.");
            Check(timings.Zip(timings.Skip(1), (a, b) => b - a).Distinct().Count() > 1 && inherited && onRim,
                difficulty + ": irregular cadence, rim origins, inherited damage/generation and exact pixel contacts.");
            release.Invoke(orb, null);
        }
        Check(rates[RunDifficulty.Hard] > rates[RunDifficulty.Normal] * 1.4f && rates[RunDifficulty.Normal] > rates[RunDifficulty.Easy] * 1.5f,
            "Difficulty thins the same rapid stream through cadence.");
        Difficulty.Select(RunDifficulty.Hard);
        var ordinary = CombatRound.Launch<GiantOrb>(new Vector3(0, 2), Vector2.down, 0, .66f);
        Check(ReferenceEquals(orb, ordinary), "The same pooled object is reused as an ordinary orb.");
        for (int frame = 0; frame < 100; frame++) yield return null;
        Check(CombatRound.Live.Count == 1, "An ordinary reused orb never inherits side fire.");
        Clear();
        orb = CombatRound.Launch<GiantOrb>(new Vector3(0, 2), Vector2.down, 0, .66f).SideSpray()
            .Pause(.05f, .7f);
        for (int frame = 0; frame < 50; frame++) yield return null;
        Check(CombatRound.Live.Count == 1, "A paused spray waits through the hold and its renewed release warning.");
        Clear();
        orb = CombatRound.Launch<GiantOrb>(new Vector3(0, CombatRound.Border.yMax + .8f), Vector2.down, 0, .66f).SideSpray();
        for (int frame = 0; frame < 90; frame++) yield return null;
        Check(CombatRound.Live.Count == 1, "An offscreen orb does not release unseen side shots.");
        CombatRound.ClearAll();
        for (int frame = 0; frame < 90; frame++) yield return null;
        Check(CombatRound.Live.Count == 0, "A full clear leaves no delayed side releases.");
        Check(((GiantOrb)null).SideSpray() == null, "A launch refused by the live cap remains null-safe.");

        int beforeStress = CombatRound.CreatedCount;
        for (int i = 0; i < 800; i++)
        {
            var shot = CombatRound.Launch<GiantOrb>(new Vector3((i % 20 - 9.5f) * .35f, 6 - (i / 20) * .26f), Vector2.down, 0, .45f);
            if (i < 128) shot.SideSpray();
        }
        for (int i = 0; i < 2400; i++)
            CombatRound.Launch<SimpleRound>(new Vector3((i % 60 - 29.5f) * .12f, 7 - (i / 60) * .34f), Vector2.down, 0, .14f);
        using var main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "PlayerLoop", 1);
        using var native = ProfilerRecorder.StartNew(ProfilerCategory.Physics2D, "Physics2D.Simulate", 1,
            ProfilerRecorderOptions.Default | ProfilerRecorderOptions.SumAllSamplesInFrame);
        int peak = 0;
        for (int frame = 0; frame < 600; frame++)
        {
            yield return null;
            peak = Mathf.Max(peak, CombatRound.Live.Count);
            frames.Add(main.LastValue / 1000000.0); physics.Add(native.LastValue / 1000000.0);
        }
        Check(peak <= CombatRound.Limit && peak > 3400, "Dense side-spray stress respects the 3600-round cap; peak=" + peak + ".");
        Check(beforeStress == CombatRound.CreatedCount, "Dense stress creates no projectile objects after preparation.");
        frames.Sort(); physics.Sort();
        report.Add($"600 complete editor frames, 128 spraying giants / 800 total giants with 2400 ordinary rounds: CPU p95={frames[570]:F3}ms, max={frames[599]:F3}ms; native physics p95={physics[570]:F3}ms. Rendering excluded.");
        Clear(); report.AddRange(errors);
        File.WriteAllLines(ReviewSideSpray.Folder + "/Checks.txt", report);
        SessionState.SetBool("ReviewSideSpray", false);
        EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
    }
}
#endif
