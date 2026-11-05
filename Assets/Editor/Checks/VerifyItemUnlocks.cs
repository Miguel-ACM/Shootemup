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
            CheckModels();
            File.WriteAllText(Folder + "/Verification.txt", "PASS\n" + checks + " checks. All 12 areas / 16 bosses / 3 difficulties.\n" +
                "Verified hit windows, duplicate completions, Warp-Core retry semantics, travel, loss, interrupted run, queue acknowledgement, old saves, shop gates, simulator isolation and model coverage.\n" +
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
        FlightTrials.Active = null;
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
            Check(MetaProgress.PendingUnlocks.Select(n => n.key).ToHashSet().SetEquals(expected), "Exact earned set " + area + " / " + mode);
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
        Check(MetaProgress.LastFlight.unlocks.Count == 1 && MetaProgress.IsUnlocked("ElectricityItem"), "Boss hit breaks both no-hit rewards");
        Fresh(); Begin(LevelId.Fields, RunDifficulty.Normal);
        Type before = MetaProgress.EngageBoss(); MetaProgress.TookHit();
        Type retry = MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Lost");
        Check(before == retry && MetaProgress.LastFlight.unlocks.Count == 2 && !MetaProgress.IsUnlocked("SquadWhistle"), "Retry keeps boss draw and cannot erase area hit");
        Fresh(); Begin(LevelId.Void, RunDifficulty.Normal);
        MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Victory");
        Check(MetaProgress.LastFlight.unlocks.Count == 3, "Void earns both no-hit rewards together");
    }
    static void CheckTravel()
    {
        Fresh(); Begin(LevelId.Fields, RunDifficulty.Normal);
        MetaProgress.EngageBoss(); MetaProgress.TookHit(); MetaProgress.DefeatBoss();
        LevelRun.Advance(LevelId.Snow); MetaProgress.EnterDistrict();
        MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Lost");
        Check(MetaProgress.IsUnlocked("BulletTime") && !MetaProgress.IsUnlocked("SquadWhistle"), "Full-area hit window resets on travel");
        Check(MetaProgress.LastFlight.unlocks.Count == 4, "Earlier unlocks retained across districts");
    }
    static void CheckPersistence()
    {
        Fresh(); Begin(LevelId.Thunder, RunDifficulty.Normal);
        MetaProgress.EngageBoss(); MetaProgress.DefeatBoss();
        // Simulate a process closing before the result screen. The actual saved queue is authoritative.
        Reload();
        Check(MetaProgress.PendingUnlocks.Count == 3 && MetaProgress.IsUnlocked("PolarityEngine"), "Earned immediately before run end");
        string first = MetaProgress.PendingUnlocks[0].key;
        MetaProgress.DismissUnlock("not-the-front");
        Check(MetaProgress.PendingUnlocks.Count == 3, "Only the visible tag can be acknowledged");
        MetaProgress.DismissUnlock(first); Reload();
        Check(MetaProgress.PendingUnlocks.Count == 2 && MetaProgress.PendingUnlocks[0].key != first, "Acknowledgement persists");
        Begin(LevelId.Thunder, RunDifficulty.Hard);
        MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Lost");
        Check(MetaProgress.LastFlight.unlocks.Count == 0 && MetaProgress.PendingUnlocks.Count == 2, "No duplicate celebrations across difficulties");
        Fresh(); Begin(LevelId.Thunder, RunDifficulty.Normal);
        MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Lost"); Reload();
        Check(MetaProgress.History[0].unlocks.Count == 3 && MetaProgress.PendingUnlocks.Count == 3, "Loss receipt and queue survive reload");
    }
    static void CheckMigration()
    {
        Fresh();
        File.WriteAllText(Path.Combine(saveDirectory, "hangar.json"), "{\"version\":2,\"coins\":321,\"lastFrame\":3,\"difficulty\":2,\"ship\":\"Warden\",\"unlocked\":[\"Popcorn\",\"Icicle\",\"ThreeMoonsItem\"],\"stock\":[\"BeamEmitterItem\",\"Icicle\",\"HeatSink\"],\"pin\":\"BeamEmitterItem\",\"loadout\":[\"Icicle\"]}");
        Reload();
        Check(MetaProgress.IsUnlocked("Icicle") && MetaProgress.IsUnlocked("ThreeMoonsItem") && MetaProgress.Coins == 321, "Preserve prior unlocks and bank");
        Check(MetaProgress.Ship == ShipFrame.Warden && MetaProgress.Outbound.Contains("Icicle"), "Preserve ship and purchased loadout");
        Check(MetaProgress.Pin == null && MetaProgress.Stock.All(MetaProgress.CanStock) && !MetaProgress.TryBuy("BeamEmitterItem"), "Remove unearned pinned offer");
        Check(MetaProgress.PendingUnlocks.Count == 0, "Migration never re-celebrates owned items");
        Begin(LevelId.Snow, RunDifficulty.Normal); MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Lost");
        Check(MetaProgress.LastFlight.unlocks.All(n => n.key != "Icicle") && MetaProgress.IsUnlocked("BulletTime"), "Grandfathered clear, new achievement on another hull");
    }
    static void CheckSimulators()
    {
        foreach (var trial in FlightTrials.All)
        {
            Fresh(); FlightTrials.Active = trial; Begin(trial.district, trial.difficulty);
            MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Certified");
            Check(MetaProgress.LastFlight.unlocks.Count == 1 && MetaProgress.LastFlight.unlocks[0].key == trial.reward, "Only simulator blueprint earned");
            Check(ItemUnlocks.All.All(r => !MetaProgress.IsUnlocked(r.Key)), "Simulator cannot award campaign items");
            Check(MetaProgress.CanStock(trial.reward), "Certified blueprint can be stocked");
        }
        FlightTrials.Active = null;
    }
    static void CheckModels()
    {
        for (int row = 0; row < (ItemCatalog.All.Count + GalleryShelf.Columns - 1) / GalleryShelf.Columns; row++)
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
