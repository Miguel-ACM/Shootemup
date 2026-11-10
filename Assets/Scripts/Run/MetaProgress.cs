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
    // Fifty easy-to-read items for variety from the first run (Miguel, 9 November 2026):
    // weapons, secondary fire, companions, actives and plain passives. Achievement rewards
    // and simulator blueprints stay locked; the rest comes from the outfitter.
    static readonly string[] startingPool =
    {
        // The original first pool.
        "Popcorn", "PocketFan", "HotSauce", "BulletDroneItem", "BadGlasses", "ScatterCannonItem", "Magnet", "SparePlating",
        "LaserDroneItem", "Poison", "BouncingBall", "WindUpCannonItem", "AnotherArm", "BlankBullet",
        // Weapons and secondary fire.
        "NeedleCannonItem", "AK45Item", "PaperclipBox", "SaltShaker", "SewingKit", "FireflyJar",
        // Companions.
        "TwerpItem", "BabyYouItem",
        // Actives.
        "BarrelRoll", "FirecrackerBelt", "NailBomb", "Overdrive", "HealthPacks", "Redline", "DecoyProjector", "FillableFlask",
        // Plain passives.
        "ClockworkSpring", "SteelWashers", "RollerBearings", "LuckyPenny", "ProteinSupplement", "QualityGlasses", "Lighter",
        "GasolineLeak", "FocusingLens", "BallBearing", "RubberBand", "EarlyRise", "EnergyCore", "ReserveCell", "Capacitor",
        "MagnifyingGlass", "SalvageConverter", "RabbitsFoot", "FocusTime", "EmpPulse",
    };

    [Serializable]
    sealed class SaveData
    {
        public int version = 5;
        public int lastFrame;
        public RunDifficulty difficulty = RunDifficulty.Normal;
        public string pin;
        public List<FlightLog> history = new List<FlightLog>();
        public List<ServiceRecord> records = new List<ServiceRecord>();
        public List<BossRotation> bossRotations = new List<BossRotation>();
        public List<string> completed = new List<string>();
        public int coins;
        public string ship = nameof(ShipFrame.GR07);
        public bool runActive;
        public List<string> unlocked = new List<string>();
        // Districts opened beyond the first route (LevelId names).
        public List<string> routes = new List<string>();
        public List<ItemUnlockNotice> pendingUnlocks = new List<ItemUnlockNotice>();
        public List<string> stock = new List<string>();
        // Bought in the hangar: equipped at the start of the next run, then gone.
        public List<string> loadout = new List<string>();
    }

    static SaveData data;
#if UNITY_EDITOR
    // Play-mode reviews can route all reads, recovery and writes to their disposable checkout.
    public static string ReviewSaveDirectory { private get; set; }
    static string FilePath => Path.Combine(ReviewSaveDirectory ?? Application.persistentDataPath, "hangar.json");
#else
    static string FilePath => Path.Combine(Application.persistentDataPath, "hangar.json");
#endif
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
                return 1000;
            case "epic":
                return 500;
            case "rare":
                return 300;
            default:
                return 150;
        }
    }

    /// <summary>Buys a stocked item; a fresh item takes its place on the same platform (null when none is left).</summary>
    public static bool TryBuy(string item, out string replacement)
    {
        replacement = null;
        int slot = Data.stock.IndexOf(item);
        if (slot < 0 || !CanStock(item))
            return false;
        int price = Price(ItemPool.RarityOf(item));
        if (Data.coins < price)
            return false;
        Data.coins -= price;
        if (Pin == item) Data.pin = null;
        replacement = DrawStock(item);
        if (replacement != null)
            Data.stock[slot] = replacement;
        else
            Data.stock.RemoveAt(slot);
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

    /// <summary>Debug: the whole item pool and every district become available. The shop keeps its stock.</summary>
    public static void UnlockAll()
    {
        foreach (var item in ItemPool.AllItems)
            if (!IsUnlocked(item))
                Data.unlocked.Add(item);
        OpenAllRoutes();
        Save();
    }

    // Locked items first; once few or none are left, owned items fill the shelf, so it always shows three.
    static void Restock()
    {
        var locked = new List<string>();
        var owned = new List<string>();
        foreach (var item in ItemPool.AllItems)
            if (item != Pin && CanStock(item))
                (IsUnlocked(item) ? owned : locked).Add(item);
        Data.stock.Clear();
        if (CanStock(Pin)) Data.stock.Add(Pin);
        else Data.pin = null;
        foreach (var pool in new[] { locked, owned })
            while (Data.stock.Count < ShopSize && pool.Count > 0)
            {
                int pick = UnityEngine.Random.Range(0, pool.Count);
                Data.stock.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
    }

    // One fresh offer, by the same preference as a restock: a locked item if any remain, else an owned one.
    static string DrawStock(string sold)
    {
        var locked = new List<string>();
        var owned = new List<string>();
        foreach (var item in ItemPool.AllItems)
            if (item != sold && !Data.stock.Contains(item) && CanStock(item))
                (IsUnlocked(item) ? owned : locked).Add(item);
        var pool = locked.Count > 0 ? locked : owned;
        return pool.Count > 0 ? pool[UnityEngine.Random.Range(0, pool.Count)] : null;
    }

    public static bool DeleteProgress()
    {
        try
        {
            // If erasure is interrupted, the committed save is the last file touched.
            File.Delete(FilePath + ".tmp");
            File.Delete(FilePath + ".bak");
            File.Delete(FilePath);
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            Debug.LogWarning("Could not erase flight data. " + e.Message);
            return false;
        }
        data = null;
        flight = LastFlight = null;
        return true;
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
        data.bossRotations ??= new List<BossRotation>();
        data.pendingUnlocks ??= new List<ItemUnlockNotice>();
        data.routes ??= new List<string>();
        foreach (var log in data.history) log.unlocks ??= new List<ItemUnlockNotice>();
        if (data.version < 2)
        {
            data.version = 2;
            data.difficulty = RunDifficulty.Normal;
            data.ship = nameof(ShipFrame.GR07);
            if (data.stock.Exists(k => !IsUnlocked(k) && FlightTrials.Blueprint(k) != null)) Restock();
            Save();
        }
        if (data.version < 3)
        {
            // Grandfather every owned item. Only remove unearned offers, including pinned ones.
            data.version = 3;
            if (!CanStock(Pin)) data.pin = null;
            if (data.stock.Exists(k => !CanStock(k))) Restock();
            Save();
        }
        if (data.version < 4)
        {
            // Route locks arrive for every save (Miguel's choice): all pilots restart on the first route.
            data.version = 4;
            data.routes.Clear();
            Save();
        }
        if (data.version < 5)
        {
            // The starting pool grew to fifty; existing saves receive the new starters too.
            data.version = 5;
            foreach (var key in startingPool)
                if (!data.unlocked.Contains(key))
                    data.unlocked.Add(key);
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
