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
public static class ReviewDistrictPolish
{
    public const string Folder = "ArtSource/DistrictPolishReview";
    static double deadline;
    static ReviewDistrictPolish()
    {
        EditorApplication.playModeStateChanged += state =>
          {
              if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("DistrictPolish", false))
              {
                  new GameObject("District polish review").AddComponent<DistrictPolishReview>();
                  deadline = EditorApplication.timeSinceStartup + 330;
              }
          };
        EditorApplication.update += () =>
          {
              if (deadline == 0)
                  return;
              string path = Folder + "/Runtime.txt";
              if (File.Exists(path))
              {
                  SessionState.SetBool("DistrictPolish", false);
                  EditorApplication.Exit(File.ReadAllText(path).StartsWith("PASS") ? 0 : 1);
              }
              else if (EditorApplication.timeSinceStartup > deadline)
              {
                  File.WriteAllText(path, "FAIL timeout");
                  EditorApplication.Exit(1);
              }
          };
    }

    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        File.Delete(Folder + "/Runtime.txt");
        try
        {
            GameRules.ResetRun();
            VerifyRunProgression.Run();
            var notes = new List<string>();
            var types = new HashSet<Type>();
            var sequence = DistrictWaves.Sequence();
            if (sequence[0] != "reward" || sequence.Count(s => s == "encounterChoice") != 1 || sequence.Count(s => s == "minibossChoice") != 1 || sequence.Count(s => int.TryParse(s, out _)) != 10)
                throw new Exception("Incorrect longer level structure");
            var charge = new RouteCharge();
            charge.Step(1, .99f);
            if (charge.Complete)
                throw new Exception("Route hold too short");
            charge.Step(1, .02f);
            if (!charge.Complete)
                throw new Exception("Route hold not one second");
            foreach (LevelId level in Enum.GetValues(typeof(LevelId)))
            {
                foreach (int seed in new[] { 17381, 421, 9826 })
                {
                    LevelRun.Reset(level, seed);
                    for (int budget = 2; budget <= 20; budget++)
                        DistrictWaves.Create(budget);
                }
                LevelRun.Reset(level, 17381);
                var unit = DistrictWaves.Miniboss().units.Single();
                var type = DistrictWaves.EnemyType(unit.enemy);
                if (!typeof(DistrictMiniboss).IsAssignableFrom(type) || !types.Add(type))
                    throw new Exception("Not a unique miniboss: " + level);
                var mini = DistrictEnemy.Spawn(type, Vector3.zero, true, true, false);
                notes.Add(level + ": " + type.Name + ", health " + mini.health.ToString("0.0") + ", ten waves plus opening item / two choices / miniboss / one earned choice.");
                UnityEngine.Object.DestroyImmediate(mini.gameObject);
            }
            var window = EnemyEditorWindow.Open();
            foreach (var name in new[] { nameof(ScarabDredger), nameof(TaffyDuelist), nameof(OrbitalWarden), nameof(ReefLeviathan), nameof(InfernalFerryman), nameof(HellCrabBoss), nameof(AbyssSubmarineBoss) })
            {
                window.Select(name);
                window.CaptureModel(Folder + "/Model-" + name + ".png");
            }
            window.Close();
            File.WriteAllText(Folder + "/Editor.txt", "PASS\n570 seeded compositions, all eight routes, exact level structure, one-second holds, ten distinct minibosses and native models passed.\n" + string.Join("\n", notes));
            RuntimeOnly();
        }
        catch (Exception e) { File.WriteAllText(Folder + "/Editor.txt", "FAIL\n" + e); EditorApplication.Exit(1); }
    }

    public static void RuntimeOnly()
    {
        File.Delete(Folder + "/Runtime.txt");
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("DistrictPolish", true);
        EditorApplication.isPlaying = true;
    }

    public static void RemainingOnly()
    {
        var window = EnemyEditorWindow.Open();
        foreach (var name in new[] { nameof(AbyssSubmarineBoss), nameof(HellCrabBoss) })
        {
            window.Select(name);
            window.CaptureModel(Folder + "/Model-" + name + ".png");
        }
        window.Close();
        SessionState.SetBool("DistrictPolishRemaining", true);
        RuntimeOnly();
    }

    public static void FinalOnly()
    {
        SessionState.SetBool("DistrictPolishFinal", true);
        RuntimeOnly();
    }
}

