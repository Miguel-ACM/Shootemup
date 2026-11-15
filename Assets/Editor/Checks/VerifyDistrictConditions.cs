#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Batch: -executeMethod VerifyDistrictConditions.Run
// Every district part (condition parts included) must place on both sides, and every condition
// must compose a full district that flies its own parts and none of the parts it hides.
public static class VerifyDistrictConditions
{
    static readonly List<string> failures = new List<string>();
    static readonly List<string> report = new List<string>();
    static readonly int[] Budgets = { 2, 3, 4, 6, 8, 10, 12, 14, 16, 20 };

    public static void Run()
    {
        failures.Clear();
        report.Clear();
        try
        {
            Placement();
            Compositions();
            Seeds();
            Elites();
            Modifiers();
            Events();
        }
        catch (Exception ex)
        {
            failures.Add("Exception: " + ex);
        }
        finally
        {
            DistrictConditions.Forced = null;
            LevelModifiers.Forced = null;
            Difficulty.Select(RunDifficulty.Normal);
        }
        report.Add(failures.Count == 0 ? "PASS" : "FAIL " + failures.Count);
        report.AddRange(failures);
        Directory.CreateDirectory("ArtSource/DistrictConditionsReview");
        File.WriteAllLines("ArtSource/DistrictConditionsReview/Verification.txt", report);
        Debug.Log("VerifyDistrictConditions: " + string.Join("\n", report));
        if (Application.isBatchMode)
            EditorApplication.Exit(failures.Count == 0 ? 0 : 1);
    }

    static void Check(bool condition, string message)
    {
        if (!condition)
            failures.Add(message);
    }

    static IEnumerable<LevelId> Districts() => ((LevelId[])Enum.GetValues(typeof(LevelId))).Where(l => l != LevelId.Void);

    static void Placement()
    {
        foreach (var level in Districts())
        {
            LevelRun.Reset(level, 17381);
            var parts = WavePartLibrary.All(level);
            Check(parts.Select(p => p.Name).Distinct().Count() == parts.Length, level + ": duplicate part names");
            int placed = 0;
            foreach (var part in parts)
            {
                DistrictConditions.Forced = part.Only ?? DistrictCondition.Ordinary;
                foreach (int side in new[] { -1, 1 })
                {
                    // The composer tries further variants, so one awkward variant is reported, not fatal.
                    int sidePlaced = 0;
                    foreach (int variant in new[] { 0, 5, 10 })
                    {
                        if (WaveComposer.PreviewPart(part, variant, side, out _) != null)
                            sidePlaced++;
                        else
                            report.Add("Note: " + level + " / " + part.Name + " skips variant " + variant + " on side " + side);
                    }
                    Check(sidePlaced >= 2, "Unplaceable: " + level + " / " + part.Name + " / side " + side);
                    placed += sidePlaced;
                }
            }
            DistrictConditions.Forced = null;
            report.Add(level + ": " + parts.Length + " parts, " + placed + " placements");
        }
    }

    static void Compositions()
    {
        var cases = new[]
        {
            (LevelId.Fields, DistrictCondition.Ordinary), (LevelId.Fields, DistrictCondition.Dusk), (LevelId.Fields, DistrictCondition.Night),
            (LevelId.Snow, DistrictCondition.Ordinary), (LevelId.Snow, DistrictCondition.Blizzard),
            (LevelId.Desert, DistrictCondition.Ordinary), (LevelId.Desert, DistrictCondition.Sandstorm),
            (LevelId.Underwater, DistrictCondition.Ordinary), (LevelId.Underwater, DistrictCondition.Minefield)
        };
        foreach (var (level, condition) in cases)
        {
            int own = 0, total = 0, hiddenFlown = 0;
            var names = new HashSet<string>();
            foreach (int seed in new[] { 17381, 4417, 90210, 31337 })
            {
                DistrictConditions.Forced = condition;
                LevelRun.Reset(level, seed);
                var lookup = WavePartLibrary.All(level).ToDictionary(p => p.Name);
                foreach (int budget in Budgets)
                {
                    var wave = DistrictWaves.Create(budget);
                    Check(wave.units.Count >= 5, level + "/" + condition + " budget " + budget + " composed only " + wave.units.Count + " ships");
                    foreach (var name in wave.parts)
                    {
                        if (!lookup.TryGetValue(name, out var part))
                            continue;
                        total++;
                        names.Add(name);
                        if (part.Only == condition)
                            own++;
                        if (!part.Flies(condition))
                            hiddenFlown++;
                    }
                }
            }
            Check(hiddenFlown == 0, level + "/" + condition + ": flew " + hiddenFlown + " parts that this condition hides");
            if (condition != DistrictCondition.Ordinary)
                Check(own >= total / 4, level + "/" + condition + ": only " + own + " of " + total + " parts were its own");
            else
                Check(own == 0, level + " ordinary flew condition parts");
            report.Add(level + "/" + condition + ": " + names.Count + " distinct parts, " + own + " of " + total + " its own");
        }
        DistrictConditions.Forced = null;
    }

