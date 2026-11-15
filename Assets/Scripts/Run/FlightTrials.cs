using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static LevelId;

public enum SimulatorApproach { Standard, UnlimitedEnergy, VerticalSteering, WaterHandling, Autopilot, Downbeat, HotFloor, Silent, MovingWindow, Slow }

public sealed class FlightTrials
{
    public sealed class Leg
    {
        public readonly LevelId district;
        public readonly Type boss;
        public readonly string[] sequence;
        public readonly string[] kit;
        public readonly bool fullLevel;

        public Leg(LevelId district, Type boss, string[] kit, bool fullLevel, params string[] waves)
        {
            this.district = district;
            this.boss = boss;
            this.kit = kit;
            this.fullLevel = fullLevel;
            sequence = new string[waves.Length + 1];
            waves.CopyTo(sequence, 0);
            sequence[waves.Length] = boss == null ? "end" : "boss";
        }
    }

    public readonly string title, objective, reward;
    public readonly ShipFrame frame;
    public readonly RunDifficulty difficulty;
    public readonly LevelId district;
    public readonly string[] kit;
    public readonly Leg[] legs;
    public readonly SimulatorApproach approach;
    public readonly int startingCoins, bossCount;
    public bool Advanced => legs != null;
    public string Requirement => "Complete " + title + " in the simulator.";

    FlightTrials(string name, string goal, ShipFrame ship, LevelId level, RunDifficulty mode, string blueprint, params string[] items)
    {
        title = name;
        objective = goal;
        frame = ship;
        district = level;
        difficulty = mode;
        reward = blueprint;
        kit = items;
    }

    FlightTrials(string name, Leg[] route, string[] items, string blueprint = null,
        SimulatorApproach approach = SimulatorApproach.Standard, RunDifficulty difficulty = RunDifficulty.Normal, int coins = 0)
    {
        title = name;
        frame = ShipFrame.GR07;
        legs = route;
        district = route[0].district;
        kit = route[0].kit ?? items;
        reward = blueprint;
        this.approach = approach;
        this.difficulty = difficulty;
        startingCoins = coins;
        var stops = new List<string>();
        foreach (var leg in route)
        {
            if (leg.boss != null) bossCount++;
            stops.Add(leg.fullLevel ? LevelLabel(leg) : BossLabel(leg.boss));
        }
        objective = string.Join(" > ", stops);
    }

    static string[] Kit(params string[] items) => items;
    // Every simulator leg is either a whole district (its ten ordinary waves and miniboss before
    // its own boss) or a single boss in a boss rush (Miguel, 14 November 2026): no wave snippets
    // in front of another district's boss. Tapes across districts follow real routes.
    static readonly string[] FullLevel = Array.FindAll(DistrictWaves.CampaignSequence, key =>
        RunSlot.Parse(key).Kind == RunSlotKind.Combat || RunSlot.Parse(key).Kind == RunSlotKind.Miniboss);
    static Leg Level<T>(LevelId level, string[] items = null) where T : DistrictBoss => new Leg(level, typeof(T), items, true, FullLevel);
    static Leg Boss<T>(LevelId level, string[] items = null) where T : DistrictBoss => new Leg(level, typeof(T), items, false);

