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
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class VerifyCombatPolish
{
    public const string Folder = "ArtSource/CombatPolishReview";
    static double deadline;
    static VerifyCombatPolish()
    {
        EditorApplication.playModeStateChanged += state =>
          {
              if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("CombatPolishReview", false))
                  return;
              new GameObject("Combat performance and clarity review").AddComponent<CombatPolishReview>();
              deadline = EditorApplication.timeSinceStartup + 900;
          };
        EditorApplication.update += () =>
          {
              if (deadline == 0)
                  return;
              string file = Folder + (SessionState.GetBool("CombatPolishBaseline", false) ? "/Baseline.txt" : "/Verification.txt");
              if (File.Exists(file))
              {
                  SessionState.SetBool("CombatPolishReview", false);
                  EditorApplication.Exit(File.ReadAllText(file).StartsWith("PASS") ? 0 : 1);
              }
              else if (EditorApplication.timeSinceStartup > deadline)
              {
                  File.WriteAllText(file, "FAIL timeout");
                  EditorApplication.Exit(1);
              }
          };
    }

    public static void RunBaseline() => Begin(true);
    public static void Run() => Begin(false);
    public static void CheckDiscRotation()
    {
        try
        {
            var round = CombatRound.Launch<ReturningDisc>(Vector3.zero, Vector2.down, 0);
            typeof(CombatRound).GetField("age", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(round, .6f);
            round.SendMessage("Tick", 0f);
            var first = round.transform.rotation;
            round.SendMessage("Tick", 0f);
            if (Quaternion.Angle(first, round.transform.rotation) > .01f)
                throw new Exception("Disc spin changed with orientation caching");
            UnityEngine.Object.DestroyImmediate(round.gameObject);
            File.WriteAllText(Folder + "/DiscRotation.txt", "PASS cached orientation preserves the returning disc's authored spin");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { File.WriteAllText(Folder + "/DiscRotation.txt", "FAIL " + e); EditorApplication.Exit(1); }
    }

    static void Begin(bool baseline)
    {
        Directory.CreateDirectory(Folder + "/Frames");
        File.Delete(Folder + "/Progress.txt");
        File.Delete(Folder + (baseline ? "/Baseline.txt" : "/Verification.txt"));
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("CombatPolishReview", true);
        SessionState.SetBool("CombatPolishBaseline", baseline);
        EditorApplication.isPlaying = true;
    }
}
public sealed class CombatPolishReview : MonoBehaviour
{
    const string Folder = VerifyCombatPolish.Folder;
    readonly List<string> checks = new List<string>(), errors = new List<string>();
    readonly Type[] kinds = { typeof(SimpleRound), typeof(NeedleRound), typeof(BurstShell), typeof(SuspendedRound), typeof(HelixRound), typeof(RicochetBolt), typeof(GuidedMissile), typeof(PlasmaOrb), typeof(ProximityMine), typeof(ReturningDisc), typeof(ScatterCarrier), typeof(LaserSideRound) };
    static readonly MethodInfo release = typeof(CombatRound).GetMethod("Release", BindingFlags.Instance | BindingFlags.NonPublic);
    SimulationMode2D previousSimulation;
    Shipcontroller ship;
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
        Application.logMessageReceived += Log;
        previousSimulation = Physics2D.simulationMode;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= Log;
        Physics2D.simulationMode = previousSimulation;
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
        File.WriteAllText(Folder + (SessionState.GetBool("CombatPolishBaseline", false) ? "/Baseline.txt" : "/Verification.txt"), (errors.Count == 0 ? "PASS" : "FAIL") + "\n" + string.Join("\n", checks) + "\n" + string.Join("\n", errors));
    }

    static CombatRound Launch(Type type, Vector3 p, float radius = .16f) =>
        (CombatRound)typeof(CombatRound).GetMethod("Launch").MakeGenericMethod(type).Invoke(null, new object[] { p, Vector2.down, 4f, radius });
    void Clear()
    {
        foreach (var shot in CombatRound.Live.ToArray())
        {
            release.Invoke(shot, null);
            CombatRound.Live.Remove(shot);
        }
        foreach (var enemy in FindObjectsByType<AEnemy>())
            if (enemy != null)
                Destroy(enemy.gameObject);
        foreach (var shot in FindObjectsByType<ABullet>())
            Destroy(shot.gameObject);
    }

    IEnumerator Run()
    {
        GameRules.StartNewRun();
        LevelRun.Reset(LevelId.Snow, 17381);
        yield return SceneManager.LoadSceneAsync("L1");
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
        yield return Profile();
        Clear();
        yield return null;
        yield return Pool();
        Clear();
        yield return null;
        if (SessionState.GetBool("CombatPolishBaseline", false))
            yield break;
        yield return ExtraChecks();
    }

    IEnumerator Profile()
    {
        Clear();
        yield return null;
        for (int i = 0; i < 24; i++)
        {
            var enemy = DistrictEnemy.Spawn(typeof(ScoutInterceptor), new Vector3(-3.5f + i % 6 * 1.4f, -5 + i / 6 * 3.1f, 1));
            enemy.enabled = false;
            foreach (var mover in enemy.GetComponents<AMover>())
                mover.enabled = false;
        }
        var shots = new CombatRound[CombatRound.Limit];
        for (int i = 0; i < shots.Length; i++)
        {
            var type = i >= 268 ? typeof(PlasmaOrb) : i % 10 < 5 ? typeof(SimpleRound) : i % 10 < 8 ? typeof(NeedleRound) : i % 10 == 8 ? typeof(SuspendedRound) : typeof(BurstShell);
            shots[i] = Launch(type, Vector3.zero);
            shots[i].enabled = false;
            CombatRound.Live.Add(shots[i]);
        }
        yield return null;
        Physics2D.simulationMode = SimulationMode2D.Script;
        var samples = new List<double>();
        var watch = new System.Diagnostics.Stopwatch();
        for (int frame = 0; frame < 220; frame++)
        {
            for (int i = 0; i < shots.Length; i++)
                shots[i].transform.position = new Vector3(Mathf.Sin(i * .37f + frame * .031f) * 3.5f, Mathf.Cos(i * .41f + frame * .023f) * 6, -.5f);
            watch.Restart();
            Physics2D.SyncTransforms();
            Physics2D.Simulate(1f / 60);
            watch.Stop();
            if (frame >= 30)
                samples.Add(watch.Elapsed.TotalMilliseconds);
            if (frame % 20 == 0)
            {
                File.WriteAllText(Folder + "/Progress.txt", "Physics step " + frame);
                yield return null;
            }
        }
        Physics2D.simulationMode = previousSimulation;
        samples.Sort();
        Check(CombatRound.Live.Count == 280, "profile kept all 280 hostile projectiles active");
        checks.Add("MEASURE physics+transform sync 280 mixed rounds / 24 hulls: median " + samples[samples.Count / 2].ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + " ms; p95 " + samples[(int)(samples.Count * .95)].ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + " ms.");
        Clear();
        yield return null;
    }

    IEnumerator Pool()
    {
        var seen = new HashSet<CombatRound>();
        for (int burst = 0; burst < 4; burst++)
        {
            for (int i = 0; i < 280; i++)
            {
                var shot = CombatRound.Launch<SimpleRound>(new Vector3((i % 14 - 7) * .4f, 6 - i / 14 * .5f), Vector2.down, 1, .14f);
                shot.enabled = false;
                CombatRound.Live.Add(shot);
                seen.Add(shot);
            }
            foreach (var shot in CombatRound.Live.ToArray())
            {
                release.Invoke(shot, null);
                CombatRound.Live.Remove(shot);
            }
            yield return null;
        }
        checks.Add("MEASURE unique round objects across four 280-round bursts: " + seen.Count + ".");
    }

    IEnumerator Environment(LevelId level)
    {
        Clear();
        LevelRun.Reset(level, 17381);
        LevelEnvironment.Apply();
        yield return null;
        while (!WavePreparation.Ready)
            yield return null;
        yield return new WaitForSeconds(.6f);
    }

    IEnumerator ExtraChecks()
    {
        Check(CombatRound.Limit == 280, "hostile bullet cap remains 280");
        Check(Physics2D.GetIgnoreLayerCollision(9, 8) && Physics2D.GetIgnoreLayerCollision(9, 9) && Physics2D.GetIgnoreLayerCollision(9, 3), "hostile rounds ignore enemy hulls, hostile rounds and ordinary friendly rounds");
        Check(!Physics2D.GetIgnoreLayerCollision(9, 7) && !Physics2D.GetIgnoreLayerCollision(9, 0) && !Physics2D.GetIgnoreLayerCollision(10, 3) && !Physics2D.GetIgnoreLayerCollision(10, 7), "player hits, companion interception and destructible plasma contacts retained");
        var orb = CombatRound.Launch<PlasmaOrb>(Vector3.zero, Vector2.down, 0, .4f);
        orb.enabled = false;
        CombatRound.Live.Add(orb);
        var friendly = PlayerRound.Spawn(Vector3.zero, Vector2.up, 20, 0);
        friendly.enabled = false;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return null;
        Check(!orb.gameObject.activeSelf && friendly == null, "actual friendly projectile destroys plasma through the new collision filter");
        Clear();
        yield return null;
        foreach (int layer in new[] { 0, 7 })
        {
            var target = new GameObject("Collision target");
            target.layer = layer;
            target.AddComponent<CircleCollider2D>().isTrigger = true;
            var probe = target.AddComponent<CombatContactProbe>();
            var round = CombatRound.Launch<SimpleRound>(Vector3.zero, Vector2.down, 0);
            round.enabled = false;
            CombatRound.Live.Add(round);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Check(probe.Hits > 0, "actual hostile projectile contact retained on layer " + layer);
            Destroy(target);
            Clear();
            yield return null;
        }
        foreach (var type in kinds)
        {
            var shot = Launch(type, Vector3.zero);
            shot.enabled = false;
            var sprite = shot.GetComponent<SpriteRenderer>().sprite;
            var pixels = sprite.texture.GetPixels32();
            var collider = shot.GetComponent<CustomCollider2D>();
            collider.enabled = true;
            int samples = 0;
            foreach (float angle in new[] { 0, 45, 90 })
            {
                shot.transform.rotation = Quaternion.Euler(0, 0, angle);
                Physics2D.SyncTransforms();
                for (int y = 0; y < sprite.texture.height; y++)
                    for (int x = 0; x < sprite.texture.width; x++)
                    {
                        var local = (new Vector2(x + .5f, y + .5f) - sprite.pivot) / sprite.pixelsPerUnit;
                        CheckPixel(collider.OverlapPoint(shot.transform.TransformPoint(local)) == (pixels[y * sprite.texture.width + x].a > 128), type.Name, x, y, angle);
                        samples++;
                    }
            }
            checks.Add("PASS " + type.Name + ": unchanged pixel collision outline, " + samples + " opaque/transparent samples at three angles");
            release.Invoke(shot, null);
        }
        foreach (LevelId level in Enum.GetValues(typeof(LevelId)))
        {
            yield return Environment(level);
            Material shared = null;
            for (int i = 0; i < kinds.Length; i++)
            {
                var p = new Vector3(-3.4f + i % 4 * 2.25f, 5.8f - i / 4 * 4.1f);
                var shot = Launch(kinds[i], p, .36f);
                shot.enabled = false;
                CombatRound.Live.Add(shot);
                var small = Launch(kinds[i], p + new Vector3(.70f, -.85f), .13f);
                small.enabled = false;
                CombatRound.Live.Add(small);
                var visual = shot.GetComponent<SpriteRenderer>();
                if (shared == null)
                    shared = visual.sharedMaterial;
                Check(visual.sharedMaterial == shared && visual.sharedMaterial.GetColor("_SpriteTint") == LevelProjectilePalette.Accent(level), level + " " + kinds[i].Name + ": shared district ink on reused sprite");
            }
            for (int i = 0; i < 4; i++)
            {
                var p = PlayerRound.Spawn(new Vector3(-2.8f + i * 1.8f, -6), Vector2.up, 1, 0);
                p.enabled = false;
            }
            yield return null;
            Capture("Palette-" + level);
        }
        yield return Environment(LevelId.Fields);
        var wave = WaveComposer.PreviewPart(WavePartLibrary.PlainsGround[0], 0, 1, out _);
        int next = 0, frame = 0, observations = 0;
        float start = Time.time, last = -1, maxAngle = 0;
        bool down = true;
        var previous = new Dictionary<GroundVehicle, float>();
        var field = FindAnyObjectByType<PSXGrassField>();
        while (Time.time - start < 5.7f)
        {
            float elapsed = Time.time - start;
            while (next < wave.units.Count && wave.units[next].time <= elapsed)
            {
                var u = wave.units[next++];
                var e = DistrictEnemy.Spawn(typeof(MeadowTank), Vector3.up * (Camera.UpLimit + 2));
                e.FlyPhrase(u);
            }
            yield return null;
            foreach (var tank in AEnemy.Live.OfType<GroundVehicle>())
            {
                if (!tank.Grounded || tank.transform.position.y > Camera.UpLimit || tank.transform.position.y < Camera.DownLimit)
                    continue;
                float y = (int.Parse(tank.GroundSection.name.Split(' ').Last()) - 1) * 24 + tank.GroundPoint.y;
                if (previous.TryGetValue(tank, out var before))
                    down &= Mathf.DeltaAngle(before / 96 * 360, y / 96 * 360) <= .005f;
                previous[tank] = y;
                observations++;
                maxAngle = Mathf.Max(maxAngle, Vector2.Angle(tank.Hull.TransformDirection(Vector3.down), field.RoadDownDirection(tank.transform.position.y)));
            }
            if (elapsed - last > .12f)
            {
                last = elapsed;
                Capture("Frames/Tanks-" + (frame++).ToString("D4"));
                if (elapsed > 1.8f && elapsed < 2.1f)
                    Capture("Tanks-Road");
            }
        }
        Check(observations > 30 && down && maxAngle < 12, "tanks drive down relative to terrain and turn chassis along road: " + observations + " observations; max heading error " + maxAngle.ToString("F2") + " degrees");
        Clear();
        yield return null;
        var unit = WavePartLibrary.PlainsGround[0].Generate(0, 1)[0];
        var targetTank = DistrictEnemy.Spawn(typeof(MeadowTank), Vector3.up * (Camera.UpLimit + 2));
        targetTank.FlyPhrase(unit);
        yield return null;
        targetTank.lifeDropChance = targetTank.manaDropChance = targetTank.moneyDropChance = 0;
        var measurement = new PlainsTtkProbe.Measurement { health = targetTank.health };
        targetTank.gameObject.AddComponent<PlainsTtkTarget>().Initialize(targetTank, measurement);
        var gun = ship.GetComponentInChildren<BaseWeapon>();
        gun.enabled = true;
        gun.isStopingPrimary = false;
        gun.WeaponPressed(true);
        start = Time.time;
        while (targetTank != null && Time.time - start < 8)
        {
            var p = ship.transform.position;
            p.x = Mathf.MoveTowards(p.x, targetTank.transform.position.x, GameRules.playerMoveSpeed * Time.deltaTime);
            p.y = Camera.DownLimit + 1.8f;
            ship.transform.position = p;
            yield return null;
        }
        gun.StopFiring();
        gun.enabled = false;
        yield return null;
        Check(measurement.killed && measurement.death - measurement.firstHit < 2.5f, "moving road tank remains killable with the stock gun: " + (measurement.death - measurement.firstHit).ToString("F2") + "s from first hit");
        yield return Environment(LevelId.Snow);
        var boss = (SnowSiegeBoss)DistrictEnemy.Spawn(typeof(SnowSiegeBoss), new Vector3(0, Camera.UpLimit - 2.3f, 1));
        yield return null;
        Check(Mathf.Abs(boss.health - 2100 * GameRules.enemyHealth * LevelRun.EnemyHealthScale) < .1f, "Snow boss health reduced from 3200 to 2100 with run scaling preserved");
        float health = boss.health;
        int peak = 0;
        start = Time.time;
        last = -1;
        frame = 0;
        while (Time.time - start < 13)
        {
            float elapsed = Time.time - start;
            if (elapsed > 5 && boss.health == health)
                boss.health = health * .49f;
            peak = Mathf.Max(peak, CombatRound.Live.Count);
            if (elapsed - last > .15f)
            {
                last = elapsed;
                Capture("Frames/Snow-" + (frame++).ToString("D4"));
                if (elapsed > 8 && elapsed < 8.3f)
                    Capture("Snow-Boss");
            }
            yield return null;
        }
        Check((bool)typeof(DistrictBoss).GetField("phaseTwo", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boss) && peak > 50, "Snow boss retains its shell barrage and harder second phase; peak " + peak + " rounds");
        Clear();
        yield return null;
    }

    static void CheckPixel(bool ok, string type, int x, int y, float angle)
    {
        if (!ok)
            throw new Exception(type + " pixel collision changed at " + x + "," + y + " angle " + angle);
    }
}
public sealed class CombatContactProbe : MonoBehaviour
{
    public int Hits;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<AEnemyBullet>() != null)
            Hits++;
    }
}
#endif
