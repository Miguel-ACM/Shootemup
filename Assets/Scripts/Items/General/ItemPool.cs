using System.Collections.Generic;
using UnityEngine;

public static class ItemPool
{
    static List<string>[] remaining;
    static bool kitOffered;
    public static int RemainingCount => remaining[0].Count + remaining[1].Count + remaining[2].Count + remaining[3].Count;

    static ItemPool() => ResetRun();

    public static IEnumerable<string> AllItems
    {
        get
        {
            foreach (var item in ItemCatalog.All)
                if (item.InRewardPool) yield return item.Key;
        }
    }

    public static void ResetRun()
    {
        remaining = new[] { new List<string>(), new List<string>(), new List<string>(), new List<string>() };
        kitOffered = false;
        foreach (var item in ItemCatalog.All)
            if (item.InRewardPool && MetaProgress.IsUnlocked(item.Key))
                remaining[(int)item.Rarity].Add(item.Key);
    }

    public static string RarityOf(string key) => ItemCatalog.Get(key).RarityKey;

    public static bool ReserveHealthKit()
    {
        if (kitOffered) return false;
        kitOffered = true;
        return true;
    }

    public static void ReserveGrant(string key)
    {
        if (ItemCatalog.TryGet(key, out var item) && item.InRewardPool)
            remaining[(int)item.Rarity].Remove(key);
        if (key == "LargeHealthKit") kitOffered = true;
    }

    public static List<string> ReforgeAlternatives(string original, string rarity)
    {
        if (!System.Enum.TryParse<ItemRarity>(rarity, true, out var tier) || (uint)tier >= remaining.Length)
            return new List<string>();
        return remaining[(int)tier].FindAll(key => key != original);
    }

    public static List<string> DrawReforge(string original, string rarity, int count)
    {
        var choices = ReforgeAlternatives(original, rarity);
        var result = new List<string>();
        while (choices.Count > 0 && result.Count < count)
        {
            string key = Draw(choices);
            ReserveGrant(key);
            result.Add(key);
        }
        return result;
    }

    static string Draw(List<string> items)
    {
        int index = Random.Range(0, items.Count);
        string key = items[index];
        items.RemoveAt(index);
        return key;
    }

    static string DrawTier(int wanted, out string rarity)
    {
        int tier = wanted;
        while (tier >= 0 && remaining[tier].Count == 0) tier--;
        if (tier < 0)
        {
            tier = wanted + 1;
            while (tier < remaining.Length && remaining[tier].Count == 0) tier++;
        }
        if (tier == remaining.Length) { rarity = null; return null; }
        var key = Draw(remaining[tier]);
        rarity = RarityOf(key);
        return key;
    }

    public static string GetCargoReward(out string rarity, ItemRarity quality = ItemRarity.Common)
    {
        int tier = Mathf.Clamp((int)quality, 0, 3);
        rarity = ((ItemRarity)tier).ToString().ToLowerInvariant();
        // The box's outline is a promise: luck and pact rarity boosts cannot reroll it.
        return remaining[tier].Count > 0 ? Draw(remaining[tier]) : null;
    }

    public static string GetItem(out string rarity)
    {
        float roll = Random.value + GameRules.luck;
        int tier = roll < .5f ? 0 : roll < .88f ? 1 : roll < .98f ? 2 : 3;
        return DrawTier(Mathf.Min(3, tier + (PactPool.RarityUpgrade ? 1 : 0)), out rarity);
    }
}
