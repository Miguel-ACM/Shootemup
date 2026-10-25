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
public static class ReviewCombatBalance
{
    static double deadline;
    static ReviewCombatBalance()
    {
        EditorApplication.playModeStateChanged += state =>
          {
              if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("CombatBalance", false))
                  return;
              new GameObject("Combined contact, coatings and balance review").AddComponent<CombatBalanceReview>();
              deadline = EditorApplication.timeSinceStartup + 300;
          };
        EditorApplication.update += () =>
          {
              if (deadline == 0)
                  return;
              string file = VerifyProjectileContacts.Folder + "/Runtime.txt";
              if (File.Exists(file))
              {
                  SessionState.SetBool("CombatBalance", false);
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
        Directory.CreateDirectory(VerifyProjectileContacts.Folder + "/Frames");
        File.Delete(VerifyProjectileContacts.Folder + "/Runtime.txt");
        try
        {
            VerifyProjectileContacts.RunChecks();
            var notes = new List<string>();
            foreach (LevelId level in Enum.GetValues(typeof(LevelId)))
                foreach (int seed in new[] { 17381, 421 })
                {
                    LevelRun.Reset(level, seed);
                    foreach (int budget in new[] { 2, 8, 20 })
                    {
                        var wave = DistrictWaves.Create(budget);
                        if (wave.units.Count < 5 || wave.units.Any(u => u.fireTimes == null || u.fireTimes.Length == 0))
                            throw new Exception("Invalid authored score: " + level + " / " + budget);
                        notes.Add(level + " / " + seed + " / " + budget + ": " + wave.units.Count + " actors, " + wave.units.Sum(u => u.fireTimes.Length) + " scheduled member accents, " + wave.duration.ToString("F2") + "s");
                    }
                }
            File.WriteAllLines(VerifyProjectileContacts.Folder + "/Formations.txt", notes);
        }
        catch (Exception e) { File.WriteAllText(VerifyProjectileContacts.Folder + "/Runtime.txt", "FAIL editor checks\n" + e); EditorApplication.Exit(1); return; }
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("CombatBalance", true);
        EditorApplication.isPlaying = true;
    }
}
public sealed class CombatBalanceReview : MonoBehaviour
{
    const string Folder = VerifyProjectileContacts.Folder;
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    readonly List<string> checks = new List<string>(), errors = new List<string>();
    Shipcontroller ship; WaveOrchestrer flow;
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