    // Tapes run from the first district to the last; within a tier, single levels come before rushes.
    public static readonly FlightTrials[] All = {
        // Plains
        new FlightTrials("Crossfire", "Defeat the Plains bomber with secondary fire.", ShipFrame.GR07, Fields, RunDifficulty.Normal, "WindUpTeeth", "AK45Item", "WindUpTeeth"),
        new FlightTrials("On the Beat", new[] { Level<PlainsThreshingDrumBoss>(Fields) },
            Kit("BurstLauncherItem"), approach: SimulatorApproach.Downbeat),
        new FlightTrials("Change of Plans", new[] { Level<PlainsRedKiteBoss>(Fields) },
            Kit("CarouselItem", "BarrelRoll"), "CarouselItem"),
        new FlightTrials("Triple Harvest", new[] { Boss<FieldsBomberBoss>(Fields), Boss<PlainsRedKiteBoss>(Fields), Boss<PlainsThreshingDrumBoss>(Fields) },
            Kit("NeedleCannonItem", "ScatterCannonItem", "RelaySwitch")),
        new FlightTrials("Three Gun Salute", new[] {
            Boss<FieldsBomberBoss>(Fields, Kit("RailDriverItem", "Capacitor")),
            Boss<PlainsRedKiteBoss>(Fields, Kit("ScatterCannonItem", "BarrelRoll")),
            Boss<PlainsThreshingDrumBoss>(Fields, Kit("DiscThrowerItem", "PocketFan"))
        }, Array.Empty<string>()),

        // Snow and City
        new FlightTrials("Cold Delivery", "Bring the crate through two Snow waves.", ShipFrame.Warden, Snow, RunDifficulty.Normal, "Thermos", "Magnet", "SparePlating"),
        new FlightTrials("Blank Check", new[] { Level<SnowSiegeBoss>(Snow) },
            Kit("NeedleCannonItem", "MirrorBurst", "RocketBouquet"), approach: SimulatorApproach.UnlimitedEnergy),
        new FlightTrials("Cold Snap", new[] { Boss<SnowIcebreakerBoss>(Snow), Boss<SnowSiegeBoss>(Snow) },
            Kit("ScatterCannonItem", "BulletTime", "Overdrive")),
        new FlightTrials("Drift School", new[] { Level<CityDuoBoss>(City) },
            Kit("DiscThrowerItem", "FocusTime"), approach: SimulatorApproach.WaterHandling),
        new FlightTrials("Black Box", new[] { Level<CitySwitchyardBoss>(City) },
            Kit("RailDriverItem", "ScatterCannonItem", "BlankBullet"), approach: SimulatorApproach.Autopilot),
        new FlightTrials("Split Verdict", new[] { Boss<CityDuoBoss>(City), Boss<CitySwitchyardBoss>(City) },
            Kit("RailDriverItem", "ScatterCannonItem", "RelaySwitch")),

        // Thunder, Tunnel, Desert and Candyland
        new FlightTrials("Line Discipline", "Clear the Thunder boss's beam lanes.", ShipFrame.Lancer, Thunder, RunDifficulty.Hard, "HeatSink", "HeatSink", "PocketFan"),
        new FlightTrials("Dead Stick", new[] { Level<ThunderLanceBoss>(Thunder) },
            Kit("NeedleCannonItem", "BarrelRoll"), approach: SimulatorApproach.VerticalSteering),
        new FlightTrials("Jade Spark", new[] { Level<ThunderLanceBoss>(Thunder) },
            Kit("ElectricityItem", "Poison", "GasolineLeak")),
        new FlightTrials("Bank Job", new[] { Level<TunnelSawBoss>(Tunnel) },
            Kit("BankShotItem", "PocketFan"), "BankShotItem"),
        new FlightTrials("Rear Guard", new[] { Level<TunnelSawBoss>(Tunnel) },
            Kit("SwarmRackItem", "LaserDroneItem", "ThrowableShield")),
        new FlightTrials("Side Hustle", new[] { Level<DesertWormBoss>(Desert) },
            Kit("AK45Item", "BackdraftItem", "ProteinSupplement")),
        new FlightTrials("Slow Lane", new[] { Level<DesertWormBoss>(Desert) },
            Kit("SwarmRackItem", "Bookmark"), approach: SimulatorApproach.Slow),
        new FlightTrials("Exact Change", new[] { Level<CandyRegentBoss>(Candyland) },
            Kit("VendingGunItem", "NeedleCannonItem"), "VendingGunItem", coins: 12),
        new FlightTrials("Switchback", new[] { Level<CandyRegentBoss>(Candyland), Level<AbyssSubmarineBoss>(Underwater) },
            Kit("CounterweightItem", "BarrelRoll"), "CounterweightItem"),

        // Space and Underwater
        new FlightTrials("Moon Tax", new[] { Level<SpaceCitadelBoss>(Space) },
            Kit("NeedleCannonItem", "ThreeMoonsItem", "RecoveryCoil")),
        new FlightTrials("Window Seat", new[] { Level<SpaceCitadelBoss>(Space) },
            Kit("BeamEmitterItem", "Telekinesis"), approach: SimulatorApproach.MovingWindow),
        new FlightTrials("Postmarked", new[] { Level<AbyssSubmarineBoss>(Underwater) },
            Kit("DeadLetterItem", "DecoyProjector"), "DeadLetterItem"),
        new FlightTrials("Power Exchange", new[] { Boss<SnowSiegeBoss>(Snow), Boss<ThunderLanceBoss>(Thunder), Boss<SpaceCitadelBoss>(Space) },
            Kit("BeamEmitterItem", "PolarityEngine", "MirrorBurst")),
        new FlightTrials("Three Fronts", new[] { Boss<SnowIcebreakerBoss>(Snow), Boss<DesertWormBoss>(Desert), Boss<SpaceCitadelBoss>(Space) },
            Kit("WindUpCannonItem", "SwarmRackItem", "Overdrive")),
        new FlightTrials("After the Dive", new[] { Boss<AbyssSubmarineBoss>(Underwater), Boss<HellCrabBoss>(Hell) },
            Kit("DepthChargeItem", "NeedleCannonItem", "BarrelRoll")),

        // Hell and Heaven
        new FlightTrials("Kitchen Fire", new[] { Level<HellCrabBoss>(Hell) },
            Kit("FlameProjectorItem", "Popcorn", "ChainReactor")),
        new FlightTrials("Red Zone", new[] { Level<HellCrabBoss>(Hell) },
            Kit("ScatterCannonItem", "Bookmark"), approach: SimulatorApproach.HotFloor),
        new FlightTrials("Last Light", "Defeat the Hell crab. Repairs offline.", ShipFrame.Ember, Hell, RunDifficulty.Hard, "EnergyShield", "EnergyShield"),
        new FlightTrials("In Reverse", new[] { Boss<HellCrabBoss>(Hell), Boss<AbyssSubmarineBoss>(Underwater), Boss<TunnelSawBoss>(Tunnel) },
            Kit("ScatterCannonItem", "SwarmRackItem", "Overdrive")),
        new FlightTrials("Silent Approach", new[] { Level<SeraphJudicatorBoss>(Heaven) },
            Kit("BeamEmitterItem", "BarrelRoll", "PocketFan", "ProteinSupplement"), approach: SimulatorApproach.Silent),
        new FlightTrials("Pressure Test", new[] { Level<AbyssSubmarineBoss>(Underwater), Level<HellCrabBoss>(Hell) },
            Kit("BeamEmitterItem", "Bookmark", "PocketFan", "SteelWashers")),

        // Marathons
        new FlightTrials("Second Shift", new[] {
            Level<CitySwitchyardBoss>(City, Kit("RailDriverItem", "Capacitor")),
            Level<TunnelSawBoss>(Tunnel, Kit("ScatterCannonItem", "BarrelRoll")),
            Level<AbyssSubmarineBoss>(Underwater, Kit("BeamEmitterItem", "Telekinesis"))
        }, Array.Empty<string>()),
        new FlightTrials("Final Exam", new[] { Boss<SeraphJudicatorBoss>(Heaven), Boss<VoidSovereignBoss>(LevelId.Void) },
            Kit("WindUpCannonItem", "ProteinSupplement", "Bookmark", "MirrorBurst"), difficulty: RunDifficulty.Hard)
    };

