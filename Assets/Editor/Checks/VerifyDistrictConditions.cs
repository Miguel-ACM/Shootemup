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
        }
        catch (Exception ex)
        {
            failures.Add("Exception: " + ex);
        }
        finally
        {
            DistrictConditions.Forced = null;
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
            (LevelId.Desert, DistrictCondition.Ordinary), (LevelId.Desert, DistrictCondition.Sandstorm)
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
