using System;
using System.Collections.Generic;
using UnityEngine;

// Filler: repeatable small stat items offered only once every other item is gone.
public enum ItemRarity { Common, Rare, Epic, Legendary, Pact, Special, Filler }
public enum ItemId
{
    Popcorn, PocketFan, RubberBand, HotSauce, BulletDroneItem, FocusTime, BarrelRoll, BadGlasses, ScatterCannonItem, BallBearing, ReserveCell, SalvageConverter, FocusingLens, ReboundCoil, RabbitsFoot, BlankBullet, EarlyRise, TwerpItem, Magnet, AK45Item, SparePlating, Icicle, WindUpCannonItem, DecoyProjector, StaticSweater, MagnifyingGlass, ChewingGum, LaserDroneItem, HealthPacks, EnergyCore, BouncingBall, Poison, GasolineLeak, QualityGlasses, ShrinkerJuice, Overdrive, GravityWell, MirrorBurst, BeamEmitterItem, FlameProjectorItem, BackdraftItem, DrillItem, ThrowableShield, FillableFlask, DepthChargeItem, SwarmRackItem, DiscThrowerItem, BurstLauncherItem, NeedleCannonItem, Lighter, ReturnModule, DroneLink, RecoveryCoil, Capacitor, ProximityFuse, SplitterCore, Caffeine, ProteinSupplement, AMoonItem, BabyYouItem, ElectricityItem, DoubleHull, SquadWhistle, BulletTime, DoubleTrouble, EmpPulse, RailDriverItem, ChainReactor, SpareBattery, AnotherArm, AngryMoonItem, EnergyShield, SpellCard, Kusabimaru, Telekinesis, ThreeMoonsItem, PolarityEngine, WarpCore, BloodPrice, CursedCoin, DevilImp, ExplodingShield, GlassHull, Hunger, LargeHealthKit, MirrorOath, SteelWashers, ClockworkSpring, RollerBearings, Thermos, HeatSink, LuckyPenny, PaperclipBox, FireflyJar, SaltShaker, PocketMortar, WindUpTeeth, RibbonSpool, SewingKit, RocketBouquet, Redline, NailBomb, CanOpener, FirecrackerBelt, CarbonPaper, Thunderhead, DemolitionPlunger, Whetstone, OilCan, TailFin, Horseshoe, Corkscrew, SlingshotBand, InkPot, Grudge, BallOfYarn, PaperPlane, GlassMarbles, Hourglass, LoadedDice,
    Bookmark, Tripod, RelaySwitch, SignalFlare, LiveWire,
    CarouselItem, CounterweightItem, DeadLetterItem, BankShotItem, VendingGunItem,
    Hector, TapeMeasure, BrassKnuckles, Matchbox, Compost, PiggyBank, LoanShark
}
public sealed class ItemDefinition
{
    public readonly ItemId Id;
    public readonly Type ComponentType;
    public readonly ItemRarity Rarity;
    public string Key => Id.ToString();
    public string RarityKey => Rarity >= ItemRarity.Special ? "common" : Rarity.ToString().ToLowerInvariant();
    public bool InRewardPool => Rarity <= ItemRarity.Legendary;
    public bool IsFiller => Rarity == ItemRarity.Filler;
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
        new ItemDefinition(ItemId.SteelWashers, typeof(SteelWashers), ItemRarity.Common),
        new ItemDefinition(ItemId.ClockworkSpring, typeof(ClockworkSpring), ItemRarity.Common),
        new ItemDefinition(ItemId.RollerBearings, typeof(RollerBearings), ItemRarity.Common),
        new ItemDefinition(ItemId.Thermos, typeof(Thermos), ItemRarity.Common),
        new ItemDefinition(ItemId.HeatSink, typeof(HeatSink), ItemRarity.Common),
        new ItemDefinition(ItemId.LuckyPenny, typeof(LuckyPenny), ItemRarity.Common),
        new ItemDefinition(ItemId.PaperclipBox, typeof(PaperclipBox), ItemRarity.Common),
        new ItemDefinition(ItemId.FireflyJar, typeof(FireflyJar), ItemRarity.Rare),
        new ItemDefinition(ItemId.SaltShaker, typeof(SaltShaker), ItemRarity.Common),
        new ItemDefinition(ItemId.PocketMortar, typeof(PocketMortar), ItemRarity.Rare),
        new ItemDefinition(ItemId.WindUpTeeth, typeof(WindUpTeeth), ItemRarity.Common),
        new ItemDefinition(ItemId.RibbonSpool, typeof(RibbonSpool), ItemRarity.Rare),
        new ItemDefinition(ItemId.SewingKit, typeof(SewingKit), ItemRarity.Common),
        new ItemDefinition(ItemId.RocketBouquet, typeof(RocketBouquet), ItemRarity.Rare),
        new ItemDefinition(ItemId.Redline, typeof(Redline), ItemRarity.Rare),
        new ItemDefinition(ItemId.NailBomb, typeof(NailBomb), ItemRarity.Common),
        new ItemDefinition(ItemId.CanOpener, typeof(CanOpener), ItemRarity.Rare),
        new ItemDefinition(ItemId.FirecrackerBelt, typeof(FirecrackerBelt), ItemRarity.Common),
        new ItemDefinition(ItemId.CarbonPaper, typeof(CarbonPaper), ItemRarity.Epic),
        new ItemDefinition(ItemId.Thunderhead, typeof(Thunderhead), ItemRarity.Epic),
        new ItemDefinition(ItemId.DemolitionPlunger, typeof(DemolitionPlunger), ItemRarity.Rare),
        new ItemDefinition(ItemId.Whetstone, typeof(Whetstone), ItemRarity.Filler),
        new ItemDefinition(ItemId.OilCan, typeof(OilCan), ItemRarity.Filler),
        new ItemDefinition(ItemId.TailFin, typeof(TailFin), ItemRarity.Filler),
        new ItemDefinition(ItemId.Horseshoe, typeof(Horseshoe), ItemRarity.Filler),
        new ItemDefinition(ItemId.Corkscrew, typeof(Corkscrew), ItemRarity.Common),
        new ItemDefinition(ItemId.SlingshotBand, typeof(SlingshotBand), ItemRarity.Rare),
        new ItemDefinition(ItemId.InkPot, typeof(InkPot), ItemRarity.Rare),
        new ItemDefinition(ItemId.Grudge, typeof(Grudge), ItemRarity.Rare),
        new ItemDefinition(ItemId.BallOfYarn, typeof(BallOfYarn), ItemRarity.Rare),
        new ItemDefinition(ItemId.PaperPlane, typeof(PaperPlane), ItemRarity.Common),
        new ItemDefinition(ItemId.GlassMarbles, typeof(GlassMarbles), ItemRarity.Common),
        new ItemDefinition(ItemId.Hourglass, typeof(Hourglass), ItemRarity.Rare),
        new ItemDefinition(ItemId.LoadedDice, typeof(LoadedDice), ItemRarity.Epic),
        new ItemDefinition(ItemId.Bookmark, typeof(Bookmark), ItemRarity.Rare),
        new ItemDefinition(ItemId.Tripod, typeof(Tripod), ItemRarity.Rare),
        new ItemDefinition(ItemId.RelaySwitch, typeof(RelaySwitch), ItemRarity.Rare),
        new ItemDefinition(ItemId.SignalFlare, typeof(SignalFlare), ItemRarity.Rare),
        new ItemDefinition(ItemId.LiveWire, typeof(LiveWire), ItemRarity.Pact),
        new ItemDefinition(ItemId.CarouselItem, typeof(CarouselItem), ItemRarity.Epic),
        new ItemDefinition(ItemId.CounterweightItem, typeof(CounterweightItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.DeadLetterItem, typeof(DeadLetterItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.BankShotItem, typeof(BankShotItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.VendingGunItem, typeof(VendingGunItem), ItemRarity.Rare),
        new ItemDefinition(ItemId.Hector, typeof(Hector), ItemRarity.Common),
        new ItemDefinition(ItemId.TapeMeasure, typeof(TapeMeasure), ItemRarity.Common),
        new ItemDefinition(ItemId.BrassKnuckles, typeof(BrassKnuckles), ItemRarity.Common),
        new ItemDefinition(ItemId.Matchbox, typeof(Matchbox), ItemRarity.Rare),
        new ItemDefinition(ItemId.Compost, typeof(Compost), ItemRarity.Rare),
        new ItemDefinition(ItemId.PiggyBank, typeof(PiggyBank), ItemRarity.Common),
        new ItemDefinition(ItemId.LoanShark, typeof(LoanShark), ItemRarity.Pact),
    };
    static readonly Dictionary<string, ItemDefinition> byKey = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
    public static IReadOnlyList<ItemDefinition> All => definitions;
    // The gallery and its baked shelves show every item except the repeatable fillers.
    static ItemDefinition[] exhibits;
    public static IReadOnlyList<ItemDefinition> Exhibits => exhibits ??= Array.FindAll(definitions, item => !item.IsFiller);
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
