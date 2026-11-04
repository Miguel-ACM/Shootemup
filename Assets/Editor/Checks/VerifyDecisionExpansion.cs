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
using UnityEngine.UI;

// One isolated session exercises actual pickups, trades, contact damage and UI.
[InitializeOnLoad]
public static class VerifyDecisionExpansion
{
    public const string Folder = "ArtSource/DecisionExpansionReview";
    const string Key = "DecisionExpansionReview";
    static double deadline;
    static VerifyDecisionExpansion()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false))
                return;
            deadline = EditorApplication.timeSinceStartup + 240;
            new GameObject("Decision expansion review").AddComponent<DecisionExpansionReview>();
        };
        EditorApplication.update += () =>
        {
            if (deadline == 0 || !SessionState.GetBool(Key, false))
                return;
            var path = Folder + "/Verification.txt";
            if (!File.Exists(path) && EditorApplication.timeSinceStartup < deadline)
                return;
            SessionState.SetBool(Key, false);
            if (!File.Exists(path))
                File.WriteAllText(path, "FAIL timeout");
            CheckHarness.Exit(File.ReadAllText(path).StartsWith("PASS") ? 0 : 1);
        };
    }

    public static void Run()
    {
        string project = Application.dataPath.Replace('\\', '/');
        if (!project.Contains("/Temp/DecisionHoverReview/") && !project.Contains("/Temp/StatHexReview/"))
            throw new Exception("Run from the isolated decision review project");
        Directory.CreateDirectory(Folder);
        File.Delete(Folder + "/Verification.txt");
        // Keep the review's save files separate from the player's run and hangar.
        PlayerSettings.companyName = "ShootemupReview";
        PlayerSettings.productName = "DecisionExpansionReview";
        SessionState.SetBool(Key, true);
        SessionState.SetString(Key + "Focus", VerifyCombatRework.Argument("--decision-focus", "all"));
        EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Show();
        EditorSceneManager.OpenScene("Assets/Scenes/L1.unity");
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/L1.unity");
        EditorApplication.isPlaying = true;
    }

    public static void Build()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/Temp/DecisionHoverReview/"))
            throw new Exception("Build from the isolated decision review project");
        if (!File.ReadAllText(Folder + "/Verification.txt").StartsWith("PASS"))
            throw new Exception("Complete the decision review first");
        PlayerSettings.companyName = "DefaultCompany";
        PlayerSettings.productName = "seup";
        string output = VerifyCombatRework.Argument("--decision-output", "");
        if (!Path.IsPathRooted(output) || Path.GetExtension(output) != ".exe")
            throw new Exception("Supply an absolute --decision-output .exe path");
        var report = UnityEditor.BuildPipeline.BuildPlayer(new UnityEditor.BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            target = BuildTarget.StandaloneWindows64, locationPathName = output,
            options = UnityEditor.BuildOptions.None });
        bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
        File.WriteAllText(Folder + "/Build.txt", (ok ? "PASS" : "FAIL") + "\nWindows x64: " + output +
            "\nErrors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings + "\n");
        EditorApplication.Exit(ok ? 0 : 1);
    }
}

