using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public sealed class FlightTrials
{
    public readonly string title, objective, reward;
    public readonly ShipFrame frame;
    public readonly RunDifficulty difficulty;
    public readonly LevelId district;
    public readonly string[] kit;
    FlightTrials(string name, string goal, ShipFrame ship, LevelId level, RunDifficulty mode, string blueprint, params string[] items)
    {
        title = name; objective = goal; frame = ship; district = level; difficulty = mode; reward = blueprint; kit = items;
    }
    public static readonly FlightTrials[] All = {
        new FlightTrials("Crossfire", "Defeat the Plains bomber with secondary fire.", ShipFrame.GR07, LevelId.Fields, RunDifficulty.Normal, "WindUpTeeth", "AK45Item", "WindUpTeeth"),
        new FlightTrials("Cold Delivery", "Bring the crate through two Snow waves.", ShipFrame.Warden, LevelId.Snow, RunDifficulty.Normal, "Thermos", "Magnet", "SparePlating"),
        new FlightTrials("Line Discipline", "Clear the Thunder boss's beam lanes.", ShipFrame.Lancer, LevelId.Thunder, RunDifficulty.Hard, "HeatSink", "HeatSink", "PocketFan"),
        new FlightTrials("Last Light", "Defeat the Hell crab. Repairs offline.", ShipFrame.Ember, LevelId.Hell, RunDifficulty.Hard, "EnergyShield", "EnergyShield")
    };
    public static FlightTrials Active { get; set; }
    public static FlightTrials Blueprint(string item) => Array.Find(All, t => t.reward == item);
    public static string Display(string key) => Regex.Replace(key.Replace("Item", ""), "(?<=[a-z])(?=[A-Z])", " ");
    public string[] Sequence()
    {
        if (district == LevelId.Snow) return new[] { "2", "3", "end" };
        if (district == LevelId.Hell) return new[] { "boss" };
        var keys = new List<string>();
        foreach (var key in DistrictWaves.Sequence())
        {
            var slot = RunSlot.Parse(key);
            if (slot.Kind == RunSlotKind.Combat || slot.Kind == RunSlotKind.Miniboss || slot.Kind == RunSlotKind.Boss)
                keys.Add(key);
        }
        return keys.ToArray();
    }
}
