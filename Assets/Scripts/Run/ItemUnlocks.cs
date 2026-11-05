using System;
using System.Collections.Generic;

public enum ItemUnlockCondition { Clear, NoHitBoss, NoHitArea }

// Stable item keys own progression across hulls and difficulties. Boss types distinguish
// alternate encounters; the City duo is one encounter, completed by its last survivor.
public sealed class ItemUnlock
{
    public readonly LevelId Area;
    public readonly ItemId Item;
    public readonly ItemUnlockCondition Condition;
    public readonly Type Boss;
    public readonly string Title, BossTitle;
    public string Key => Item.ToString();
    public string AreaName => Area == LevelId.Fields ? "Plains" : Area.ToString();
    string BossName => DistrictWaves.Bosses(Area).Count == 1 ? AreaName + " boss" :
        BossTitle.StartsWith(AreaName, StringComparison.Ordinal) ? BossTitle : BossTitle + " in " + AreaName;
    public string Requirement => Condition == ItemUnlockCondition.Clear ? "Clear " + AreaName + " on any difficulty." :
        Condition == ItemUnlockCondition.NoHitBoss ? "No-hit " + BossName + " on Normal+ difficulty." :
        "No-hit the full " + AreaName + " level on Normal+ difficulty.";

    public ItemUnlock(LevelId area, ItemId item, string title, ItemUnlockCondition condition = ItemUnlockCondition.Clear,
        Type boss = null, string bossTitle = null)
    {
        Area = area; Item = item; Title = title; Condition = condition; Boss = boss; BossTitle = bossTitle;
    }
}

[Serializable]
public sealed class ItemUnlockNotice
{
    public string key, title, source;
    public LevelId area;
    public ItemUnlockCondition condition;
    // Resolve the current wording for old saved tags too, without changing their rewards.
    public string Requirement => ItemUnlocks.For(key)?.Requirement ?? FlightTrials.Blueprint(key)?.Requirement ?? source;
}

public static class ItemUnlocks
{
    static ItemUnlock Clear(LevelId area, ItemId item, string title) => new ItemUnlock(area, item, title);
    static ItemUnlock Boss<T>(LevelId area, ItemId item, string title, string boss) =>
        new ItemUnlock(area, item, title, ItemUnlockCondition.NoHitBoss, typeof(T), boss);
    static ItemUnlock Area(LevelId area, ItemId item, string title) => new ItemUnlock(area, item, title, ItemUnlockCondition.NoHitArea);