    // Elites stay occasional and grow with difficulty and in an ELITES district.
    static void Elites()
    {
        var rates = new Dictionary<string, float>();
        foreach (var difficulty in new[] { RunDifficulty.Easy, RunDifficulty.Normal, RunDifficulty.Hard })
            foreach (var forced in new[] { LevelModifier.None, LevelModifier.Elites })
            {
                Difficulty.Select(difficulty);
                LevelModifiers.Forced = forced;
                int waves = 0, elites = 0;
                foreach (var level in new[] { LevelId.Fields, LevelId.City, LevelId.Space, LevelId.Hell })
                    for (int seed = 1; seed <= 12; seed++)
                    {
                        LevelRun.Reset(level, seed * 7717);
                        foreach (int budget in Budgets)
                        {
                            var wave = DistrictWaves.Create(budget);
                            waves++;
                            elites += wave.units.Count(u => u.elite);
                            Check(wave.units.Where(u => u.elite).Select(u => u.part).Distinct().Count() == wave.units.Count(u => u.elite), "Two elites in one formation");
                        }
                    }
                float rate = elites / (float)waves;
                rates[difficulty + "/" + forced] = rate;
                report.Add("Elites per wave " + difficulty + " / " + forced + ": " + rate.ToString("0.00"));
            }
        Check(rates["Easy/None"] < rates["Normal/None"] && rates["Normal/None"] < rates["Hard/None"], "Elite rate does not grow with difficulty");
        Check(rates["Normal/None"] < .25f, "Ordinary elites too common on Normal");
        Check(rates["Normal/Elites"] > rates["Normal/None"] * 2, "ELITES district does not add enough elites");
        LevelModifiers.Forced = null;
        Difficulty.Select(RunDifficulty.Normal);

        LevelRun.Reset(LevelId.Fields, 17381);
        var enemy = DistrictEnemy.Spawn(typeof(ScoutInterceptor), Vector3.zero, true, true, false);
        try
        {
            float health = enemy.health;
            enemy.MakeElite();
            Check(enemy.Elite && Mathf.Approximately(enemy.health, health * 3) && enemy.GetComponent<EliteMark>() != null, "MakeElite did not toughen and mark the ship");
        }
        finally { UnityEngine.Object.DestroyImmediate(enemy.gameObject); }
    }

