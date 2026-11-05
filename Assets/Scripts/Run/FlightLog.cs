using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FlightLog
{
    public ShipFrame frame;
    public RunDifficulty difficulty;
    public string contract, outcome, route;
    public int score, earned, banked, bosses, clean, hits, peak = 1, seconds;
    public List<string> awards = new List<string>();
    public List<ItemUnlockNotice> unlocks = new List<ItemUnlockNotice>();
    public string Duration => TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
}

[Serializable]
public sealed class ServiceRecord
{
    public ShipFrame frame;
    public RunDifficulty difficulty;
    public int clears, flawless, flights, victories, deaths, best, seconds;
    public long coins;
}

// The log only opens on a real hangar departure. A simulator flight has its own log
// and can never stamp the campaign hull's service plate.
public static partial class MetaProgress
{
    static FlightLog flight;
    static float departed;
    static int bossHits, districtHits;
    static Type engagedBoss;
    static bool districtDefeated;
    public static event Action<BossClearNotice> BossCleared;
    public static FlightLog LastFlight
    {
        get; private set;
    }
    public static IReadOnlyList<FlightLog> History => Data.history;
    public static string Pin => Data.pin;
    public static RunDifficulty FlightSetting
    {
        get => Data.difficulty;
        set
        {
            Data.difficulty = value;
            Save();
        }
    }
    public static bool CanFly(ShipFrame frame) => (int)frame >= 0 && (int)frame < ShipFrames.Count && (int)frame <= Data.lastFrame;
    public static bool ContractClear(string id) => Data.completed.Contains(id);
    public static ServiceRecord Record(ShipFrame ship, RunDifficulty difficulty)
    {
        return Data.records.Find(r => r.frame == ship && r.difficulty == difficulty)
            ?? new ServiceRecord { frame = ship, difficulty = difficulty };
    }
    public static void TogglePin(string key)
    {
        if (!Data.stock.Contains(key))
            return;
        Data.pin = Pin == key ? null : key;
        Save();
    }
    public static void TookHit()
    {
        if (flight != null)
            flight.hits++;
    }
    public static void Multiplier(int value)
    {
        if (flight != null)
            flight.peak = Mathf.Max(flight.peak, value);
    }
    public static void EnterDistrict()
    {
        if (flight != null)
        {
            flight.route += " / " + LevelRun.CurrentName;
            districtHits = flight.hits;
            engagedBoss = null;
            districtDefeated = false;
        }
    }
    [Serializable]
    sealed class BossRotation
    {
        public LevelId district;
        public int played, last = -1, seed, stage;
    }

    public static Type EngageBoss()
    {
        if (flight != null)
            bossHits = flight.hits;
        engagedBoss = SelectBoss();
        return engagedBoss;
    }

