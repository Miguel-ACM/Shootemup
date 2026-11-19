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
using UnityEngine.Rendering;
using Unity.Profiling;

[InitializeOnLoad]
public static class ReviewVoidGiants
{
    public const string Folder = "C:/Users/Miguel/Shootemup/ArtSource/VoidGiantsReview";
    static ReviewVoidGiants()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("ReviewVoidGiants", false)) return;
            MetaProgress.ReviewSaveDirectory = Path.GetFullPath("ReviewOutput/VoidGiants/Save");
            new GameObject("Void giant review").AddComponent<VoidGiantsReviewRuntime>();
        };
    }
    static void Guard()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\', '/').EndsWith("/Temp/UnlockReview/Assets"))
            throw new Exception("Use the isolated command-line review checkout.");
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory(Path.GetFullPath("ReviewOutput/VoidGiants/Save"));
    }
    public static void Run()
    {
        Guard(); PlayerSettings.companyName = "ShootemupReview"; PlayerSettings.productName = "VoidGiantsReview";
        MetaProgress.ReviewSaveDirectory = Path.GetFullPath("ReviewOutput/VoidGiants/Save");
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("ReviewVoidGiants", true); EditorApplication.isPlaying = true;
    }
    public static void Build()
    {
        Guard(); PlayerSettings.companyName = "DefaultCompany"; PlayerSettings.productName = "seup";
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = BuildTarget.StandaloneWindows64, locationPathName = "Build/VoidBackdrop/Shootemup.exe", options = BuildOptions.None
        });
        File.WriteAllText(Folder + "/Build.txt", result.summary.result + "\nErrors=" + result.summary.totalErrors + "; warnings=" + result.summary.totalWarnings);
        EditorApplication.Exit(result.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
}

