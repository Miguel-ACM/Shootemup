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
public static class VerifyGroundWaves
{
    public const string Folder = "ArtSource/GroundWavesReview";
    static double deadline;
    static VerifyGroundWaves()
    {
        EditorApplication.playModeStateChanged += state =>
          {
              if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("GroundWavesReview", false))
                  return;
              new GameObject("Ground convoy verification").AddComponent<GroundWavesReview>();
              deadline = EditorApplication.timeSinceStartup + 480;
          };
        EditorApplication.update += () =>
          {
              if (deadline == 0)
                  return;
              if (File.Exists(Folder + "/Verification.txt"))
              {
                  SessionState.SetBool("GroundWavesReview", false);
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
            var report = new List<string>();
            foreach (var level in new[] { LevelId.Fields, LevelId.Snow })
                foreach (int seed in new[] { 17381, 421, 9826 })
                {
                    LevelRun.Reset(level, seed);
                    int groundWaves = 0;
                    var used = new HashSet<string>();
                    for (int budget = 2; budget <= 20; budget++)
                    {
                        var wave = DistrictWaves.Create(budget);
                        if (!WaveGeometry.Clear(WaveVarietyAudit.Tracks(wave)))
                            throw new Exception("Swept hull conflict: " + level + " " + seed + " " + budget);
                        if (wave.units.Any(u => u.enemy == "MeadowTank" || u.enemy == "FrostAuger"))
                            groundWaves++;
                        foreach (var name in wave.parts)
                            used.Add(name);
                        File.WriteAllText(Folder + "/Progress.txt", level + " " + seed + " " + budget);
                    }
                    if (groundWaves < 4)
                        throw new Exception("Too few ground formations in normal compositions");
                    foreach (var part in level == LevelId.Fields ? WavePartLibrary.PlainsGround : WavePartLibrary.SnowGround)
                    {
                        if (!used.Contains(part.Name))
                            throw new Exception("Unreachable ground part: " + part.Name);
                        if (WaveComposer.PreviewPart(part, 0, 1, out _) == null || WaveComposer.PreviewPart(part, 0, -1, out _) == null)
                            throw new Exception("Unplayable ground motif: " + part.Name);
                    }
                    report.Add("PASS " + level + " seed " + seed + ": 19 safe compositions; " + groundWaves + " include ground troops; all six ground motifs reachable and mirrored.");
                }
            File.WriteAllText(Folder + "/Composition.txt", string.Join("\n", report));
            EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
            SessionState.SetBool("GroundWavesReview", true);
            CheckHarness.EnterPlayMode();
        }
        catch (Exception e) { File.WriteAllText(Folder + "/Verification.txt", "FAIL\n" + e); CheckHarness.Exit(1); }
    }
}
public sealed class GroundWavesReview : MonoBehaviour
{
    const string Folder = VerifyGroundWaves.Folder;
    readonly List<string> checks = new List<string>(), errors = new List<string>();
    bool backfaces;
    Shipcontroller ship;
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
        Application.logMessageReceived += Log;
        backfaces = Physics.queriesHitBackfaces;
        Physics.queriesHitBackfaces = true;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= Log;
        Physics.queriesHitBackfaces = backfaces;
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
        File.WriteAllText(Folder + "/RuntimeProgress.txt", string.Join("\n", checks));
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
        foreach (var shot in CombatRound.Live.ToArray())
            shot.Vanish();
        foreach (var shot in FindObjectsByType<ABullet>())
            Destroy(shot.gameObject);
        foreach (var pickup in FindObjectsByType<APickable>())
            Destroy(pickup.gameObject);
    }

    IEnumerator Run()
    {
        foreach (var level in new[] { LevelId.Fields, LevelId.Snow })
        {
            GameRules.StartNewRun();
            LevelRun.Reset(level, 17381);
            if (level == LevelId.Fields)
                yield return SceneManager.LoadSceneAsync("L1");
            else
            {
                Clear();
                LevelEnvironment.Apply();
            }
            yield return null;
            var flow = FindAnyObjectByType<WaveOrchestrer>();
            flow.enabled = false;
            flow.CancelInvoke();
            flow.StopAllCoroutines();
            ship = FindAnyObjectByType<Shipcontroller>();
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
            ship.transform.position = new Vector3(0, Camera.DownLimit + 1.8f, -1);
            foreach (var model in FindObjectsByType<MeshFilter>())
            {
                if (model.sharedMesh == null)
                    continue;
                string name = model.gameObject.name;
                if ((level == LevelId.Fields && name.StartsWith("Valley section ")) || (level == LevelId.Snow && model.sharedMesh.name.StartsWith("SnowGround")))
                {
                    model.gameObject.layer = 30;
                    model.gameObject.AddComponent<MeshCollider>().sharedMesh = model.sharedMesh;
                }
            }
            var parts = level == LevelId.Fields ? WavePartLibrary.PlainsGround : WavePartLibrary.SnowGround;
            yield return Play(WaveComposer.PreviewPart(parts[0], 0, 1, out _), level, "ConvoyA");
            yield return Play(WaveComposer.PreviewPart(parts[5], 0, -1, out _), level, "ConvoyB");
            bool Ground(Wave w) => w.units.Any(u => u.enemy == "MeadowTank" || u.enemy == "FrostAuger");
            int early = Enumerable.Range(8, 5).First(b => Ground(DistrictWaves.Create(b)));
            int late = Enumerable.Range(16, 5).Last(b => Ground(DistrictWaves.Create(b)));
            yield return Play(DistrictWaves.Create(early), level, "MixedEarly");
            yield return Play(DistrictWaves.Create(late), level, "MixedLate");
            yield return StockGun(level, parts[0]);
        }
    }

    IEnumerator Play(Wave wave, LevelId level, string label)
    {
        Clear();
        yield return null;
        int next = 0, peakRounds = 0, peakGround = 0, marks = 0;
        bool grounded = true, surface = true, noThrusters = true, dry = true;
        float start = Time.time, last = -1;
        int frame = 0;
        bool pictured = false;
        var layout = new PSXValleyLayout(LevelRun.Seed);
        while (Time.time - start < wave.duration + 1)
        {
            float elapsed = Time.time - start;
            while (next < wave.units.Count && wave.units[next].time <= elapsed)
            {
                var unit = wave.units[next++];
                var enemy = DistrictEnemy.Spawn(DistrictWaves.EnemyType(unit.enemy), Vector3.up * (Camera.UpLimit + 2));
                enemy.FlyPhrase(unit);
            }
            Physics.SyncTransforms();
            int count = 0, visible = 0;
            foreach (var enemy in AEnemy.Live)
                if (enemy is GroundVehicle vehicle)
                {
                    count++;
                    if (!vehicle.Grounded)
                    {
                        if (vehicle.transform.position.y < Camera.UpLimit && vehicle.transform.position.y > Camera.DownLimit)
                            surface = false;
                        continue;
                    }
                    var p = vehicle.GroundSection.TransformPoint(vehicle.GroundPoint);
                    grounded &= Vector2.Distance(vehicle.transform.position, p) < .20f;
                    noThrusters &= vehicle.GetComponent<EnemyThrusters>() == null;
                    marks = Mathf.Max(marks, vehicle.GetComponent<GroundTreadMarks>()?.MarkCount ?? 0);
                    if (vehicle.transform.position.y < Camera.UpLimit && vehicle.transform.position.y > Camera.DownLimit)
                    {
                        visible++;
                        if (Physics.Raycast(new Vector3(p.x, p.y, 60), Vector3.forward, out var hit, 40, 1 << 30))
                            surface &= Mathf.Abs(hit.point.z - p.z) < .14f;
                        else
                            surface = false;
                        if (level == LevelId.Fields)
                        {
                            int i = int.Parse(vehicle.GroundSection.name.Split(' ').Last());
                            float y = (i - 1) * 24 + vehicle.GroundPoint.y;
                            dry &= Mathf.Abs(vehicle.GroundPoint.x - layout.Centre(y)) > layout.HalfWidth(y) + .7f;
                        }
                    }
                }
            peakGround = Mathf.Max(peakGround, count);
            peakRounds = Mathf.Max(peakRounds, CombatRound.Live.Count);
            if (elapsed - last > .16f)
            {
                last = elapsed;
                Capture("Frames/" + level + "-" + label + "-" + Mathf.RoundToInt(elapsed * 100).ToString("D4"));
                if (frame++ == 18)
                    Capture(level + "-" + label);
                if (!pictured && visible >= 2 && peakRounds > 0 && elapsed > 2.1f)
                {
                    Capture(level + "-" + label);
                    pictured = true;
                }
            }
            yield return null;
        }
        Check(next == wave.units.Count && peakRounds > 0, level + " " + label + ": real firing with peak " + peakRounds + " rounds");
        if (peakGround > 0)
        {
            Check(peakGround >= 2 && grounded && surface && dry && noThrusters, level + " " + label + ": terrain contact, clear routes and no flying effects");
            Check(marks >= 4, level + " " + label + ": attached moving tread/screw marks");
        }
    }

    IEnumerator StockGun(LevelId level, WavePart part)
    {
        Clear();
        yield return null;
        var unit = part.Generate(0, 1)[0];
        var target = DistrictEnemy.Spawn(DistrictWaves.EnemyType(unit.enemy), Vector3.up * (Camera.UpLimit + 2));
        target.FlyPhrase(unit);
        yield return null;
        target.lifeDropChance = target.manaDropChance = target.moneyDropChance = 0;
        var measurement = new PlainsTtkProbe.Measurement { health = target.health };
        target.gameObject.AddComponent<PlainsTtkTarget>().Initialize(target, measurement);
        var gun = ship.GetComponentInChildren<BaseWeapon>();
        Check(gun != null && GameRules.playerShootDamage == 2, "stock BaseWeapon retained for " + level + " ground damage test");
        gun.enabled = true;
        gun.isStopingPrimary = false;
        gun.WeaponPressed(true);
        float start = Time.time;
        while (target != null && Time.time - start < 8)
        {
            var p = ship.transform.position;
            p.x = Mathf.MoveTowards(p.x, target.transform.position.x, GameRules.playerMoveSpeed * Time.deltaTime);
            p.y = Camera.DownLimit + 1.8f;
            ship.transform.position = p;
            yield return null;
        }
        gun.StopFiring();
        gun.enabled = false;
        yield return null;
        Check(measurement.killed && measurement.death - measurement.firstHit < 2.5f, level + " moving ground enemy killed with the stock gun in " + (measurement.death - measurement.firstHit).ToString("F2") + "s from first hit");
        Clear();
        yield return null;
    }
}
#endif
