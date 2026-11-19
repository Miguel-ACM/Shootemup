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
    static readonly float[] sentryBeats = { 1.25f, 2.85f, 4.3f }, ramBeats = { 1.7f, 3.7f };

    // Rows enter from the top in reading order. # is a bank, F a fuse shortcut,
    // < and > are sliding fuses, S and R are gun blocks, c and C are escorts.
    static readonly string[][] rooms = {
        new[] { "#........#", "##......##", "##.c....##", "###.....##", "###....F##",
                "###..>...#", "##...>...#", "##......##", "#......###", "#..S...###" },
        new[] { "#.......##", "#...C...##", "#.....F###", "##....####", "##....####",
                "##......##", "##...R...#", "###......#", "####.....#", "###......#" },
        new[] { "##......##", "##..c...##", "#..<....##", "#..<....##", "#......###",
                "##...F####", "##....####", "#.....####", "#.....S.##", "#.......##" },
        new[] { "#.......##", "##......##", "###....c.#", "####F....#", "####.....#",
                "###..>...#", "##...>...#", "#......F##", "#.....####", "#....S.###" },
        new[] { "##.......#", "###......#", "###...C..#", "##...>..##", "#..>.>...#",
                "#.......##", "#.....F###", "##....####", "##.....###", "##.S....##" },
        new[] { "#......###", "#.....####", "##....####", "##...F####", "#.......##",
                "#...R....#", "##.......#", "###....c.#", "####.....#", "###.....##" },
        new[] { "#.......##", "#...S...##", "##.....###", "###....###", "###F....##",
                "###...<..#", "##....<..#", "#........#", "#..c...F##", "#.....####" },
        new[] { "##.......#", "###...c..#", "####.....#", "####F....#", "###......#",
                "#...>....#", "#...>...##", "##......##", "##...C.###", "##.....###" }
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
                    int direction = tile == '<' ? -hand : hand;
                    string actor = tile switch {
                        '#' => nameof(TransitCube), 'S' => nameof(SentryCube), 'R' => nameof(RamCube),
                        'c' => nameof(RivetCutter), 'C' => nameof(RelayCarrier), _ => nameof(FuseCube)
                    };
                    var score = tile == 'R' || tile == 'C' ? ramBeats :
                        tile == 'S' || tile == 'c' ? sentryBeats : Array.Empty<float>();
                    units.Add(new WaveUnit(actor, row * beat / tempo, Vector3.zero) {
                        phrase = FlightPhrase.Flow, curve = moves ? shuttle : bank,
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
        AddRows(landing, second, mirror);
        AddRows(rooms[second], second, (wave & 1) == 0 ? -mirror : mirror);
        return new Wave(budget, ((row - 1) * beat + Travel) / tempo, 1, ConflictGroup.cg.center, units) {
            parts = new[] { "Core chamber " + (first + 1), "Core chamber " + (second + 1) },
            continuationAt = row * beat / tempo
        };
    }

    static readonly string[] landing = { "#........#", "#........#", "#........#" };
}
