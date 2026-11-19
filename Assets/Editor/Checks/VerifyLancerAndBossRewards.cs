#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Edit-time integration checks in an isolated project, with a disposable save directory.
public static class VerifyLancerAndBossRewards
{
    const string Folder = "ReviewOutput/LancerRewards";
    const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
    const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
    static int checks, caseNumber, seed = 24000;
    static readonly string Session = DateTime.UtcNow.Ticks.ToString();

    public static void Run()
    {
        if (!Application.dataPath.Replace('\\', '/').EndsWith("/Temp/UnlockReview/Assets", StringComparison.Ordinal))
            throw new Exception("Use the isolated UnlockReview checkout");
        Directory.CreateDirectory(Folder);
        AudioListener.pause = true;
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CheckLaser();
            CheckSnow();
            CheckRewards();
            CheckMandatoryHandoff();
            CheckPresentation();
            File.WriteAllText(Folder + "/Verification.txt", "PASS\n" + checks + " checks.\n" +
                "Actual laser hits, overlapping beams, plasma, damage guards and firing-rate upgrades; Snow miniboss launch positions; boss-clear events and saved unlocks; end-screen reason layout and stacked no-hit labels.\n" +
                "Headless checks only; no gameplay or visual review.\n");
            if (Environment.GetCommandLineArgs().Contains("--reward-build")) Build();
            VerifyCleanCompilation.Run();
        }
        catch (Exception exception)
        {
            File.WriteAllText(Folder + "/Verification.txt", "FAIL\n" + exception);
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    static void CheckLaser()
    {
        GameRules.ResetRun();
        ShipFrames.ApplyStats(ShipFrames.Get(ShipFrame.Lancer));
        Check(GameRules.playerShootDamage == 4 && GameRules.playerShootNumber == 1, "Lancer stronger starting beam");
        Check(ShipFrames.Get(ShipFrame.GR07).shootDamage == 2 && ShipFrames.Get(ShipFrame.Ember).shootDamage == 2.4f,
            "Other hull damage preserved");
        var beam = new GameObject("Laser damage check").AddComponent<BeamEmitter>();
        var enemy = new GameObject("Laser target", typeof(BoxCollider2D)).AddComponent<LancerReviewTarget>();
        enemy.GetComponent<BoxCollider2D>().size = Vector2.one * 2;
        typeof(AEnemy).GetField("nextImpact", Instance).SetValue(enemy, float.MaxValue);
        AEnemy.Live.Add(enemy);
        Physics2D.SyncTransforms();
        for (int count = 1; count <= 3; count++)
        {
            Paths(beam, Enumerable.Range(0, count).Select(i => (i - (count - 1) * .5f) * .2f).ToArray());
            enemy.health = 100;
            Invoke(beam, "DamageTick", .14f, 1f);
            Near(100 - enemy.health, 4.8f * count, "Each beam deals one hit despite multiple intersecting segments");
        }
        Paths(beam, -.2f, .2f, 4);
        enemy.health = 100;
        Invoke(beam, "DamageTick", .14f, 1f);
        Near(100 - enemy.health, 9.6f, "A missed beam does not add damage");
        enemy.immune = true;
        Invoke(beam, "DamageTick", .14f, 1f);
        Near(enemy.health, 90.4f, "Invulnerable weakpoint rejects all beams");
        enemy.immune = false;
        enemy.GetComponent<Collider2D>().enabled = false;
        Invoke(beam, "DamageTick", .14f, 1f);
        Near(enemy.health, 90.4f, "Disabled contact rejects all beams");
        enemy.GetComponent<Collider2D>().enabled = true;
        enemy.targetable = false;
        Invoke(beam, "DamageTick", .14f, 1f);
        Near(enemy.health, 90.4f, "Hidden target rejects all beams");
        Object.DestroyImmediate(enemy.gameObject);
        AEnemy.Live.Clear();
        var interval = typeof(BeamEmitter).GetProperty("TickInterval", Instance);
        Near((float)interval.GetValue(beam), .12f, "Base laser cadence");
        GameRules.playerShootSpeed = 20;
        Near((float)interval.GetValue(beam), .06f, "Double firing speed doubles sustained damage cadence");
        GameRules.playerShootSpeed = 5;
        Near((float)interval.GetValue(beam), .24f, "Slower firing speed affects sustained beam");
        GameRules.playerShootSpeed = 10;
        Paths(beam, 0, 0, 0);
        var plasma = CombatRound.Launch<PlasmaOrb>(Vector3.zero, Vector2.down);
        Physics2D.SyncTransforms();
        Invoke(beam, "DamageTick", .14f, 1f);
        Near((float)typeof(PlasmaOrb).GetField("health", Instance).GetValue(plasma), 10.6f,
            "Three beams damage destructible plasma three times");
        Invoke(plasma, "Release");
        CombatRound.Live.Clear();
        Object.DestroyImmediate(beam.gameObject);
    }

    static void Paths(BeamEmitter beam, params float[] x)
    {
        var paths = x.Select(lane => Enumerable.Range(0, 17).Select(i => new Vector3(lane, -2 + i * .25f, 0)).ToArray()).ToArray();
        typeof(BeamEmitter).GetField("paths", Instance).SetValue(beam, paths);
    }

    static void CheckSnow()
    {
        GameRules.ResetRun();
        Difficulty.Select(RunDifficulty.Normal);
        var view = new GameObject("Visible screen", typeof(UnityEngine.Camera)).GetComponent<UnityEngine.Camera>();
        view.tag = "MainCamera";
        view.orthographic = true;
        view.orthographicSize = 10;
        view.transform.position = new Vector3(0, 2, -10);
        typeof(CombatRound).GetField("borderFrame", Static).SetValue(null, -1);
        var snow = new GameObject("Snow launch check").AddComponent<SnowSiegeCraft>();
        typeof(DistrictEnemy).GetProperty("Hull").SetValue(snow, snow.transform);
        Invoke(snow, "Attack");
        var flakes = CombatRound.Live.Where(s => s.transform.position.y > 10).ToArray();
        Check(flakes.Length == Difficulty.Dense(18), "Existing snowfall count preserved");
        foreach (var flake in flakes)
            Near(flake.transform.position.y, 12.8f, "Flake begins above the actual visible top");
        Check(CombatRound.Live.Count == flakes.Length * 2, "Upward muzzle spray preserved");
        foreach (var shot in CombatRound.Live.ToArray()) Invoke(shot, "Release");
        CombatRound.Live.Clear();
        Object.DestroyImmediate(snow.gameObject);
        Object.DestroyImmediate(view.gameObject);
    }

    static void Fresh()
    {
        var save = Path.GetFullPath(Folder + "/Saves/" + Session + "/" + ++caseNumber);
        Directory.CreateDirectory(save);
        MetaProgress.ReviewSaveDirectory = save;
        typeof(MetaProgress).GetField("data", Static).SetValue(null, null);
        typeof(MetaProgress).GetField("flight", Static).SetValue(null, null);
        FlightTrials.Reset();
    }
    static void Begin(RunDifficulty difficulty)
    {
        Difficulty.Select(difficulty);
        LevelRun.Reset(LevelId.Hell, ++seed);
        RunCoins.Reset();
        MetaProgress.BeginRun();
    }
    static void CheckRewards()
    {
        var events = new List<BossClearNotice>();
        MetaProgress.BossCleared += events.Add;
        try
        {
            foreach (var difficulty in new[] { RunDifficulty.Easy, RunDifficulty.Normal, RunDifficulty.Hard })
                for (int hit = 0; hit < 3; hit++)
                {
                    Fresh(); Begin(difficulty); events.Clear();
                    if (hit == 1) MetaProgress.TookHit();
                    MetaProgress.EngageBoss();
                    if (hit == 2) MetaProgress.TookHit();
                    MetaProgress.DefeatBoss(); MetaProgress.DefeatBoss();
                    Check(events.Count == 1, "Only one victory event per encounter");
                    bool eligible = difficulty != RunDifficulty.Easy;
                    Check(events[0].NoHitLevel == (eligible && hit == 0), "Full-level no-hit qualification");
                    Check(events[0].NoHitBoss == (eligible && hit != 2), "Boss-only no-hit qualification");
                    // The first Hell clear also opens Void.
                    int expected = (!eligible || hit == 2 ? 1 : hit == 1 ? 2 : 3) + 1;
                    Check(events[0].Unlocks.Count == expected, "Exact newly earned rewards in boss clear notice");
                    Check(events[0].Unlocks.Count(n => n.key == RouteUnlocks.Key(LevelId.Void)) == 1, "Hell opens Void");
                    Check(MetaProgress.PendingUnlocks.Count == expected, "Victory leaves hangar reveal queue intact");
                    MetaProgress.EndRun("Lost");
                    Check(MetaProgress.LastFlight.unlocks.Count == expected, "End screen keeps all awarded items");
                    Check(MetaProgress.LastFlight.unlocks.All(n => !string.IsNullOrEmpty(n.Requirement)), "Saved item reasons resolve");
                }
            Fresh(); Begin(RunDifficulty.Normal); MetaProgress.EngageBoss(); MetaProgress.DefeatBoss(); MetaProgress.EndRun("Lost");
            Begin(RunDifficulty.Hard); events.Clear(); MetaProgress.EngageBoss(); MetaProgress.DefeatBoss();
            Check(events.Count == 1 && events[0].Unlocks.Count == 0 && events[0].NoHitBoss && events[0].NoHitLevel,
                "Repeat clean victories celebrate no-hit without duplicate unlocks");
            Check(MetaProgress.PendingUnlocks.Count == 4, "Existing hangar queue survives repeat victory");
            Fresh(); Check(FlightTrials.Find("Crossfire").TrySelect(), "Crossfire is available"); Begin(FlightTrials.Active.difficulty);
            events.Clear(); MetaProgress.EngageBoss(); MetaProgress.DefeatBoss();
            Check(events.Count == 0, "Simulator cannot emit campaign unlock banner");
            FlightTrials.Reset();
        }
        finally { MetaProgress.BossCleared -= events.Add; }
    }

    static void CheckMandatoryHandoff()
    {
        var go = new GameObject("Route handoff check");
        var waves = go.AddComponent<WaveOrchestrer>();
        try
        {
            foreach (var from in new[] { LevelId.Thunder, LevelId.Desert, LevelId.Tunnel, LevelId.Candyland,
                LevelId.Space, LevelId.Underwater, LevelId.Heaven, LevelId.Hell, LevelId.Snow })
            {
                LevelRun.Reset(from, 17381);
                var handoff = (IEnumerator)typeof(WaveOrchestrer).GetMethod("ChooseNextRoute", Instance).Invoke(waves, null);
                Check(handoff.MoveNext() && handoff.Current is WaitForSeconds && waves.Phase == RunPhase.Routing && CombatDecision.Active,
                    "Boss reward enters a protected route handoff before travel");
                float seconds = (float)typeof(WaitForSeconds).GetField("m_Seconds", Instance).GetValue(handoff.Current);
                Near(seconds, from == LevelId.Snow ? .2f : 1f, "Post-reward handoff from " + from);
            }
        }
        finally
        {
            CombatDecision.End();
            LevelRun.Reset();
            Object.DestroyImmediate(go);
        }
    }

    static void CheckPresentation()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/prstartk.asset");
        var notices = ItemUnlocks.All.Select(r => new ItemUnlockNotice {
            key = r.Key, title = r.Title, area = r.Area, condition = r.Condition, source = r.Requirement
        }).ToList();
        foreach (var trial in FlightTrials.All.Where(tape => tape.reward != null))
            notices.Add(new ItemUnlockNotice { key = trial.reward, title = trial.title, source = trial.Requirement });
        var route = new ItemUnlockNotice { key = RouteUnlocks.Key(LevelId.Underwater), title = "Underwater", area = LevelId.Underwater, source = RouteUnlocks.Requirement(LevelId.Tunnel) };
        notices.Add(route);
        var root = new GameObject("Receipt check", typeof(RectTransform), typeof(Canvas));
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(405, 720);
        UnlockReceipt.Show(root.transform, new FlightLog { unlocks = notices }, font);
        var receipt = root.GetComponentInChildren<UnlockReceipt>();
        Check(receipt.GetComponentsInChildren<TextMeshProUGUI>().Any(l => l.text == "NEW ROUTE"), "New routes take priority on the first receipt page");
        var reasons = root.GetComponentsInChildren<TextMeshProUGUI>(true).Where(t => t.name == "Unlock reason").ToArray();
        Check(reasons.Length == notices.Count, "Every earned item has a reason");
        for (int page = 0; page < notices.Count; page++)
        {
            Invoke(receipt, "SetPage", page);
            Check(reasons.Count(r => r.gameObject.activeInHierarchy) == 1, "One selected reward per page");
            foreach (var reason in reasons.Where(r => r.gameObject.activeInHierarchy))
            {
                reason.SendMessage("Awake");
                reason.ForceMeshUpdate(true, true);
                Check(!reason.isTextOverflowing && reason.textInfo.characterCount == reason.text.Length,
                    "Whole reason fits: " + reason.text);
                var tile = (RectTransform)reason.transform.parent;
                Check(tile.anchoredPosition.y - tile.sizeDelta.y * .5f > -310, "Reason row clears pagination and footer");
            }
        }
        DisposeStudio(receipt);
        Object.DestroyImmediate(root);
        foreach (int marks in new[] { 0, 1, 2 })
        {
            var banner = new GameObject("Banner check", typeof(RectTransform)).AddComponent<BossClearBanner>();
            var unlocked = notices.Where(n => n.area == LevelId.Core).Take(marks == 2 ? 4 : marks).Prepend(route).ToList();
            Invoke(banner, "Build", new BossClearNotice(marks == 2, marks >= 1, unlocked), font);
            var labels = banner.GetComponentsInChildren<TextMeshProUGUI>();
            var routeCard = (RectTransform)banner.transform.Find("Flight area/New route");
            Check(routeCard != null && routeCard.GetComponentsInChildren<TextMeshProUGUI>().Any(l => l.text == "NEXT FLIGHT"),
                "New route has a dedicated stamped announcement and clear availability");
            Check(labels.Count(l => l.text.StartsWith("No-hit")) == marks, "Only earned no-hit labels appear");
            if (marks == 2)
                Check(labels.Single(l => l.text == "No-hit level").rectTransform.anchoredPosition.y >
                    labels.Single(l => l.text == "No-hit boss").rectTransform.anchoredPosition.y, "No-hit labels stack level above boss");
            var group = banner.GetComponent<CanvasGroup>();
            Check(!group.interactable && !group.blocksRaycasts, "Victory display leaves boss reward controls available");
            foreach (var label in labels)
            {
                label.SendMessage("Awake"); label.ForceMeshUpdate(true, true);
                Check(!label.isTextOverflowing && label.textBounds.size.x <= label.rectTransform.rect.width + .1f, "Victory text fits: " + label.text);
            }
            DisposeStudio(banner);
            Object.DestroyImmediate(banner.gameObject);
        }
    }