    static void Modifiers()
    {
        var counts = new Dictionary<LevelModifier, int>();
        int none = 0;
        for (int seed = 1; seed <= 2000; seed++)
        {
            var set = LevelModifiers.For(LevelId.Snow, seed, 2);
            if (set == LevelModifier.None)
                none++;
            foreach (var modifier in LevelModifiers.All)
                if ((set & modifier) != 0)
                    counts[modifier] = counts.TryGetValue(modifier, out int n) ? n + 1 : 1;
        }
        foreach (var modifier in LevelModifiers.All)
        {
            counts.TryGetValue(modifier, out int n);
            report.Add("Modifier " + modifier + ": " + n + " of 2000 districts");
            Check(n > 300 && n < 600, "Modifier share off: " + modifier);
            Check(LevelModifiers.Icon(modifier) != null, "Missing icon: " + modifier);
        }
        report.Add("No modifier: " + none + " of 2000 districts");
        Check(LevelModifiers.For(LevelId.Void, 5, 6) == LevelModifier.None, "Void takes modifiers");
        LevelModifiers.Forced = LevelModifier.FastFire;
        LevelRun.Reset(LevelId.Fields, 3);
        float fast = LevelRun.EnemyShotSpeedScale;
        LevelModifiers.Forced = LevelModifier.None;
        Check(Mathf.Approximately(fast, LevelRun.EnemyShotSpeedScale * 1.2f), "FAST FIRE is not +20%");
        LevelModifiers.Forced = null;
        report.Add("Salvage rain coins by stage: " + string.Join(", ", Enumerable.Range(1, 7).Select(stage => CoinRain.Coins(stage, 0))));
    }

