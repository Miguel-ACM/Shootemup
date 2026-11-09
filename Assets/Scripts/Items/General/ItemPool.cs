using System.Collections.Generic;
using UnityEngine;

// Three layers, drawn in order: items never offered this run; once every one of those
// has been seen, the items that were offered but passed over; and once those are gone
// too, small repeatable filler items, so a long run never runs dry.
public static class ItemPool
{
    static List<string>[] remaining, passed;
    // Drawn for the offer being built now. They join the passed-over pool when the next
    // offer is built, unless the pilot collects them first.
    static readonly List<string> pending = new List<string>();
    static readonly List<string> fillersThisOffer = new List<string>();
    static int offerFrame = -1;
    static bool kitOffered;
    static int Count(List<string>[] tiers) => tiers[0].Count + tiers[1].Count + tiers[2].Count + tiers[3].Count;
    static bool FreshLeft => Count(remaining) > 0;
    // Fillers repeat, so an ordinary offer can always be filled.
    public static int RemainingCount => Count(remaining) + Count(passed) + ItemCatalog.Keys(ItemRarity.Filler).Length;

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
        passed = new[] { new List<string>(), new List<string>(), new List<string>(), new List<string>() };
        pending.Clear();
        fillersThisOffer.Clear();
        offerFrame = -1;
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
        {
            remaining[(int)item.Rarity].Remove(key);
            passed[(int)item.Rarity].Remove(key);
            pending.Remove(key);
        }
        if (key == "LargeHealthKit") kitOffered = true;
    }

    // A collected item never returns through the passed-over pool.
    public static void Collected(string key)
    {
        pending.Remove(key);
        if (ItemCatalog.TryGet(key, out var item) && item.InRewardPool)
            passed[(int)item.Rarity].Remove(key);
    }

    // Each offer is built within one frame. Starting a new one retires the previous
    // offer's uncollected items to the passed-over pool.
    static void BeginOffer()
    {
        if (offerFrame == Time.frameCount)
            return;
        offerFrame = Time.frameCount;
        foreach (var key in pending)
            if (ItemCatalog.TryGet(key, out var item) && item.InRewardPool && !passed[(int)item.Rarity].Contains(key))
                passed[(int)item.Rarity].Add(key);
        pending.Clear();
        fillersThisOffer.Clear();
    }

    public static List<string> ReforgeAlternatives(string original, string rarity)
    {
        if (!System.Enum.TryParse<ItemRarity>(rarity, true, out var tier) || (uint)tier >= remaining.Length)
            return new List<string>();
        // Fresh alternatives first; once a tier has none, the ones passed over earlier.
        var fresh = remaining[(int)tier].FindAll(key => key != original);
        return fresh.Count >= 2 ? fresh : passed[(int)tier].FindAll(key => key != original);
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

    static string DrawTier(List<string>[] pool, int wanted, out string rarity)
    {
        int tier = wanted;
        while (tier >= 0 && pool[tier].Count == 0) tier--;
        if (tier < 0)
        {
            tier = wanted + 1;
            while (tier < pool.Length && pool[tier].Count == 0) tier++;
        }
        if (tier == pool.Length) { rarity = null; return null; }
        var key = Draw(pool[tier]);
        rarity = RarityOf(key);
        return key;
    }

    // Distinct fillers within one offer while there are enough of them.
    static string DrawFiller(out string rarity)
    {
        rarity = "common";
        var keys = new List<string>(ItemCatalog.Keys(ItemRarity.Filler));
        if (keys.Count == 0)
            return null;
        var unused = keys.FindAll(key => !fillersThisOffer.Contains(key));
        string chosen = (unused.Count > 0 ? unused : keys)[Random.Range(0, unused.Count > 0 ? unused.Count : keys.Count)];
        fillersThisOffer.Add(chosen);
        return chosen;
    }

    public static string GetCargoReward(out string rarity, ItemRarity quality = ItemRarity.Common)
    {
        int tier = Mathf.Clamp((int)quality, 0, 3);
        rarity = ((ItemRarity)tier).ToString().ToLowerInvariant();
        // The box's outline is a promise: luck and pact rarity boosts cannot reroll it.
        // A crate's item is reserved, so it never joins the passed-over pool.
        if (remaining[tier].Count > 0)
            return Draw(remaining[tier]);
        return passed[tier].Count > 0 ? Draw(passed[tier]) : null;
    }

    public static string GetItem(out string rarity)
    {
        float roll = Random.value + GameRules.luck;
        int tier = roll < .5f ? 0 : roll < .88f ? 1 : roll < .98f ? 2 : 3;
        tier = Mathf.Min(3, tier + (PactPool.RarityUpgrade ? 1 : 0));
        BeginOffer();
        string key = DrawTier(FreshLeft ? remaining : passed, tier, out rarity);
        if (key == null)
            return DrawFiller(out rarity);
        pending.Add(key);
        return key;
    }
}
