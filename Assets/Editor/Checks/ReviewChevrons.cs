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

[InitializeOnLoad]
public static class ReviewChevrons
{
    public const string Folder = "C:/Users/Miguel/Shootemup/ArtSource/ChevronReview";
    static ReviewChevrons()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("ReviewChevrons", false)) return;
            MetaProgress.ReviewSaveDirectory = Path.GetFullPath("ReviewOutput/Chevrons/Save");
            new GameObject("Chevron encounter review").AddComponent<ChevronReviewRuntime>();
        };
    }
    static void Guard()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\', '/').EndsWith("/Temp/UnlockReview/Assets"))
            throw new Exception("Use the isolated command-line review checkout.");
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory(Path.GetFullPath("ReviewOutput/Chevrons/Save"));
    }
    public static void Run()
    {
        Guard(); PlayerSettings.companyName = "ShootemupReview"; PlayerSettings.productName = "ChevronReview";
        MetaProgress.ReviewSaveDirectory = Path.GetFullPath("ReviewOutput/Chevrons/Save");
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("ReviewChevrons", true); EditorApplication.isPlaying = true;
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

public sealed class ChevronReviewRuntime : MonoBehaviour
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly MethodInfo release = typeof(CombatRound).GetMethod("Release", Hidden);
    readonly List<string> report = new(), errors = new();
    RenderPipelineAsset visible, invisible;
    WaveWatcher watcher;
    void Check(bool okay, string message)
    {
        report.Add((okay ? "PASS " : "FAIL ") + message); if (!okay) errors.Add(message);
        File.WriteAllLines(ReviewChevrons.Folder + "/Progress.txt", report);
    }
    IEnumerator Ready() { yield return null; while (CloudTransition.Busy || !WavePreparation.Ready || !watcher.Ready) yield return null; }
    void Capture(string name)
    {
        QualitySettings.renderPipeline = visible;
        var camera = UnityEngine.Camera.main; var presentation = camera.GetComponent<PSXPresentation>();
        camera.Render();
        var target = RenderTexture.GetTemporary(1280, 720, 24);
        Graphics.Blit(presentation.SceneTexture, target, presentation.CompositeMaterial);
        var previous = RenderTexture.active; RenderTexture.active = target;
        var pixels = new Texture2D(405, 720, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect((1280-405)/2, 0, 405, 720), 0, 0); pixels.Apply();
        File.WriteAllBytes(ReviewChevrons.Folder + "/" + name + ".png", pixels.EncodeToPNG()); Destroy(pixels);
        RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target);
        QualitySettings.renderPipeline = invisible;
    }
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject); AudioListener.pause = true; Time.captureDeltaTime = 1f / 60;
        Application.logMessageReceived += (message, stack, type) => { if (type == LogType.Error || type == LogType.Exception) errors.Add(message + "\n" + stack); };
        visible = QualitySettings.renderPipeline; invisible = ScriptableObject.CreateInstance<CelestialNoRenderAsset>();
        QualitySettings.renderPipeline = invisible;
        var stack = new Stack<IEnumerator>(); stack.Push(Run());
        while (stack.Count > 0)
        {
            bool more = false; object step = null;
            try { more = stack.Peek().MoveNext(); if (more) step = stack.Peek().Current; }
            catch (Exception ex) { errors.Add(ex.ToString()); break; }
            if (!more) { stack.Pop(); continue; }
            if (step is IEnumerator nested) stack.Push(nested); else yield return step;
        }
        report.AddRange(errors); File.WriteAllLines(ReviewChevrons.Folder + "/Checks.txt", report);
        SessionState.SetBool("ReviewChevrons", false); EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
    }
    IEnumerator Sample(string label, Type expected, int frames, bool capture)
    {
        int created = CombatRound.CreatedCount, peak = 0, total = 0; bool photographed = false;
        for (int frame = 0; frame < frames; frame++)
        {
            yield return null;
            int count = 0; foreach (var shot in CombatRound.Live) if (shot.GetType() == expected) count++;
            peak = Mathf.Max(peak, count); total = Mathf.Max(total, CombatRound.Live.Count);
            if (capture && !photographed && count >= 2 && frame > 200)
            { Capture(label.Replace(' ', '-')); photographed = true; }
        }
        Check(peak > 0 && total < CombatRound.Limit && CombatRound.CreatedCount == created,
            label + ": " + expected.Name + " peak=" + peak + ", all shots=" + total + ", newly constructed=" + (CombatRound.CreatedCount-created) + ".");
    }
    IEnumerator Run()
    {
        while (CloudTransition.Busy || Time.frameCount < 5) yield return null;
        WaveWatcher.Launch();
        while (WaveWatcher.Current == null || !WaveWatcher.Current.Ready || CloudTransition.Busy || !WavePreparation.Ready) yield return null;
        watcher = WaveWatcher.Current; if (watcher.Looping) watcher.ToggleLoop();
        var catalog = WatcherCatalog.For(WatcherView.Projectiles, LevelId.City);
        Check(!catalog.Any(e => e.Id.Contains("Disc")) && catalog.Any(e => e.Id == "projectile/ChevronRound"), "Projectile catalog exposes chevrons and no returning disc.");
        watcher.SelectView(WatcherView.Enemies); yield return Ready();
        var cases = new[] {
            (LevelId.City, typeof(CommandGunship)), (LevelId.Tunnel, typeof(BaffleSpinner)),
            (LevelId.Snow, typeof(ColdWeatherFighter)), (LevelId.Desert, typeof(DuneMoth)), (LevelId.Underwater, typeof(ReefDart))
        };
        foreach (var (level, type) in cases)
        {
            watcher.SelectLevel(level); yield return Ready();
            watcher.SelectEntry(watcher.Entries.ToList().FindIndex(e => e.Enemy == type)); yield return Ready();
            foreach (var difficulty in new[] { RunDifficulty.Easy, RunDifficulty.Normal, RunDifficulty.Hard })
            {
                watcher.SelectDifficulty(difficulty); watcher.Replay(); yield return Ready();
                yield return Sample(type.Name + " " + difficulty, type == typeof(BaffleSpinner) ? typeof(RicochetBolt) : typeof(ChevronRound), 600, difficulty == RunDifficulty.Hard);
            }
        }
        watcher.SelectView(WatcherView.Bosses); yield return Ready();
        watcher.SelectLevel(LevelId.City); yield return Ready();
        foreach (int encounter in new[] { WaveWatcher.BossOne, WaveWatcher.BossThree, WaveWatcher.BossOther })
        {
            watcher.SelectEncounter(encounter); yield return Ready();
            int phase = encounter == WaveWatcher.BossThree ? 3 : encounter == WaveWatcher.BossOther ? 2 : 1;
            for (int guard = 0; guard < 1800; guard++)
            {
                if (FindObjectsByType<CityDuoBoss>().Any(b => b.CurrentPhase == phase && !b.Invulnerable && b.transform.position.y < Camera.UpLimit - 1)) break;
                yield return null;
            }
            yield return Sample("City-boss-" + encounter, typeof(ChevronRound), 1200, true);
        }
        watcher.SelectLevel(LevelId.Void); yield return Ready();
        watcher.SelectEncounter(WaveWatcher.BossOne); yield return Ready();
        VoidSovereignBoss boss = null;
        while (boss == null) { boss = FindAnyObjectByType<VoidSovereignBoss>(); yield return null; }
        for (int phase = 1; phase < 4; phase++)
        {
            while (boss.Invulnerable || boss.Hidden || boss.Teleporting) yield return null;
            if (phase == 2) yield return Sample("Void-scythe-ribbons", typeof(ChevronRound), 1200, true);
            while (boss.Invulnerable || boss.Teleporting) yield return null;
            boss.damage(1000000); while (!boss.Hidden) yield return null;
            int created = CombatRound.CreatedCount, peak = 0; bool captured = false;
            while (boss.Hidden)
            {
                yield return null;
                int count = 0; foreach (var shot in CombatRound.Live) if (shot is ChevronRound) count++;
                peak = Mathf.Max(peak, count);
                if (!captured && boss.InterludeElapsed > 8) { Capture("Void-interlude-" + phase); captured = true; }
            }
            Check(peak > 8 && CombatRound.CreatedCount == created, "Void interlude " + phase + ": chevron peak=" + peak + ", newly constructed=" + (CombatRound.CreatedCount-created) + ".");
        }
        watcher.StopAllCoroutines(); watcher.enabled = false;
        foreach (var enemy in FindObjectsByType<DistrictEnemy>()) Destroy(enemy.gameObject);
        yield return null;
        foreach (var shot in CombatRound.Live.ToArray()) release.Invoke(shot, null);
        yield return Programs();
        Contacts();
        CombatRound.ClearAll(); yield return null;
        Check(CombatRound.Live.Count == 0, "Screen clearing removes the new chevron volleys.");
    }
    IEnumerator Programs()
    {
        var shot = CombatRound.Launch<ChevronRound>(Vector3.zero, Vector2.down, 2, .2f)
            .Pause(.25f,.6f,CombatRound.Turn.Rotate,60,.75f).FadeAt(2);
        yield return new WaitForSeconds(.35f); Vector3 held = shot.transform.position;
        yield return new WaitForSeconds(.3f);
        Check(Vector3.Distance(held,shot.transform.position)<.001f, "Chevrons hold during an authored pause.");
        yield return new WaitForSeconds(.4f);
        Check(Vector3.Distance(held,shot.transform.position)>.05f && Mathf.Abs(Vector2.SignedAngle(Vector2.down,shot.Heading)-60)<.1f,
            "Chevrons resume along the authored new heading.");
        release.Invoke(shot,null);
        var reused = CombatRound.Launch<ChevronRound>(Vector3.zero,Vector2.down,2,.2f);
        Check(ReferenceEquals(shot,reused) && (float)typeof(CombatRound).GetField("pauseAt",Hidden).GetValue(reused) < 0 &&
            (float)typeof(CombatRound).GetField("fadeAt",Hidden).GetValue(reused) < 0, "Pool reuse clears the chevron's previous motion program.");
        release.Invoke(reused,null);
    }
    void Contacts()
    {
        var shot = CombatRound.Launch<ChevronRound>(Vector3.zero,Vector2.up,0,1.4f);
        var native = new GameObject("Chevron pixel reference"); native.AddComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;
        var exact=native.AddComponent<CustomCollider2D>(); exact.isTrigger=true;
        var sprite=shot.GetComponent<SpriteRenderer>().sprite; ProjectileSprites.Fit(exact,sprite);
        var ship=FindAnyObjectByType<Shipcontroller>(); var hex=ship.GetComponent<Collider2D>();
        shot.transform.position=hex.bounds.center;
        Check(!shot.PixelOverlaps(hex), "The chevron's transparent central gap does not damage the production ship.");
        shot.transform.position=Vector3.zero;
        var circleObject=new GameObject("Companion reference"); var circle=circleObject.AddComponent<CircleCollider2D>(); circle.radius=.045f;
        var random=new System.Random(6112602); int mismatches=0;
        foreach(var target in new[]{hex,(Collider2D)circle})
            for(int i=0;i<600;i++)
            {
                shot.transform.localScale=Vector3.one*(.4f+(float)random.NextDouble()*1.6f);
                shot.transform.rotation=Quaternion.Euler(0,0,(float)random.NextDouble()*360);
                native.transform.SetPositionAndRotation(shot.transform.position,shot.transform.rotation);
                native.transform.localScale=shot.transform.localScale*shot.HitboxScale;
                target.transform.position=new Vector3((float)random.NextDouble()*.9f-.45f,(float)random.NextDouble()*.9f-.45f);
                Physics2D.SyncTransforms(); var distance=Physics2D.Distance(exact,target);
                if(shot.PixelOverlaps(target)!=distance.isOverlapped && Mathf.Abs(distance.distance)>.0001f)mismatches++;
            }
        Check(mismatches==0,"1200 rotated/scaled chevron contacts match the native pixel mask; mismatches="+mismatches+".");
        Destroy(native);Destroy(circleObject);
    }
}
#endif