    static Type SelectBoss()
    {
        var roster = DistrictWaves.Bosses(LevelRun.Current);
        if (roster.Count == 1 || FlightTrials.Active != null)
            return roster[0];
        var random = new System.Random(unchecked(LevelRun.Seed + 397 * LevelRun.Stage + 7919 * (int)LevelRun.Current));
        if (flight == null) return roster[random.Next(roster.Count)];

        // Draw only when the pilot actually reaches the boss. Reloading the same district
        // (including a Warp Core retry) must reproduce its encounter without spending another draw.
        var bag = Data.bossRotations.Find(b => b.district == LevelRun.Current);
        if (bag == null)
        {
            bag = new BossRotation { district = LevelRun.Current };
            Data.bossRotations.Add(bag);
        }
        if (bag.seed == LevelRun.Seed && bag.stage == LevelRun.Stage && bag.last >= 0 && bag.last < roster.Count)
            return roster[bag.last];
        int all = (1 << roster.Count) - 1;
        int available = all & ~bag.played;
        if (available == 0)
        {
            bag.played = 0;
            available = all & ~(1 << bag.last);
        }
        var choices = new List<int>(roster.Count);
        for (int i = 0; i < roster.Count; i++)
            if ((available & (1 << i)) != 0) choices.Add(i);
        bag.last = choices[random.Next(choices.Count)];
        bag.played |= 1 << bag.last;
        bag.seed = LevelRun.Seed;
        bag.stage = LevelRun.Stage;
        Save();
        return roster[bag.last];
    }
    public static void DefeatBoss()
    {
        if (flight == null || engagedBoss == null || districtDefeated)
            return;
        districtDefeated = true;
        flight.bosses++;
        bool clean = bossHits == flight.hits;
        if (clean)
            flight.clean++;
        if (flight.contract != null)
            return;
        var record = StoredRecord();
        int mark = 1 << (int)LevelRun.Current;
        if ((record.clears & mark) == 0)
            flight.awards.Add(LevelRun.CurrentName + "  /  boss stamp");
        record.clears |= mark;
        if (clean && (record.flawless & mark) == 0)
            flight.awards.Add(LevelRun.CurrentName + "  /  no-hit star");
        if (clean)
            record.flawless |= mark;
        int previousUnlocks = flight.unlocks.Count;
        AwardDistrictItems(clean);
        Save();
        bool noHitEligible = ItemUnlocks.AllowsNoHit(flight.difficulty);
        BossCleared?.Invoke(new BossClearNotice(noHitEligible && flight.hits == districtHits,
            noHitEligible && clean, flight.unlocks.GetRange(previousUnlocks, flight.unlocks.Count - previousUnlocks)));
    }
    static ServiceRecord StoredRecord()
    {
        var r = Record(flight.frame, flight.difficulty);
        if (!Data.records.Contains(r))
            Data.records.Add(r);
        return r;
    }
    public static void BeginRun()
    {
        Data.runActive = true;
        flight = new FlightLog
        {
            frame = ShipFrames.Current.frame,
            difficulty = Difficulty.Current,
            route = LevelRun.CurrentName,
            contract = FlightTrials.Active?.title
        };
        LastFlight = null;
        bossHits = districtHits = 0;
        engagedBoss = null;
        districtDefeated = false;
        departed = Time.time;
        Save();
    }
    public static void EndRun(string outcome = "Recalled")
    {
        if (!Data.runActive || flight == null)
            return;
        flight.outcome = outcome;
        flight.seconds = Mathf.RoundToInt(Time.time - departed);
        flight.score = Score.CurrentScore;
        flight.earned = RunCoins.Earned;
        flight.banked = Mathf.Min(MaxCoins - Data.coins, RunCoins.Count);
        Data.coins += flight.banked;
        if (flight.contract == null)
        {
            var r = StoredRecord();
            r.flights++;
            r.seconds += flight.seconds;
            r.coins += flight.earned;
            r.best = Mathf.Max(r.best, flight.score);
            if (outcome == "Lost")
                r.deaths++;
            if (outcome == "Victory")
            {
                r.victories++;
                if ((int)flight.frame == Data.lastFrame && Data.lastFrame + 1 < ShipFrames.Count)
                {
                    Data.lastFrame++;
                    flight.awards.Insert(0, "Hull released  /  " + (ShipFrame)Data.lastFrame);
                }
            }
        }
        else if (outcome == "Certified" && !ContractClear(flight.contract))
        {
            Data.completed.Add(flight.contract);
            string key = FlightTrials.Active.reward;
            UnlockItem(key, GalleryShelf.TryGet(key, out var exhibit) ? exhibit.title : FlightTrials.Display(key),
                FlightTrials.Active.Requirement, FlightTrials.Active.district);
            flight.awards.Add("Blueprint  /  " + FlightTrials.Display(key));
        }
        Data.history.Insert(0, flight);
        if (Data.history.Count > 64)
            Data.history.RemoveAt(64);
        LastFlight = flight;
        flight = null;
        Data.runActive = false;
        Restock();
        Save();
    }
}
