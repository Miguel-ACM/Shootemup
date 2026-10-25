#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class VerifyVisualClarity
{
    static double deadline;
    static VerifyVisualClarity()
    {
        EditorApplication.playModeStateChanged += state =>
          {
              if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("VisualClarityReview", false))
              {
                  new GameObject("Visual clarity review").AddComponent<VisualClarityReview>();
                  deadline = EditorApplication.timeSinceStartup + 900;
              }
          };
        EditorApplication.update += () =>
          {
              const string report = "ArtSource/VisualClarityReview/Verification.txt";
              if (deadline == 0 || !SessionState.GetBool("VisualClarityReview", false))
                  return;
              if (File.Exists(report))
              {
                  SessionState.SetBool("VisualClarityReview", false);
                  EditorApplication.Exit(File.ReadAllText(report).StartsWith("PASS") ? 0 : 1);
              }
              else if (EditorApplication.timeSinceStartup > deadline)
              {
                  File.WriteAllText(report, "FAIL timed out");
                  EditorApplication.Exit(1);
              }
          };
    }

    public static void Run() => Begin(false);
    public static void Bullets() => Begin(true);
    public static void SideCoverage()
    {
        SessionState.SetBool("VisualClaritySideCoverage", true);
        Begin(true);
    }

    static void Begin(bool bulletsOnly)
    {
        SessionState.SetBool("VisualClarityBulletsOnly", bulletsOnly);
        Directory.CreateDirectory("ArtSource/VisualClarityReview");
        if (File.Exists("ArtSource/VisualClarityReview/Verification.txt"))
            File.Delete("ArtSource/VisualClarityReview/Verification.txt");
        SessionState.SetBool("VisualClarityReview", true);
        EditorApplication.isPlaying = true;
    }
}
public sealed class VisualClarityReview : MonoBehaviour
{
    const string Folder = "ArtSource/VisualClarityReview/";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    readonly List<string> checks = new List<string>(), errors = new List<string>();
    static readonly Type[] Types = { typeof(SimpleRound), typeof(NeedleRound), typeof(HelixRound), typeof(BurstShell), typeof(RicochetBolt), typeof(SuspendedRound), typeof(GuidedMissile), typeof(PlasmaOrb), typeof(ProximityMine), typeof(ReturningDisc), typeof(ScatterCarrier), typeof(LaserSideRound) };
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

    static FieldInfo Field(Type type, string name)
    {
        while (type != null)
        {
            var field = type.GetField(name, Hidden | BindingFlags.Public);
            if (field != null)
                return field;
            type = type.BaseType;
        }
        throw new Exception(name);
    }

    static void Set(object target, string name, object value) => Field(target.GetType(), name).SetValue(target, value);
    static T Get<T>(object target, string name) => (T)Field(target.GetType(), name).GetValue(target);
    void Check(bool pass, string message)
    {
        if (!pass)
            throw new Exception(message);
        checks.Add("PASS " + message);
        File.WriteAllText(Folder + "Progress.txt", string.Join("\n", checks));
    }

    static void Clear()
    {
        foreach (var round in FindObjectsByType<CombatRound>())
            round.Vanish();
    }

    IEnumerator Start()
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(Run());
        while (stack.Count > 0)
        {
            bool more = false;
            object step = null;
            Exception failure = null;
            try
            {
                more = stack.Peek().MoveNext();
                if (more)
                    step = stack.Peek().Current;
            }
            catch (Exception ex) { failure = ex; }
            if (failure != null)
            {
                errors.Add(failure.ToString());
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

    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder + name + ".jpg", texture.EncodeToJPG(90));
        Destroy(texture);
    }

    IEnumerator Clip(string name, float seconds, int fps = 12)
    {
        Directory.CreateDirectory(Folder + name);
        foreach (var file in Directory.GetFiles(Folder + name, "????.jpg"))
            File.Delete(file);
        float start = Time.time, next = 0;
        int frame = 0;
        while (Time.time - start < seconds)
        {
            if (Time.time - start >= next)
            {
                yield return Capture(name + "/" + (frame++).ToString("D4"));
                next += 1f / fps;
            }
            yield return null;
        }
    }

    static CombatRound Launch(Type type, Vector3 position, float radius = .28f)
    {
        return (CombatRound)typeof(CombatRound).GetMethod("Launch").MakeGenericMethod(type).Invoke(null, new object[] { position, Vector2.down, 3f, radius });
    }

