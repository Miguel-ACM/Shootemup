using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed class SaveSlotSummary
{
    public int Slot { get; internal set; }
    public bool Exists { get; internal set; }
    public bool Readable { get; internal set; }
    public int Completion, Coins, Districts, Items, TotalItems, Hulls, Trials, Flights, Victories, Best;
    public long Seconds;
    public string Ship, Difficulty;
    public string PlayTime => (Seconds / 3600) + "h " + (Seconds / 60 % 60).ToString("00") + "m";
}

public static partial class MetaProgress
{
    public const int SlotCount = 3;
    public static int CurrentSlot { get; private set; } = 1;

    static string SlotPath(int slot)
    {
        if (slot < 1 || slot > SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
        // File 1 keeps the original path, including its recovery copies.
        return Path.Combine(SaveDirectory, slot == 1 ? "hangar.json" : "hangar-" + slot + ".json");
    }

    static bool SlotExists(string path) => File.Exists(path) || File.Exists(path + ".tmp") || File.Exists(path + ".bak");

    static SaveData ReadSlotData(string path)
    {
        string saved = AtomicSaveFile.Read(path, contents =>
        {
            var candidate = JsonUtility.FromJson<SaveData>(contents);
            return candidate != null && candidate.unlocked != null && candidate.unlocked.Count > 0 &&
                candidate.stock != null && candidate.loadout != null;
        });
        return saved == null ? null : JsonUtility.FromJson<SaveData>(saved);
    }

    public static SaveSlotSummary InspectSlot(int slot)
    {
        string path = SlotPath(slot);
        var summary = new SaveSlotSummary { Slot = slot, Exists = SlotExists(path) };
        var saved = ReadSlotData(path);
        if (saved == null) return summary;
        summary.Readable = true;
        var items = new HashSet<string>(saved.unlocked);
        if (saved.version < 5) items.UnionWith(startingPool);
        var catalog = ItemCatalog.All.Where(item => item.InRewardPool).ToArray();
        var starters = new HashSet<string>(startingPool);
        int earnedItems = catalog.Count(item => !starters.Contains(item.Key) && items.Contains(item.Key));
        int availableItems = catalog.Count(item => !starters.Contains(item.Key));
        var records = saved.records ?? new List<ServiceRecord>();
        int clears = records.Aggregate(0, (mask, record) => mask | record.clears);
        int totalDistricts = Enum.GetValues(typeof(LevelId)).Length;
        summary.Districts = Enumerable.Range(0, totalDistricts).Count(i => (clears & (1 << i)) != 0);
        summary.Items = catalog.Count(item => items.Contains(item.Key));
        summary.TotalItems = catalog.Length;
        summary.Hulls = Mathf.Clamp(saved.lastFrame + 1, 1, ShipFrames.Count);
        summary.Trials = FlightTrials.All.Count(trial => saved.completed?.Contains(trial.title) == true);
        int earned = summary.Districts + earnedItems + summary.Hulls - 1 + summary.Trials;
        int total = totalDistricts + availableItems + ShipFrames.Count - 1 + FlightTrials.All.Length;
        summary.Completion = earned * 100 / total;
        summary.Coins = saved.coins;
        summary.Ship = Enum.TryParse(saved.ship, out ShipFrame hull) && (int)hull >= 0 && (int)hull < summary.Hulls
            ? hull.ToString() : nameof(ShipFrame.GR07);
        summary.Difficulty = Enum.IsDefined(typeof(RunDifficulty), saved.difficulty) ? saved.difficulty.ToString() : "Normal";
        summary.Flights = records.Sum(record => record.flights);
        summary.Victories = records.Sum(record => record.victories);
        summary.Seconds = records.Sum(record => (long)record.seconds);
        summary.Best = records.Count == 0 ? 0 : records.Max(record => record.best);
        return summary;
    }

    public static bool SelectSlot(int slot)
    {
        var summary = InspectSlot(slot);
        if (flight != null || HangarHub.Active || summary.Exists && !summary.Readable) return false;
        int previousSlot = CurrentSlot;
        var previousData = data;
        CurrentSlot = slot;
        data = null;
        Load();
        if (!Save())
        {
            CurrentSlot = previousSlot;
            data = previousData;
            return false;
        }
        LastFlight = null;
        runRoutes = null;
        engagedBoss = null;
        districtDefeated = districtSkipped = false;
        bossHits = districtHits = 0;
        FlightTrials.Active = null;
        return true;
    }

    public static bool DeleteSlot(int slot)
    {
        string path = SlotPath(slot);
        if (slot == CurrentSlot && (flight != null || HangarHub.Active)) return false;
        try
        {
            // Keep the committed file until both recovery copies are gone.
            File.Delete(path + ".tmp");
            File.Delete(path + ".bak");
            File.Delete(path);
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            Debug.LogWarning("Could not erase File " + slot + ". " + e.Message);
            return false;
        }
        if (slot == CurrentSlot)
        {
            data = null;
            LastFlight = null;
            runRoutes = null;
        }
        return true;
    }
}