public sealed class DecisionExpansionReview : MonoBehaviour
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    readonly List<string> checks = new List<string>(), errors = new List<string>();
    Shipcontroller ship;
    WaveOrchestrer flow;
    RunInventory inventory;
    AItem granted;
    void Awake() { Application.runInBackground = true; Application.logMessageReceived += Log; }
    void OnDestroy() { Application.logMessageReceived -= Log; }
    void Log(string message, string trace, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception) && !trace.Contains("UnityEditor.Search"))
            errors.Add(message + "\n" + trace);
    }
    void Check(bool ok, string message)
    {
        if (!ok)
            throw new Exception(message);
        checks.Add(message);
        File.WriteAllLines(VerifyDecisionExpansion.Folder + "/Progress.txt", checks);
    }
    static object Get(object target, string name) => target.GetType().GetField(name, Hidden).GetValue(target);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Hidden).SetValue(target, value);
    static void Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Hidden).Invoke(target, args);
    DecisionShop Shop => FindAnyObjectByType<DecisionShop>();
    List<AItem> Offers => (List<AItem>)Get(Shop, "items");

    IEnumerator Start()
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(Run());
        while (stack.Count > 0)
        {
            object step = null;
            bool more = false;
            Exception failure = null;
            try { more = stack.Peek().MoveNext(); if (more) step = stack.Peek().Current; }
            catch (Exception e) { failure = e; }
            if (failure != null) { errors.Add(failure.ToString()); break; }
            if (!more) { stack.Pop(); continue; }
            if (step is IEnumerator nested) { stack.Push(nested); continue; }
            yield return step;
        }
        File.WriteAllText(VerifyDecisionExpansion.Folder + "/Verification.txt", (errors.Count == 0 ? "PASS" : "FAIL") +
            "\n" + string.Join("\n", checks) + "\n" + string.Join("\n", errors));
    }

    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var image = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(VerifyDecisionExpansion.Folder + "/" + name + ".png", image.EncodeToPNG());
        Destroy(image);
    }
    IEnumerator Grant<T>() where T : AItem
    {
        var go = Instantiate(Resources.Load<GameObject>("Items/Item"), new Vector3(60, 60, 1), Quaternion.identity);
        granted = go.AddComponent<T>();
        granted.SetRarity(ItemCatalog.Get(typeof(T).Name).RarityKey);
        yield return null;
        granted.GrantTo(ship);
        yield return null;
    }
    IEnumerator Claim(AItem item)
    {
        float deadline = Time.time + 4;
        while (item != null && !item.Collected && Time.time < deadline)
        {
            ship.transform.position = new Vector3(item.transform.position.x, item.transform.position.y, -1);
            if (item.Settled && ship.GetComponent<ShipLoadout>().SlotFor(item) < 0)
                item.TryPick(1);
            yield return null;
        }
        Check(item == null || item.Collected, "close-contact collection completes");
        ship.transform.position = new Vector3(0, -3, -1);
        yield return null;
    }
    IEnumerator Skip()
    {
        float deadline = Time.time + 5;
        while (Shop != null && Time.time < deadline)
        {
            ship.transform.position = new Vector3(0, Camera.DownLimit, -1);
            yield return null;
        }
        Check(Shop == null, "bottom hold skips the exchange");
        ship.transform.position = new Vector3(0, -3, -1);
        yield return null;
    }
    void Open(RunSlotKind kind) => Call(flow, "OpenDecision", kind);

    IEnumerator Run()
    {
        float loadingDeadline = Time.realtimeSinceStartup + 60;
        while ((flow = FindAnyObjectByType<WaveOrchestrer>()) == null && Time.realtimeSinceStartup < loadingDeadline)
            yield return null;
        Check(flow != null, "additive Run scene loads before reviewing its orchestrator");
        flow.enabled = false;
        flow.StopAllCoroutines();
        flow.CancelInvoke();
        ship = FindAnyObjectByType<Shipcontroller>();
        ship.ObserverMode = true;
        GameRules.StartNewRun();
        ShipFrames.ApplyStats(ShipFrames.Current);
        LevelRun.Reset(LevelId.Fields, 17381);
        float timeout = Time.realtimeSinceStartup + 70;
        while ((CloudTransition.Busy || !WavePreparation.Ready) && Time.realtimeSinceStartup < timeout)
            yield return null;
        Check(!CloudTransition.Busy && WavePreparation.Ready, "opening preparation completes in the isolated review");
        foreach (var shop in FindObjectsByType<DecisionShop>())
            shop.Abort();
        yield return null;
        flow.waves = Array.Empty<string>();
        Set(flow, "currentWaveNumber", 0);
        CombatDecision.End();
        foreach (var enemy in FindObjectsByType<AEnemy>())
            Destroy(enemy.gameObject);
        CombatRound.ClearAll();
        MetaProgress.UnlockAll();
        ItemPool.ResetRun();
        inventory = RunInventory.For(ship);
        ship.transform.position = new Vector3(0, -3, -1);
        yield return null;
        string focus = SessionState.GetString("DecisionExpansionReviewFocus", "all");
        if (focus == "all" || focus == "inventory")
        {
            PureChecks();
            int wallet = RunCoins.Count;
            float score = Score.CurrentScore;
            Call(flow, "ResolveEncounter", EncounterKind.Salvage);
            Check(RunCoins.Count == wallet + EncounterChoice.SalvageCoins(1, 0) && Score.CurrentScore == score,
                "Salvage credits only the run wallet, never score");
            yield return InventoryChecks();
            File.WriteAllText(VerifyDecisionExpansion.Folder + "/InventoryVerification.txt", "PASS\n" + string.Join("\n", checks));
        }
        else if (focus == "exchange")
        {
            for (int i = 0; i < 4; i++)
                yield return Grant<Caffeine>();
        }
        if (focus == "all" || focus == "exchange")
            yield return ExchangeChecks();
        if (focus == "all" || focus == "exchange" || focus == "cargo")
            yield return CargoChecks();
        if (focus == "all" || focus == "cadence")
            yield return CadenceChecks();
        CombatDecision.End();
        var hexes = RewardHexes.Open(new[] { EncounterKind.Salvage, EncounterKind.HealthTrade, EncounterKind.Cargo }, ship.transform, 17381, _ => { });
        yield return new WaitForSeconds(.7f);
        yield return Capture("ThreeHexagons");
        var zones = (Array)Get(hexes, "zones");
        var zone = zones.GetValue(0);
        var centre = (Vector2)zone.GetType().GetField("centre").GetValue(zone);
        ship.transform.position = new Vector3(centre.x, centre.y, -1);
        yield return new WaitForSeconds(.4f);
        var body = (CanvasGroup)zone.GetType().GetField("body").GetValue(zone);
        var charge = (HexagonGraphic)zone.GetType().GetField("charge").GetValue(zone);
        Check(Mathf.Abs(body.alpha - .1f) < .001f && charge.color.a == 1, "three-zone hover retains the 10% body and solid filling border");
        yield return Capture("Hover");
        hexes.Dismiss();
        Check(errors.Count == 0, "no runtime errors during the combined decision review");
    }

    void PureChecks()
    {
        int baseTriples = 0, luckyTriples = 0;
        var place = typeof(RewardHexes).GetMethod("Place", BindingFlags.Static | BindingFlags.NonPublic);
        for (int seed = 0; seed < 512; seed++)
        {
            var ordinary = EncounterChoice.Draw(seed, true, 6, 6);
            var lucky = EncounterChoice.Draw(seed, true, 6, 6, false, 1);
            if (ordinary.Length == 3) baseTriples++;
            if (lucky.Length == 3) luckyTriples++;
            if (ordinary.Length < 2 || ordinary.Length > 4 || ordinary.Distinct().Count() != ordinary.Length ||
                ordinary.Contains(EncounterKind.Health) || ordinary.Contains(EncounterKind.HealthTrade) ||
                ordinary.Contains(EncounterKind.Item) && ordinary.Contains(EncounterKind.Challenge))
                throw new Exception("Invalid healthy offer: " + seed);
            var rich = EncounterChoice.Draw(seed, false, 3, 6, true, 1, true, true, false);
            if (!rich.Contains(EncounterKind.Pact) || rich.Contains(EncounterKind.Challenge) || rich.Contains(EncounterKind.Cargo) ||
                rich.Contains(EncounterKind.Health) && rich.Contains(EncounterKind.HealthTrade))
                throw new Exception("Invalid eligible offer: " + seed);
            var spots = (Vector2[])place.Invoke(null, new object[] { new[] { EncounterKind.Salvage, EncounterKind.Reforge, EncounterKind.Cargo }, seed, new Vector2(0, Camera.DownLimit + 1) });
            for (int i = 0; i < spots.Length; i++)
            {
                if (spots[i].x - RewardHexes.Radius <= Camera.LeftLimit || spots[i].x + RewardHexes.Radius >= Camera.RightLimit ||
                    spots[i].y - RewardHexes.Radius <= Camera.DownLimit || spots[i].y + RewardHexes.Radius >= Camera.UpLimit)
                    throw new Exception("Clipped three-zone offer: " + seed);
                for (int j = 0; j < i; j++)
                    if (Vector2.Distance(spots[i], spots[j]) <= RewardHexes.Radius * 2 + .2f)
                        throw new Exception("Overlapping three-zone offer: " + seed);
            }
        }
        Check(luckyTriples > baseTriples && baseTriples > 0, "512 seeds: healthy offers have 2-4 compatible hexagons; luck raises three-zone frequency from " + baseTriples + " to " + luckyTriples);
        Check(DistrictWaves.Sequence().Count(key => key == "encounterChoice") == 2 && DistrictWaves.Sequence().Count(key => int.TryParse(key, out _)) == 10 &&
            DistrictWaves.Sequence().Contains("minibossChoice"), "two unconditional selections preserve ten combat waves and a separate earned miniboss reward");
        foreach (var kind in new[] { EncounterKind.Salvage, EncounterKind.HealthTrade, EncounterKind.Reforge, EncounterKind.Cargo })
        {
            var sprite = EncounterChoice.Icon(kind);
            if (sprite.texture.width != 32 || sprite.texture.height != 32 || sprite.texture.filterMode != FilterMode.Point || sprite.texture.mipmapCount != 1)
                throw new Exception("Invalid pixel icon import: " + kind);
        }
        Check(true, "all four directly drawn 32px encounter icons use point sampling without mipmaps");
    }

    IEnumerator InventoryChecks()
    {
        float speed = GameRules.playerMoveSpeed;
        yield return Grant<Caffeine>();
        var firstCoffee = inventory.Entries.Last();
        yield return Grant<Caffeine>();
        Check(inventory.Remove(firstCoffee) && Mathf.Abs(GameRules.playerMoveSpeed - speed - 4) < .001f,
            "trading one Caffeine removes exactly one copy's movement bonus");
        float damage = GameRules.playerShootDamage;
        yield return Grant<ProteinSupplement>();
        var firstProtein = inventory.Entries.Last();
        yield return Grant<BloodPrice>();
        yield return Grant<ProteinSupplement>();
        Check(inventory.Remove(firstProtein) && Mathf.Abs(GameRules.playerShootDamage - (damage * 1.6f + 1)) < .001f,
            "removal preserves a later pact multiplier and a later additive item");
        yield return Grant<EarlyRise>();
        var early = inventory.Entries.Last();
        yield return Grant<EarlyRise>();
        inventory.Remove(early);
        Check(Mathf.Abs(GameRules.manaDropMultiplier - 2) < .001f, "multiplicative energy pickups lose only the traded copy");
        yield return Grant<Popcorn>();
        var popcorn = inventory.Entries.Last();
        yield return Grant<Popcorn>();
        var lastPopcorn = inventory.Entries.Last();
        inventory.Remove(popcorn);
        yield return null;
        Check(ship.GetComponent<PopcornHits>() != null, "duplicate passive keeps its shared firing effect");
        inventory.Remove(lastPopcorn);
        yield return null;
        Check(ship.GetComponent<PopcornHits>() == null, "last passive copy removes its firing effect");
        float handling = ship.slowMultiplier;
        yield return Grant<FocusTime>();
        var focus = inventory.Entries.Last();
        yield return Grant<FocusTime>();
        var lastFocus = inventory.Entries.Last();
        inventory.Remove(focus);
        Check(Mathf.Abs(ship.slowMultiplier - handling * .6f) < .001f, "duplicate Focus Brake retains its single handling modifier");
        inventory.Remove(lastFocus);
        Check(Mathf.Abs(ship.slowMultiplier - handling) < .001f, "last Focus Brake restores original handling");
        yield return Grant<Poison>();
        var poison = inventory.Entries.Last();
        yield return Grant<Poison>();
        inventory.Remove(poison);
        Check(ship.GetComponent<CombatUpgrades>().Count(UpgradeKind.Poison) == 1, "coating trades preserve the remaining upgrade stack");
        int guns = ship.GetComponentsInChildren<AWeapon>().Length;
        yield return Grant<AK45Item>();
        var sidegun = inventory.Entries.Last();
        Check(ship.GetComponentsInChildren<AWeapon>().Length == guns + 1, "passive shooting pickup owns its separate gun");
        inventory.Remove(sidegun);
        yield return null;
        Check(ship.GetComponentsInChildren<AWeapon>().Length == guns, "trading a passive shooting item removes only its gun");
        Check(inventory.Entries.Where(entry => entry.key == nameof(BloodPrice) || entry.key == nameof(ExplodingShield)).All(entry => !entry.tradeable),
            "pacts and the starter shield are protected from exchanges");
    }

    IEnumerator ExchangeChecks()
    {
        ship.health = ship.maxHealth - 3;
        int owned = inventory.Tradeable().Count;
        Open(RunSlotKind.HealthTrade);
        yield return new WaitForSeconds(1.8f);
        yield return Capture("HealthTrade");
        if ((int)Get(Shop, "tradePage") == 0 && inventory.Tradeable().Count > 3)
        {
            ship.transform.position = new Vector3(Camera.RightLimit - 1, Mathf.Lerp(Camera.DownLimit, Camera.UpLimit, .35f), -1);
            yield return new WaitForSeconds(1.15f);
            Check((int)Get(Shop, "tradePage") == 1, "spatial paging exposes items beyond the first three owned passives");
            ship.transform.position = new Vector3(0, -3, -1);
            yield return null;
            ship.transform.position = new Vector3(Camera.LeftLimit + 1, Mathf.Lerp(Camera.DownLimit, Camera.UpLimit, .35f), -1);
            yield return new WaitForSeconds(1.15f);
            Check((int)Get(Shop, "tradePage") == 0, "spatial paging returns without consuming or copying items");
            ship.transform.position = new Vector3(0, -3, -1);
            yield return new WaitForSeconds(1.8f);
        }
        var trade = (ItemExchangeOffer)Offers[0];
        var source = trade.Entry;
        yield return Claim(trade);
        yield return new WaitForSeconds(.9f);
        Check(!inventory.Contains(source) && inventory.Tradeable().Count == owned - 1 && ship.health == ship.maxHealth,
            "health exchange consumes the chosen item once and repairs three actual hits");
        Open(RunSlotKind.Reforge);
        yield return new WaitForSeconds(1.8f);
        source = ((ItemExchangeOffer)Offers[0]).Entry;
        yield return Claim(Offers[0]);
        yield return new WaitForSeconds(1.8f);
        Check(inventory.Contains(source) && Offers.Count == 2 && Offers.Select(item => item.GetType().Name).Distinct().Count() == 2 &&
            Offers.All(item => item.rarity == source.rarity && item.GetType().Name != source.key),
            "reforge previews two distinct same-rarity alternatives while preserving the source");
        yield return Capture("Reforge");
        yield return Skip();
        Check(inventory.Contains(source), "skipping reforge keeps the original item and all its effects");
        Open(RunSlotKind.Reforge);
        yield return new WaitForSeconds(1.8f);
        source = ((ItemExchangeOffer)Offers[0]).Entry;
        yield return Claim(Offers[0]);
        yield return new WaitForSeconds(1.8f);
        string replacement = Offers[0].GetType().Name;
        string rarity = Offers[0].rarity;
        yield return Claim(Offers[0]);
        Check(!inventory.Contains(source) && inventory.Entries.Last().key == replacement && inventory.Entries.Last().rarity == rarity,
            "taking a reforge replacement atomically removes the source and records the real new item");
        yield return new WaitForSeconds(.4f);
    }

    IEnumerator CargoChecks()
    {
        CombatDecision.End();
        ship.transform.position = new Vector3(0, -3, -1);
        // Exercise the actual offer gate on both sides of the five-wave boundary.
        flow.waves = new[] { "2", "2", "2", "2", "2", "boss" };
        bool offeredAtFive = false;
        for (int seed = 1; seed <= 64; seed++)
        {
            LevelRun.Reset(LevelId.Fields, seed);
            Set(flow, "currentWaveNumber", 1);
            Call(flow, "OpenEncounterChoice");
            offeredAtFive |= flow.EncounterOptions.Contains(EncounterKind.Cargo);
            Call(flow, "DismissOffer");
            Set(flow, "currentWaveNumber", 2);
            Call(flow, "OpenEncounterChoice");
            Check(!flow.EncounterOptions.Contains(EncounterKind.Cargo), "four remaining waves exclude cargo: " + seed);
            Call(flow, "DismissOffer");
        }
        Check(offeredAtFive, "cargo is eligible with exactly five ordinary waves remaining");
        LevelRun.Reset(LevelId.Fields, 17381);
        flow.waves = Array.Empty<string>();
        Set(flow, "currentWaveNumber", 0);
        Call(flow, "ResolveEncounter", EncounterKind.Cargo);
        var cargo = flow.Cargo;
        yield return new WaitForSeconds(1.2f);
        Check(cargo.Quality == ItemRarity.Common && Vector2.Distance(cargo.transform.position, ship.transform.position) < .8f,
            "cargo begins common and sits close behind the ship");
        yield return Capture("Cargo-Common");
        var bullet = CombatRound.Launch<SimpleRound>(cargo.transform.position, Vector2.down, 0);
        Check(bullet != null && bullet.PixelOverlaps(cargo.GetComponent<Collider2D>()), "cargo uses actual opaque-pixel projectile contact");
        yield return new WaitForSeconds(.16f);
        Check(cargo.Hits == 2, "a real hostile round damages the crate through native physics");
        for (int tier = 0; tier < 4; tier++)
        {
            if (tier > 0)
            {
                Call(flow, "ResolveEncounter", EncounterKind.Cargo);
                cargo = flow.Cargo;
                for (int wave = 0; wave < tier; wave++) Call(flow, "FinishCargoWave");
                yield return new WaitForSeconds(1.1f);
                yield return Capture("Cargo-" + cargo.Quality);
            }
            Check((int)cargo.Quality == tier && flow.waves.Length == 0, "cargo improves in order without scheduling decisions: " + tier);
            var outline = cargo.GetComponentsInChildren<LineRenderer>().First(line => line.name == "Cargo rarity outline");
            Check(Vector4.Distance(outline.startColor, AItem.GetRarityColor(cargo.Rarity)) < .01f, "outline matches the real reward tier: " + tier);
            if (tier == 3)
            {
                Call(flow, "FinishCargoWave");
                Check(cargo.Quality == ItemRarity.Legendary && !cargo.Finished, "legendary cargo stays available until opened");
                float until = Time.time + .6f;
                while (Time.time < until)
                {
                    ship.transform.position = new Vector3(Mathf.Sin(Time.time * 14) * 2.5f, -3, -1);
                    yield return null;
                    Check(Vector2.Distance(cargo.transform.position, ship.transform.position) <= 1.03f, "hard turns keep cargo close");
                }
                ship.transform.position = new Vector3(0, -3, -1);
                yield return new WaitForSeconds(.3f);
                Call(flow, "ResetDistrict", (LevelId?)null);
                Check(flow.Cargo == cargo && cargo.Quality == ItemRarity.Legendary, "unopened cargo and its quality survive district reset");
                flow.waves = Array.Empty<string>();
            }
            int wallet = RunCoins.Count;
            CombatDecision.Begin();
            Check(!cargo.TryClaim(), "decisions cannot accidentally open cargo");
            yield return null;
            CombatDecision.End();
            if (tier < 3)
                Check(cargo.TryClaim(), "cargo can be opened immediately at tier " + tier);
            else
            {
                var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
                yield return null;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
                    new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.C));
                yield return new WaitForSeconds(.3f);
                Check(!cargo.Finished && cargo.ClaimProgress > .1f, "collect needs a deliberate hold");
                yield return Capture("Cargo-Opening");
                yield return new WaitForSeconds(.4f);
                UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
                Check(flow.Cargo == null, "holding C opens cargo during active flight");
            }
            Check(flow.Cargo == null && RunCoins.Count == wallet + EncounterChoice.SalvageCoins(1, GameRules.luck) * 2,
                "cash-in pays the existing coins once: " + tier);
            var drops = (List<GameObject>)Get(flow, "items");
            var reward = drops.Last().GetComponent<AItem>();
            Check(reward.rarity == ((ItemRarity)tier).ToString().ToLowerInvariant() && flow.waves.Length == 0 && Shop == null && !CombatDecision.Active,
                "cash-in drops the exact rarity immediately without pausing combat: " + tier);
            if (cargo != null) Check(!cargo.TryClaim(), "a claimed crate cannot pay twice");
            yield return null;
            reward.Dismiss();
            yield return new WaitForSeconds(.5f);
        }
        Call(flow, "ResolveEncounter", EncounterKind.Cargo);
        cargo = flow.Cargo;
        Set(cargo, "grace", 0f);
        cargo.Hit();
        Set(cargo, "grace", 0f);
        cargo.Hit();
        Set(cargo, "grace", 0f);
        cargo.Hit();
        Check(!cargo.TryClaim(), "a destroyed crate cannot be cashed in");
        yield return null;
        int lostWallet = RunCoins.Count;
        Call(flow, "FinishCargoWave");
        Check(flow.Cargo == null && RunCoins.Count == lostWallet && !flow.waves.Contains("cargoreward"), "a destroyed crate grants no coins or item reward");
        FlightTrials.Active = FlightTrials.All[1];
        var trial = CargoEscort.Create(ship);
        Check(!trial.TryClaim() && !trial.FinishWave() && trial.FinishWave(), "Cold Delivery still requires two complete waves and cannot be cashed early");
        trial.Deliver();
        FlightTrials.Active = null;
        GameRules.luck = 2;
        PactPool.BindCursedCoin();
        for (int tier = 0; tier < 4; tier++)
        {
            string key = ItemPool.GetCargoReward(out var rarity, (ItemRarity)tier);
            Check(key != null && ItemCatalog.Get(key).Rarity == (ItemRarity)tier, "luck and pacts cannot change the displayed cargo rarity: " + rarity);
        }
        GameRules.luck = 0;
        PactPool.ResetRun();
    }

    IEnumerator CadenceChecks()
    {
        ship.health = ship.maxHealth;
        flow.waves = DistrictWaves.Sequence();
        Set(flow, "currentWaveNumber", 4);
        Call(flow, "GenerateWave");
        Check((bool)Get(flow, "offerPending") && (int)Get(flow, "currentWaveNumber") == 6,
            "the first selection is scheduled with ordinary wave four");
        Call(flow, "OpenEncounterChoice");
        Check(flow.RewardOffer != null && flow.EncounterOptions.Length >= 2,
            "healthy Plains opens a real first hexagon offer");
        Call(flow, "DismissOffer");
        var plan = new RunPlan();
        plan.Load(DistrictWaves.Sequence());
        Check(plan.Following(11).Kind == RunSlotKind.MinibossChoice,
            "the earned miniboss reward prevents an overlapping combat handoff");
        foreach (bool victory in new[] { false, true })
        {
            ((WaveKillLedger)Get(flow, "killLedger")).Clear();
            flow.waves = DistrictWaves.Sequence();
            Set(flow, "currentWaveNumber", 11);
            Set(flow, "minibossVictory", victory);
            Call(flow, "GenerateWave");
            if (victory)
            {
                Check(Shop != null && !Shop.paid && (int)Get(flow, "currentWaveNumber") == 12,
                    "a timely miniboss kill opens its separate earned item decision");
                yield return Skip();
            }
            Check((bool)Get(flow, "offerPending") && (int)Get(flow, "currentWaveNumber") == 14,
                "ordinary wave nine schedules selection two after miniboss " + (victory ? "victory" : "timeout"));
            Call(flow, "OpenEncounterChoice");
            Check(flow.RewardOffer != null && flow.EncounterOptions.Length >= 2 && !flow.EncounterOptions.Contains(EncounterKind.Challenge),
                "the second hexagon offer survives either miniboss result and excludes late challenges");
            Call(flow, "DismissOffer");
            yield return null;
        }
        flow.waves = Array.Empty<string>();
        Set(flow, "currentWaveNumber", 0);
    }
}
#endif
