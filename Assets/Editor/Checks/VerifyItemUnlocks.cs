#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Stateful progression/save checks, with no scene loading or Play mode. Every save lives
// inside the dedicated review project, never in Miguel's application data directory.
public static class VerifyItemUnlocks
{
    const string Folder = "ReviewOutput/ItemUnlocks";
    static readonly BindingFlags Private = BindingFlags.Static | BindingFlags.NonPublic;
    static int checks, caseNumber, seed = 19000;
    static readonly string Session = DateTime.UtcNow.Ticks.ToString();
    static string saveDirectory;

    public static void Run()
    {
        if (!Application.dataPath.Replace('\\', '/').EndsWith("/Temp/UnlockReview/Assets", StringComparison.Ordinal))
            throw new Exception("Use the isolated UnlockReview checkout");
        Directory.CreateDirectory(Folder);
        try
        {
            Check(ItemUnlocks.All.Count == 40 && ItemUnlocks.All.Select(r => r.Key).Distinct().Count() == 40, "40 unique rewards");
            Check(ItemUnlocks.All.Count(r => r.Condition == ItemUnlockCondition.Clear) == 12, "12 area clears");
            Check(ItemUnlocks.All.Count(r => r.Condition == ItemUnlockCondition.NoHitBoss) == 16, "16 separate boss rewards");
            Check(ItemUnlocks.All.Count(r => r.Condition == ItemUnlockCondition.NoHitArea) == 12, "12 full-area rewards");
            foreach (var reward in ItemUnlocks.All)
            {
                Check(ItemCatalog.Get(reward.Key).InRewardPool, "Valid reward " + reward.Key);
                Check(FlightTrials.Blueprint(reward.Key) == null, "Simulator reward overlap " + reward.Key);
                if (reward.Boss != null) Check(DistrictWaves.Bosses(reward.Area).Contains(reward.Boss), "Boss belongs to area");
            }
            foreach (RunDifficulty mode in Enum.GetValues(typeof(RunDifficulty)))
                foreach (LevelId area in Enum.GetValues(typeof(LevelId)))
                    CheckArea(area, mode);
            CheckHitsAndRetries();
            CheckTravel();
            CheckPersistence();
            CheckMigration();
            CheckSimulators();
            CheckAdvancedTapes();
            CheckRoutes();
            CheckModels();
            File.WriteAllText(Folder + "/Verification.txt", "PASS\n" + checks + " checks. All 12 areas / 16 bosses / 3 difficulties.\n" +
                "Verified hit windows, duplicate completions, Warp-Core retry semantics, travel, loss, interrupted run, queue acknowledgement, old saves, shop gates, simulator isolation, route locks and model coverage.\n" +
                "No gameplay or visual review was started.\n");
            if (Environment.GetCommandLineArgs().Contains("--unlock-build")) Build();
            VerifyCleanCompilation.Run();
        }
        catch (Exception exception)
        {
            File.WriteAllText(Folder + "/Verification.txt", "FAIL\n" + exception);
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    static void Fresh()
    {
        saveDirectory = Path.GetFullPath(Folder + "/Saves/" + Session + "/" + ++caseNumber);
        Directory.CreateDirectory(saveDirectory);
        MetaProgress.ReviewSaveDirectory = saveDirectory;
        Reload();
        FlightTrials.Reset();
        Check(ItemUnlocks.All.All(r => !MetaProgress.IsUnlocked(r.Key)), "New profile keeps all achievement items locked");
        Check(MetaProgress.Stock.All(MetaProgress.CanStock), "New shop respects gates");
    }
    static void Reload()
    {
        typeof(MetaProgress).GetField("data", Private).SetValue(null, null);
        typeof(MetaProgress).GetField("flight", Private).SetValue(null, null);
    }
    static void Begin(LevelId area, RunDifficulty mode)
    {
        Difficulty.Select(mode);
        LevelRun.Reset(area, ++seed);
        RunCoins.Reset();
        MetaProgress.BeginRun();
    }
    static void CheckArea(LevelId area, RunDifficulty mode)
    {
        Fresh();
        var seen = new HashSet<Type>();
        var expected = new HashSet<string>();
        for (int encounter = 0; encounter < DistrictWaves.Bosses(area).Count; encounter++)
        {
            Begin(area, mode);
            Type boss = MetaProgress.EngageBoss();
            Check(seen.Add(boss), "Alternate boss rotation without repeats");
            foreach (var reward in ItemUnlocks.All.Where(r => r.Area == area))
                if (reward.Condition == ItemUnlockCondition.Clear || mode != RunDifficulty.Easy && (reward.Boss == null || reward.Boss == boss))
                    expected.Add(reward.Key);
            MetaProgress.DefeatBoss();
            MetaProgress.DefeatBoss();
            MetaProgress.EndRun("Lost");
            Check(MetaProgress.LastFlight.bosses == 1, "One completion for a whole encounter");
            Check(MetaProgress.PendingUnlocks.Select(n => n.key).Where(k => !RouteUnlocks.IsRoute(k)).ToHashSet().SetEquals(expected), "Exact earned set " + area + " / " + mode);
            foreach (var reward in ItemUnlocks.All.Where(r => r.Area == area))
                Check(MetaProgress.IsUnlocked(reward.Key) == expected.Contains(reward.Key), "Eligibility " + reward.Key);
            Check(MetaProgress.Stock.All(MetaProgress.CanStock), "Restock respects earned set");
        }
        ItemPool.ResetRun();
        foreach (var reward in ItemUnlocks.All.Where(r => r.Area == area))
        {
            var alternatives = ItemPool.ReforgeAlternatives("", ItemPool.RarityOf(reward.Key));
            Check(alternatives.Contains(reward.Key) == expected.Contains(reward.Key), "Reward pool eligibility " + reward.Key);
        }
    }
    static void CheckHitsAndRetries()
    {
        Fresh(); Begin(LevelId.Thunder, RunDifficulty.Normal);
        MetaProgress.TookHit();
        MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Lost");
        Check(MetaProgress.IsUnlocked("Thunderhead") && !MetaProgress.IsUnlocked("PolarityEngine"), "Pre-boss hit only breaks area no-hit");
        Fresh(); Begin(LevelId.Thunder, RunDifficulty.Hard);
        MetaProgress.EngageBoss(); MetaProgress.TookHit(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Lost");
        Check(Items(MetaProgress.LastFlight.unlocks) == 1 && MetaProgress.IsUnlocked("ElectricityItem"), "Boss hit breaks both no-hit rewards");
        Fresh(); Begin(LevelId.Fields, RunDifficulty.Normal);
        Type before = MetaProgress.EngageBoss(); MetaProgress.TookHit();
        Type retry = MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Lost");
        Check(before == retry && Items(MetaProgress.LastFlight.unlocks) == 2 && !MetaProgress.IsUnlocked("SquadWhistle"), "Retry keeps boss draw and cannot erase area hit");
        Fresh(); Begin(LevelId.Void, RunDifficulty.Normal);
        MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Victory");
        Check(Items(MetaProgress.LastFlight.unlocks) == 3, "Void earns both no-hit rewards together");
    }
    static void CheckTravel()
    {
        Fresh(); Begin(LevelId.Fields, RunDifficulty.Normal);
        MetaProgress.EngageBoss(); MetaProgress.TookHit(); MetaProgress.DefeatBoss();
        LevelRun.Advance(LevelId.Snow); MetaProgress.EnterDistrict();
        MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Lost");
        Check(MetaProgress.IsUnlocked("BulletTime") && !MetaProgress.IsUnlocked("SquadWhistle"), "Full-area hit window resets on travel");
        Check(Items(MetaProgress.LastFlight.unlocks) == 4, "Earlier unlocks retained across districts");
    }
    static void CheckPersistence()
    {
        Fresh(); Begin(LevelId.Thunder, RunDifficulty.Normal);
        MetaProgress.EngageBoss(); MetaProgress.DefeatBoss();
        // Simulate a process closing before the result screen. The actual saved queue is authoritative.
        Reload();
        Check(Items(MetaProgress.PendingUnlocks) == 3 && MetaProgress.IsUnlocked("PolarityEngine"), "Earned immediately before run end");
        Check(MetaProgress.PendingUnlocks[0].key == RouteUnlocks.Key(LevelId.Space), "The route is revealed before items");
        MetaProgress.DismissUnlock(RouteUnlocks.Key(LevelId.Space)); Reload();
        Check(MetaProgress.RouteOpen(LevelId.Space), "Dismissing the route notice keeps it open");
        string first = MetaProgress.PendingUnlocks[0].key;
        MetaProgress.DismissUnlock("not-the-front");
        Check(Items(MetaProgress.PendingUnlocks) == 3, "Only the visible tag can be acknowledged");
        MetaProgress.DismissUnlock(first); Reload();
        Check(Items(MetaProgress.PendingUnlocks) == 2 && MetaProgress.PendingUnlocks[0].key != first, "Acknowledgement persists");
        Begin(LevelId.Thunder, RunDifficulty.Hard);
        MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Lost");
        Check(Items(MetaProgress.LastFlight.unlocks) == 0 && Items(MetaProgress.PendingUnlocks) == 2, "No duplicate celebrations across difficulties");
        Fresh(); Begin(LevelId.Thunder, RunDifficulty.Normal);
        MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Lost"); Reload();
        Check(Items(MetaProgress.History[0].unlocks) == 3 && Items(MetaProgress.PendingUnlocks) == 3, "Loss receipt and queue survive reload");
    }
    static void CheckMigration()
    {
        Fresh();
        File.WriteAllText(Path.Combine(saveDirectory, "hangar.json"), "{\"version\":2,\"coins\":321,\"lastFrame\":3,\"difficulty\":2,\"ship\":\"Warden\",\"unlocked\":[\"Popcorn\",\"Icicle\",\"ThreeMoonsItem\"],\"stock\":[\"BeamEmitterItem\",\"Icicle\",\"HeatSink\"],\"pin\":\"BeamEmitterItem\",\"loadout\":[\"Icicle\"]}");
        Reload();
        Check(MetaProgress.IsUnlocked("Icicle") && MetaProgress.IsUnlocked("ThreeMoonsItem") && MetaProgress.Coins == 321, "Preserve prior unlocks and bank");
        Check(MetaProgress.Ship == ShipFrame.Warden && MetaProgress.Outbound.Contains("Icicle"), "Preserve ship and purchased loadout");
        Check(MetaProgress.Pin == null && MetaProgress.Stock.All(MetaProgress.CanStock) && !MetaProgress.TryBuy("BeamEmitterItem", out _), "Remove unearned pinned offer");
        Check(Items(MetaProgress.PendingUnlocks) == 0, "Migration never re-celebrates owned items");
        Begin(LevelId.Snow, RunDifficulty.Normal); MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Lost");
        Check(MetaProgress.LastFlight.unlocks.All(n => n.key != "Icicle") && MetaProgress.IsUnlocked("BulletTime"), "Grandfathered clear, new achievement on another hull");
    }
    static void CheckSimulators()
    {
        foreach (var trial in FlightTrials.All.Where(tape => !tape.Advanced))
        {
            Fresh();
            if (trial.frame != ShipFrame.GR07)
            {
                SetCommissionedHull((ShipFrame)((int)trial.frame - 1));
                Check(!trial.Available && !trial.TrySelect() && FlightTrials.Active == null, trial.title + " requires " + trial.frame);
            }
            SetCommissionedHull(trial.frame);
            Check(trial.TrySelect(), trial.title + " opens with its hull");
            Begin(trial.district, trial.difficulty);
            MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Certified");
            Check(Items(MetaProgress.LastFlight.unlocks) == 1 && MetaProgress.LastFlight.unlocks[0].key == trial.reward, "Only simulator blueprint earned");
            Check(ItemUnlocks.All.All(r => !MetaProgress.IsUnlocked(r.Key)), "Simulator cannot award campaign items");
            Check(MetaProgress.CanStock(trial.reward), "Certified blueprint can be stocked");
        }
        FlightTrials.Reset();
    }

    internal static void SetCommissionedHull(ShipFrame frame)
    {
        var saved = typeof(MetaProgress).GetProperty("Data", Private).GetValue(null);
        saved.GetType().GetField("lastFrame").SetValue(saved, (int)frame);
    }

    static void CheckAdvancedTapes()
    {
        Fresh();
        var tapes = FlightTrials.All.Where(tape => tape.Advanced).ToArray();
        Check(tapes.Length == 30 && tapes.All(tape => tape.legs.Last().boss != null), "Thirty advanced tapes end with a full boss");
        int fullLevel = DistrictWaves.CampaignSequence.Count(key => int.TryParse(key, out _) || key == "miniboss") + 1;
        Check(tapes.All(tape => tape.legs.All(leg => leg.boss != null && leg.sequence.Length == (leg.fullLevel ? fullLevel : 1))),
            "Every simulator leg is a whole district or a lone boss, never a wave snippet");
        Check(tapes.Where(tape => tape.legs.Any(leg => leg.fullLevel)).All(tape => Enumerable.Range(1, tape.legs.Length - 1)
            .All(i => tape.legs[i].district == tape.legs[i - 1].district || LevelRun.GetOptions(tape.legs[i - 1].district).Contains(tape.legs[i].district))),
            "Tapes that fly whole districts follow real routes");
        Check(FlightTrials.Find("Change of Plans").TrySelect(), "Fresh save can launch a tape whose only locked item is its own loaned blueprint");
        FlightTrials.Reset();
        Check(!FlightTrials.Find("Power Exchange").Available, "Fresh save still needs the tape's campaign equipment");
        var saved = typeof(MetaProgress).GetProperty("Data", Private).GetValue(null);
        saved.GetType().GetField("lastFrame").SetValue(saved, ShipFrames.Count - 1);
        MetaProgress.UnlockAll();
        var rewards = tapes.Where(tape => tape.reward != null).ToArray();
        Check(rewards.Length == 5 && rewards.All(tape => !MetaProgress.CanStock(tape.reward)), "Tape prizes are excluded from the access gate and outfitter");
        Check(tapes.All(tape => tape.Available), "Every tape opens once its own campaign kit and hull are unlocked");
        var owned = (List<string>)saved.GetType().GetField("unlocked").GetValue(saved);
        owned.Remove("BeamEmitterItem");
        Check(!FlightTrials.Find("Power Exchange").TrySelect() && FlightTrials.Find("Change of Plans").Available,
            "A missing beam emitter locks only tapes using it");
        owned.Add("BeamEmitterItem");

        var harvest = FlightTrials.Find("Triple Harvest");
        Check(harvest.TrySelect() && harvest.kit.All(key => key != "CarouselItem"), "Triple Harvest has its own kit");
        Begin(harvest.district, RunDifficulty.Hard);
        Check(MetaProgress.EngageBoss() == typeof(FieldsBomberBoss), "Triple Harvest begins with the Bomber");
        MetaProgress.DefeatBoss();
        MetaProgress.EndRun("Certified");
        Check(!MetaProgress.ContractClear(harvest.title), "The first boss cannot certify a gauntlet");

        var carousel = FlightTrials.Find("Change of Plans");
        Check(carousel.TrySelect() && carousel.kit.Contains(carousel.reward), "The Carousel tape loans its own prize");
        Begin(carousel.district, carousel.difficulty);
        Check(MetaProgress.EngageBoss() == typeof(PlainsRedKiteBoss), "Carousel tape ends at the Red Kite");
        MetaProgress.DefeatBoss();
        MetaProgress.EndRun("Certified");
        Check(MetaProgress.CanStock(carousel.reward) && MetaProgress.LastFlight.unlocks.Single().key == carousel.reward, "Final boss certifies the tape and releases its prize");
        FlightTrials.Reset();
    }

    // Item counts ignore district tags, which share the unlock queue.
    static int Items(IEnumerable<ItemUnlockNotice> notices) => notices.Count(n => !RouteUnlocks.IsRoute(n.key));
    static void CheckRoutes()
    {
        VerifyRunProgression.Run();
        Fresh();
        var starting = new[] { LevelId.Fields, LevelId.Snow, LevelId.Thunder, LevelId.City, LevelId.Tunnel };
        foreach (LevelId area in Enum.GetValues(typeof(LevelId)))
            Check(MetaProgress.RouteOpen(area) == starting.Contains(area), "Fresh save has both starting branches: " + area);
        var unlocks = new[]
        {
            (LevelId.Snow, LevelId.Desert), (LevelId.City, LevelId.Mushroom),
            (LevelId.Thunder, LevelId.Space), (LevelId.Desert, LevelId.Sewers), (LevelId.Sewers, LevelId.Core), (LevelId.Core, LevelId.Void),
            (LevelId.Tunnel, LevelId.Underwater), (LevelId.Mushroom, LevelId.Candyland), (LevelId.Candyland, LevelId.Dream), (LevelId.Dream, LevelId.Void),
            (LevelId.Space, LevelId.Heaven), (LevelId.Underwater, LevelId.Hell),
            (LevelId.Heaven, LevelId.Void), (LevelId.Hell, LevelId.Void)
        };
        foreach (var (from, next) in unlocks)
        {
            Fresh(); Begin(from, RunDifficulty.Easy);
            MetaProgress.EngageBoss(); MetaProgress.DefeatBoss();
            Check(!MetaProgress.RouteOpen(next) && MetaProgress.RouteOpensNextFlight(next), "New route waits for the next flight: " + next);
            MetaProgress.EndRun("Victory");
            foreach (LevelId area in Enum.GetValues(typeof(LevelId)))
                Check(MetaProgress.RouteOpen(area) == (starting.Contains(area) || area == next), from + " opens only " + next);
            var tags = MetaProgress.LastFlight.unlocks.Where(n => RouteUnlocks.IsRoute(n.key)).ToArray();
            Check(tags.Length == 1 && tags[0].key == RouteUnlocks.Key(next) && tags[0].area == next &&
                tags[0].Requirement == RouteUnlocks.Requirement(from), "One route tag with the correct destination and source");
            Check(MetaProgress.CanFly(ShipFrame.Lancer) == (next == LevelId.Void), "Either fifth-tier victory can commission the next ship");
            Begin(from, RunDifficulty.Normal);
            Check(MetaProgress.RouteOpen(next) && !MetaProgress.RouteOpensNextFlight(next), "Opened route is flyable on the next flight");
            MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Lost");
            Check(MetaProgress.LastFlight.unlocks.All(n => !RouteUnlocks.IsRoute(n.key)), "A district is announced once");
        }
        foreach (var trial in FlightTrials.All.Where(tape => !tape.Advanced))
        {
            Fresh();
            SetCommissionedHull(trial.frame);
            Check(trial.TrySelect(), trial.title + " opens with its hull");
            Begin(trial.district, trial.difficulty);
            MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Certified");
            Check(Enum.GetValues(typeof(LevelId)).Cast<LevelId>().All(a => MetaProgress.RouteOpen(a) == starting.Contains(a)), "Simulator never opens districts");
        }
        FlightTrials.Reset();
        Fresh();
        File.WriteAllText(Path.Combine(saveDirectory, "hangar.json"), "{\"version\":3,\"coins\":5,\"routes\":[\"Desert\",\"Void\"],\"unlocked\":[\"Popcorn\"],\"stock\":[],\"loadout\":[]}");
        Reload();
        Check(!MetaProgress.RouteOpen(LevelId.Desert) && !MetaProgress.RouteOpen(LevelId.Void) && MetaProgress.Coins == 5, "Pre-route saves use the starting branches");
        File.WriteAllText(Path.Combine(saveDirectory, "hangar.json"), "{\"version\":5,\"coins\":5,\"routes\":[\"Desert\",\"Void\"],\"unlocked\":[\"Popcorn\"],\"stock\":[],\"loadout\":[]}");
        Reload();
        Check(MetaProgress.RouteOpen(LevelId.Desert) && MetaProgress.RouteOpen(LevelId.Void) && MetaProgress.Coins == 5, "Current saves keep earned routes and coins");
    }
    static void CheckModels()
    {
        for (int row = 0; row < (ItemCatalog.Exhibits.Count + GalleryShelf.Columns - 1) / GalleryShelf.Columns; row++)
            Resources.Load<GalleryShelf>("Gallery/Shelf" + row)?.Register();
        foreach (var reward in ItemUnlocks.All)
        {
            var mesh = IndustrialMeshFactory.ExpansionItem(reward.Key);
            Check(mesh != null && mesh.vertexCount > 0 && mesh.bounds.size.sqrMagnitude > .0001f, "Existing 3D model " + reward.Key);
            Check(GalleryShelf.TryGet(reward.Key, out var exhibit) && exhibit.title == reward.Title, "Actual displayed item name " + reward.Key);
        }
    }
    static void Build()
    {
        string output = Path.GetFullPath("Build/Unlocks/Shootemup.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = BuildTarget.StandaloneWindows64, locationPathName = output, options = BuildOptions.None
        });
        File.WriteAllText(Folder + "/Build.txt", report.summary.result + "\n" + output + "\nErrors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings);
        Check(report.summary.result == BuildResult.Succeeded, "Windows build");
    }
    static void Check(bool value, string message)
    {
        checks++;
        if (!value) throw new Exception(message);
    }
}
#endif
