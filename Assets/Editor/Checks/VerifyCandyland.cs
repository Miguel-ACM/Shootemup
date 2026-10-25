#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class VerifyCandyland
{
    public const string Folder = "ArtSource/CandylandReview";
    static double deadline;
    static VerifyCandyland()
    {
        EditorApplication.playModeStateChanged += state =>
          {
              if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("CandylandReview", false))
                  return;
              new GameObject("Candyland verification").AddComponent<CandylandReview>();
              deadline = EditorApplication.timeSinceStartup + 360;
          };
        EditorApplication.update += () =>
          {
              if (deadline == 0)
                  return;
              if (File.Exists(Folder + "/Verification.txt"))
              {
                  SessionState.SetBool("CandylandReview", false);
                  CheckHarness.Exit(File.ReadAllText(Folder + "/Verification.txt").StartsWith("PASS") ? 0 : 1);
              }
              else if (EditorApplication.timeSinceStartup > deadline)
              {
                  File.WriteAllText(Folder + "/Verification.txt", "FAIL timeout");
                  CheckHarness.Exit(1);
              }
          };
    }

    public static void Run()
    {
        Directory.CreateDirectory(Folder + "/Frames");
        if (File.Exists(Folder + "/Verification.txt"))
            File.Delete(Folder + "/Verification.txt");
        try
        {
            VerifyRunProgression.Run();
            var used = new HashSet<string>();
            int minimum = 1000;
            float longest = 0;
            foreach (int seed in new[] { 17381, 421, 9826 })
            {
                LevelRun.Reset(LevelId.Candyland, seed);
                for (int budget = 2; budget <= 20; budget++)
                {
                    var wave = DistrictWaves.Create(budget);
                    minimum = Mathf.Min(minimum, wave.units.Count);
                    longest = Mathf.Max(longest, wave.duration);
                    foreach (var unit in wave.units)
                        used.Add(unit.enemy);
                    if (wave.units.Count < 12 || !WaveGeometry.Clear(WaveVarietyAudit.Tracks(wave)))
                        throw new Exception("Invalid Candyland formation " + seed + " / " + budget);
                    File.WriteAllText(Folder + "/Progress.txt", "Composed " + seed + " / " + budget);
                }
            }
            if (used.Count != 3)
                throw new Exception("Candyland enemy absent from compositions");
            foreach (var part in WavePartLibrary.Candyland)
            {
                bool found = false;
                for (int variant = 0; variant < 13; variant++)
                    if (WaveComposer.PreviewPart(part, variant, 1, out _) != null)
                    {
                        found = true;
                        break;
                    }
                if (!found)
                    throw new Exception("Unplayable Candyland motif: " + part.Name);
            }
            File.WriteAllText(Folder + "/Geometry.txt", "PASS: route loops, all three choices, 57 swept formation checks, all 18 Candyland motifs. Minimum ships per wave: " + minimum + "; longest wave: " + longest.ToString("F2") + "s.\n");
            EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
            SessionState.SetBool("CandylandReview", true);
            CheckHarness.EnterPlayMode();
        }
        catch (Exception e) { File.WriteAllText(Folder + "/Verification.txt", "FAIL\n" + e); CheckHarness.Exit(1); }
    }
}
public sealed class CandylandReview : MonoBehaviour
{
    readonly List<string> errors = new List<string>(), checks = new List<string>();
    const string Folder = VerifyCandyland.Folder;
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
            errors.Add(message + "\n" + trace);
    }

    void Check(bool ok, string message)
    {
        if (!ok)
            throw new Exception(message);
        checks.Add("PASS " + message);
    }

    void Capture(string name) => DesertReview.Capture(name, Folder);
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
        File.WriteAllText(Folder + "/Verification.txt", (errors.Count == 0 ? "PASS" : "FAIL") + "\n" + string.Join("\n", checks) + "\n" + string.Join("\n", errors));
    }

    void Clear()
    {
        foreach (var enemy in AEnemy.Live.ToArray())
            if (enemy != null)
                Destroy(enemy.gameObject);
        foreach (var round in CombatRound.Live.ToArray())
            round.Vanish();
        foreach (var round in FindObjectsByType<ABullet>())
            Destroy(round.gameObject);
    }

    IEnumerator Run()
    {
        GameRules.StartNewRun();
        LevelRun.Reset(LevelId.Candyland, 17381);
        yield return SceneManager.LoadSceneAsync("L1");
        yield return null;
        var flow = FindAnyObjectByType<WaveOrchestrer>();
        flow.enabled = false;
        flow.CancelInvoke();
        flow.StopAllCoroutines();
        var ship = FindAnyObjectByType<Shipcontroller>();
        ship.ObserverMode = true;
        ship.enabled = false;
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
        ship.transform.position = new Vector3(0, Camera.DownLimit + 2, -1);
        yield return new WaitForSeconds(1);
        Capture("Scenery");
        var backdrop = FindAnyObjectByType<CandylandBackdrop>();
        Check(backdrop != null && backdrop.CandyCount == 30, "layered cotton-candy clouds and 30 sweets across five modeled types loaded");
        foreach (int budget in new[] { 2, 10, 20 })
        {
            Clear();
            yield return null;
            var wave = DistrictWaves.Create(budget);
            int next = 0, peak = 0, peakEnemies = 0;
            float start = Time.time, last = -1;
            while (Time.time - start < wave.duration + 1)
            {
                float elapsed = Time.time - start;
                while (next < wave.units.Count && wave.units[next].time <= elapsed)
                {
                    var unit = wave.units[next++];
                    var enemy = DistrictEnemy.Spawn(DistrictWaves.EnemyType(unit.enemy), Vector3.up * (Camera.UpLimit + 2));
                    enemy.FlyPhrase(unit);
                }
                peak = Mathf.Max(peak, CombatRound.Live.Count);
                peakEnemies = Mathf.Max(peakEnemies, AEnemy.Live.Count);
                if (elapsed - last > .25f)
                {
                    last = elapsed;
                    Capture("Frames/Wave" + budget + "-" + Mathf.RoundToInt(elapsed * 100).ToString("D4"));
                }
                yield return null;
            }
            Check(next == wave.units.Count && peak > 0 && peakEnemies >= 6, "wave " + budget + ": " + next + " ships, peak " + peakEnemies + " enemies / " + peak + " bullets");
        }
        Clear();
        yield return null;
        var boss = (CandyRegentBoss)DistrictEnemy.Spawn(typeof(CandyRegentBoss), new Vector3(0, Camera.UpLimit + 3, 1));
        yield return null;
        float hp = boss.health;
        Check(hp < 1440 * GameRules.enemyHealth * LevelRun.EnemyHealthScale, "regent has less health than the Plains boss");
        Vector3 lastPosition = boss.transform.position;
        Vector2 previousVelocity = Vector2.zero;
        float maxSpeed = 0, minX = 100, maxX = -100, minY = 100, maxY = -100;
        int maxMinions = 0, peakBossRounds = 0, turns = 0;
        var minionTypes = new HashSet<System.Type>();
        for (int phase = 0; phase < 2; phase++)
        {
            if (phase == 1)
                boss.damage(hp * .53f);
            for (int frame = 0; frame < 52; frame++)
            {
                float start = Time.time;
                yield return new WaitForSeconds(.25f);
                Vector2 velocity = (boss.transform.position - lastPosition) / (Time.time - start);
                maxSpeed = Mathf.Max(maxSpeed, velocity.magnitude);
                lastPosition = boss.transform.position;
                if (velocity.magnitude > 3 && previousVelocity.magnitude > 3 && Vector2.Angle(velocity, previousVelocity) > 65)
                    turns++;
                previousVelocity = velocity;
                minX = Mathf.Min(minX, lastPosition.x);
                maxX = Mathf.Max(maxX, lastPosition.x);
                maxMinions = Mathf.Max(maxMinions, boss.LiveMinions);
                if (frame > 8)
                {
                    minY = Mathf.Min(minY, lastPosition.y);
                    maxY = Mathf.Max(maxY, lastPosition.y);
                }
                peakBossRounds = Mathf.Max(peakBossRounds, CombatRound.Live.Count);
                foreach (var minion in AEnemy.Live)
                    if (minion != boss)
                        minionTypes.Add(minion.GetType());
                Capture("Frames/Boss" + phase + "-" + frame.ToString("D3"));
            }
            Capture("BossPhase" + (phase + 1));
        }
        Check(maxSpeed > 10 && maxX - minX > 3 && maxY - minY > 4 && turns >= 8, "boss makes fast multi-height dashes and repeated sharp direction changes");
        Check(peakBossRounds >= 45, "harder boss sustains pressure: peak " + peakBossRounds + " projectiles");
        Check(boss.SpawnedMinions >= 32 && maxMinions > 8 && maxMinions <= 18 && minionTypes.Count == 3, "boss releases frequent mixed broods, capped at eighteen living minions");
        var victim = AEnemy.Live.FirstOrDefault(e => e is BonbonSkirmisher);
        Check(victim != null, "minions remain independently targetable");
        victim.damage(100000);
        yield return null;
        Check(victim == null, "minions can be destroyed");
        float before = boss.health;
        for (int i = 0; i < 16; i++)
        {
            PlayerRound.Spawn(boss.transform.position + Vector3.down * .35f, Vector2.up, 20, 2, PlayerDelivery.Pulse, 0);
            yield return new WaitForSeconds(.04f);
        }
        Check(boss.health < before, "stock projectiles can hit the fast moving boss");
        boss.damage(1000000);
        yield return null;
        yield return null;
        Check(boss == null && !AEnemy.Live.Any(), "boss death clears every type of surviving minion");
        Clear();
        yield return new WaitForSeconds(2);
        LevelRun.Reset(LevelId.Thunder, 17381);
        LevelId? chosen = null;
        var choice = LevelChoice.Open(ship.transform, p => chosen = p);
        yield return new WaitForSeconds(.5f);
        Capture("RouteChoice");
        Check(choice.Options.SequenceEqual(new[] { LevelId.Desert, LevelId.Candyland, LevelId.Fields }), "three-way route includes Desert, Candyland and Plains");
        yield return new WaitForSeconds(3);
        Check(chosen == null, "bottom of route choice stays neutral");
        ship.transform.position = new Vector3(0, Camera.UpLimit * .35f, -1);
        yield return new WaitForSeconds(3.3f);
        Check(chosen == LevelId.Candyland, "holding the middle route selects Candyland");
        Destroy(choice.gameObject);
    }
}
#endif
