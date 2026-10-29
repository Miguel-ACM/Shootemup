using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// What survives between runs: banked coins, the item unlock pool, the hangar shop's stock and the chosen ship.
/// Coins left in the run wallet are banked when the run ends; the shop restocks only then.
/// </summary>
public static class MetaProgress
{
    public const int ShopSize = 3;
    // A deliberately small first pool; everything else is bought in the hangar.
    static readonly string[] startingPool =
    {
        "Popcorn", "PocketFan", "HotSauce", "BulletDroneItem", "BadGlasses", "ScatterCannonItem", "Magnet", "SparePlating",
        "Icicle", "LaserDroneItem", "Poison", "BouncingBall", "ChewingGum", "WindUpCannonItem",
        "AnotherArm", "BulletTime",
        "ThreeMoonsItem",
    };

    [Serializable]
    sealed class SaveData
    {
        public int version = 1;
        public int coins;
        public string ship = nameof(ShipFrame.GR07);
        public bool runActive;
        public List<string> unlocked = new List<string>();
        public List<string> stock = new List<string>();
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
    public static IReadOnlyList<string> Stock => Data.stock;
    public static bool IsUnlocked(string item) => Data.unlocked.Contains(item);

    public static ShipFrame Ship
    {
        get => Enum.TryParse(Data.ship, out ShipFrame frame) ? frame : ShipFrame.GR07;
        set
        {
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
        if (!Data.stock.Contains(item) || IsUnlocked(item))
            return false;
        int price = Price(ItemPool.RarityOf(item));
        if (Data.coins < price)
            return false;
        Data.coins -= price;
        Data.stock.Remove(item);
        Data.unlocked.Add(item);
        Save();
        return true;
    }

    public static void BeginRun()
    {
        Data.runActive = true;
        Save();
    }

    /// <summary>Banks the wallet and restocks the shop. Safe to call more than once per run.</summary>
    public static void EndRun()
    {
        if (!Data.runActive)
            return;
        Data.coins = (int)Math.Min(int.MaxValue, (long)Data.coins + RunCoins.Count);
        Data.runActive = false;
        Restock();
        Save();
    }

    static void Restock()
    {
        var locked = new List<string>();
        foreach (var item in ItemPool.AllItems)
            if (!IsUnlocked(item))
                locked.Add(item);
        Data.stock.Clear();
        while (Data.stock.Count < ShopSize && locked.Count > 0)
        {
            int pick = UnityEngine.Random.Range(0, locked.Count);
            Data.stock.Add(locked[pick]);
            locked.RemoveAt(pick);
        }
    }

    static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
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
            string temporary = FilePath + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
            if (File.Exists(FilePath))
                File.Delete(FilePath);
            File.Move(temporary, FilePath);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Hangar save could not be written: " + exception.Message);
        }
    }
}