    static void DisposeStudio(Component owner)
    {
        var field = owner.GetType().GetField("studio", Instance);
        var studio = (ItemPortraitStudio)field.GetValue(owner);
        if (studio != null)
        {
            var atlas = typeof(ItemPortraitStudio).GetField("atlas", Instance);
            var texture = (RenderTexture)atlas.GetValue(studio);
            atlas.SetValue(studio, null);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(studio.gameObject);
        }
        field.SetValue(owner, null);
    }
    static void Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Instance).Invoke(target, args);
    static void Near(float actual, float expected, string message) => Check(Mathf.Abs(actual - expected) < .001f, message + ": " + actual);
    static void Check(bool value, string message)
    {
        checks++;
        if (!value) throw new Exception(message);
    }
    static void Build()
    {
        string output = Path.GetFullPath("Build/LancerRewards/Shootemup.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = BuildTarget.StandaloneWindows64, locationPathName = output, options = BuildOptions.None
        });
        File.WriteAllText(Folder + "/Build.txt", report.summary.result + "\n" + output + "\nErrors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings);
        Check(report.summary.result == BuildResult.Succeeded, "Windows build");
    }
}

public sealed class LancerReviewTarget : AEnemy
{
    public bool immune, targetable = true;
    public override bool CanBeTargeted => targetable && base.CanBeTargeted;
    public override bool CanReceiveDamage => !immune && base.CanReceiveDamage;
    public override void damage(float amount) => health -= amount;
}
#endif
