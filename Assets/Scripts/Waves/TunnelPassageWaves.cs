using System.Collections.Generic;
using UnityEngine;

public static class TunnelPassageWaves
{
    // DEEP PASSAGES: more walled waves, each joining two layouts, chained into one another
    // where the run allows (6-8-10 run straight on; 14 and 16 open their own tunnels).
    public static bool Chained => DistrictConditions.Is(DistrictCondition.DeepPassages);
    static readonly int[] ordinaryBudgets = { 6, 10, 16 }, deepBudgets = { 6, 8, 10, 14, 16 };
    public static bool HasPassage(int budget) => System.Array.IndexOf(Chained ? deepBudgets : ordinaryBudgets, budget) >= 0;

    public static Wave Create(int budget, int previewLayout = 0)
    {
        if (Chained && previewLayout == 0)
            return Deep(budget);
        int layout = budget == 6 ? 1 : budget == 10 ? 2 : 3;
        if (((LevelRun.Seed >> (layout - 1)) & 1) != 0)
            layout += 3;
        if (previewLayout > 0)
            layout = previewLayout;
        var units = new List<WaveUnit>();
        // Each passage of the district flies a different crew order through its bends.
        var actors = crews[(budget == 6 ? 0 : budget == 10 ? 2 : 3) % crews.Length];
        float tempo = WaveComposer.Tempo;
        for (int phrase = 0; phrase < 4; phrase++)
        {
            var curve = Lane(phrase, (phrase == 1 ? -1 : phrase == 2 ? 1 : 0) * .55f);
            int count = CrewSize(actors[phrase], false);
            float start = phrase * 2.5f;
            for (int i = 0; i < count; i++)
                units.Add(new WaveUnit(actors[phrase], (start + i * .72f) / tempo, Vector3.zero)
                {
                    phrase = FlightPhrase.Flow, curve = curve, flightDuration = 7.5f, side = 1, syncFire = true, firePhase = i * .72f,
                    fireTimes = new[] { 2.1f, 3.4f, 4.8f, 6.1f }, passageRoute = true,
                    part = "Solid passage / " + actors[phrase], passage = phrase == 0 && i == 0 ? layout : 0
                });
        }
        return new Wave(budget, 16.5f / tempo, 1, ConflictGroup.cg.center, units) { parts = new[] { "Solid passage", "Threaded procession", "Crossing baffles", "Rail battery", "Exit spinners" } };
    }

    static FlightCurve Lane(int phrase, float lane) => new FlightCurve("Tunnel passage / " + phrase + " / " + lane.ToString("0.00"), Vector2.right, 0,
        lane / Camera.RightLimit, 1.24f,
        lane * .85f / Camera.RightLimit, .85f,
        -lane * .65f / Camera.RightLimit, .5f,
        lane * .7f / Camera.RightLimit, .12f,
        lane / Camera.RightLimit, -.24f);

    // Two layouts per wave, every layout of the district different, drawn from all twelve.
    public static int[] DeepLayouts(int seed)
    {
        var order = new List<int>();
        for (int layout = 1; layout <= TunnelPassage.LayoutCount; layout++)
            order.Add(layout);
        var random = new System.Random(seed ^ 0x6d2b79f5);
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        return order.ToArray();
    }

    // Ships per phrase: lighter than before (Miguel found the tunnels too crowded, 15 November
    // 2026). Deep passages fly one more drone and turret than an ordinary passage.
    static int CrewSize(string actor, bool deep) => actor == nameof(MaintenanceDrone) ? (deep ? 3 : 2) : actor == nameof(WallTurret) ? (deep ? 2 : 1) : 2;

    // Crew sets: each wave of the run mixes different tunnel ships along its bends.
    static readonly string[][] crews =
    {
        new[] { nameof(MaintenanceDrone), nameof(RivetMantis), nameof(WallTurret), nameof(BaffleSpinner), nameof(MaintenanceDrone), nameof(WallTurret) },
        new[] { nameof(BaffleSpinner), nameof(MaintenanceDrone), nameof(RivetMantis), nameof(WallTurret), nameof(BaffleSpinner), nameof(RivetMantis) },
        new[] { nameof(WallTurret), nameof(MaintenanceDrone), nameof(BaffleSpinner), nameof(RivetMantis), nameof(WallTurret), nameof(MaintenanceDrone) },
        new[] { nameof(RivetMantis), nameof(BaffleSpinner), nameof(MaintenanceDrone), nameof(WallTurret), nameof(RivetMantis), nameof(BaffleSpinner) },
        new[] { nameof(MaintenanceDrone), nameof(WallTurret), nameof(RivetMantis), nameof(BaffleSpinner), nameof(WallTurret), nameof(RivetMantis) }
    };

    static Wave Deep(int budget)
    {
        int index = System.Array.IndexOf(deepBudgets, budget);
        var layouts = DeepLayouts(LevelRun.Seed);
        int first = layouts[index * 2 % layouts.Length], second = layouts[(index * 2 + 1) % layouts.Length];
        // Walls scroll at a fixed speed; the wave lasts until the second layout nears its end,
        // so the next chained passage joins the walls instead of opening a fresh tunnel.
        float walls = (TunnelPassage.Length(first) + TunnelPassage.Length(second)) / LevelEnvironment.TunnelSpeed;
        float duration = Mathf.Max(8, walls - 1.1f);
        float tempo = WaveComposer.Tempo;
        var crew = crews[index % crews.Length];
        var units = new List<WaveUnit>();
        var parts = new List<string> { "Deep passage" };
        int phrases = 5;
        float spacing = Mathf.Max(1.6f, (duration - 4) / phrases);
        for (int phrase = 0; phrase < phrases; phrase++)
        {
            float lane = (phrase % 3 == 1 ? -1 : phrase % 3 == 2 ? 1 : 0) * (.4f + .2f * (index % 2));
            var curve = Lane(phrase, lane);
            string actor = crew[phrase];
            int count = CrewSize(actor, true);
            float start = phrase * spacing;
            for (int i = 0; i < count; i++)
                units.Add(new WaveUnit(actor, start + i * .62f, Vector3.zero)
                {
                    phrase = FlightPhrase.Flow, curve = curve, flightDuration = 7.0f, side = 1, syncFire = true, firePhase = i * .62f,
                    fireTimes = new[] { 1.6f, 2.6f, 3.6f, 4.7f, 5.8f }, passageRoute = true,
                    part = "Deep passage / " + actor + " / " + phrase, passage = phrase == 0 && i == 0 ? first : phrase == 0 && i == 1 ? second : 0
                });
            parts.Add("Deep passage / " + actor + " / " + phrase);
        }
        units.Sort((a, b) => a.time.CompareTo(b.time));
        foreach (var unit in units)
            unit.time /= tempo;
        return new Wave(budget, duration, 1, ConflictGroup.cg.center, units) { parts = parts.ToArray() };
    }
}