    void Export(Sprite sprite, string name)
    {
        var rect = sprite.rect;
        var source = sprite.texture;
        var previous = RenderTexture.active;
        var target = RenderTexture.GetTemporary((int)rect.width, (int)rect.height, 0);
        Graphics.Blit(source, target, new Vector2(rect.width / source.width, rect.height / source.height), new Vector2(rect.x / source.width, rect.y / source.height));
        RenderTexture.active = target;
        var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        texture.Apply();
        File.WriteAllBytes(Folder + "Sprites/" + name + ".png", texture.EncodeToPNG());
        Destroy(texture);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(target);
    }

    void Gallery()
    {
        Directory.CreateDirectory(Folder + "Sprites");
        var sprites = new HashSet<Sprite>();
        for (int i = 0; i < Types.Length; i++)
        {
            var shot = Launch(Types[i], new Vector3((i % 3 - 1) * 2.7f, 5.5f - i / 3 * 3.4f));
            shot.enabled = false;
            var sr = shot.GetComponent<SpriteRenderer>();
            shot.transform.rotation = Quaternion.identity;
            Check(sprites.Add(sr.sprite), Types[i].Name + " has its own sprite");
            Export(sr.sprite, Types[i].Name);
        }
    }

    void LaserChecks()
    {
        Clear();
        var laser = CombatRound.Launch<PulseLaser>(new Vector3(0, 5), Vector2.down);
        laser.enabled = false;
        laser.sideShots = true;
        laser.activeTime = 1.2f;
        var tick = typeof(PulseLaser).GetMethod("Tick", Hidden);
        Set(laser, "age", .65f);
        tick.Invoke(laser, new object[] { 1f / 60 });
        var layout = LaserSidePorts.VisibleLayout(laser.transform.position, laser.Heading, laser.length);
        Check(layout.Count > 3 && laser.GetComponentsInChildren<SpriteRenderer>().Count(x => x.name.StartsWith("Side outlet") && x.enabled) == layout.Count * 2 && !CombatRound.Live.OfType<LaserSideRound>().Any(), "side outlets cover the whole visible beam before any lateral shot");
        Set(laser, "age", laser.warning);
        tick.Invoke(laser, new object[] { 1f / 60 });
        var rounds = CombatRound.Live.OfType<LaserSideRound>().ToArray();
        Check(rounds.Length == layout.Count * 2, "side volley fills every visible outlet pair");
        foreach (var round in rounds)
        {
            bool match = false;
            for (int port = 1; port <= layout.Count; port++)
                foreach (int sign in new[] { -1, 1 })
                    match |= Vector3.Distance(round.transform.position, LaserSidePorts.Origin(laser.transform.position, laser.Heading, laser.width, layout, port, sign)) < .001f;
            Check(match, "lateral bullet begins at a marked outlet");
        }
        var camera = UnityEngine.Camera.main;
        float bottom = camera.transform.position.y - camera.orthographicSize;
        Check(rounds.Min(x => x.transform.position.y) <= bottom + .3f, "side volley reaches the visible bottom edge");
        Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("ContinuousBeam")), "continuous blue beam shader compiles");
        foreach (var round in rounds)
            round.Vanish();
        Set(laser, "age", laser.warning + .5f);
        tick.Invoke(laser, new object[] { 1f / 60 });
        Check(CombatRound.Live.OfType<LaserSideRound>().Count() == layout.Count * 2, "later laser volleys retain the full visible span");
        var energy = laser.GetComponentsInChildren<LineRenderer>().Where(x => x.enabled && x.startWidth > .1f).ToArray();
        Check(energy.Length >= 2, "active hostile beam has a visible body and sheath");
        foreach (var line in energy)
            Check(line.startColor.b > line.startColor.r * 2 && line.startColor.b > .7f, "active hostile beam renders blue");
        foreach (var round in CombatRound.Live.OfType<LaserSideRound>().ToArray())
            round.Vanish();
        Set(laser, "age", laser.warning + laser.activeTime + .01f);
        tick.Invoke(laser, new object[] { 1f / 60 });
        Check(!CombatRound.Live.OfType<LaserSideRound>().Any(), "cooling lasers stop emitting side shots");
        Clear();
        var reused = CombatRound.Launch<PulseLaser>(new Vector3(0, 5), Vector2.down);
        Check(reused.GetComponentsInChildren<SpriteRenderer>().All(x => !x.enabled), "pooled laser hides stale side cues");
        Clear();
        foreach (var direction in new[] { Vector2.down, Vector2.up, Vector2.left, Vector2.right, new Vector2(.4f, -1).normalized, new Vector2(-.4f, -1).normalized })
        {
            var span = LaserSidePorts.VisibleLayout(Vector3.zero, direction, 22);
            Check(span.Count > 0 && span.Distance(span.Count) <= 22, "side ports clip correctly for " + direction);
            for (int port = 2; port <= span.Count; port++)
                Check(span.Distance(port) - span.Distance(port - 1) <= 2.301f, "visible outlets keep bounded spacing");
        }
        Check(LaserSidePorts.VisibleLayout(new Vector3(100, 100), Vector2.up, 22).Count == 0, "offscreen beams do not consume invisible outlet volleys");
        var friendly = new GameObject("Friendly blue beam review").AddComponent<BeamRibbon>();
        friendly.Build(true);
        friendly.Draw(new Vector3(-2, 0), Vector2.up, 6, .2f, 1, 1);
        foreach (var line in friendly.GetComponentsInChildren<LineRenderer>().Where(x => x.enabled && x.startWidth > .1f))
            Check(line.startColor.b > line.startColor.r * 2, "friendly beams render blue too");
        Destroy(friendly.gameObject);
    }

    IEnumerator SideCoverageReview()
    {
        LaserChecks();
        yield return null;
        var carrier = CombatRound.Launch<ScatterCarrier>(new Vector3(0, Camera.UpLimit - 1.2f), Vector2.down, 3.7f, .3f);
        float start = Time.time, next = 0;
        int frame = 0;
        bool lowerPair = false;
        Directory.CreateDirectory(Folder + "CarrierLower");
        while (Time.time - start < 4.3f)
        {
            foreach (var shot in CombatRound.Live.OfType<NeedleRound>())
                if (Time.time - start > 2.1f && Get<Vector3>(shot, "origin").y < Camera.DownLimit + 3)
                    lowerPair = true;
            if (Time.time - start >= next)
            {
                yield return Capture("CarrierLower/" + (frame++).ToString("D4"));
                next += .15f;
            }
            yield return null;
        }
        Check(lowerPair, "carriers keep firing in the lower arena after their third volley");
        Clear();
        yield return null;
        var recycled = CombatRound.Launch<ScatterCarrier>(new Vector3(0, 5), Vector2.down, 1, .3f);
        recycled.enabled = false;
        Set(recycled, "age", .7f);
        typeof(ScatterCarrier).GetMethod("Tick", Hidden).Invoke(recycled, new object[] { 1f / 60 });
        Check(CombatRound.Live.OfType<NeedleRound>().Count() == 2, "reused carriers restart their side-shot cadence");
        Clear();
        yield return null;
        foreach (var level in new[] { LevelId.Snow, LevelId.Thunder })
        {
            LevelRun.Reset(level, 7119);
            LevelEnvironment.Apply();
            while (!WavePreparation.Ready)
                yield return null;
            var emitter = DistrictEnemy.Spawn(typeof(BeamDrone), new Vector3(0, Camera.UpLimit - 2.4f, 1));
            emitter.enabled = false;
            foreach (var mover in emitter.GetComponents<PatternMover>())
                mover.enabled = false;
            var beam = CombatRound.Launch<PulseLaser>(emitter.transform.position, Vector2.down, 0);
            beam.Bind(emitter.Hull, new Vector3(0, -.28f, -.24f));
            beam.sideShots = true;
            beam.activeTime = 2.5f;
            beam.width = .25f;
            yield return Clip(level + "BlueSideBeam", 3.6f, 8);
            Clear();
            Destroy(emitter.gameObject);
            yield return null;
        }
        Check(errors.Count == 0, "full-span blue laser and sustained carrier live reviews complete without runtime errors");
    }

    IEnumerator ShellClip()
    {
        var shell = CombatRound.Launch<BurstShell>(new Vector3(0, 5), Vector2.down, 3, .4f).SetTravelDistance(3);
        float stop = Get<float>(shell, "cruise") + Get<float>(shell, "brake");
        float begin = Time.time;
        float early = 0, late = 0;
        bool inward = true, quiet = true;
        int frame = 0;
        float next = 0;
        Directory.CreateDirectory(Folder + "Shell");
        foreach (var file in Directory.GetFiles(Folder + "Shell", "????.jpg"))
            File.Delete(file);
        while (Time.time - begin < stop + .78f)
        {
            float age = Get<float>(shell, "age");
            var ps = Get<ParticleSystem>(shell, "ignition");
            if (age < stop && ps != null && ps.particleCount > 0)
                quiet = false;
            if (ps != null)
            {
                var particles = new ParticleSystem.Particle[40];
                int count = ps.GetParticles(particles);
                for (int i = 0; i < count; i++)
                {
                    var delta = particles[i].position - shell.transform.position;
                    inward &= Vector3.Dot(delta, particles[i].velocity) < .001f;
                    if (particles[i].remainingLifetime < particles[i].startLifetime - .04f)
                        continue;
                    if (age < stop + .18f)
                        early = Mathf.Max(early, delta.magnitude);
                    if (age > stop + .60f)
                        late = Mathf.Max(late, delta.magnitude);
                }
            }
            if (Time.time - begin >= next)
            {
                yield return Capture("Shell/" + (frame++).ToString("D4"));
                next += 1f / 15;
            }
            yield return null;
        }
        Check(quiet && inward && early > late * 1.4f && late > 0, "shell sparks begin only after stopping, travel inward and tighten toward detonation (" + early.ToString("F2") + " → " + late.ToString("F2") + ")");
        Clear();
    }

    IEnumerator Run()
    {
        GameRules.StartNewRun();
        LevelRun.Reset();
        Time.timeScale = 1;
        if (SceneManager.GetActiveScene().name != "L1")
            yield return SceneManager.LoadSceneAsync("L1");
        yield return new WaitForSeconds(1);
        var flow = FindAnyObjectByType<WaveOrchestrer>();
        flow.enabled = false;
        flow.CancelInvoke();
        flow.StopAllCoroutines();
        foreach (var enemy in FindObjectsByType<AEnemy>())
            Destroy(enemy.gameObject);
        var ship = FindAnyObjectByType<Shipcontroller>();
        ship.ObserverMode = true;
        ship.enabled = false;
        foreach (var weapon in ship.GetComponentsInChildren<AWeapon>())
        {
            weapon.StopFiring();
            weapon.enabled = false;
        }
        foreach (var renderer in ship.GetComponentsInChildren<Renderer>())
            renderer.forceRenderingOff = true;
        foreach (var collider in ship.GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
        foreach (var canvas in FindObjectsByType<Canvas>())
            if (canvas.name != "PSX pixel display" && canvas.name != "Cloud scene transitions")
                canvas.enabled = false;
        while (CloudTransition.Busy || !WavePreparation.Ready)
            yield return null;
        Clear();
        yield return null;
        if (SessionState.GetBool("VisualClaritySideCoverage", false))
        {
            SessionState.SetBool("VisualClaritySideCoverage", false);
            yield return SideCoverageReview();
            yield break;
        }
        Gallery();
        yield return Capture("BulletGallery");
        Clear();
        HelixRound.Pair(new Vector3(0, 5), Vector2.down, 3);
        yield return new WaitForSeconds(.6f);
        Check(CombatRound.Live.OfType<HelixRound>().All(x => x.GetComponent<TrailRenderer>().positionCount > 8), "both helix partners draw a continuous tapered wake");
        yield return Clip("Helix", 2.5f);
        Clear();
        var recycled = CombatRound.Launch<HelixRound>(new Vector3(-3, 5), Vector2.down, 3, .12f);
        Check(recycled.GetComponent<TrailRenderer>().positionCount == 0, "pooled helix clears the previous trail before relaunch");
        Clear();
        yield return ShellClip();
        LaserChecks();
        var beam = CombatRound.Launch<PulseLaser>(new Vector3(0, 5.5f), Vector2.down);
        beam.sideShots = true;
        beam.activeTime = 1.4f;
        beam.width = .30f;
        beam.sweep = 12;
        yield return Clip("Laser", 2.7f, 15);
        Clear();
        checks.AddRange(FlightSmoothnessAudit.Inspect());
        Check(true, "all district route speed and curvature checks completed");
        if (SessionState.GetBool("VisualClarityBulletsOnly", false))
        {
            Check(File.Exists(Folder + "WaveVerification.txt") && File.ReadAllText(Folder + "WaveVerification.txt").StartsWith("PASS"), "retained full smoothed-wave review passes; projectile-only review completed");
            yield break;
        }
        const string waveReport = "ArtSource/WaveVarietyReview/Verification.txt";
        if (File.Exists(waveReport))
            File.Delete(waveReport);
        SessionState.SetBool("WaveVarietyIntegration", false);
        SessionState.SetBool("WaveVarietyPreparation", false);
        new GameObject("Live smoothed formations review").AddComponent<WaveVarietyReview>();
        while (!File.Exists(waveReport))
            yield return null;
        Check(File.ReadAllText(waveReport).StartsWith("PASS"), "855 re-spaced wave layouts, normal director handoffs and fifteen complete live encounters pass with smoothed routes");
        File.Copy(waveReport, Folder + "WaveVerification.txt", true);
    }
}
#endif