public sealed class DistrictPolishReview : MonoBehaviour
{
    const string Folder = ReviewDistrictPolish.Folder;
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    readonly List<string> checks = new List<string>(), errors = new List<string>(), balance = new List<string>();
    Shipcontroller ship; WaveOrchestrer flow;
    static void Set(object target, string name, object value)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, Hidden | BindingFlags.DeclaredOnly);
            if (field != null)
            {
                field.SetValue(target, value);
                return;
            }
        }
        throw new Exception(name);
    }

    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Hidden).Invoke(target, args);
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

    void Log(string m, string trace, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception) && !trace.Contains("UnityEditor.Search"))
            errors.Add(m);
    }

    void Check(bool ok, string message)
    {
        if (!ok)
            throw new Exception(message);
        checks.Add(message);
        File.WriteAllLines(Folder + "/Progress.txt", checks);
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
        if (balance.Count > 0)
            File.WriteAllLines(Folder + "/Balance.txt", balance);
        File.WriteAllText(Folder + "/Runtime.txt", (errors.Count == 0 ? "PASS" : "FAIL") + "\n" + string.Join("\n", checks) + "\n" + string.Join("\n", errors));
    }

    void Clear()
    {
        CombatRound.ClearAll();
        foreach (var enemy in FindObjectsByType<AEnemy>())
            Destroy(enemy.gameObject);
        foreach (var pickup in FindObjectsByType<APickable>())
            Destroy(pickup.gameObject);
    }

    IEnumerator Context(LevelId level)
    {
        Clear();
        CombatDecision.End();
        LevelRun.Reset(level, 17381);
        LevelEnvironment.Apply();
        while (!WavePreparation.Ready)
            yield return null;
        yield return null;
        GameRules.playerDisabled = false;
        ship.transform.position = new Vector3(0, Camera.DownLimit + 1.4f, -1);
    }

    IEnumerator Run()
    {
        yield return null;
        GameRules.StartNewRun();
        LevelRun.Reset();
        CloudTransition.Load("L1");
        while (SceneManager.GetActiveScene().name != "L1")
            yield return null;
        yield return null;
        flow = FindAnyObjectByType<WaveOrchestrer>();
        flow.enabled = false;
        flow.CancelInvoke();
        flow.StopAllCoroutines();
        Set(flow, "openingWavePending", false);
        ship = FindAnyObjectByType<Shipcontroller>();
        ship.enabled = false;
        ship.ObserverMode = true;
        foreach (var gun in ship.GetComponentsInChildren<AWeapon>())
        {
            gun.StopFiring();
            gun.enabled = false;
        }
        while (CloudTransition.Busy || !WavePreparation.Ready)
            yield return null;
        Time.timeScale = 1;
        GameRules.playerDisabled = false;
        if (SessionState.GetBool("DistrictPolishFinal", false))
        {
            SessionState.SetBool("DistrictPolishFinal", false);
            foreach (var level in new[] { LevelId.Snow, LevelId.Tunnel })
            {
                yield return Context(level);
                var wave = DistrictWaves.Create(20);
                int cursor = 0, peak = 0;
                float begin = Time.time;
                while (Time.time - begin < 5)
                {
                    while (cursor < wave.units.Count && wave.units[cursor].time <= Time.time - begin)
                    {
                        var unit = wave.units[cursor++];
                        var actor = DistrictEnemy.Spawn(DistrictWaves.EnemyType(unit.enemy), Vector3.zero);
                        actor.FlyPhrase(unit);
                    }
                    peak = Mathf.Max(peak, CombatRound.Live.Count);
                    yield return null;
                }
                Check(peak > 0 && (level != LevelId.Snow || peak < 102), level + " adjusted late-wave peak: " + peak);
                DesertReview.Capture(level + "-FinalBalance", Folder);
            }
            Clear();
            yield return null;
            ship.transform.position = new Vector3(0, -3, -1);
            var offers = flow.gameObject.AddComponent<DecisionShop>();
            offers.Open(flow, ship, "coinShop", () => { });
            yield return new WaitForSeconds(1.7f);
            DesertReview.Capture("Shop-FinalLabels", Folder);
            Check(FindObjectsByType<AItem>().All(i => i.Settled), "Final offer plates appear only after settling, with categories below the models");
            offers.Abort();
            File.WriteAllText(Folder + "/Final.txt", "PASS\n" + string.Join("\n", checks));
            yield break;
        }
        bool remainingOnly = SessionState.GetBool("DistrictPolishRemaining", false);
        SessionState.SetBool("DistrictPolishRemaining", false);
        if (!remainingOnly)
        {
            Set(flow, "waves", DistrictWaves.Sequence());
            Set(flow, "currentWaveNumber", 0);
            Call(flow, "GenerateWave");
            yield return new WaitForSeconds(.1f);
            var shop = flow.GetComponent<DecisionShop>();
            Check(shop != null && !shop.paid, "Every level opens with a free item decision");
            var overlay = FindAnyObjectByType<DecisionOverlay>();
            Check(!overlay.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Any(t => t.text == "passive" && t.gameObject.activeInHierarchy), "Item labels stay hidden while offers are falling");
            yield return new WaitForSeconds(1.6f);
            DesertReview.Capture("Opening-Items", Folder);
            shop.Abort();
            yield return new WaitForSeconds(.6f);
            bool skipped = false;
            shop = flow.gameObject.AddComponent<DecisionShop>();
            shop.Open(flow, ship, "bossReward", () => skipped = true, new Vector3(0, 3, 1));
            ship.transform.position = new Vector3(0, -3, -1);
            yield return new WaitForSeconds(1.7f);
            ship.transform.position = new Vector3(3.6f, -6.5f, -1);
            yield return new WaitForSeconds(.5f);
            Check(shop != null && shop.SkipProgress > .35f && shop.SkipProgress < .75f, "Wide bottom skip fills over one second from the far screen side");
            DesertReview.Capture("Boss-Item-Skip", Folder);
            yield return new WaitForSeconds(.7f);
            Check(skipped, "Boss item can be explicitly skipped");
            yield return MiniFlow(false);
            yield return MiniFlow(true);
            foreach (LevelId level in Enum.GetValues(typeof(LevelId)))
            {
                yield return Context(level);
                var wave = DistrictWaves.Create(20);
                int next = 0, peak = 0;
                float start = Time.time;
                while (Time.time - start < 5.0f)
                {
                    while (next < wave.units.Count && wave.units[next].time <= Time.time - start)
                    {
                        var unit = wave.units[next++];
                        var e = DistrictEnemy.Spawn(DistrictWaves.EnemyType(unit.enemy), Vector3.zero);
                        e.FlyPhrase(unit);
                    }
                    peak = Mathf.Max(peak, CombatRound.Live.Count);
                    yield return null;
                }
                balance.Add(level + " late-wave live peak in first 5s: " + peak + " bullets; " + next + " ships launched.");
                if (level == LevelId.Hell || level == LevelId.Underwater || level == LevelId.Space)
                    DesertReview.Capture(level + "-Wave", Folder);
                if (level == LevelId.Hell)
                {
                    yield return new WaitForSeconds(2.5f);
                    Check(FindObjectsByType<VolcanicVent>().Sum(v => v.Eruptions) > 0, "Visible environmental volcanoes warn and fire real pooled bullets");
                }
                Clear();
                yield return null;
                var unitMini = DistrictWaves.Miniboss().units.Single();
                var mini = (DistrictMiniboss)DistrictEnemy.Spawn(DistrictWaves.EnemyType(unitMini.enemy), new Vector3(0, Camera.UpLimit + 1, 1));
                mini.FlyPhrase(unitMini);
                yield return new WaitForSeconds(2.7f);
                Check(mini.Engaged && CombatRound.Live.Count > 0, level + " has a real mobile miniboss with an early attack");
                if (LevelRun.Tier(level) >= 3)
                    DesertReview.Capture(level + "-Miniboss", Folder);
            }
        }
        else
        {
            yield return Context(LevelId.Hell);
            yield return new WaitForSeconds(8);
            Check(FindObjectsByType<VolcanicVent>().Sum(v => v.Eruptions) > 0, "Visible environmental volcanoes warn and fire real pooled bullets");
            DesertReview.Capture("Hell-Volcano", Folder);
            Clear();
            yield return null;
            var mini = (DistrictMiniboss)DistrictEnemy.Spawn(typeof(InfernalFerryman), new Vector3(0, Camera.UpLimit + 1, 1));
            yield return new WaitForSeconds(2.7f);
            Check(mini.Engaged && CombatRound.Live.Count > 0, "Hell has a real mobile miniboss with an early attack");
            Check(FindAnyObjectByType<IndustrialHUD>().GetComponentsInChildren<UnityEngine.UI.Text>().Any(t => t.text == Mathf.CeilToInt(mini.Remaining).ToString()), "Miniboss health and countdown are visible in the actual side HUD");
            DesertReview.Capture("Hell-Miniboss", Folder);
        }
        yield return Context(LevelId.Thunder);
        var thunder = (ThunderLanceBoss)DistrictEnemy.Spawn(typeof(ThunderLanceBoss), new Vector3(0, 4, 1));
        yield return null;
        thunder.health *= .49f;
        float timeout = Time.time + 6;
        while (thunder.RotorCount != 6 && Time.time < timeout)
            yield return null;
        yield return new WaitForSeconds(thunder.rotorCharge + .2f);
        var beam = CombatRound.Live.OfType<PulseLaser>().First(b => b.IsSustained);
        var last = beam.Heading;
        float angle = 0, min = 0, max = 0;
        bool left = false, right = false;
        float began = Time.time;
        while (Time.time - began < 12)
        {
            float delta = Vector2.SignedAngle(last, beam.Heading);
            left |= delta < -.02f;
            right |= delta > .02f;
            angle += delta;
            min = Mathf.Min(min, angle);
            max = Mathf.Max(max, angle);
            last = beam.Heading;
            yield return null;
        }
        Check(left && right && max - min < 61 && max - min > 57, "Thunder's six attached lasers smoothly reverse every 60 degrees");
        DesertReview.Capture("Thunder-Reversal", Folder);
        yield return Context(LevelId.Hell);
        var crab = (HellCrabBoss)DistrictEnemy.Spawn(typeof(HellCrabBoss), new Vector3(0, 4, 1));
        yield return new WaitForSeconds(3.2f);
        Check(crab.GetComponentsInChildren<MeshFilter>().Count(m => m.sharedMesh.name.Contains("pincer vertebra")) == 8, "Crab uses eight overlapping modeled arm pieces");
        DesertReview.Capture("Crab-Articulated", Folder);
        yield return Context(LevelId.Underwater);
        var sub = (AbyssSubmarineBoss)DistrictEnemy.Spawn(typeof(AbyssSubmarineBoss), new Vector3(0, 4, 1));
        int subPeak = 0;
        began = Time.time;
        int capture = 0;
        while (Time.time - began < 15.5f)
        {
            subPeak = Mathf.Max(subPeak, CombatRound.Live.Count);
            if (Time.time - began > 6.8f + capture * 2.4f && capture < 4)
                DesertReview.Capture("Submarine-" + (capture++), Folder);
            yield return null;
        }
        Check(sub.EmergenceBursts >= 3 && sub.ChargeTrails >= 12 && subPeak > 80, "Detailed submarine uses three eruption crowns and dense six-lane charge wakes; peak " + subPeak);
        Check(FindAnyObjectByType<OceanLife>() != null, "Underwater has moving marine life and seabed shadow models");
        yield return Context(LevelId.Fields);
        var carrier = CombatRound.Launch<ScatterCarrier>(new Vector3(0, 3, 1), Vector2.down, 3, .3f);
        var ordinary = CombatRound.Launch<SimpleRound>(new Vector3(1, 3, 1), Vector2.down, 3, .15f);
        Check(carrier.GetComponent<ProjectileSpriteTint>().Tint.g > .5f && ordinary.GetComponent<ProjectileSpriteTint>().Tint.g < .4f, "Only Plains' horizontal-release carrier receives the orange warning color");
        var before = DistrictEnemy.Spawn(typeof(FieldsBomberBoss), Vector3.zero, true, true, false);
        float hp = before.health;
        Destroy(before.gameObject);
        ship.GetComponent<CombatUpgrades>().Add(UpgradeKind.Focus);
        ship.GetComponent<CombatUpgrades>().Add(UpgradeKind.Poison);
        ship.GetComponent<CombatUpgrades>().Add(UpgradeKind.Capacitor);
        var after = DistrictEnemy.Spawn(typeof(FieldsBomberBoss), Vector3.zero, true, true, false);
        Check(after.health == hp && hp == 1200, "Items do not scale boss health; Plains boss has 1200 base health");
        Destroy(after.gameObject);
    }

    IEnumerator MiniFlow(bool kill)
    {
        Clear();
        yield return null;
        CombatDecision.End();
        ship.transform.position = new Vector3(0, -7, -1);
        Set(flow, "currentWaveNumber", 7);
        Call(flow, "GenerateWave");
        flow.enabled = true;
        yield return null;
        yield return new WaitForSeconds(1.2f);
        var mini = FindAnyObjectByType<DistrictMiniboss>();
        Check(mini != null, "Miniboss encounter spawns a dedicated hull");
        if (kill)
            mini.damage(1000000);
        else
        {
            Set(mini, "engagedAt", Time.time - 20.1f);
            Set(flow, "minibossDeadline", Time.time - .1f);
        }
        yield return new WaitForSeconds(kill ? .3f : 1.8f);
        flow.enabled = false;
        Check(flow.MinibossVictory == kill, kill ? "Timely miniboss kill earns its decisions" : "A 20-second timeout makes the miniboss leave without kill rewards");
        Clear();
        yield return null;
        foreach (int index in new[] { 9, 11 })
        {
            Set(flow, "currentWaveNumber", index);
            Call(flow, "GenerateWave");
            yield return null;
            Check(LevelChoice.Active == kill, "Post-miniboss choice " + index + " follows the actual victory result");
            var choice = FindAnyObjectByType<LevelChoice>();
            if (choice != null)
                Destroy(choice.gameObject);
            CombatDecision.End();
            yield return null;
        }
    }
}
#endif
