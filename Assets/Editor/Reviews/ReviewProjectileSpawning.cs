#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ReviewProjectileSpawning
{
    public const string Folder = "ArtSource/ProjectileSpawnReview";
    static double deadline;
    static ReviewProjectileSpawning()
    {
        EditorApplication.playModeStateChanged += state =>
          {
              if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("ProjectileSpawning", false))
                  return;
              new GameObject("Projectile spawning review").AddComponent<ProjectileSpawnReview>();
              deadline = EditorApplication.timeSinceStartup + 150;
          };
        EditorApplication.update += () =>
          {
              if (deadline == 0)
                  return;
              string file = Folder + "/Runtime.txt";
              if (File.Exists(file))
              {
                  SessionState.SetBool("ProjectileSpawning", false);
                  EditorApplication.Exit(File.ReadAllText(file).StartsWith("PASS") ? 0 : 1);
              }
              else if (EditorApplication.timeSinceStartup > deadline)
              {
                  File.WriteAllText(file, "FAIL timeout");
                  EditorApplication.Exit(1);
              }
          };
    }

    public static void Run()
    {
        Directory.CreateDirectory(Folder + "/Frames");
        File.Delete(Folder + "/Runtime.txt");
        typeof(ReviewBossRadiance).GetMethod("Gallery", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        File.Copy(ReviewBossRadiance.Folder + "/After.png", Folder + "/Glow.png", true);
        File.Copy(ReviewBossRadiance.Folder + "/Radiance.txt", Folder + "/Radiance.txt", true);
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("ProjectileSpawning", true);
        EditorApplication.isPlaying = true;
    }
}
public sealed class ProjectileSpawnReview : MonoBehaviour
{
    const string Folder = ReviewProjectileSpawning.Folder;
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    readonly List<string> checks = new List<string>(), errors = new List<string>();
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
        Screen.SetResolution(640, 480, false);
        Application.logMessageReceived += Log;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= Log;
    }

    void Log(string text, string trace, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception) && !trace.Contains("UnityEditor.Search"))
            errors.Add(text);
    }

    static void Set(object target, string field, object value) => target.GetType().GetField(field, Hidden).SetValue(target, value);
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Hidden).Invoke(target, args);
    void Check(bool ok, string message)
    {
        if (!ok)
            throw new Exception(message);
        checks.Add("PASS " + message);
        File.WriteAllText(Folder + "/Progress.txt", string.Join("\n", checks));
    }

    IEnumerator Start()
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(Review());
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
            catch (Exception e) { failure = e; }
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
        File.WriteAllText(Folder + "/Runtime.txt", (errors.Count == 0 ? "PASS" : "FAIL") + "\n" + string.Join("\n", checks) + "\n" + string.Join("\n", errors));
    }

    (double milliseconds, int created) Volley<T>(int count) where T : CombatRound
    {
        int before = CombatRound.CreatedCount;
        var timer = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
            CombatRound.Launch<T>(new Vector3((i % 16 - 7.5f) * .4f, (i / 16) * .4f, 0), Vector2.down, 4, .14f);
        timer.Stop();
        int created = CombatRound.CreatedCount - before;
        CombatRound.ClearAll();
        return (timer.Elapsed.TotalMilliseconds, created);
    }

    void Report<T>(int count, string label, bool expectWarm) where T : CombatRound
    {
        var result = Volley<T>(count);
        Check(!expectWarm || result.created == 0, label + " " + typeof(T).Name + " x" + count + ": " + result.milliseconds.ToString("F3") + " ms, " + result.created + " new objects");
    }

    IEnumerator Review()
    {
        yield return null;
        GameRules.StartNewRun();
        LevelRun.Reset(LevelId.Tunnel, 17381);
        CloudTransition.Load("L1");
        while (SceneManager.GetActiveScene().name != "L1")
            yield return null;
        yield return null;
        var flow = FindAnyObjectByType<WaveOrchestrer>();
        flow.enabled = false;
        flow.CancelInvoke();
        flow.StopAllCoroutines();
        var ship = FindAnyObjectByType<Shipcontroller>();
        ship.enabled = false;
        ship.ObserverMode = true;
        foreach (var weapon in ship.GetComponentsInChildren<AWeapon>())
        {
            weapon.StopFiring();
            weapon.enabled = false;
        }
        foreach (var collider in ship.GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
        while (CloudTransition.Busy || !WavePreparation.Ready)
            yield return null;
        GameRules.playerDisabled = false;
        Time.timeScale = 1;
        foreach (var enemy in FindObjectsByType<DistrictEnemy>())
            Destroy(enemy.gameObject);
        CombatRound.ClearAll();
        yield return null;
        Check(CombatRound.CreatedCount == 768, "Curtain prepares all thirteen deliveries within the existing 768-object budget");
        Report<NeedleRound>(128, "Prepared", true);
        Report<RicochetBolt>(96, "Prepared", true);
        Report<BurstShell>(40, "Prepared", true);
        Report<PulseLaser>(16, "Prepared", true);
        // Reproduce the previous basic-round-only plan, keeping texture/shape caches hot.
        // This is a conservative CPU comparison, excluding cold disk/texture/driver work.
        var pools = (Dictionary<Type, Stack<CombatRound>>)typeof(CombatRound).GetField("pools", Hidden).GetValue(null);
        foreach (var pair in pools)
            if (pair.Key != typeof(SimpleRound))
                while (pair.Value.Count > 0)
                    Destroy(pair.Value.Pop().gameObject);
        typeof(CombatRound).GetField("pooled", Hidden).SetValue(null, pools[typeof(SimpleRound)].Count);
        yield return null;
        Report<NeedleRound>(128, "Previous preparation plan", false);
        Report<RicochetBolt>(96, "Previous preparation plan", false);
        Report<BurstShell>(40, "Previous preparation plan", false);
        Report<PulseLaser>(16, "Previous preparation plan", false);
        yield return CombatRound.PrepareBursts();
        Report<NeedleRound>(128, "Prepared repeat", true);
        Report<RicochetBolt>(96, "Prepared repeat", true);
        Report<BurstShell>(40, "Prepared repeat", true);
        var presentation = UnityEngine.Camera.main.GetComponent<PSXPresentation>();
        var glow = (RenderTexture)presentation.CompositeMaterial.GetTexture("_GlowTex");
        Check(glow.width == presentation.SceneTexture.width / 4 && glow.height == presentation.SceneTexture.height / 4, "Shared glow processes 75% fewer pixels than the previous half-resolution bloom");
        var boss = (TunnelSawBoss)DistrictEnemy.Spawn(typeof(TunnelSawBoss), new Vector3(0, 0, 1));
        yield return null;
        Check(Mathf.Approximately(boss.health, 1800 * GameRules.enemyHealth * LevelRun.EnemyHealthScale), "Main saw base health reduced from 2800 to 1800");
        Set(boss, "entered", true);
        Set(boss, "velocity", Vector2.right);
        Set(boss, "nextAttack", 0f);
        CombatRound.ClearAll();
        yield return new WaitForSeconds(.3f);
        boss.damage(1);
        yield return null;
        Check(CombatRound.Live.Count == 0, "Timers and hits still emit no saw bullets");
        boss.transform.position = new Vector3(0, 0, 1);
        Call(boss, "ImpactBurst", Vector2.up);
        VerifyCircle(44, 17);
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSeconds(.12f);
            DesertReview.Capture("Frames/Circle-" + i, Folder);
        }
        DesertReview.Capture("Full-Circle", Folder);
        CombatRound.ClearAll();
        boss.health *= .45f;
        yield return null;
        var minis = FindObjectsByType<TunnelSawBoss>().Where(s => s.miniature).ToArray();
        Check(minis.Length == 2 && minis.All(s => Mathf.Approximately(s.health, 360 * GameRules.enemyHealth * LevelRun.EnemyHealthScale)), "Two destroyable auxiliary saws now have 360 base health each");
        CombatRound.ClearAll();
        Call(boss, "ImpactBurst", Vector2.left);
        VerifyCircle(60, 21);
        CombatRound.ClearAll();
        Call(minis[0], "ImpactBurst", Vector2.down);
        VerifyCircle(24, 9);
        CombatRound.ClearAll();
        int impacts = (int)boss.GetType().GetField("impacts", Hidden).GetValue(boss);
        bool bounce = false;
        float until = Time.time + 9, next = 0;
        int frame = 0;
        while (Time.time < until)
        {
            bounce |= (int)boss.GetType().GetField("impacts", Hidden).GetValue(boss) > impacts;
            if (Time.time >= next)
            {
                next = Time.time + .5f;
                DesertReview.Capture("Frames/Patrol-" + (frame++).ToString("D2"), Folder);
            }
            yield return null;
        }
        Check(bounce && CombatRound.Live.Count > 0 && CombatRound.Live.Count <= CombatRound.Limit, "Actual wall bounces produce circular bursts during the full-arena patrol, within the cap");
        Check(boss.GetComponent<EnemyThrusters>() == null && minis.All(s => s.GetComponent<EnemyThrusters>() == null), "All saws retain their thruster-free models");
        Check(CombatRound.Live.All(s => s.GetComponent<SpriteRenderer>() != null && s.GetComponent<MeshRenderer>() == null), "Rounds retain the approved sprite renderers");
        DesertReview.Capture("Phase-Two", Folder);
    }

    void VerifyCircle(int small, int large)
    {
        var rounds = CombatRound.Live.ToArray();
        Check(rounds.Length == small + large, "Circular mixed-size volley contains " + small + " quick and " + large + " heavy rounds");
        CheckRing(rounds.Where(s => s is SimpleRound).ToArray(), small);
        CheckRing(rounds.Where(s => s is RicochetBolt).ToArray(), large);
    }

    void CheckRing(CombatRound[] rounds, int count)
    {
        var angles = rounds.Select(s => Mathf.Repeat(Mathf.Atan2(s.Heading.y, s.Heading.x) * Mathf.Rad2Deg, 360)).OrderBy(a => a).ToArray();
        bool uniform = angles.Length == count;
        for (int i = 0; i < angles.Length; i++)
            uniform &= Mathf.Abs(Mathf.Repeat(angles[(i + 1) % angles.Length] - angles[i], 360) - 360f / count) < .01f;
        Check(uniform, rounds[0].GetType().Name + ": evenly spaced through the complete 360 degrees");
    }
}
#endif
