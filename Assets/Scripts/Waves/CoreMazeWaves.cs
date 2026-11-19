using System;
using System.Collections.Generic;
using UnityEngine;

public static class CoreMazeWaves
{
    const int Columns = 10;
    const float Travel = 6.4f, Top = 1.08f, Bottom = -.08f, RowPitch = .96f;
    public static float CellWidth => Camera.RightLimit * 2 / Columns;
    static readonly FlightCurve bank = new FlightCurve("Core conveyor", Vector2.right, 0, 0, Top, 0, Bottom);
    static readonly FlightCurve shuttle = new FlightCurve("Core shuttle", Vector2.right, 0,
        0, Top, -.18f, .848f, -.18f, .616f, .22f, .384f, .22f, .152f, 0, Bottom);
    static readonly FlightCurve escort = new FlightCurve("Core escort weave", Vector2.right, 0,
        0, Top, .07f, .80f, -.08f, .52f, .07f, .24f, 0, Bottom);
    static readonly float[] sentryBeats = { 1.08f, 2.12f, 3.18f, 4.32f };
    static readonly float[] ramBeats = { 1.32f, 2.72f, 4.08f };
    static readonly float[] cutterBeats = { 1.16f, 1.82f, 3.04f, 3.70f, 4.65f };
    static readonly float[] relayBeats = { 1.48f, 2.58f, 3.72f, 4.78f };

    // Rows enter from the top in reading order. # is a bank, F a fuse shortcut,
    // < and > are sliding fuses, S and R are gun blocks, c and C are escorts.
    static readonly string[][] rooms = {
        new[] { "#.....C..#", "##......##", "##.c....##", "###.....##", "###....F##",
                "###..>...#", "##...>...#", "##....c.##", "#......###", "#..S...###" },
        new[] { "#.......##", "#...C...##", "#.....S###", "##....####", "##.c..####",
                "##......##", "##...R...#", "###......#", "####..c..#", "###......#" },
        new[] { "##....C.##", "##..c...##", "#..<....##", "#..<....##", "#......###",
                "##...F####", "##....####", "#..c..####", "#.....S.##", "#.......##" },
        new[] { "#...C...##", "##......##", "###....c.#", "####F....#", "####.....#",
                "###..>...#", "##...>...#", "#..c...F##", "#.....####", "#....S.###" },
        new[] { "##.......#", "###.c....#", "###...C..#", "##...>..##", "#..>.>...#",
                "#.......##", "#.....F###", "##.c..####", "##.....###", "##.S....##" },
        new[] { "#..C...###", "#.....####", "##....####", "##...S####", "#.......##",
                "#...R....#", "##.......#", "###....c.#", "####.....#", "###..c..##" },
        new[] { "#.......##", "#...S...##", "##....c###", "###....###", "###F....##",
                "###...<..#", "##....<..#", "#....C...#", "#..c...F##", "#.....####" },
        new[] { "##.....C.#", "###...c..#", "####.....#", "####S....#", "###......#",
                "#...>....#", "#...>...##", "##.c....##", "##...C.###", "##.....###" }
    };

    public static Wave Create(int budget, int seed)
    {
        int[] order = { 0, 1, 2, 3, 4, 5, 6, 7 };
        var random = new System.Random(seed);
        for (int i = order.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        // Rotate through all eight rooms before repeating an entrance. A second
        // permutation of their pairings gives later waves different junctions.
        int wave = budget < 5 ? budget - 2 : budget / 2;
        int first = order[wave % order.Length];
        int second = order[(wave + 1 + wave / 3) % order.Length];
        int mirror = random.Next(2) == 0 ? -1 : 1;
        var units = new List<WaveUnit>();
        int row = 0;
        float beat = RowPitch * Travel / ((Top - Bottom) * (Camera.UpLimit - Camera.DownLimit));
        float tempo = WaveComposer.Tempo;

        void AddRows(string[] layout, int room, int hand)
        {
            foreach (string tiles in layout)
            {
                for (int x = 0; x < Columns; x++)
                {
                    char tile = tiles[x];
                    if (tile == '.') continue;
                    int start = x;
                    while (tile == '#' && x + 1 < Columns && tiles[x + 1] == '#') x++;
                    float lane = ((start + x + 1f) / Columns - 1) * hand;
                    bool moves = tile == '<' || tile == '>' || tile == 'R';
                    int direction = tile == '<' || tile == 'C' ? -hand : hand;
                    string actor = tile switch {
                        '#' => nameof(TransitCube), 'S' => nameof(SentryCube), 'R' => nameof(RamCube),
                        'c' => nameof(RivetCutter), 'C' => nameof(RelayCarrier), _ => nameof(FuseCube)
                    };
                    var score = tile switch {
                        'R' => ramBeats, 'C' => relayBeats, 'S' => sentryBeats,
                        'c' => cutterBeats, _ => Array.Empty<float>()
                    };
                    units.Add(new WaveUnit(actor, row * beat / tempo, Vector3.zero) {
                        phrase = FlightPhrase.Flow, curve = moves ? shuttle : tile == 'c' || tile == 'C' ? escort : bank,
                        lane = lane * direction, side = direction, flightDuration = Travel,
                        coreBankTiles = tile == '#' ? x - start + 1 : 0, syncFire = true,
                        fireTimes = score, part = "Core chamber " + (room + 1)
                    });
                }
                row++;
            }
        }
        // A widened landing between turns lets the pilot cross to either exit.
        AddRows(landing, first, mirror);
        AddRows(rooms[first], first, mirror);
        AddRows(budget >= 8 ? guardedLanding : landing, second, mirror);
        AddRows(rooms[second], second, (wave & 1) == 0 ? -mirror : mirror);
        return new Wave(budget, ((row - 1) * beat + Travel) / tempo, 1, ConflictGroup.cg.center, units) {
            parts = new[] { "Core chamber " + (first + 1), "Core chamber " + (second + 1) },
            continuationAt = row * beat / tempo
        };
    }

    static readonly string[] landing = { "#........#", "#........#", "#........#" };
    static readonly string[] guardedLanding = { "#..c.....#", "#........#", "#.....C..#" };
}