public sealed class VoidGiantsReviewRuntime : MonoBehaviour
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly MethodInfo release = typeof(CombatRound).GetMethod("Release", Hidden);
    readonly List<string> report = new(), errors = new();
    RenderPipelineAsset visible, invisible;
    int capturedAt = -100, movieFrame;
    void Check(bool okay, string message)
    {
        report.Add((okay ? "PASS " : "FAIL ") + message); if (!okay) errors.Add(message);
        File.WriteAllLines(ReviewVoidGiants.Folder + "/Progress.txt", report);
    }
    IEnumerator Ready() { yield return null; while (CloudTransition.Busy || !WavePreparation.Ready) yield return null; }
    void Capture(string label)
    {
        QualitySettings.renderPipeline = visible;
        var camera = UnityEngine.Camera.main;
        var presentation = camera.GetComponent<PSXPresentation>();
        camera.Render();
        var target = RenderTexture.GetTemporary(1280, 720, 24);
        Graphics.Blit(presentation.SceneTexture, target, presentation.CompositeMaterial);
        var previous = RenderTexture.active; RenderTexture.active = target;
        var pixels = new Texture2D(405, 720, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect((1280-405)/2, 0, 405, 720), 0, 0); pixels.Apply();
        File.WriteAllBytes(label + ".png", pixels.EncodeToPNG()); Destroy(pixels);
        RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target);
        QualitySettings.renderPipeline = invisible; capturedAt = Time.frameCount;
    }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject); AudioListener.pause = true; Time.captureDeltaTime = 1f / 60;
        Application.logMessageReceived += (message, stack, type) => { if (type == LogType.Error || type == LogType.Exception) errors.Add(message + "\n" + stack); };
        visible = QualitySettings.renderPipeline; invisible = ScriptableObject.CreateInstance<CelestialNoRenderAsset>();
        QualitySettings.renderPipeline = invisible;
        while (CloudTransition.Busy || Time.frameCount < 5) yield return null;
        WaveWatcher.Launch();
        while (WaveWatcher.Current == null || !WaveWatcher.Current.Ready || CloudTransition.Busy || !WavePreparation.Ready) yield return null;
        var watcher = WaveWatcher.Current; if (watcher.Looping) watcher.ToggleLoop();
        watcher.SelectView(WatcherView.Bosses); yield return Ready();
        watcher.SelectLevel(LevelId.Void); yield return Ready();
        watcher.SelectEncounter(WaveWatcher.BossOne); yield return Ready();
        string motion = Path.GetFullPath("ReviewOutput/VoidGiants/Motion"); Directory.CreateDirectory(motion);
        using var frameTimer = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "PlayerLoop", 1);
        using var physicsTimer = ProfilerRecorder.StartNew(ProfilerCategory.Physics2D, "Physics2D.Simulate", 1,
            ProfilerRecorderOptions.Default | ProfilerRecorderOptions.SumAllSamplesInFrame);
        foreach (var difficulty in new[] { RunDifficulty.Easy, RunDifficulty.Normal, RunDifficulty.Hard })
        {
            watcher.SelectDifficulty(difficulty); yield return Ready();
            VoidSovereignBoss boss = null;
            while (boss == null) { boss = FindAnyObjectByType<VoidSovereignBoss>(); yield return null; }
            while (boss.Invulnerable || boss.Hidden) yield return null;
            int created = CombatRound.CreatedCount;
            for (int phase = 1; phase <= boss.PhaseCount; phase++)
            {
                while (boss.Invulnerable || boss.Hidden) yield return null;
                int peak = 0, giantPeak = 0, lastCard = -1;
                float activeCard = 0; bool second = false, complete = false;
                var byCard = new int[2]; var captures = new bool[2]; var samples = new List<double>(2200);
                double physicsPeak = 0;
                for (int frame = 0; frame < 2400; frame++)
                {
                    yield return null;
                    int giants = 0; foreach (var round in CombatRound.Live) if (round is GiantOrb) giants++;
                    peak = Mathf.Max(peak, CombatRound.Live.Count); giantPeak = Mathf.Max(giantPeak, giants);
                    int card = boss.CardIndex;
                    if (card == 1) second = true;
                    if (second && card == 0) { complete = true; break; }
                    if (card != lastCard) { activeCard = 0; lastCard = card; }
                    if (!boss.Teleporting) activeCard += Time.deltaTime;
                    byCard[card] = Mathf.Max(byCard[card], giants);
                    if (difficulty == RunDifficulty.Hard && !boss.Teleporting && activeCard > 4 && !captures[card])
                    { Capture(ReviewVoidGiants.Folder + "/Form-" + boss.Form + "-Card-" + card); captures[card] = true; }
                    if (difficulty == RunDifficulty.Hard && boss.Form == 4 && card == 1 && activeCard > 1 && activeCard < 5.8f && frame % 4 == 0)
                        Capture(motion + "/" + (movieFrame++).ToString("D3"));
                    if (Time.frameCount > capturedAt + 3) samples.Add(frameTimer.LastValue / 1000000.0);
                    physicsPeak = Math.Max(physicsPeak, physicsTimer.LastValue / 1000000.0);
                }
                Check(complete && byCard[0] >= 2 && byCard[1] >= 2,
                    $"{difficulty} form {boss.Form}: both cards complete with giant peaks {byCard[0]}/{byCard[1]}.");
                Check(peak < CombatRound.Limit && giantPeak < 800 && CombatRound.CreatedCount == created,
                    $"{difficulty} form {boss.Form}: total peak {peak}, giant peak {giantPeak}; warmed reserves only.");
                samples.Sort();
                report.Add($"{difficulty} form {boss.Form}: complete CPU frames p95={samples[(int)(samples.Count*.95)]:F3}ms, max={samples.Last():F3}ms; physics max={physicsPeak:F3}ms. Rendering/capture frames excluded.");
                if (phase == boss.PhaseCount) continue;
                while (boss.Invulnerable || boss.Teleporting) yield return null;
                boss.damage(1000000);
                while (!boss.Hidden) yield return null;
                int interludePeak = 0; bool shot = false;
                while (boss.Hidden)
                {
                    yield return null;
                    int giants = 0; foreach (var round in CombatRound.Live) if (round is GiantOrb) giants++;
                    interludePeak = Mathf.Max(interludePeak, giants);
                    if (difficulty == RunDifficulty.Hard && boss.InterludeElapsed > 8 && !shot)
                    { Capture(ReviewVoidGiants.Folder + "/Interlude-" + phase); shot = true; }
                }
                Check(interludePeak >= 2 && CombatRound.CreatedCount == created,
                    difficulty + " interlude " + phase + " giant peak=" + interludePeak + "; no projectile construction.");
            }
        }
        watcher.StopAllCoroutines(); watcher.enabled = false;
        foreach (var enemy in FindObjectsByType<DistrictEnemy>()) Destroy(enemy.gameObject);
        yield return null;
        foreach (var round in CombatRound.Live.ToArray()) release.Invoke(round, null);
        Contacts();
        CombatRound.ClearAll(); yield return null;
        Check(CombatRound.Live.Count == 0, "Screen clearing removes giant orbs and other rounds.");
        report.AddRange(errors); File.WriteAllLines(ReviewVoidGiants.Folder + "/Checks.txt", report);
        SessionState.SetBool("ReviewVoidGiants", false); EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
    }
    void Contacts()
    {
        var giant = CombatRound.Launch<GiantOrb>(Vector3.zero, Vector2.down, 0, .7f);
        var ordinary = CombatRound.Launch<SimpleRound>(new Vector3(9, 9), Vector2.down, 0);
        Check(giant.HitboxScale == .15f && ordinary.HitboxScale == .3f, "Giant hitboxes use 15% of artwork size and ordinary rounds use 30%.");
        Check(giant.GetComponent<SpriteRenderer>().sortingOrder < ordinary.GetComponent<SpriteRenderer>().sortingOrder &&
            giant.GetComponent<SpriteRenderer>().sortingLayerName == "Bullet", "Giant artwork sits behind smaller hostile rounds, still above the player hull.");
        var native = new GameObject("Scaled authored giant pixel reference");
        native.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        var exact = native.AddComponent<CustomCollider2D>(); exact.isTrigger = true;
        var sprite = giant.GetComponent<SpriteRenderer>().sprite; ProjectileSprites.Fit(exact, sprite);
        var circleObject = new GameObject("Companion contact reference");
        var circle = circleObject.AddComponent<CircleCollider2D>(); circle.radius = .045f;
        var polygonObject = new GameObject("Player hexagon reference");
        var polygon = polygonObject.AddComponent<PolygonCollider2D>();
        polygon.points = new[] { new Vector2(0,.07f), new Vector2(.06f,.035f), new Vector2(.06f,-.035f), new Vector2(0,-.07f), new Vector2(-.06f,-.035f), new Vector2(-.06f,.035f) };
        var boxObject = new GameObject("Cargo contact reference"); var box = boxObject.AddComponent<BoxCollider2D>(); box.size = new Vector2(.09f,.11f);
        giant.GetComponent<Rigidbody2D>().simulated = true;
        var proxy = giant.GetComponent<CircleCollider2D>(); var random = new System.Random(61126);
        var boxes = ProjectileSprites.Boxes(sprite);
        var edge = boxes.OrderByDescending(rect => rect.xMax).First();
        var cockpit = new GameObject("Player cockpit reference").AddComponent<CircleCollider2D>();
        cockpit.tag = "Player";
        cockpit.radius = .02f;
        Vector2 rim = giant.transform.TransformPoint(new Vector2(edge.xMax, edge.center.y) * giant.HitboxScale);
        cockpit.transform.position = rim + Vector2.right * (cockpit.radius - .001f);
        Check(giant.PixelOverlaps(cockpit) && !ProjectilePixelContact.Overlap(giant.transform, boxes, cockpit, .125f),
            "A cockpit just inside the enlarged opaque edge registers a new hit.");
        cockpit.transform.position = rim + Vector2.right * (cockpit.radius + .001f);
        Check(!giant.PixelOverlaps(cockpit), "A cockpit just outside that edge still grazes.");
        Destroy(cockpit.gameObject);
        int mismatches = 0, misses = 0, checks = 0, giantHits = 0, previousHits = 0, ordinaryHits = 0;
        foreach (var target in new Collider2D[] { circle, polygon, box })
            for (int i = 0; i < 600; i++)
            {
                float scale = .7f + (float)random.NextDouble() * 1.3f;
                giant.transform.localScale = Vector3.one * scale;
                giant.transform.rotation = Quaternion.Euler(0,0,(float)random.NextDouble()*360);
                native.transform.SetPositionAndRotation(giant.transform.position, giant.transform.rotation);
                native.transform.localScale = giant.transform.localScale * giant.HitboxScale;
                target.transform.position = new Vector3((float)random.NextDouble()*.9f-.45f,(float)random.NextDouble()*.9f-.45f);
                target.transform.rotation = Quaternion.Euler(0,0,(float)random.NextDouble()*360);
                Physics2D.SyncTransforms();
                var distance = Physics2D.Distance(exact, target);
                bool hit = giant.PixelOverlaps(target);
                if (hit) { giantHits++; if (!Physics2D.Distance(proxy,target).isOverlapped) misses++; }
                if (ProjectilePixelContact.Overlap(giant.transform, boxes, target, .125f)) previousHits++;
                if (ProjectilePixelContact.Overlap(giant.transform, boxes, target, ordinary.HitboxScale)) ordinaryHits++;
                if (hit != distance.isOverlapped && Mathf.Abs(distance.distance) > .0001f) mismatches++;
                checks++;
            }
        Check(mismatches == 0 && misses == 0, checks + " rotated/scaled giant contacts match native scaled opaque pixels; broad-phase misses=" + misses + ", mismatches=" + mismatches + ".");
        Check(previousHits < giantHits && giantHits < ordinaryHits, $"Contact poses at the previous giant, current giant and ordinary scales: {previousHits}, {giantHits}, {ordinaryHits}.");
        Destroy(native); Destroy(circleObject); Destroy(polygonObject); Destroy(boxObject);
        release.Invoke(giant, null);
        var reused = CombatRound.Launch<GiantOrb>(Vector3.zero, Vector2.down, 0, .7f);
        float expectedRadius = sprite.bounds.extents.magnitude * .15f + ProjectilePixelContact.PolygonSkin / reused.transform.localScale.x;
        Check(ReferenceEquals(giant,reused) && reused.HitboxScale == .15f && Mathf.Approximately(reused.GetComponent<CircleCollider2D>().radius, expectedRadius),
            "Pooled giants retain the compact native broad phase and exact small hitbox.");
    }
}
#endif
