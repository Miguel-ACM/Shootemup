using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ItemPool
{
    private static readonly string[] allCommon = ItemCatalog.Keys(ItemRarity.Common);
    private static readonly string[] allRare = ItemCatalog.Keys(ItemRarity.Rare);
    private static readonly string[] allEpic = ItemCatalog.Keys(ItemRarity.Epic);
    private static readonly string[] allLegendary = ItemCatalog.Keys(ItemRarity.Legendary);

    // Items are removed from these as they are offered, so they are refilled at the start of each run.
    private static List<string> common;
    private static List<string> rare;
    private static List<string> epic;
    private static List<string> legendary;
    private static List<string> defaultPool;

    static ItemPool()
    {
        ResetRun();
    }

    // Runs only draw from the items unlocked in the hangar shop.
    public static void ResetRun()
    {
        common = new List<string>(System.Array.FindAll(allCommon, MetaProgress.IsUnlocked));
        rare = new List<string>(System.Array.FindAll(allRare, MetaProgress.IsUnlocked));
        epic = new List<string>(System.Array.FindAll(allEpic, MetaProgress.IsUnlocked));
        legendary = new List<string>(System.Array.FindAll(allLegendary, MetaProgress.IsUnlocked));
        // Exhausted common offers repeat eligible commons, never bypass hangar unlocks.
        defaultPool = new List<string>(common);
        if (defaultPool.Count == 0)
            foreach (string item in AllItems)
                if (MetaProgress.IsUnlocked(item))
                    defaultPool.Add(item);
    }

    public static IEnumerable<string> AllItems
    {
        get
        {
            foreach (var pool in new[] { allCommon, allRare, allEpic, allLegendary })
                foreach (var item in pool)
                    yield return item;
        }
    }

    public static string RarityOf(string item) => ItemCatalog.Get(item).RarityKey;

    private static string LowerPool(string pool)
    {
        if (pool == "rare")
            return "common";
        if (pool == "epic")
            return "rare";
        if (pool == "legendary")
            return "epic";
        return "default";
    }


    private static string GetNonEmptyPoolString(string pool)
    {
        int i = 0;
        List<string> p;
        while (i < 3)
        {
            pool = LowerPool(pool);
            p = PoolStringToList(pool);
            if (p.Count > 0)
            {
                return pool;
            }
            i++;
        }
        return "default";
    }

    private static List<string> PoolStringToList(string pool)
    {
        List<string> p;
        switch (pool)
        {
            case "common":
                p = common;
                break;
            case "rare":
                p = rare;
                break;
            case "epic":
                p = epic;
                break;
            case "legendary":
                p = legendary;
                break;
            default:
                p = defaultPool;
                break;
        }
        return p;
    }

    private static string GetItemStringFromPool(string pool, out string actualPool)
    {
        List<string> p = PoolStringToList(pool);
        if (p.Count == 0)
        {
            pool = GetNonEmptyPoolString(pool);
            p = PoolStringToList(pool);
        }
        int itemI = Random.Range(0, p.Count);
        string itemString = p[itemI];
        if (pool != "default")
        {
            p.RemoveAt(itemI);
        }
        actualPool = pool == "default" ? RarityOf(itemString) : pool;
        return itemString;



    }

    public static string GetItem(out string pool)
    {
        // Weights: common 50%, rare 38%, epic 10%, legendary 2%. Luck shifts the roll upward.
        float randomNumber = Random.Range(0f, 1f) + GameRules.luck;
        pool = "legendary";

        if (randomNumber < 0.50f)
        {
            pool = "common";
        }
        else if (randomNumber < 0.88f)
        {
            pool = "rare";
        }
        else if (randomNumber < 0.98f)
        {
            pool = "epic";
        }
        // Cursed Coin: every later item is one rarity higher.
        if (PactPool.RarityUpgrade)
            pool = pool == "common" ? "rare" : pool == "rare" ? "epic" : "legendary";
        string itemString = GetItemStringFromPool(pool, out string actualPool);
        pool = actualPool;
        return itemString;
    }


}


