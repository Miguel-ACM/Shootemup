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

        public Leg(LevelId district, Type boss, string[] kit, params string[] waves)
        {
            this.district = district;
            this.boss = boss;
            this.kit = kit;
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
            stops.Add(leg.boss == null ? RouteUnlocks.Name(leg.district) : BossLabel(leg.boss));
        }
        objective = string.Join(" > ", stops);
    }

    static string[] Kit(params string[] items) => items;
    static Leg Waves(LevelId level, params string[] waves) => new Leg(level, null, null, waves);
    static Leg WavesWithKit(LevelId level, string[] items, params string[] waves) => new Leg(level, null, items, waves);
    static Leg Boss<T>(LevelId level, params string[] waves) where T : DistrictBoss => new Leg(level, typeof(T), null, waves);
    static Leg BossWithKit<T>(LevelId level, string[] items, params string[] waves) where T : DistrictBoss => new Leg(level, typeof(T), items, waves);

    public static readonly FlightTrials[] All = {
        new FlightTrials("Crossfire", "Defeat the Plains bomber with secondary fire.", ShipFrame.GR07, Fields, RunDifficulty.Normal, "WindUpTeeth", "AK45Item", "WindUpTeeth"),
        new FlightTrials("Cold Delivery", "Bring the crate through two Snow waves.", ShipFrame.Warden, Snow, RunDifficulty.Normal, "Thermos", "Magnet", "SparePlating"),
        new FlightTrials("Line Discipline", "Clear the Thunder boss's beam lanes.", ShipFrame.Lancer, Thunder, RunDifficulty.Hard, "HeatSink", "HeatSink", "PocketFan"),
        new FlightTrials("Last Light", "Defeat the Hell crab. Repairs offline.", ShipFrame.Ember, Hell, RunDifficulty.Hard, "EnergyShield", "EnergyShield"),

        new FlightTrials("Triple Harvest", new[] { Boss<FieldsBomberBoss>(Fields), Boss<PlainsRedKiteBoss>(Fields), Boss<PlainsThreshingDrumBoss>(Fields) },
            Kit("NeedleCannonItem", "ScatterCannonItem", "RelaySwitch")),
        new FlightTrials("Change of Plans", new[] { Boss<CitySwitchyardBoss>(City, "2", "6", "10", "14") },
            Kit("CarouselItem", "BarrelRoll"), "CarouselItem"),
        new FlightTrials("Kitchen Fire", new[] { Boss<HellCrabBoss>(Hell, "4", "8", "12") },
            Kit("FlameProjectorItem", "Popcorn", "ChainReactor")),
        new FlightTrials("Side Hustle", new[] { Waves(City, "4", "8"), Boss<DesertWormBoss>(Desert, "4", "8") },
            Kit("AK45Item", "BackdraftItem")),
        new FlightTrials("Jade Spark", new[] { Waves(Thunder, "6", "10"), Boss<DesertWormBoss>(Desert, "8", "12") },
            Kit("ElectricityItem", "Poison", "GasolineLeak")),
        new FlightTrials("Moon Tax", new[] { Boss<SpaceCitadelBoss>(Space, "6", "10") },
            Kit("NeedleCannonItem", "ThreeMoonsItem", "RecoveryCoil")),

        new FlightTrials("Blank Check", new[] { Boss<ThunderLanceBoss>(Thunder, "4", "8") },
            Kit("NeedleCannonItem", "MirrorBurst", "RocketBouquet"), approach: SimulatorApproach.UnlimitedEnergy),
        new FlightTrials("Dead Stick", new[] { Boss<ThunderLanceBoss>(Thunder, "4", "10") },
            Kit("NeedleCannonItem", "BarrelRoll"), approach: SimulatorApproach.VerticalSteering),
        new FlightTrials("Drift School", new[] { Waves(Underwater, "4", "8"), Boss<CityDuoBoss>(City, "6", "10") },
            Kit("DiscThrowerItem", "FocusTime"), approach: SimulatorApproach.WaterHandling),
        new FlightTrials("Black Box", new[] { Boss<CitySwitchyardBoss>(City, "2", "6", "10") },
            Kit("RailDriverItem", "ScatterCannonItem", "BlankBullet"), approach: SimulatorApproach.Autopilot),
        new FlightTrials("On the Beat", new[] { Boss<ThunderLanceBoss>(Thunder, "6", "10") },
            Kit("BurstLauncherItem"), approach: SimulatorApproach.Downbeat),
        new FlightTrials("Red Zone", new[] { Boss<HellCrabBoss>(Hell, "6", "10") },
            Kit("ScatterCannonItem", "Bookmark"), approach: SimulatorApproach.HotFloor),
        new FlightTrials("Silent Approach", new[] { Boss<SeraphJudicatorBoss>(Heaven, "4", "8") },
            Kit("BeamEmitterItem", "BarrelRoll", "PocketFan"), approach: SimulatorApproach.Silent),
        new FlightTrials("Window Seat", new[] { Boss<SpaceCitadelBoss>(Space, "4", "10") },
            Kit("BeamEmitterItem", "Telekinesis"), approach: SimulatorApproach.MovingWindow),
        new FlightTrials("Slow Lane", new[] { Waves(Desert, "4", "8"), Boss<CandyRegentBoss>(Candyland, "6", "10") },
            Kit("SwarmRackItem", "Bookmark"), approach: SimulatorApproach.Slow),
        new FlightTrials("Second Shift", new[] {
            WavesWithKit(Snow, Kit("RailDriverItem", "Capacitor"), "4", "8"),
            WavesWithKit(Candyland, Kit("ScatterCannonItem", "BarrelRoll"), "6", "10"),
            BossWithKit<SpaceCitadelBoss>(Space, Kit("BeamEmitterItem", "Telekinesis"), "8", "12")
        }, Array.Empty<string>()),

        new FlightTrials("Three Gun Salute", new[] {
            BossWithKit<FieldsBomberBoss>(Fields, Kit("RailDriverItem", "Capacitor")),
            BossWithKit<PlainsRedKiteBoss>(Fields, Kit("ScatterCannonItem", "BarrelRoll")),
            BossWithKit<PlainsThreshingDrumBoss>(Fields, Kit("DiscThrowerItem", "PocketFan"))
        }, Array.Empty<string>()),
        new FlightTrials("Cold Snap", new[] { Boss<SnowIcebreakerBoss>(Snow), Boss<CandyRegentBoss>(Candyland) },
            Kit("ScatterCannonItem", "BulletTime", "Overdrive")),
        new FlightTrials("Power Exchange", new[] { Boss<SnowSiegeBoss>(Snow), Boss<ThunderLanceBoss>(Thunder), Boss<SpaceCitadelBoss>(Space) },
            Kit("BeamEmitterItem", "PolarityEngine", "MirrorBurst")),
        new FlightTrials("Split Verdict", new[] { Boss<CityDuoBoss>(City), Boss<CitySwitchyardBoss>(City) },
            Kit("RailDriverItem", "ScatterCannonItem", "RelaySwitch")),
        new FlightTrials("Rear Guard", new[] { Waves(Underwater, "6", "10"), Boss<TunnelSawBoss>(Tunnel, "4", "8") },
            Kit("SwarmRackItem", "LaserDroneItem", "ThrowableShield")),
        new FlightTrials("Three Fronts", new[] { Boss<DesertWormBoss>(Desert), Boss<CandyRegentBoss>(Candyland), Boss<AbyssSubmarineBoss>(Underwater) },
            Kit("WindUpCannonItem", "SwarmRackItem", "Overdrive")),
        new FlightTrials("In Reverse", new[] { Boss<HellCrabBoss>(Hell), Boss<AbyssSubmarineBoss>(Underwater), Boss<TunnelSawBoss>(Tunnel) },
            Kit("ScatterCannonItem", "SwarmRackItem", "Overdrive")),
        new FlightTrials("Pressure Test", new[] { Waves(Underwater, "8", "12"), Waves(Thunder, "8", "12"), Boss<SeraphJudicatorBoss>(Heaven) },
            Kit("BeamEmitterItem", "Bookmark", "PocketFan")),
        new FlightTrials("After the Dive", new[] { Boss<AbyssSubmarineBoss>(Underwater), Boss<HellCrabBoss>(Hell) },
            Kit("DepthChargeItem", "NeedleCannonItem", "BarrelRoll")),
        new FlightTrials("Final Exam", new[] { Boss<SeraphJudicatorBoss>(Heaven), Boss<VoidSovereignBoss>(LevelId.Void) },
            Kit("Bookmark", "MirrorBurst"), difficulty: RunDifficulty.Hard)
    };

    public const string AccessRequirement = "Unlock every standard item, hull and district.";
    public static FlightTrials Active { get; private set; }
    static int legIndex;
    static bool firstBossStarted;
    public static Leg CurrentLeg => Active?.legs?[legIndex];
    public static Leg NextLeg => Active?.legs != null && legIndex + 1 < Active.legs.Length ? Active.legs[legIndex + 1] : null;
    public static bool EscortsCargo => Active == All[1];
    public static bool DisableRepairs => Active == All[3];
    public static bool SecondaryFireOnly => Active == All[0];
    public static bool HasApproach => Active?.Advanced == true && !firstBossStarted;
    public static SimulatorApproach Approach => HasApproach ? Active.approach : SimulatorApproach.Standard;
    public RunDifficulty DifficultyFor(RunDifficulty preferred) => title == "Triple Harvest" && preferred == RunDifficulty.Hard ? preferred : difficulty;
    public string CompletionKey(RunDifficulty mode) => title == "Triple Harvest" && mode == RunDifficulty.Hard ? title + " / Hard" : title;
    public int Seed => 17381 + Array.IndexOf(All, this) * 7919;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        Active = null;
        legIndex = 0;
        firstBossStarted = false;
    }

    public static bool HasAccess(Func<string, bool> ownsItem, Func<ShipFrame, bool> ownsHull, Func<LevelId, bool> ownsDistrict)
    {
        foreach (var hull in (ShipFrame[])Enum.GetValues(typeof(ShipFrame)))
            if (!ownsHull(hull)) return false;
        foreach (var level in (LevelId[])Enum.GetValues(typeof(LevelId)))
            if (!ownsDistrict(level)) return false;
        foreach (var item in ItemCatalog.All)
            if (item.InRewardPool && Blueprint(item.Key)?.Advanced != true && !ownsItem(item.Key)) return false;
        return true;
    }

    public bool Available => !Advanced || HasAccess(MetaProgress.IsUnlocked, MetaProgress.CanFly, MetaProgress.HasUnlockedRoute);

    public bool TrySelect()
    {
        if (!Available) return false;
        Reset();
        Active = this;
        return true;
    }

    public static void BeginBoss() => firstBossStarted = true;
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