    void Clear()
    {
        CombatRound.ClearAll();
        foreach (var enemy in FindObjectsByType<DistrictEnemy>())
            Destroy(enemy.gameObject);
        foreach (var bullet in FindObjectsByType<ABullet>())
            Destroy(bullet.gameObject);
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

    IEnumerator Review()
    {
        yield return null;
        GameRules.StartNewRun();
        LevelRun.Reset(LevelId.Space, 17381);
        CloudTransition.Load("L1");
        while (SceneManager.GetActiveScene().name != "L1")
            yield return null;
        yield return null;
        flow = FindAnyObjectByType<WaveOrchestrer>();
        flow.enabled = false;
        flow.CancelInvoke();
        flow.StopAllCoroutines();
        ship = FindAnyObjectByType<Shipcontroller>();
        ship.enabled = false;
        ship.ObserverMode = true;
        foreach (var weapon in ship.GetComponentsInChildren<AWeapon>())
        {
            weapon.StopFiring();
            weapon.enabled = false;
        }
        while (CloudTransition.Busy || !WavePreparation.Ready)
            yield return null;
        GameRules.playerDisabled = false;
        Time.timeScale = 1;
        Clear();
        yield return null;
        ship.transform.position = new Vector3(0, Camera.DownLimit + 2, -1);
        var hitbox = ship.GetComponent<Collider2D>();
        ship.ObserverMode = false;
        Set(ship, "invulnerable", false);
        float health = ship.health;
        var incoming = CombatRound.Launch<SimpleRound>(hitbox.bounds.center + Vector3.up * .6f, Vector2.down, 12, .14f);
        yield return new WaitForSeconds(.18f);
        Check(ship.health < health && !incoming.gameObject.activeSelf, "Real moving hostile pixels still hit the production ship and release the round");
        ship.StopCoroutine("Invulnerability");
        Set(ship, "invulnerable", false);
        health = ship.health;
        var ring = CombatRound.Launch<ReturningDisc>(hitbox.bounds.center, Vector2.up, 0, .85f);
        yield return new WaitForSeconds(.06f);
        Check(!ring.PixelOverlaps(hitbox) && ship.health == health, "Transparent centre of the original ring cannot damage the player");
        bool found = false;
        for (float x = .05f; x < 1.5f; x += .025f)
        {
            ring.transform.position = hitbox.bounds.center + Vector3.right * x;
            if (ring.PixelOverlaps(hitbox))
            {
                found = true;
                break;
            }
        }
        Check(found, "Found the ring's opaque rim after its transparent centre");
        yield return new WaitForSeconds(.07f);
        Check(ship.health < health && !ring.gameObject.activeSelf, "A later opaque contact inside an existing broad trigger damages correctly");
        ship.ObserverMode = true;
        CombatRound.ClearAll();
        var moon = Instantiate(Resources.Load<GameObject>("Companions/AMoon"), new Vector3(-3, 2, -1), Quaternion.identity);
        moon.GetComponent<OrbitPlayer>().enabled = false;
        yield return null;
        var blocked = CombatRound.Launch<SimpleRound>(moon.GetComponent<Collider2D>().bounds.center + Vector3.up * .35f, Vector2.down, 6, .14f);
        yield return new WaitForSeconds(.15f);
        Check(!blocked.gameObject.activeSelf, "Distant blocking companion still erases actual bullet pixels");
        Destroy(moon);
        var plasma = CombatRound.Launch<PlasmaOrb>(new Vector3(-3, 4, 0), Vector2.down, 0, .3f);
        Check(plasma.GetComponent<Rigidbody2D>().simulated, "Destructible plasma retains native collision anywhere on screen");
        PlayerRound.Spawn(plasma.transform.position + Vector3.down * .6f, Vector2.up, 30, 12);
        yield return new WaitForSeconds(.15f);
        Check(!plasma.gameObject.activeSelf, "Real player shots still destroy distant plasma");
        Clear();
        yield return null;
        // Two real orbital waves with the new authored accents and existing routes.
        foreach (int budget in new[] { 8, 20 })
        {
            var wave = DistrictWaves.Create(budget);
            float start = Time.time, nextFrame = 1;
            int next = 0, frame = 0, peak = 0;
            bool lasers = false;
            while (Time.time - start < Mathf.Min(12, wave.duration + 1))
            {
                float age = Time.time - start;
                while (next < wave.units.Count && wave.units[next].time <= age)
                {
                    var unit = wave.units[next++];
                    var enemy = DistrictEnemy.Spawn(DistrictWaves.EnemyType(unit.enemy), Vector3.zero);
                    enemy.FlyPhrase(unit);
                }
                peak = Mathf.Max(peak, CombatRound.Live.Count);
                lasers |= CombatRound.Live.Any(s => s is PulseLaser);
                if (age >= nextFrame)
                {
                    nextFrame += 1;
                    DesertReview.Capture("Frames/Space" + budget + "-" + (frame++).ToString("D2"), Folder);
                }
                yield return null;
            }
            Check(peak > 15 && peak <= CombatRound.Limit, "Space wave " + budget + " has sustained authored pressure, peak " + peak + " rounds; beams=" + lasers);
            Clear();
            yield return null;
        }
        var station = (SpaceCitadelBoss)DistrictEnemy.Spawn(typeof(SpaceCitadelBoss), new Vector3(0, Camera.UpLimit - 3, 1));
        yield return null;
        float protectedHealth = station.Parts[4].health;
        station.Parts[4].damage(100000);
        Check(station.Parts[4].health == protectedHealth, "Space's core remains gated by both generators");
        yield return new WaitForSeconds(5.5f);
        DesertReview.Capture("Space-Shielded", Folder);
        station.Parts[0].damage(100000);
        station.Parts[1].damage(100000);
        station.Parts[2].damage(100000);
        station.Parts[3].damage(100000);
        yield return null;
        Check(station.CoreExposed && station.Parts[4].CanBeTargeted, "Both generators still open the core after optional gun destruction");
        CombatRound.ClearAll();
        for (int beat = 8; beat <= 10; beat++)
        {
            Set(station, "volley", beat + 1);
            Call(station, "Attack");
        }
        Check(CombatRound.Live.OfType<SimpleRound>().Count() == 27, "Exposed core owns three nine-shot fan replies even when both side guns are destroyed");
        CombatRound.ClearAll();
        yield return new WaitForSeconds(6.5f);
        DesertReview.Capture("Space-Core-Pressure", Folder);
        Check(CombatRound.Live.Count > 15, "Core-only station maintains pressure during its natural attack score");
        // Electricity now targets actual breakable modules, including their coatings.
        var upgrades = CombatUpgrades.Current;
        upgrades.Add(UpgradeKind.Gasoline);
        upgrades.Add(UpgradeKind.Poison);
        float before = station.Parts[4].health;
        var electricity = Instantiate(Resources.Load<GameObject>("Bullets/PlayerElectricityBullet"), station.Parts[4].transform.position + Vector3.down * .8f, Quaternion.identity).GetComponent<ElectricityBullet>();
        electricity.damage = 8;
        electricity.followness = 1;
        yield return new WaitForSeconds(.05f);
        Check(station.Parts[4].health < before && station.Parts[4].GetComponent<BurningHull>() != null && station.Parts[4].GetComponent<PoisonedHull>() != null, "Electricity reaches exposed module targets and applies both coatings");
        DesertReview.Capture("Electric-Coatings", Folder);
        Clear();
        yield return null;
        LevelRun.Reset(LevelId.Snow, 17381);
        LevelEnvironment.Apply();
        yield return null;
        var snow = (SnowSiegeBoss)DistrictEnemy.Spawn(typeof(SnowSiegeBoss), new Vector3(0, Camera.UpLimit - 2.3f, 1));
        yield return null;
        snow.health *= .45f;
        int snowPeak = 0;
        float end = Time.time + 10;
        bool held = false;
        while (Time.time < end)
        {
            snowPeak = Mathf.Max(snowPeak, CombatRound.Live.Count);
            foreach (var shell in CombatRound.Live.OfType<BurstShell>())
            {
                float age = (float)typeof(CombatRound).GetField("age", Hidden).GetValue(shell);
                float stop = (float)typeof(BurstShell).GetField("cruise", Hidden).GetValue(shell) + (float)typeof(BurstShell).GetField("brake", Hidden).GetValue(shell);
                held |= age > stop + 1.2f;
            }
            yield return null;
        }
        Check(snowPeak > 80 && snowPeak <= CombatRound.Limit && !held, "Snow retains dense ring patterns without shells waiting excessively at the cap; peak " + snowPeak);
        DesertReview.Capture("Snow-Balanced", Folder);
        Clear();
        yield return null;
        LevelRun.Reset(LevelId.Thunder, 17381);
        var choice = LevelChoice.Open(ship.transform, _ => { });
        yield return new WaitForSeconds(.4f);
        Check(choice.Options.SequenceEqual(new[] { LevelId.Desert, LevelId.Candyland }), "The actual third-district selection contains only Desert and Candyland");
        DesertReview.Capture("Two-Routes", Folder);
        Destroy(choice.gameObject);
    }
}
#endif
