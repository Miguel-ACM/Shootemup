#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

public static class WaveVarietyAudit
{
    static void Require(bool pass, string message)
    {
        if (!pass)
            throw new Exception(message);
    }

    public static string Signature(Wave wave) => string.Join(";", wave.units.Select(x => $"{x.part}:{x.enemy}:{x.curve.Name}:{x.time:F4}:{x.side}:{x.lane:F4}:{x.formationDepth:F4}:{x.flightDuration:F3}"));
    public static List<WaveGeometry.Track> Tracks(Wave wave)
    {
        var logical = new List<WaveUnit>();
        foreach (var original in wave.units)
        {
            var unit = (WaveUnit)original.Clone();
            unit.time *= WaveComposer.Tempo;
            logical.Add(unit);
        }
        return WaveGeometry.Tracks(logical);
    }

    public static List<string> Inspect(int sets = 8, Action<string> progress = null)
    {
        var report = new List<string>();
        var routeOwners = new Dictionary<FlightCurve, LevelId>();
        var milliseconds = new List<double>();
        foreach (var level in new[] { LevelId.Fields, LevelId.Snow, LevelId.City, LevelId.Thunder, LevelId.Tunnel })
        {
            var catalog = WavePartLibrary.For(level);
            Require(catalog.Length >= 16, level + " needs sixteen parts");
            Require(catalog.Select(x => x.Name).Distinct().Count() == catalog.Length, level + " duplicate part name");
            Require(catalog.Select(x => x.Route).Distinct().Count() >= 10, level + " needs ten exclusive routes");
            foreach (var part in catalog)
            {
                if (routeOwners.TryGetValue(part.Route, out var owner))
                    Require(owner == level, "Route shared between districts");
                else
                    routeOwners[part.Route] = level;
            }
            var unique = new HashSet<string>();
            float longest = 0;
            int most = 0;
            for (int set = 0; set < sets; set++)
            {
                int seed = 17381 + set * 7919;
                LevelRun.Reset(level, seed);
                string[] previous = Array.Empty<string>();
                Wave preceding = null;
                for (int budget = 2; budget <= 20; budget++)
                {
                    var watch = Stopwatch.StartNew();
                    var wave = DistrictWaves.Create(budget);
                    watch.Stop();
                    milliseconds.Add(watch.Elapsed.TotalMilliseconds);
                    progress?.Invoke($"{level} set {set + 1}/{sets} wave {budget}: {watch.Elapsed.TotalMilliseconds:F2}ms");
                    Require(wave.parts.Length >= 2 && wave.parts.Length <= 3, level + " missing modular parts");
                    Require(wave.duration * WaveComposer.Tempo <= 12.51f, "Composition stretches its combat phrase");
                    Require(!previous.Intersect(wave.parts).Any(), level + " repeats a part in consecutive waves");
                    previous = wave.parts;
                    Require(wave.units.Count >= 5 && wave.units.Count <= 28, level + " empty or overcrowded wave");
                    Require(wave.units[0].time == 0, level + " delayed opening");
                    Require(wave.units.All(x => x.fixedDepth && x.curve != null && x.fireTimes != null && x.fireTimes.Length >= 3), "Missing route, formation spacing or authored score");
                    var tracks = Tracks(wave);
                    Require(WaveGeometry.Clear(tracks), level + " / " + seed + " / " + budget + " has intersecting swept hulls");
                    foreach (var track in tracks)
                    {
                        Require(!WaveGeometry.Visible(track.At(track.Start), track.Radius) && !WaveGeometry.Visible(track.At(track.End), track.Radius), "A hull appears or disappears inside combat");
                        Require(track.End / WaveComposer.Tempo <= wave.duration + .001f, "Wave handoff can overlap its departing ships");
                    }
                    if (preceding != null)
                    {
                        var incoming = Tracks(wave);
                        foreach (var track in incoming)
                            track.Start += preceding.duration * WaveComposer.Tempo;
                        Require(WaveGeometry.Compatible(Tracks(preceding), incoming), "Consecutive waves overlap at handoff");
                    }
                    preceding = wave;
                    var clone = (Wave)wave.Clone();
                    Require(Signature(clone) == Signature(wave) && clone.parts.SequenceEqual(wave.parts), "Cloning changes a wave");
                    Require(Signature(DistrictWaves.Create(budget)) == Signature(wave), "Replay changes the seeded arrangement");
                    unique.Add(Signature(wave));
                    longest = Mathf.Max(longest, wave.duration);
                    most = Mathf.Max(most, wave.units.Count);
                }
            }
            Require(unique.Count >= sets * 17, level + " lacks run-to-run variety");
            report.Add($"PASS {level}: {catalog.Length} authored parts, ten exclusive routes, {unique.Count} distinct waves across {sets} seeds; no repeated neighboring parts or swept hull intersections; maximum {most} ships / {longest:F2}s.");
            LevelRun.Reset(LevelId.Fields, 4137);
            for (int loop = 0; loop < 2; loop++)
            {
                LevelRun.Advance(LevelId.Snow);
                LevelRun.Advance(LevelId.Thunder);
                LevelRun.Advance(LevelId.Fields);
            }
            if (LevelRun.Tier(level) == 2)
                LevelRun.Advance(level);
            if (LevelRun.Tier(level) == 3)
            {
                LevelRun.Advance(LevelId.Snow);
                LevelRun.Advance(level);
            }
            for (int budget = 2; budget <= 20; budget++)
            {
                var harder = DistrictWaves.Create(budget);
                Require(WaveGeometry.Clear(Tracks(harder)), "Third-loop ships overlap in " + level + " wave " + budget);
                Require(harder.duration * WaveComposer.Tempo <= 12.51f, "Third-loop phrase stretches its timing");
            }
            report.Add("PASS " + level + ": all nineteen third-loop waves preserve model spacing and capped timing at the faster tempo.");
            LevelRun.Reset(level, 4137);
            string last = Signature(DistrictWaves.Create(20));
            LevelRun.Reset(level, 9143);
            DistrictWaves.Create(4);
            LevelRun.Reset(level, 4137);
            Require(Signature(DistrictWaves.Create(20)) == last, "Preview selection order changes the seeded plan");
        }
        milliseconds.Sort();
        report.Add($"PASS total {sets * 95 + 95} waves (including 95 third-loop encounters); 80 authored parts / {routeOwners.Count} district-exclusive routes. Build median {milliseconds[milliseconds.Count / 2]:F2} ms, p95 {milliseconds[(int)(milliseconds.Count * .95)]:F2} ms, maximum {milliseconds[milliseconds.Count - 1]:F2} ms (includes first mesh generation).");
        LevelRun.Reset();
        return report;
    }
}
#endif
