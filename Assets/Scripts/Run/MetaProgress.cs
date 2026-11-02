using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// What survives between runs: banked coins, the item unlock pool, the hangar shop's stock and the chosen ship.
/// Coins left in the run wallet are banked when the run ends; the shop restocks only then.
/// </summary>
public static partial class MetaProgress
{
    public const int ShopSize = 3;
    // The hangar counter shows four digits.
    public const int MaxCoins = 9999;
    // A deliberately small first pool; everything else is bought in the hangar.
    static readonly string[] startingPool =
    {
        "Popcorn", "PocketFan", "HotSauce", "BulletDroneItem", "BadGlasses", "ScatterCannonItem", "Magnet", "SparePlating",
        "Icicle", "LaserDroneItem", "Poison", "BouncingBall", "ChewingGum", "WindUpCannonItem",
        "AnotherArm", "BulletTime",
        "ThreeMoonsItem", "BlankBullet",
    };

    [Serializable]
    sealed class SaveData
    {
        public int version = 2;
        public int lastFrame;
        public RunDifficulty difficulty = RunDifficulty.Normal;
        public string pin;
        public List<FlightLog> history = new List<FlightLog>();
        public List<ServiceRecord> records = new List<ServiceRecord>();
        public List<string> completed = new List<string>();
        public int coins;
        public string ship = nameof(ShipFrame.GR07);
        public bool runActive;
        public List<string> unlocked = new List<string>();
        public List<string> stock = new List<string>();
        // Bought in the hangar: equipped at the start of the next run, then gone.
        public List<string> loadout = new List<string>();
    }

    static SaveData data;
    static string FilePath => Path.Combine(Application.persistentDataPath, "hangar.json");
    static SaveData Data
    {
        get
        {
            if (data == null)
                Load();
            return data;
        }
    }

    public static int Coins => Data.coins;
    public static IReadOnlyList<string> Outbound => Data.loadout;
    public static IReadOnlyList<string> Stock => Data.stock;
    public static bool IsUnlocked(string item) => Data.unlocked.Contains(item);

    public static ShipFrame Ship
    {
        get => Enum.TryParse(Data.ship, out ShipFrame frame) && CanFly(frame) ? frame : ShipFrame.GR07;
        set
        {
            if (!CanFly(value)) return;
            Data.ship = value.ToString();
            Save();
        }
    }

    public static int Price(string rarity)
    {
        switch (rarity)
        {
            case "legendary":
                return 500;
            case "epic":
                return 300;
            case "rare":
                return 150;
            default:
                return 75;
        }
    }

    public static bool TryBuy(string item)
    {
        if (!Data.stock.Contains(item))
            return false;
        int price = Price(ItemPool.RarityOf(item));
        if (Data.coins < price)
            return false;
        Data.coins -= price;
        Data.stock.Remove(item);
        if (Pin == item) Data.pin = null;
        if (!IsUnlocked(item))
            Data.unlocked.Add(item);
        Data.loadout.Add(item);
        Save();
        return true;
    }

    /// <summary>The items bought since the last run; taking them empties the list.</summary>
    public static string[] TakeLoadout()
    {
        var items = Data.loadout.ToArray();
        Data.loadout.Clear();
        Save();
        return items;
    }

    /// <summary>Debug: the whole item pool becomes available. The shop keeps its stock.</summary>
    public static void UnlockAll()
    {
        foreach (var item in ItemPool.AllItems)
            if (!IsUnlocked(item))
                Data.unlocked.Add(item);
        Save();
    }

    // Locked items first; once few or none are left, owned items fill the shelf, so it always shows three.
    static void Restock()
    {
        var locked = new List<string>();
        var owned = new List<string>();
        foreach (var item in ItemPool.AllItems)
            if (item != Pin && (IsUnlocked(item) || FlightTrials.Blueprint(item) == null))
                (IsUnlocked(item) ? owned : locked).Add(item);
        Data.stock.Clear();
        if (!string.IsNullOrEmpty(Pin)) Data.stock.Add(Pin);
        foreach (var pool in new[] { locked, owned })
            while (Data.stock.Count < ShopSize && pool.Count > 0)
            {
                int pick = UnityEngine.Random.Range(0, pool.Count);
                Data.stock.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
    }

    static void Load()
    {
        try
        {
            string saved = AtomicSaveFile.Read(FilePath, contents =>
            {
                var candidate = JsonUtility.FromJson<SaveData>(contents);
                return candidate != null && candidate.unlocked != null && candidate.unlocked.Count > 0 &&
                    candidate.stock != null && candidate.loadout != null;
            });
            if (saved != null)
                data = JsonUtility.FromJson<SaveData>(saved);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Hangar save could not be read; starting a new one. " + exception.Message);
            data = null;
        }
        if (data == null)
        {
            data = new SaveData();
            data.unlocked.AddRange(startingPool);
            Restock();
            Save();
        }
        data.history ??= new List<FlightLog>();
        data.records ??= new List<ServiceRecord>();
        data.completed ??= new List<string>();
        if (data.version < 2)
        {
            data.version = 2;
            data.difficulty = RunDifficulty.Normal;
            data.ship = nameof(ShipFrame.GR07);
            if (data.stock.Exists(k => !IsUnlocked(k) && FlightTrials.Blueprint(k) != null)) Restock();
            Save();
        }
        // A run that never reached its end (the game was closed) still earns a fresh shop.
        if (data.runActive)
        {
            data.runActive = false;
            Restock();
            Save();
        }
    }

    static void Save()
    {
        try
        {
            AtomicSaveFile.Write(FilePath, JsonUtility.ToJson(data, true));
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Hangar save could not be written: " + exception.Message);
        }
    }
}