    // Classic tapes with their own rules, named so the list can be reordered freely.
    static readonly FlightTrials secondaryFireTape = Find("Crossfire"), cargoTape = Find("Cold Delivery"), noRepairTape = Find("Last Light");

    // Tapes fly authored district conditions, chosen for variety across the simulator instead of
    // the seed's roll (Miguel, 15 November 2026). The first tape is always the clear daytime river
    // valley; anything not listed flies the ordinary district.
    static readonly Dictionary<string, (LevelId district, DistrictCondition condition)[]> tapeConditions = new Dictionary<string, (LevelId, DistrictCondition)[]>
    {
        { "Crossfire", new[] { (Fields, DistrictCondition.Ordinary) } },
        { "On the Beat", new[] { (Fields, DistrictCondition.Dusk) } },
        { "Change of Plans", new[] { (Fields, DistrictCondition.Forest) } },
        { "Triple Harvest", new[] { (Fields, DistrictCondition.Night) } },
        { "Blank Check", new[] { (Snow, DistrictCondition.Blizzard) } },
        { "Cold Snap", new[] { (Snow, DistrictCondition.Blizzard) } },
        { "Drift School", new[] { (City, DistrictCondition.RushHour) } },
        { "Dead Stick", new[] { (Thunder, DistrictCondition.Thunderstorm) } },
        { "Bank Job", new[] { (Tunnel, DistrictCondition.DeepPassages) } },
        { "Side Hustle", new[] { (Desert, DistrictCondition.Sandstorm) } },
        { "Exact Change", new[] { (Candyland, DistrictCondition.CandyRain) } },
        { "Switchback", new[] { (Underwater, DistrictCondition.Minefield) } },
        { "Moon Tax", new[] { (Space, DistrictCondition.SolarFlare) } },
        { "Three Fronts", new[] { (Desert, DistrictCondition.Sandstorm) } },
        { "Kitchen Fire", new[] { (Hell, DistrictCondition.Eruption) } },
        { "Silent Approach", new[] { (Heaven, DistrictCondition.SpearRain) } },
        { "Second Shift", new[] { (City, DistrictCondition.RushHour) } },
    };

    public DistrictCondition ConditionFor(LevelId level)
    {
        if (tapeConditions.TryGetValue(title, out var conditions))
            foreach (var (place, condition) in conditions)
                if (place == level)
                    return condition;
        return DistrictCondition.Ordinary;
    }

