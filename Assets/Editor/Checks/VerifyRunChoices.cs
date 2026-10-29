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
public static class VerifyRunChoices
{
    public const string Folder = "ArtSource/RunChoicesReview";
    static double deadline;
    static VerifyRunChoices()
    {
        EditorApplication.playModeStateChanged += state =>
          {
              if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("RunChoicesReview", false))
                  return;
              new GameObject("Run choices and explosion review").AddComponent<RunChoicesReview>();
              deadline = EditorApplication.timeSinceStartup + 900;
          };
        EditorApplication.update += () =>
          {
              if (deadline == 0)
                  return;
              var report = Folder + (SessionState.GetBool("RunChoicesBaseline", false) ? "/Baseline.txt" : "/Verification.txt");
              if (File.Exists(report))
              {
                  SessionState.SetBool("RunChoicesReview", false);
                  CheckHarness.Exit(File.ReadAllText(report).StartsWith("PASS") ? 0 : 1);
              }
              else if (EditorApplication.timeSinceStartup > deadline)
              {
                  File.WriteAllText(report, "FAIL timeout");
                  CheckHarness.Exit(1);
              }
          };
    }

    public static void Baseline() => Begin(true);
    public static void Run() => Begin(false);
    static void Begin(bool baseline)
    {
        Directory.CreateDirectory(Folder + "/Frames");
        File.Delete(Folder + (baseline ? "/Baseline.txt" : "/Verification.txt"));
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        SessionState.SetBool("RunChoicesReview", true);
        SessionState.SetBool("RunChoicesBaseline", baseline);
        CheckHarness.EnterPlayMode();
    }
}
public sealed class RunChoicesReview : MonoBehaviour
{
    const string Folder = VerifyRunChoices.Folder;
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    readonly List<string> checks = new List<string>(), errors = new List<string>();
    Shipcontroller ship; WaveOrchestrer flow;
    static readonly MethodInfo release = typeof(CombatRound).GetMethod("Release", Hidden);
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
        File.WriteAllText(Folder + "/Progress.txt", string.Join("\n", checks));
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
        File.WriteAllText(Folder + (SessionState.GetBool("RunChoicesBaseline", false) ? "/Baseline.txt" : "/Verification.txt"), (errors.Count == 0 ? "PASS" : "FAIL") + "\n" + string.Join("\n", checks) + "\n" + string.Join("\n", errors));
    }

    void Clear()
    {
        foreach (var round in CombatRound.Live.ToArray())
        {
            release.Invoke(round, null);
            CombatRound.Live.Remove(round);
        }
        foreach (var e in FindObjectsByType<AEnemy>())
            Destroy(e.gameObject);
        foreach (var b in FindObjectsByType<ABullet>())
            Destroy(b.gameObject);
        foreach (var p in FindObjectsByType<APickable>())
            Destroy(p.gameObject);
    }

    IEnumerator Run()
    {
        GameRules.StartNewRun();
        LevelRun.Reset(LevelId.Snow, 17381);
        yield return SceneManager.LoadSceneAsync("L1");
        yield return null;
        flow = FindAnyObjectByType<WaveOrchestrer>();
        flow.enabled = false;
        flow.StopAllCoroutines();
        flow.CancelInvoke();
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
        yield return ExplosionProfile();
        if (SessionState.GetBool("RunChoicesBaseline", false))
            yield break;
        yield return SystemsChecks();
    }

    IEnumerator ExplosionProfile()
    {
        Clear();
        yield return null;
        Time.timeScale = 0;
        var shells = new BurstShell[16];
        for (int i = 0; i < shells.Length; i++)
            shells[i] = CombatRound.Launch<BurstShell>(new Vector3(-3 + i % 4 * 2, 5 - i / 4 * 2.5f), Vector2.down, 4, .35f).SetTravelDistance(.1f);
        int before = FindObjectsByType<CombatRound>(FindObjectsInactive.Include).Length;
        var tick = typeof(BurstShell).GetMethod("Tick", Hidden);
        var age = typeof(CombatRound).GetField("age", Hidden);
        foreach (var shell in shells)
            age.SetValue(shell, 2f);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        foreach (var shell in shells)
            tick.Invoke(shell, new object[] { 0f });
        watch.Stop();
        int created = FindObjectsByType<CombatRound>(FindObjectsInactive.Include).Length - before;
        int sparks = FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include).Count(p => p.name.IndexOf("shell ignition sparks", StringComparison.OrdinalIgnoreCase) >= 0);
        Check(CombatRound.Live.Count == 192, "sixteen simultaneous shells release all 192 child bullets");
        checks.Add("MEASURE simultaneous shell detonation CPU: " + watch.Elapsed.TotalMilliseconds.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + " ms; new bullet objects: " + created + "; ignition particle systems: " + sparks + ".");
        Capture("Explosion-Stress");
        if (!SessionState.GetBool("RunChoicesBaseline", false))
            Check(created == 0 && sparks == 1, "first combat detonation uses prewarmed bullets and one shared ignition renderer");
        Clear();
        Time.timeScale = 1;
        yield return null;
    }

    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, Hidden).SetValue(owner, value);
    static object Get(object owner, string name) => owner.GetType().GetField(name, Hidden).GetValue(owner);
    static void Call(object owner, string name, params object[] values) => owner.GetType().GetMethod(name, Hidden).Invoke(owner, values);
    IEnumerator ExitShop(DecisionShop shop)
    {
        ship.transform.position = new Vector3(0, Camera.DownLimit - 1, -1);
        float start = Time.time;
        while (shop != null && Time.time - start < 4)
            yield return null;
        Check(shop == null, "shop continues only after holding the bottom skip zone");
        ship.transform.position = new Vector3(0, -4, -1);
    }

    IEnumerator Buy(DecisionShop shop, AItem item)
    {
        ship.transform.position = item.transform.position + Vector3.back * 2;
        Check(shop.TryClaim(item), "close contact reserves an affordable offer");
        item.GrantTo(ship);
        yield return null;
    }

    IEnumerator SystemsChecks()
    {
        int fireballs = PSXFireballs.Spawned;
        for (int i = 0; i < 280; i++)
            CombatRound.Launch<SimpleRound>(new Vector3(i % 14 * .3f - 2, 5 - i / 14 * .4f), Vector2.down, 0);
        foreach (var shot in CombatRound.Live.ToArray())
            shot.Vanish();
        Check(CombatRound.Live.Count == 0 && PSXFireballs.Spawned == fireballs, "clearing 280 bullets uses shared particles without spawning individual shockwave renderers");
        yield return null;
        var snow = (SnowSiegeBoss)DistrictEnemy.Spawn(typeof(SnowSiegeBoss), new Vector3(0, Camera.UpLimit - 2.3f, 1));
        float started = Time.time, last = -1;
        int frame = 0, peak = 0;
        while (Time.time - started < 8)
        {
            float elapsed = Time.time - started;
            peak = Mathf.Max(peak, CombatRound.Live.Count);
            if (elapsed - last > .12f)
            {
                last = elapsed;
                Capture("Frames/Explosions-" + (frame++).ToString("D4"));
            }
            yield return null;
        }
        Check(peak > 100 && FindObjectsByType<ParticleSystem>().Count(p => p.name == "Shared shell ignition sparks") == 1, "moving shell barrage retains dense patterns with one shared ignition buffer");
        Capture("Shell-Barrage");
        Clear();
        yield return null;
        Score.ResetRun();
        Score.IncreaseScore(1234);
        RunCoins.Reset();
        int score = Score.CurrentScore;
        var coin = Instantiate(Resources.Load<GameObject>("Pickables/money"), ship.transform.position, Quaternion.identity);
        coin.GetComponent<Money>().SendMessage("PickUp");
        yield return null;
        Check(RunCoins.Count == 1 && Score.CurrentScore == score, "a collected coin adds one coin without changing score");
        Check(!RunCoins.TrySpend(2) && !RunCoins.TrySpend(-1) && RunCoins.Count == 1, "wallet rejects insufficient funds and negative prices");
        RunCoins.Add(499);
        foreach (string kind in new[] { "coinShop", "multiplierShop" })
        {
            Clear();
            ship.transform.position = new Vector3(0, -4, -1);
            typeof(Score).GetMethod("SetMultiplier", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { 8 });
            var shop = flow.gameObject.AddComponent<DecisionShop>();
            Set(flow, "decision", shop);
            bool done = false;
            shop.Open(flow, ship, kind, () => done = true);
            yield return new WaitForSeconds(1.7f);
            var items = ((List<AItem>)Get(shop, "items")).Where(i => i != null).ToArray();
            var prices = (Dictionary<AItem, int>)Get(shop, "prices");
            Check(items.Length >= 3 && items.Length <= 4, kind + " offers three or four items");
            int coins = RunCoins.Count, multi = Score.CurrentMultiplier;
            score = Score.CurrentScore;
            ship.transform.position = new Vector3(0, -4, -1);
            Check(!shop.TryClaim(items[0]), "passing far from an offer cannot buy it");
            yield return Buy(shop, items[0]);
            Check(!done && shop != null && CombatDecision.Active, kind + " remains open after the first purchase");
            yield return Buy(shop, items[1]);
            Check(!done && shop != null, kind + " allows a second purchase without ending the shop");
            int total = prices[items[0]] + prices[items[1]];
            Check(Score.CurrentScore == score && (kind == "coinShop" ? RunCoins.Count == coins - total && Score.CurrentMultiplier == multi : RunCoins.Count == coins && Score.CurrentMultiplier == multi - total), kind + " spends only its own currency");
            yield return new WaitForSeconds(.2f);
            Capture(kind + "-AfterTwoPurchases");
            yield return new WaitForSeconds(.4f);
            Check(Score.CurrentMultiplier == (kind == "coinShop" ? multi : multi - total), "multiplier decay stays frozen while shopping");
            if (kind == "coinShop")
            {
                for (int i = 2; i < items.Length; i++)
                    yield return Buy(shop, items[i]);
                Check(shop != null && !done && CombatDecision.Active, "buying every shop item still requires the explicit bottom exit");
                Capture("Shop-SoldOut");
            }
            yield return ExitShop(shop);
            Check(done && !CombatDecision.Active, "shop exit resumes the run exactly once");
        }
        RunCoins.Reset();
        var poor = flow.gameObject.AddComponent<DecisionShop>();
        Set(flow, "decision", poor);
        poor.Open(flow, ship, "coinShop", () => { });
        yield return new WaitForSeconds(1.7f);
        var offer = ((List<AItem>)Get(poor, "items"))[0];
        ship.transform.position = offer.transform.position + Vector3.back * 2;
        Check(!poor.TryClaim(offer) && Score.CurrentScore == score, "a high score cannot pay for a coin shop");
        poor.Abort();
        yield return null;
        var all = new HashSet<EncounterKind>();
        for (int seed = 0; seed < 80; seed++)
        {
            var options = EncounterChoice.Draw(seed, true, 70, 100);
            Check(options.Length >= 1 && options.Length <= 2 && options.Distinct().Count() == options.Length, "encounter draw " + seed + " has one or two distinct choices");
            Check(!options.Contains(EncounterKind.Shop) && !options.Contains(EncounterKind.MultShop), "shops are not offered mid-run");
            Check(!(options.Contains(EncounterKind.Item) && options.Contains(EncounterKind.Challenge)), "item and challenge are never offered together");
            foreach (var option in options)
                all.Add(option);
            Check(!EncounterChoice.Draw(seed, false, 70, 100).Contains(EncounterKind.Challenge), "challenge excluded when three waves are unavailable");
            var healthy = EncounterChoice.Draw(seed, true, 70.01f, 100);
            Check(!healthy.Contains(EncounterKind.Health), "health excluded above 70 percent");
            Check(!(healthy.Contains(EncounterKind.Item) && healthy.Contains(EncounterKind.Challenge)), "item and challenge stay exclusive above the health threshold");
        }
        Check(all.Count == 3, "item, health and challenge encounters are reachable");
        Check(DistrictWaves.Sequence()[0] == "reward" && DistrictWaves.Sequence().Count(x => x == "encounterChoice") == 1 && DistrictWaves.Sequence().Count(x => x == "minibossChoice") == 1 && DistrictWaves.Sequence().Count(x => int.TryParse(x, out _)) == 10, "one pre-miniboss fork preserves the opening reward, ten combat waves and the earned miniboss selection");
        // The fork rides along with the next wave: it is offered half-way through without pausing combat.
        flow.waves = DistrictWaves.Sequence();
        Set(flow, "currentWaveNumber", 4);
        CombatRound.Launch<SimpleRound>(Vector3.zero, Vector2.down, 0);
        Call(flow, "GenerateWave");
        yield return null;
        Check(CombatRound.Live.Count > 0 && !CombatDecision.Active && flow.RewardOffer == null && (bool)Get(flow, "isWaveComingOut"), "wave fork keeps combat running and waits for the middle of the next wave");
        Set(flow, "openingWavePending", false);
        Set(flow, "waveTime", (float)Get(flow, "offerAt"));
        Call(flow, "Update");
        var hexes = flow.RewardOffer;
        Check(hexes != null && flow.EncounterOptions.Length == 2 && !CombatDecision.Active, "mid-wave offer opens over live combat with two hexagons");
        var zones = (Array)Get(hexes, "zones");
        var centre = (Vector2)zones.GetValue(0).GetType().GetField("centre").GetValue(zones.GetValue(0));
        // Hovering part of the way shows the faint tint and a partial border charge.
        float hover = Time.time;
        while (Time.time - hover < 1.5f)
        {
            ship.transform.position = new Vector3(centre.x, centre.y, ship.transform.position.z);
            yield return null;
        }
        Capture("Encounter-Hexagons");
        Check(flow.RewardOffer == hexes, "a hexagon needs a full three-second hold");
        Set(hexes, "age", RewardHexes.LifeSeconds);
        ship.transform.position = new Vector3(0, -4, -1);
        yield return null;
        Check(flow.RewardOffer == null && FindAnyObjectByType<DecisionShop>() == null, "an offer nobody holds lapses after twenty seconds and leaves combat untouched");
        var place = typeof(RewardHexes).GetMethod("Place", BindingFlags.Static | BindingFlags.NonPublic);
        float itemHeight = 0, healthHeight = 0;
        for (int seed = 0; seed < 60; seed++)
        {
            var spots = (Vector2[])place.Invoke(null, new object[] { new[] { EncounterKind.Item, EncounterKind.Health }, seed, new Vector2(0, -6) });
            Check(spots.All(spot => spot.x - RewardHexes.Radius > Camera.LeftLimit && spot.x + RewardHexes.Radius < Camera.RightLimit && spot.y + RewardHexes.Radius < Camera.UpLimit), "hexagon placement " + seed + " stays inside the arena");
            Check(Vector2.Distance(spots[0], spots[1]) > RewardHexes.Radius * 2, "hexagon placement " + seed + " keeps the two zones apart");
            itemHeight += spots[0].y;
            healthHeight += spots[1].y;
        }
        Check(itemHeight > healthHeight, "items lean toward the riskier upper arena");
        Call(flow, "OpenEncounterChoice");
        hexes = flow.RewardOffer;
        zones = (Array)Get(hexes, "zones");
        centre = (Vector2)zones.GetValue(1).GetType().GetField("centre").GetValue(zones.GetValue(1));
        var picked = flow.EncounterOptions[1];
        hover = Time.time;
        while (flow.RewardOffer != null && Time.time - hover < 5)
        {
            ship.transform.position = new Vector3(centre.x, centre.y, ship.transform.position.z);
            yield return null;
        }
        Check(flow.RewardOffer == null && Time.time - hover > 2.9f, "holding a hexagon for three seconds takes its " + picked + " reward");
        ship.transform.position = new Vector3(0, -4, -1);
        Set(flow, "challengeWavesRemaining", 0);
        flow.CancelInvoke();
        Call(flow, "OpenEncounterChoice");
        var strip = flow.RewardOffer;
        Set(flow, "isWaveComingOut", false);
        flow.waves = new[] { "reward", "8" };
        Set(flow, "currentWaveNumber", 0);
        Call(flow, "GenerateWave");
        Check(strip != null && flow.RewardOffer == null && flow.EncounterOptions == null, "a decision or boss silently withdraws an unanswered offer");
        foreach (var shop in FindObjectsByType<DecisionShop>())
            shop.Abort();
        flow.CancelInvoke();
        Clear();
        yield return null;
        Set(flow, "challengeWavesRemaining", 0);
        Set(flow, "challengeWave", false);
        Set(flow, "isWaveComingOut", false);
        ship.health = ship.maxHealth * .3f;
        Call(flow, "ResolveEncounter", EncounterKind.Health);
        float healStart = ship.health;
        yield return new WaitForSeconds(.3f);
        float healMiddle = ship.health;
        yield return new WaitForSeconds(.7f);
        Check(healStart < healMiddle && healMiddle < ship.health && Mathf.Abs(ship.health - ship.maxHealth * .8f) < .5f && !CombatDecision.Active, "health route visibly fills half of maximum health at once, without pausing");
        Clear();
        yield return null;
        flow.waves = new[] { "8", "10" };
        Set(flow, "currentWaveNumber", 1);
        Call(flow, "ResolveEncounter", EncounterKind.Item);
        Check(flow.waves[1] == "reward" && FindAnyObjectByType<DecisionShop>() == null, "item route waits for the next wave break");
        Call(flow, "GenerateWave");
        yield return new WaitForSeconds(1.7f);
        var reward = FindAnyObjectByType<DecisionShop>();
        Set(flow, "decision", reward);
        Check(reward != null && !reward.paid, "item route opens a free item choice at the next wave");
        var free = ((List<AItem>)Get(reward, "items"))[0];
        yield return Buy(reward, free);
        yield return null;
        Check(reward == null, "one free reward closes its item choice");
        flow.CancelInvoke();
        Set(flow, "isWaveComingOut", false);
        Clear();
        yield return null;
        flow.waves = new[] { "8", "10", "12", "2", "boss" };
        Set(flow, "currentWaveNumber", 0);
        Set(flow, "openingWavePending", false);
        Set(flow, "stopingWave", false);
        Call(flow, "ResolveEncounter", EncounterKind.Challenge);
        flow.CancelInvoke();
        Call(flow, "GenerateWave");
        Check(flow.ChallengeWavesRemaining == 3, "challenge starts with exactly three waves remaining");
        for (int wave = 0; wave < 3; wave++)
        {
            flow.enabled = true;
            yield return null;
            flow.enabled = false;
            var challenged = AEnemy.Live.OfType<DistrictEnemy>().FirstOrDefault();
            Check(challenged != null && challenged.Challenged && challenged.GetComponent<PatternMover>().TempoMultiplier > 1, "challenge wave " + (wave + 1) + " spawns faster, stronger enemies");
            var normal = DistrictEnemy.Spawn(challenged.GetType(), new Vector3(30, 30, 1));
            Check(Mathf.Abs(challenged.health - normal.health * 1.2f) < .1f, "challenge enemy health increases without changing global run stats");
            Destroy(normal.gameObject);
            if (wave == 0)
            {
                yield return new WaitForSeconds(2);
                Capture("Challenge-Active");
            }
            Clear();
            yield return null;
            Set(flow, "currentUnit", null);
            Set(flow, "waveTime", ((Wave)Get(flow, "currentWave")).duration + 1);
            Set(flow, "clearTime", .2f);
            Call(flow, "Update");
            Check(flow.ChallengeWavesRemaining == 2 - wave, "challenge counts down once for completed wave " + (wave + 1));
        }
        var prize = FindAnyObjectByType<DecisionShop>();
        Check(prize != null && !prize.paid, "clearing the third challenge wave opens its free item reward before later combat");
        ship.transform.position = new Vector3(0, -5, -1);
        yield return new WaitForSeconds(1.7f);
        Capture("Challenge-Reward");
        var earned = ((List<AItem>)Get(prize, "items"))[0];
        yield return Buy(prize, earned);
        Check(prize == null, "the challenge item is collected before normal combat resumes");
        flow.enabled = true;
        float nextSpawn = Time.time + 3;
        while (AEnemy.Live.Count == 0 && Time.time < nextSpawn)
            yield return null;
        flow.enabled = false;
        Check(AEnemy.Live.OfType<DistrictEnemy>().Any(e => !e.Challenged) && flow.ChallengeWavesRemaining == 0, "normal difficulty returns after the three-wave challenge");
        Clear();
        yield return null;
        LevelRun.Reset(LevelId.City, 17381);
        LevelEnvironment.Apply();
        while (!WavePreparation.Ready)
            yield return null;
        var city = (CityDuoBoss)DistrictEnemy.Spawn(typeof(CityDuoBoss), new Vector3(-2, 5, 1));
        yield return null;
        yield return null;
        Check(Mathf.Abs(city.health - 1000 * GameRules.enemyHealth * LevelRun.EnemyHealthScale) < .1f && Mathf.Abs(city.partner.health - 1400 * GameRules.enemyHealth * LevelRun.EnemyHealthScale) < .1f, "City duo reduced to 1000 and 1400 base health with run scaling");
        Capture("City-Duo");
        Clear();
        yield return null;
        RunCoins.Add(30);
        int saved = RunCoins.Count;
        LevelRun.Advance(LevelId.Thunder);
        Check(RunCoins.Count == saved, "district progression preserves the coin wallet");
        GameRules.StartNewRun();
        Check(RunCoins.Count == 0 && Score.CurrentScore == 0, "new runs reset coins and score independently");
    }
}
#endif