    // District events: every one has an icon and label, rush hour is denser, deep passages chain
    // distinct layouts, every tunnel layout leaves room to fly, and each hazard builds cleanly.
    static void Events()
    {
        foreach (DistrictCondition condition in Enum.GetValues(typeof(DistrictCondition)))
        {
            if (condition == DistrictCondition.Ordinary)
                continue;
            Check(DistrictConditions.Icon(condition) != null && DistrictConditions.Label(condition).Length > 0, "Missing icon or label: " + condition);
            Check(DistrictConditions.Allowed(DistrictConditions.Home(condition), condition), "Event has no home district: " + condition);
            int hits = 0;
            for (int seed = 1; seed <= 1000; seed++)
                if (DistrictConditions.For(DistrictConditions.Home(condition), seed) == condition)
                    hits++;
            report.Add("Event " + condition + ": " + hits + " of 1000 seeds");
            Check(hits > 250 && hits < 700, "Event share off: " + condition);
        }

        int ordinaryShips = 0, rushShips = 0;
        foreach (var forced in new[] { DistrictCondition.Ordinary, DistrictCondition.RushHour })
            for (int seed = 1; seed <= 6; seed++)
            {
                DistrictConditions.Forced = forced;
                LevelRun.Reset(LevelId.City, seed * 4241);
                foreach (int budget in Budgets)
                {
                    int ships = DistrictWaves.Create(budget).units.Count;
                    if (forced == DistrictCondition.RushHour)
                        rushShips += ships;
                    else
                        ordinaryShips += ships;
                }
            }
        report.Add("City ships, ordinary vs rush hour: " + ordinaryShips + " vs " + rushShips);
        Check(rushShips > ordinaryShips * 1.35f, "Rush hour is not much denser");

        DistrictConditions.Forced = DistrictCondition.DeepPassages;
        LevelRun.Reset(LevelId.Tunnel, 61211);
        var layouts = new List<int>();
        int passages = 0;
        foreach (int budget in Budgets)
        {
            if (!TunnelPassageWaves.HasPassage(budget))
                continue;
            passages++;
            var wave = DistrictWaves.Create(budget);
            Check(wave.units.All(u => u.passageRoute), "Deep passage ship off the walls");
            Check(wave.units.Count >= 20, "Deep passage crew too light: " + wave.units.Count);
            for (int i = 1; i < wave.units.Count; i++)
                Check(wave.units[i].time >= wave.units[i - 1].time, "Deep passage units out of order");
            layouts.AddRange(wave.units.Where(u => u.passage != 0).Select(u => u.passage));
        }
        Check(passages == 5, "Deep passages should wall five waves, got " + passages);
        Check(layouts.Count == 10 && layouts.Distinct().Count() == 10, "Deep passages should chain ten distinct layouts");
        report.Add("Deep passage layouts: " + string.Join(", ", layouts));
        DistrictConditions.Forced = null;

        for (int layout = 1; layout <= TunnelPassage.LayoutCount; layout++)
        {
            var walls = TunnelPassage.Sections(layout);
            float top = walls.Max(w => w.Top), bottom = walls.Min(w => w.Bottom);
            for (float y = bottom + .1f; y < top; y += .5f)
            {
                // A pilot must fit somewhere across every row of every layout.
                bool open = false;
                for (float x = Camera.LeftLimit + .4f; x <= Camera.RightLimit - .4f && !open; x += .1f)
                    open = !walls.Any(w => w.Contains(new Vector2(x, y), CombatWalls.HullRadius + .25f));
                Check(open, "Tunnel layout " + layout + " closes at height " + y.ToString("0.0"));
                if (!open)
                    break;
            }
            Check(IndustrialMeshFactory.TunnelPassageMesh(layout) != null, "Missing mesh for layout " + layout);
        }

        // ELITE HUNT rules: three catches pay out; one escape ends the hunt without a reward.
        var hunt = new ChallengeProgress();
        hunt.Start(false);
        bool paid = false;
        for (int wave = 0; wave < 3; wave++)
        {
            hunt.BeginWave(true);
            hunt.Catch();
            paid = hunt.FinishWave();
        }
        Check(paid && hunt.Pips.All(p => p == 1) && !hunt.Active, "Three caught elites should complete the hunt");
        hunt.Start(false);
        hunt.BeginWave(true);
        hunt.Catch();
        hunt.FinishWave();
        hunt.BeginWave(true);
        hunt.Escape();
        Check(!hunt.FinishWave() && hunt.Pips[1] == 2 && hunt.Failed && hunt.Remaining == 0, "An escaped elite should end the hunt");
        hunt.BeginWave(true);
        Check(!hunt.CurrentWave, "A failed hunt must not count later waves");
        Check(EncounterChoice.Label(EncounterKind.Challenge) == "ELITE HUNT", "Challenge should read ELITE HUNT");
        EncounterChoice.ChallengeRarity = "epic";
        Check(EncounterChoice.Accent(EncounterKind.Challenge) == AItem.GetRarityColor("epic"), "The hunt hexagon should wear its promised rarity");
        EncounterChoice.ChallengeRarity = null;

        LevelRun.Reset(LevelId.Thunder, 3);
        LightningStrike.Begin(0, 7);
        SpearStrike.Begin(1);
        LevelRun.Reset(LevelId.Candyland, 3);
        var candy = CandyDrop.Drop(0, 2);
        Check(candy != null && !candy.HoldsWave && !candy.CanBeTargeted && candy.CanReceiveDamage, "Candy is not a juggled hazard");
        LevelRun.Reset(LevelId.Underwater, 3);
        var mine = DepthMine.Rise(new Vector3(0, 1, 1));
        Check(mine.FromDepths && !mine.HoldsWave && !mine.CanBeTargeted, "Rising mine is targetable or holds the wave while rising");
        foreach (var leftover in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            if (leftover is LightningStrike || leftover is SpearStrike || leftover is CandyDrop || leftover is DepthMine)
                UnityEngine.Object.DestroyImmediate(leftover.gameObject);
    }

    // The seed spreads conditions roughly as authored.
    static void Seeds()
    {
        var counts = new Dictionary<DistrictCondition, int>();
        for (int seed = 1; seed <= 3000; seed++)
            foreach (var level in new[] { LevelId.Fields, LevelId.Snow, LevelId.Desert })
            {
                var condition = DistrictConditions.For(level, seed);
                counts[condition] = counts.TryGetValue(condition, out int n) ? n + 1 : 1;
            }
        foreach (var pair in counts.OrderBy(p => p.Key))
            report.Add("Seeds 1-3000: " + pair.Key + " " + pair.Value);
        Check(counts.TryGetValue(DistrictCondition.Dusk, out int dusk) && dusk > 800 && dusk < 1200, "Dusk share off: " + dusk);
        Check(counts.TryGetValue(DistrictCondition.Night, out int night) && night > 800 && night < 1200, "Night share off: " + night);
        Check(counts.TryGetValue(DistrictCondition.Blizzard, out int blizzard) && blizzard > 1300 && blizzard < 1700, "Blizzard share off: " + blizzard);
        Check(counts.TryGetValue(DistrictCondition.Sandstorm, out int sand) && sand > 1300 && sand < 1700, "Sandstorm share off: " + sand);
    }
}
#endif