    public static FlightTrials Active { get; private set; }
    static int legIndex;
    static bool approachOver;
    public static Leg CurrentLeg => Active?.legs?[legIndex];
    public static Leg NextLeg => Active?.legs != null && legIndex + 1 < Active.legs.Length ? Active.legs[legIndex + 1] : null;
    public static bool EscortsCargo => Active != null && Active == cargoTape;
    public static bool DisableRepairs => Active != null && Active == noRepairTape;
    public static bool SecondaryFireOnly => Active != null && Active == secondaryFireTape;
    public static bool HasApproach => Active?.Advanced == true && !approachOver;
    public static SimulatorApproach Approach => HasApproach ? Active.approach : SimulatorApproach.Standard;
    public RunDifficulty DifficultyFor(RunDifficulty preferred) => title == "Triple Harvest" && preferred == RunDifficulty.Hard ? preferred : difficulty;
    public string CompletionKey(RunDifficulty mode) => title == "Triple Harvest" && mode == RunDifficulty.Hard ? title + " / Hard" : title;
    public int Seed => 17381 + Array.IndexOf(All, this) * 7919;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        Active = null;
        legIndex = 0;
        approachOver = false;
    }

    public IEnumerable<string> RequiredItems
    {
        get
        {
            var seen = new HashSet<string>();
            foreach (string key in kit)
                if (seen.Add(key) && NeedsUnlock(key)) yield return key;
            if (legs == null) yield break;
            foreach (var leg in legs)
                if (leg.kit != null)
                    foreach (string key in leg.kit)
                        if (seen.Add(key) && NeedsUnlock(key)) yield return key;
        }
    }

    static bool NeedsUnlock(string key) => Blueprint(key) == null && ItemCatalog.TryGet(key, out var item) && item.InRewardPool;

    public bool HasAccess(Func<string, bool> ownsItem, Func<ShipFrame, bool> ownsHull)
    {
        if (!ownsHull(frame)) return false;
        foreach (string key in RequiredItems)
            if (!ownsItem(key)) return false;
        return true;
    }

    public string LockReason
    {
        get
        {
            if (!MetaProgress.CanFly(frame)) return "Commission " + frame + " in the campaign.";
            foreach (string key in RequiredItems)
                if (!MetaProgress.IsUnlocked(key)) return "Unlock " + Display(key) + ".";
            return null;
        }
    }

    public bool Available => LockReason == null;

    public bool TrySelect()
    {
        if (!Available) return false;
        Reset();
        Active = this;
        return true;
    }

    // The approach rule covers the run-up: it ends at the first miniboss or boss. Underwater
    // handling is the exception: it is the tape's whole condition and lasts through the boss
    // (Miguel, 15 November 2026).
    public static bool ApproachLastsWholeFlight => Active?.approach == SimulatorApproach.WaterHandling;
    public static void BeginBoss() => approachOver = !ApproachLastsWholeFlight;
    public static void EndApproach() => approachOver = !ApproachLastsWholeFlight;
    public static void AdvanceLeg() => legIndex++;

    public static FlightTrials Blueprint(string item) => string.IsNullOrEmpty(item) ? null : Array.Find(All, t => t.reward == item);
    public static FlightTrials Find(string name) => Array.Find(All, t => t.title == name);
    public static string Display(string key) => string.IsNullOrEmpty(key) ? "" : Regex.Replace(key.Replace("Item", ""), "(?<=[a-z])(?=[A-Z])", " ");
    static string BossLabel(Type type)
    {
        if (type == typeof(FieldsBomberBoss)) return "Bomber";
        if (type == typeof(PlainsRedKiteBoss)) return "Red Kite";
        if (type == typeof(PlainsThreshingDrumBoss)) return "Threshing Drum";
        if (type == typeof(SnowSiegeBoss)) return "Snow Siege";
        if (type == typeof(SnowIcebreakerBoss)) return "Icebreaker";
        if (type == typeof(CityDuoBoss)) return "City Duo";
        if (type == typeof(CitySwitchyardBoss)) return "Switchyard";
        if (type == typeof(SpaceCitadelBoss)) return "Black Hole";
        if (type == typeof(AbyssSubmarineBoss)) return "Submarine";
        if (type == typeof(SeraphJudicatorBoss)) return "Heaven Angel";
        return Display(type.Name.Replace("Boss", ""));
    }

    // A district with alternate bosses names the one this level ends with.
    static string LevelLabel(Leg leg)
    {
        string name = RouteUnlocks.Name(leg.district);
        return DistrictWaves.Bosses(leg.district).Count > 1 ? name + ": " + BossLabel(leg.boss) : name;
    }

    public string[] Sequence()
    {
        if (Advanced) return CurrentLeg.sequence;
        if (district == Snow) return new[] { "2", "3", "end" };
        if (district == Hell) return new[] { "boss" };
        var keys = new List<string>();
        foreach (var key in DistrictWaves.Sequence())
        {
            var slot = RunSlot.Parse(key);
            if (slot.Kind == RunSlotKind.Combat || slot.Kind == RunSlotKind.Miniboss || slot.Kind == RunSlotKind.Boss)
                keys.Add(key);
        }
        return keys.ToArray();
    }
}