    public static readonly IReadOnlyList<ItemUnlock> All = Array.AsReadOnly(new[] {
        Clear(LevelId.Fields, ItemId.PocketMortar, "Pocket Mortar"),
        Boss<FieldsBomberBoss>(LevelId.Fields, ItemId.BurstLauncherItem, "Burst Launcher", "Plains bomber"),
        Boss<PlainsRedKiteBoss>(LevelId.Fields, ItemId.SwarmRackItem, "Swarm Rack", "Red Kite"),
        Boss<PlainsThreshingDrumBoss>(LevelId.Fields, ItemId.ProximityFuse, "Proximity Fuse", "Threshing Drum"),
        Area(LevelId.Fields, ItemId.SquadWhistle, "Squad Whistle"),
        Clear(LevelId.Snow, ItemId.Icicle, "Icicle"),
        Boss<SnowSiegeBoss>(LevelId.Snow, ItemId.SplitterCore, "Splitting Rounds", "Snow siege boss"),
        Boss<SnowIcebreakerBoss>(LevelId.Snow, ItemId.ThrowableShield, "Throwable Shield", "Icebreaker"),
        Area(LevelId.Snow, ItemId.BulletTime, "Time Stop"),
        Clear(LevelId.City, ItemId.Caffeine, "Caffeine"),
        Boss<CityDuoBoss>(LevelId.City, ItemId.DoubleTrouble, "Linked Triggers", "City duo"),
        Boss<CitySwitchyardBoss>(LevelId.City, ItemId.RailDriverItem, "Rail Driver", "Switchyard"),
        Area(LevelId.City, ItemId.CarbonPaper, "Carbon Paper"),
        Clear(LevelId.Thunder, ItemId.ElectricityItem, "Electricity"),
        Boss<ThunderLanceBoss>(LevelId.Thunder, ItemId.Thunderhead, "Thunderhead", "Thunder boss"),
        Area(LevelId.Thunder, ItemId.PolarityEngine, "Polarity Engine"),
        Clear(LevelId.Tunnel, ItemId.DiscThrowerItem, "Disc Thrower"),
        Boss<TunnelSawBoss>(LevelId.Tunnel, ItemId.ReboundCoil, "Ricochet Spring", "Tunnel saw"),
        Area(LevelId.Tunnel, ItemId.ReturnModule, "Returning Volley"),
        Clear(LevelId.Desert, ItemId.DrillItem, "Spiral Drill"),
        Boss<DesertWormBoss>(LevelId.Desert, ItemId.CanOpener, "Can Opener", "Desert worm"),
        Area(LevelId.Desert, ItemId.DemolitionPlunger, "Demolition Plunger"),
        Clear(LevelId.Candyland, ItemId.ChewingGum, "Chewing gum"),
        Boss<CandyRegentBoss>(LevelId.Candyland, ItemId.RibbonSpool, "Ribbon Spool", "Candy Regent"),
        Area(LevelId.Candyland, ItemId.ShrinkerJuice, "Shrinker juice"),
        Clear(LevelId.Space, ItemId.AMoonItem, "A Moon"),
        Boss<SpaceCitadelBoss>(LevelId.Space, ItemId.AngryMoonItem, "Angry Moon", "Black hole"),
        Area(LevelId.Space, ItemId.ThreeMoonsItem, "Three Moons"),
        Clear(LevelId.Underwater, ItemId.DepthChargeItem, "Depth Charge"),
        Boss<AbyssSubmarineBoss>(LevelId.Underwater, ItemId.RocketBouquet, "Rocket Bouquet", "Submarine"),
        Area(LevelId.Underwater, ItemId.DoubleHull, "Double Hull"),
        Clear(LevelId.Hell, ItemId.FlameProjectorItem, "Flame Projector"),
        Boss<HellCrabBoss>(LevelId.Hell, ItemId.BackdraftItem, "Rear Flame", "Hell crab"),
        Area(LevelId.Hell, ItemId.ChainReactor, "Burn Detonator"),
        Clear(LevelId.Heaven, ItemId.BeamEmitterItem, "Beam Emitter"),
        Boss<SeraphJudicatorBoss>(LevelId.Heaven, ItemId.MirrorBurst, "Mirror Burst", "Heaven angel"),
        Area(LevelId.Heaven, ItemId.SpellCard, "Spell Card"),
        Clear(LevelId.Void, ItemId.GravityWell, "Stasis Well"),
        Boss<VoidSovereignBoss>(LevelId.Void, ItemId.Telekinesis, "Telekinesis", "Void Sovereign"),
        Area(LevelId.Void, ItemId.WarpCore, "Warp-Core")
    });

    static readonly Dictionary<string, ItemUnlock> byKey = Index();
    static Dictionary<string, ItemUnlock> Index()
    {
        var result = new Dictionary<string, ItemUnlock>(StringComparer.Ordinal);
        foreach (var reward in All) result.Add(reward.Key, reward);
        return result;
    }
    public static ItemUnlock For(string key) => key != null && byKey.TryGetValue(key, out var reward) ? reward : null;
    public static bool AllowsNoHit(RunDifficulty difficulty) => difficulty == RunDifficulty.Normal || difficulty == RunDifficulty.Hard;
}

public static partial class MetaProgress
{
    public static IReadOnlyList<ItemUnlockNotice> PendingUnlocks => Data.pendingUnlocks;

    // Also checked at purchase time: a pinned or old saved offer cannot bypass an achievement.
    public static bool CanStock(string key) => key != null && ItemCatalog.TryGet(key, out var item) && item.InRewardPool &&
        (IsUnlocked(key) || (ItemUnlocks.For(key) == null && FlightTrials.Blueprint(key) == null));

    static void UnlockItem(string key, string title, string source, LevelId area, ItemUnlockCondition condition = ItemUnlockCondition.Clear)
    {
        if (IsUnlocked(key)) return;
        Data.unlocked.Add(key);
        var notice = new ItemUnlockNotice { key = key, title = title, source = source, area = area, condition = condition };
        flight?.unlocks.Add(notice);
        Data.pendingUnlocks.Add(notice);
    }

    static void AwardDistrictItems(bool cleanBoss)
    {
        bool normal = ItemUnlocks.AllowsNoHit(flight.difficulty);
        foreach (var reward in ItemUnlocks.All)
        {
            if (reward.Area != LevelRun.Current) continue;
            bool earned = reward.Condition == ItemUnlockCondition.Clear || normal &&
                (reward.Condition == ItemUnlockCondition.NoHitBoss ? cleanBoss && reward.Boss == engagedBoss : flight.hits == districtHits);
            if (earned) UnlockItem(reward.Key, reward.Title, reward.Requirement, reward.Area, reward.Condition);
        }
    }

    public static void DismissUnlock(string key)
    {
        if (Data.pendingUnlocks.Count == 0 || Data.pendingUnlocks[0].key != key) return;
        Data.pendingUnlocks.RemoveAt(0);
        Save();
    }
}
