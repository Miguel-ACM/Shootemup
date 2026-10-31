using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemRarity { Common, Rare, Epic, Legendary, Pact, Special }
public enum ItemId
{
    Popcorn, PocketFan, RubberBand, HotSauce, BulletDroneItem, FocusTime, BarrelRoll, BadGlasses, ScatterCannonItem, BallBearing, ReserveCell, SalvageConverter, FocusingLens, ReboundCoil, RabbitsFoot, BlankBullet, EarlyRise, TwerpItem, Magnet, AK45Item, SparePlating, Icicle, WindUpCannonItem, DecoyProjector, StaticSweater, MagnifyingGlass, ChewingGum, LaserDroneItem, HealthPacks, EnergyCore, BouncingBall, Poison, GasolineLeak, QualityGlasses, ShrinkerJuice, Overdrive, GravityWell, MirrorBurst, BeamEmitterItem, FlameProjectorItem, BackdraftItem, DrillItem, ThrowableShield, FillableFlask, DepthChargeItem, SwarmRackItem, DiscThrowerItem, BurstLauncherItem, NeedleCannonItem, Lighter, ReturnModule, DroneLink, RecoveryCoil, Capacitor, ProximityFuse, SplitterCore, Caffeine, ProteinSupplement, AMoonItem, BabyYouItem, ElectricityItem, DoubleHull, SquadWhistle, BulletTime, DoubleTrouble, EmpPulse, RailDriverItem, ChainReactor, SpareBattery, AnotherArm, AngryMoonItem, EnergyShield, SpellCard, Kusabimaru, Telekinesis, ThreeMoonsItem, PolarityEngine, WarpCore, BloodPrice, CursedCoin, DevilImp, ExplodingShield, GlassHull, Hunger, LargeHealthKit, MirrorOath
}
public sealed class ItemDefinition
{
    public readonly ItemId Id;
    public readonly Type ComponentType;
    public readonly ItemRarity Rarity;
    public string Key => Id.ToString();
    public string RarityKey => Rarity == ItemRarity.Special ? "common" : Rarity.ToString().ToLowerInvariant();
    public bool InRewardPool => Rarity <= ItemRarity.Legendary;
    public ItemDefinition(ItemId id, Type type, ItemRarity rarity) { Id = id; ComponentType = type; Rarity = rarity; }
    public AItem Attach(GameObject target) => (AItem)target.AddComponent(ComponentType);
}
public static class ItemCatalog
{
    static readonly ItemDefinition[] definitions = {
        new ItemDefinition(ItemId.Popcorn, typeof(Popcorn), ItemRarity.Common),
        new ItemDefinition(ItemId.PocketFan, typeof(PocketFan), ItemRarity.Common),
        new ItemDefinition(ItemId.RubberBand, typeof(RubberBand), ItemRarity.Common),
        new ItemDefinition(ItemId.HotSauce, typeof(HotSauce), ItemRarity.Common),
        new ItemDefinition(ItemId.BulletDroneItem, typeof(BulletDroneItem), ItemRarity.Common),
        new ItemDefinition(ItemId.FocusTime, typeof(FocusTime), ItemRarity.Common),
        new ItemDefinition(ItemId.BarrelRoll, typeof(BarrelRoll), ItemRarity.Common),
        new ItemDefinition(ItemId.BadGlasses, typeof(BadGlasses), ItemRarity.Common),
        new ItemDefinition(ItemId.ScatterCannonItem, typeof(ScatterCannonItem), ItemRarity.Common),
        new ItemDefinition(ItemId.BallBearing, typeof(BallBearing), ItemRarity.Common),
        new ItemDefinition(ItemId.ReserveCell, typeof(ReserveCell), ItemRarity.Common),
        new ItemDefinition(ItemId.SalvageConverter, typeof(SalvageConverter), ItemRarity.Common),
        new ItemDefinition(ItemId.FocusingLens, typeof(FocusingLens), ItemRarity.Common),
        new ItemDefinition(ItemId.ReboundCoil, typeof(ReboundCoil), ItemRarity.Common),
        new ItemDefinition(ItemId.RabbitsFoot, typeof(RabbitsFoot), ItemRarity.Common),
        new ItemDefinition(ItemId.BlankBullet, typeof(BlankBullet), ItemRarity.Common),
        new ItemDefinition(ItemId.EarlyRise, typeof(EarlyRise), ItemRarity.Common),
        new ItemDefinition(ItemId.TwerpItem, typeof(TwerpItem), ItemRarity.Common),
        new ItemDefinition(ItemId.Magnet, typeof(Magnet), ItemRarity.Common),
        new ItemDefinition(ItemId.AK45Item, typeof(AK45Item), ItemRarity.Common),
        new ItemDefinition(ItemId.SparePlating, typeof(SparePlating), ItemRarity.Common),
        new ItemDefinition(ItemId.Icicle, typeof(Icicle), ItemRarity.Rare),
        new ItemDefinition(ItemId.WindUpCannonItem, typeof(WindUpCannonItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.DecoyProjector, typeof(DecoyProjector), ItemRarity.Rare),
        new ItemDefinition(ItemId.StaticSweater, typeof(StaticSweater), ItemRarity.Rare),
        new ItemDefinition(ItemId.MagnifyingGlass, typeof(MagnifyingGlass), ItemRarity.Rare),
        new ItemDefinition(ItemId.ChewingGum, typeof(ChewingGum), ItemRarity.Rare),
        new ItemDefinition(ItemId.LaserDroneItem, typeof(LaserDroneItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.HealthPacks, typeof(HealthPacks), ItemRarity.Rare),
        new ItemDefinition(ItemId.EnergyCore, typeof(EnergyCore), ItemRarity.Rare),
        new ItemDefinition(ItemId.BouncingBall, typeof(BouncingBall), ItemRarity.Rare),
        new ItemDefinition(ItemId.Poison, typeof(Poison), ItemRarity.Rare),
        new ItemDefinition(ItemId.GasolineLeak, typeof(GasolineLeak), ItemRarity.Rare),
        new ItemDefinition(ItemId.QualityGlasses, typeof(QualityGlasses), ItemRarity.Rare),
        new ItemDefinition(ItemId.ShrinkerJuice, typeof(ShrinkerJuice), ItemRarity.Rare),
        new ItemDefinition(ItemId.Overdrive, typeof(Overdrive), ItemRarity.Rare),
        new ItemDefinition(ItemId.GravityWell, typeof(GravityWell), ItemRarity.Rare),
        new ItemDefinition(ItemId.MirrorBurst, typeof(MirrorBurst), ItemRarity.Rare),
        new ItemDefinition(ItemId.BeamEmitterItem, typeof(BeamEmitterItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.FlameProjectorItem, typeof(FlameProjectorItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.BackdraftItem, typeof(BackdraftItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.DrillItem, typeof(DrillItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.ThrowableShield, typeof(ThrowableShield), ItemRarity.Rare),
        new ItemDefinition(ItemId.FillableFlask, typeof(FillableFlask), ItemRarity.Rare),
        new ItemDefinition(ItemId.DepthChargeItem, typeof(DepthChargeItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.SwarmRackItem, typeof(SwarmRackItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.DiscThrowerItem, typeof(DiscThrowerItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.BurstLauncherItem, typeof(BurstLauncherItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.NeedleCannonItem, typeof(NeedleCannonItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.Lighter, typeof(Lighter), ItemRarity.Rare),
        new ItemDefinition(ItemId.ReturnModule, typeof(ReturnModule), ItemRarity.Rare),
        new ItemDefinition(ItemId.DroneLink, typeof(DroneLink), ItemRarity.Rare),
        new ItemDefinition(ItemId.RecoveryCoil, typeof(RecoveryCoil), ItemRarity.Rare),
        new ItemDefinition(ItemId.Capacitor, typeof(Capacitor), ItemRarity.Rare),
        new ItemDefinition(ItemId.ProximityFuse, typeof(ProximityFuse), ItemRarity.Rare),
        new ItemDefinition(ItemId.SplitterCore, typeof(SplitterCore), ItemRarity.Rare),
        new ItemDefinition(ItemId.Caffeine, typeof(Caffeine), ItemRarity.Rare),
        new ItemDefinition(ItemId.ProteinSupplement, typeof(ProteinSupplement), ItemRarity.Rare),
        new ItemDefinition(ItemId.AMoonItem, typeof(AMoonItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.BabyYouItem, typeof(BabyYouItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.ElectricityItem, typeof(ElectricityItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.DoubleHull, typeof(DoubleHull), ItemRarity.Rare),
        new ItemDefinition(ItemId.SquadWhistle, typeof(SquadWhistle), ItemRarity.Rare),
        new ItemDefinition(ItemId.BulletTime, typeof(BulletTime), ItemRarity.Epic),
        new ItemDefinition(ItemId.DoubleTrouble, typeof(DoubleTrouble), ItemRarity.Epic),
        new ItemDefinition(ItemId.EmpPulse, typeof(EmpPulse), ItemRarity.Epic),
        new ItemDefinition(ItemId.RailDriverItem, typeof(RailDriverItem), ItemRarity.Epic),
        new ItemDefinition(ItemId.ChainReactor, typeof(ChainReactor), ItemRarity.Epic),
        new ItemDefinition(ItemId.SpareBattery, typeof(SpareBattery), ItemRarity.Epic),
        new ItemDefinition(ItemId.AnotherArm, typeof(AnotherArm), ItemRarity.Epic),
        new ItemDefinition(ItemId.AngryMoonItem, typeof(AngryMoonItem), ItemRarity.Epic),
        new ItemDefinition(ItemId.EnergyShield, typeof(EnergyShield), ItemRarity.Epic),
        new ItemDefinition(ItemId.SpellCard, typeof(SpellCard), ItemRarity.Epic),
        new ItemDefinition(ItemId.Kusabimaru, typeof(Kusabimaru), ItemRarity.Epic),
        new ItemDefinition(ItemId.Telekinesis, typeof(Telekinesis), ItemRarity.Legendary),
        new ItemDefinition(ItemId.ThreeMoonsItem, typeof(ThreeMoonsItem), ItemRarity.Legendary),
        new ItemDefinition(ItemId.PolarityEngine, typeof(PolarityEngine), ItemRarity.Legendary),
        new ItemDefinition(ItemId.WarpCore, typeof(WarpCore), ItemRarity.Legendary),
        new ItemDefinition(ItemId.BloodPrice, typeof(BloodPrice), ItemRarity.Pact),
        new ItemDefinition(ItemId.CursedCoin, typeof(CursedCoin), ItemRarity.Pact),
        new ItemDefinition(ItemId.DevilImp, typeof(DevilImp), ItemRarity.Pact),
        new ItemDefinition(ItemId.ExplodingShield, typeof(ExplodingShield), ItemRarity.Special),
        new ItemDefinition(ItemId.GlassHull, typeof(GlassHull), ItemRarity.Pact),
        new ItemDefinition(ItemId.Hunger, typeof(Hunger), ItemRarity.Pact),
        new ItemDefinition(ItemId.LargeHealthKit, typeof(LargeHealthKit), ItemRarity.Special),
        new ItemDefinition(ItemId.MirrorOath, typeof(MirrorOath), ItemRarity.Pact),
    };
    static readonly Dictionary<string, ItemDefinition> byKey = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
    public static IReadOnlyList<ItemDefinition> All => definitions;
    static ItemCatalog()
    {
        foreach (var item in definitions)
        {
            if (!typeof(AItem).IsAssignableFrom(item.ComponentType) || item.ComponentType.IsAbstract || item.Key != item.ComponentType.Name)
                throw new InvalidOperationException("Invalid item definition: " + item.Key);
            byKey.Add(item.Key, item);
        }
    }
    public static bool TryGet(string key, out ItemDefinition definition) => byKey.TryGetValue(key, out definition);
    public static ItemDefinition Get(string key) => byKey.TryGetValue(key, out var item) ? item : throw new ArgumentException("Unknown item: " + key);
    public static ItemDefinition Get(ItemId id) => Get(id.ToString());
    public static string[] Keys(ItemRarity rarity)
    {
        var keys = new List<string>();
        foreach (var item in definitions)
            if (item.Rarity == rarity) keys.Add(item.Key);
        return keys.ToArray();
    }
}
