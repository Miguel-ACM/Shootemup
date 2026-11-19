using UnityEngine;
using System.Collections.Generic;

public enum UpgradeKind
{
    Splitter, Rebound, Proximity, Capacitor, Focus, Recovery, Salvage, Drone, Reserve, Return, Bearing, Burn, Battery, ChainReactor, QualityGlasses, BadGlasses, Gasoline, Poison, BouncingBall, FocusTime, DoubleTrouble, ChewingGum, HotSauce, RubberBand, Icicle, Squad, Corkscrew, Slingshot, Ink, Grudge, Marbles, Hourglass, LoadedDice, RelaySwitch, LiveWire,
    TapeMeasure, BrassKnuckles, Matchbox, Compost, PiggyBank, LoanShark, SecondWind,
    Lighthouse, GoldenCrown, Midas, EchoChamber, Metronome, EventHorizon, Hive, TrophyCase,
    EnergyShield, RechargingShield, DeflectorPlate, SparkPlug, ColdCoil, AirPump, RubberMallet, PaddedSeat, Flyswatter, Crowbar, BenchRest, TicketPunch, SteelToe, Pinheads, BountySeal
}
public sealed class CombatUpgrades : MonoBehaviour
{
    public static CombatUpgrades Current
    {
        get; private set;
    }

    readonly Dictionary<UpgradeKind, int> stacks = new Dictionary<UpgradeKind, int>();
    readonly Dictionary<string, float> gates = new Dictionary<string, float>();
    Shipcontroller ship;
    ShipLoadout loadout;
    readonly float[] relayCharge = new float[2], relayBurst = new float[2];
    float capacitor, battery, reserve, focusUntil;
    int volleys, focus, coins, blocks, arrivedStage;
    int punchedKills;
    AEnemy focused;
    public bool ReturnThisVolley
    {
        get; private set;
    }

    public float Charge => capacitor;
    public float SalvageProgress => coins / 5f;
    public float Reserve => reserve;
    void Awake()
    {
        ItemProcVfx.Warm();
        gameObject.AddComponent<UpgradeMeters>();
        Current = this;
        ship = GetComponent<Shipcontroller>();
        loadout = GetComponent<ShipLoadout>();
        arrivedStage = LevelRun.Stage;
        PhaseBreak.Played += PhaseBroken;
        _ = GameInput.Actions;
    }

    void OnDestroy()
    {
        PhaseBreak.Played -= PhaseBroken;
        if (Current == this)
            Current = null;
    }

    public int Count(UpgradeKind kind) => stacks.TryGetValue(kind, out int n) ? n : 0;
    public void Remove(UpgradeKind kind)
    {
        stacks[kind] = Mathf.Max(0, Count(kind) - 1);
        if (kind == UpgradeKind.Reserve)
            reserve = Count(kind) > 0 ? reserve : 0;
        if (kind == UpgradeKind.Capacitor && Count(kind) == 0)
            capacitor = 0;
        if (kind == UpgradeKind.TicketPunch && Count(kind) == 0)
            punchedKills = 0;
        if (kind == UpgradeKind.Focus && Count(kind) == 0)
            focused = null;
        if (kind == UpgradeKind.RelaySwitch && Count(kind) == 0)
        {
            ResetRelaySlot(0);
            ResetRelaySlot(1);
        }
    }
    public static float HostileSpeedAt(Vector3 position)
    {
        if (Current == null)
            return Drill.SlowAt(position) * InkPuddle.SlowAt(position);
        float rate = Current.Count(UpgradeKind.QualityGlasses) > 0 ? .9f : 1;
        if (Current.Count(UpgradeKind.BadGlasses) > 0 && ((Vector2)(position - Current.transform.position)).sqrMagnitude <= 9)
            rate *= .75f;
        return rate * Drill.SlowAt(position) * InkPuddle.SlowAt(position);
    }

    public void Add(UpgradeKind kind)
    {
        stacks[kind] = Count(kind) + 1;
        if ((kind == UpgradeKind.BadGlasses || kind == UpgradeKind.QualityGlasses) && GetComponent<GlassesFieldCue>() == null)
            gameObject.AddComponent<GlassesFieldCue>();
        if (kind == UpgradeKind.Recovery && ship.GetComponentsInChildren<BlockerOrbital>().Length == 0 && FindAnyObjectByType<BlockerOrbital>() == null)
            ship.AddCompanion("AMoon");
        if (kind == UpgradeKind.Drone)
            gameObject.AddComponent<SupportDrone>();
        if (kind == UpgradeKind.SteelToe && GetComponent<SteelToeContact>() == null)
            gameObject.AddComponent<SteelToeContact>();
    }

    void Update()
    {
        if (GameRules.playerDisabled)
            return;
        // A new district has opened once its clouds have cleared and flight resumes.
        if (LevelRun.Stage != arrivedStage && GameClock.ActivePlay)
        {
            arrivedStage = LevelRun.Stage;
            PiggyBank.Pay(ship, Count(UpgradeKind.PiggyBank));
            LoanShark.Collect(ship, Count(UpgradeKind.LoanShark));
        }
        if (loadout != null && Count(UpgradeKind.RelaySwitch) > 0 && GameClock.ActivePlay)
            UpdateRelay();
        if (Count(UpgradeKind.Capacitor) > 0 && !GameInput.Snapshot.Primary && !GameInput.Snapshot.Secondary)
        {
            float previous = capacitor;
            capacitor = Mathf.Min(1, capacitor + Time.deltaTime * .45f * (GameRules.playerShootSpeed / 10));
            if (previous < 1 && capacitor >= 1)
            {
                RunInventory.Ping(UpgradeKind.Capacitor);
                ItemProcVfx.Emit(ItemProcShape.Charge, transform.position + Vector3.up * .5f, new Color(.45f, .75f, .7f), .5f);
                ItemSpark.Emit(transform.position + Vector3.up * .7f, new Color(.5f, .9f, .7f), 4, .1f);
            }
        }
    }

    void UpdateRelay()
    {
        var input = GameInput.Snapshot;
        if (input.Primary == input.Secondary || loadout.Weapons[0] == null || loadout.Weapons[1] == null)
            return;
        int firing = input.Primary ? 0 : 1;
        int idle = 1 - firing;
        float dt = GameClock.PlayerDelta;
        relayBurst[firing] = Mathf.Max(0, relayBurst[firing] - dt);
        if (relayCharge[firing] >= 1)
        {
            relayCharge[firing] = 0;
            relayBurst[firing] = .6f;
            RunInventory.Ping(UpgradeKind.RelaySwitch);
            ItemSound.Cue("itemCapReady", "ready", .07f, 1.25f);
        }
        if (!loadout.Weapons[idle].FiresOnEitherTrigger && relayBurst[idle] <= 0)
            relayCharge[idle] = Mathf.Min(1, relayCharge[idle] + dt * Count(UpgradeKind.RelaySwitch) / 1.25f);
    }

    public bool RelayReady(int slot) => relayCharge[slot] >= 1;
    public void ResetRelaySlot(int slot) => relayCharge[slot] = relayBurst[slot] = 0;

    public float RelayRate(AWeapon weapon)
    {
        if (loadout == null || Count(UpgradeKind.RelaySwitch) == 0 || loadout.Weapons[0] == null || loadout.Weapons[1] == null)
            return 1;
        int slot = weapon == loadout.Weapons[0] ? 0 : weapon == loadout.Weapons[1] ? 1 : -1;
        if (slot < 0 || relayBurst[slot] <= 0)
            return 1;
        var input = GameInput.Snapshot;
        return (slot == 0 ? input.Primary && !input.Secondary : input.Secondary && !input.Primary) ? 2 : 1;
    }

    /// <summary>Shared feedback throttle for item effects outside this class.</summary>
    public bool Gate(string key, float cooldown) => Ready(key, cooldown);

    void PhaseBroken(Vector3 at)
    {
        if (Count(UpgradeKind.SecondWind) > 0 && ship != null && ship.health > 0)
            SecondWind.Breathe(ship);
    }

    bool Ready(string key, float cooldown)
    {
        if (gates.TryGetValue(key, out float until) && Time.time < until)
            return false;
        gates[key] = Time.time + cooldown;
        return true;
    }

    public float Volley()
    {
        volleys++;
        ReturnThisVolley = Count(UpgradeKind.Return) > 0 && volleys % 4 == 0;
        if (ReturnThisVolley)
            RunInventory.Ping(UpgradeKind.Return);
        float multiplier = 1 + capacitor * 2;
        if (capacitor > .175f)
        {
            RunInventory.Ping(UpgradeKind.Capacitor);
            ItemProcVfx.Emit(ItemProcShape.Charge, transform.position + Vector3.up * .8f, new Color(.45f, .75f, .7f), .55f);
            ItemSpark.Emit(transform.position + Vector3.up * .8f, new Color(.5f, .9f, .7f), 4, .16f, true);
        }
        capacitor = 0;
        return multiplier;
    }

    public bool ProximityReady => Count(UpgradeKind.Proximity) > 0 && (!gates.TryGetValue("fuse", out float t) || Time.time >= t);
    // Loaded Dice: any chance-gated proc may land twice (30% per copy, compounding).
    static readonly Color DicePip = new Color(.96f, .93f, .84f);
    public int Rolls(Vector3 at)
    {
        int dice = Count(UpgradeKind.LoadedDice);
        if (dice == 0 || Random.value >= 1 - Mathf.Pow(.7f, dice))
            return 1;
        RunInventory.Ping(UpgradeKind.LoadedDice);
        ItemSpark.Emit(at, DicePip, 2, .07f);
        ItemSound.Cue("itemRicochet", volume: .04f, pitch: 1.6f, cooldown: .15f);
        return 2;
    }

    // How many times a chance-gated proc fires now: 0 while its gate is closed.
    int Proc(string key, float cooldown, Vector3 at) => Ready(key, cooldown) ? Rolls(at) : 0;
    public int SpendProximity(Vector3 at) => Count(UpgradeKind.Proximity) > 0 ? Proc("fuse", .75f, at) : 0;
    public int TryRebound(Vector3 at) => Count(UpgradeKind.Rebound) > 0 ? Proc("rebound", .18f, at) : 0;
    public float DirectHit(AEnemy enemy, float damage, int generation = 0, bool companion = false, bool fire = false, bool energy = false, bool baby = false)
    {
        if (enemy == null || !enemy.CanReceiveDamage)
            return 0;
        // Only a hull that was already burning amplifies the incoming hit.
        damage *= BurningHull.Vulnerability(enemy) * RuleDamage();
        int swatters = enemy.IsSmallTarget ? Count(UpgradeKind.Flyswatter) : 0;
        if (swatters > 0)
        {
            damage *= 1 + .25f * swatters;
            if (Ready("flyswatter", .3f)) RunInventory.Ping(UpgradeKind.Flyswatter);
        }
        int crowbars = enemy.IsBossTarget ? Count(UpgradeKind.Crowbar) : 0;
        if (crowbars > 0)
        {
            damage *= 1 + .2f * crowbars;
            if (Ready("crowbar", .3f)) RunInventory.Ping(UpgradeKind.Crowbar);
        }
        int rests = ship != null && ship.IsStandingStill ? Count(UpgradeKind.BenchRest) : 0;
        if (rests > 0)
        {
            damage *= 1 + .2f * rests;
            if (Ready("bench rest", .3f)) RunInventory.Ping(UpgradeKind.BenchRest);
        }
        if (companion)
            damage *= 1 + Count(UpgradeKind.Drone) * .2f + Count(UpgradeKind.Squad) * .5f;
        if (companion && !baby) return damage;
        bool primaryHit = generation == 0 && !companion;
        if (primaryHit || energy)
            damage *= ReachDamage.Scale(this, enemy, transform.position);
        if (primaryHit)
            damage *= Metronome.Scale(this, enemy.transform.position);
        if (Count(UpgradeKind.Focus) > 0 && generation == 0)
        {
            const float stepSeconds = .25f;
            if (focused != enemy || Time.time > focusUntil)
            {
                focus = 0;
                focused = enemy;
                gates["focus"] = Time.time + stepSeconds;
            }
            else if (focus < 10 && Ready("focus", stepSeconds))
            {
                focus++;
                if (focus == 1 || focus == 5 || focus == 10)
                {
                    RunInventory.Ping(UpgradeKind.Focus);
                    ItemProcVfx.Emit(ItemProcShape.Lock, enemy.transform.position, new Color(.35f, .72f, .8f), enemy.size + .2f);
                    ItemSound.Cue("itemFocus", volume: .065f, pitch: .85f + focus * .035f, cooldown: .3f);
                }
            }
            focusUntil = Time.time + 1.1f;
            damage *= 1 + focus * .015f;
        }
        int ignitions = fire || Count(UpgradeKind.Gasoline) > 0 ? 1
            : Count(UpgradeKind.Burn) > 0 ? Proc("burn", companion ? .4f : .18f, enemy.transform.position) : 0;
        bool struck = ignitions == 0 && generation == 0 && !companion && MatchStrike.Strike(enemy);
        if (struck)
            ignitions = 1;
        if (ignitions > 0)
        {
            // Only a Lighter's chance ignition is an activation; flames and gasoline ignite every hit.
            if (!fire && !struck && Count(UpgradeKind.Gasoline) == 0)
                RunInventory.Ping(UpgradeKind.Burn);
            var burn = enemy.GetComponent<BurningHull>();
            if (burn == null)
                burn = enemy.gameObject.AddComponent<BurningHull>();
            for (int i = 0; i < ignitions; i++)
                burn.Ignite(Mathf.Max(1, damage * .4f) * (companion ? .5f : 1));
        }
        if (Count(UpgradeKind.Poison) > 0)
        {
            var poison = enemy.GetComponent<PoisonedHull>();
            if (poison == null)
                poison = enemy.gameObject.AddComponent<PoisonedHull>();
            poison.Infect(damage * (companion ? .5f : 1));
        }
        if ((primaryHit || energy) && Count(UpgradeKind.Icicle) > 0)
            damage = (enemy.GetComponent<FrozenHull>() ?? enemy.gameObject.AddComponent<FrozenHull>()).Hit(damage);
        if (generation > 0)
            return FinishHit(enemy, damage); // Secondary hits stop before general procs and charge.
        if (!companion)
            GetComponent<PopcornHits>()?.Hit(enemy, damage);
        if (!companion)
            GetComponent<StaticCharge>()?.Discharge(enemy, damage);
        int splits = Count(UpgradeKind.Splitter) > 0 ? Proc("split", .45f, enemy.transform.position) : 0;
        if (splits > 0)
        {
            for (int i = 0; i < splits; i++)
                Fragments(enemy.transform.position, damage, 2, 25, i * 50);
            RunInventory.Ping(UpgradeKind.Splitter);
            ItemProcVfx.Emit(ItemProcShape.Fork, enemy.transform.position, new Color(.3f, .75f, .82f));
            ItemSound.Cue("itemSplit", volume: .08f, cooldown: .18f);
        }
        int sprays = Count(UpgradeKind.Bearing) > 0 ? Proc("bearing", .65f, enemy.transform.position) : 0;
        if (sprays > 0)
        {
            for (int i = 0; i < sprays; i++)
                Fragments(enemy.transform.position, damage * .55f, 3, 60, i * 30);
            RunInventory.Ping(UpgradeKind.Bearing);
            ItemProcVfx.Emit(ItemProcShape.Detonate, enemy.transform.position, new Color(.7f, .65f, .42f), .35f);
            ItemSound.Cue("itemBearing", volume: .07f, cooldown: .2f);
        }
        if (Count(UpgradeKind.Battery) > 0)
        {
            int previous = Mathf.FloorToInt(battery / 8.75f);
            battery += Mathf.Min(damage, enemy.health) * (companion ? .25f : .3f);
            if (Mathf.FloorToInt(battery / 8.75f) > previous)
                ItemProcVfx.Emit(ItemProcShape.Charge, transform.position + Vector3.up * .5f, new Color(.82f, .65f, .3f), .3f);
            if (battery >= 35 && Ready("battery", .7f))
            {
                battery -= 35;
                RunInventory.Ping(UpgradeKind.Battery);
                PlayerRound.Spawn(transform.position, Vector2.up, 28, 12, PlayerDelivery.Missile, 1);
                ItemProcVfx.Emit(ItemProcShape.Counter, transform.position, new Color(.82f, .65f, .3f), .5f);
                ItemSound.Cue("itemBattery", volume: .12f, cooldown: .18f);
            }
        }
        return FinishHit(enemy, damage);
    }

    // Damage rules that scale every direct hit, from guns, companions and actives alike.
    float RuleDamage()
    {
        float scale = 1;
        float grudge = GrudgeTally.Bonus;
        if (grudge > 0)
            scale *= 1 + grudge / Mathf.Max(.5f, GameRules.playerShootDamage);
        scale *= 1 + HourglassSand.Bonus;
        scale *= GoldenCrown.Scale(this, ship);
        return scale;
    }

    float FinishHit(AEnemy enemy, float damage) => Count(UpgradeKind.ChewingGum) > 0 ? StickyGum.Stick(enemy, damage) : damage;
    void Fragments(Vector3 p, float damage, int count, float angle, float offset = 0)
    {
        for (int i = 0; i < count; i++)
            PlayerRound.Spawn(p, CombatVisuals.Rotate(Vector2.up, (i - (count - 1) * .5f) * angle + offset), damage * .45f, 11, PlayerDelivery.Pulse, 1);
    }

    public void Coin()
    {
        if (Count(UpgradeKind.Salvage) == 0)
            return;
        if (++coins >= 5)
        {
            coins -= 5;
            RunInventory.Ping(UpgradeKind.Salvage);
            PlayerRound.Spawn(transform.position, Vector2.up, 12, 10, PlayerDelivery.Missile, 1);
            ItemProcVfx.Emit(ItemProcShape.Convert, transform.position, new Color(.88f, .68f, .28f), .55f);
            ItemSound.Cue("itemSalvage", volume: .12f);
        }
    }

    public void Block()
    {
        if (Count(UpgradeKind.Recovery) == 0 || !Ready("block", .04f))
            return;
        if (++blocks >= 8)
        {
            blocks = 0;
            RunInventory.Ping(UpgradeKind.Recovery);
            PlayerRound.Spawn(transform.position, Vector2.up, 14, 12, PlayerDelivery.Missile, 1);
            ItemProcVfx.Emit(ItemProcShape.Counter, transform.position, new Color(.6f, .65f, .9f), .65f);
            ItemSound.Cue("itemCounter", volume: .12f);
        }
    }

    public void Deflect(Vector2 point)
    {
        if (!Ready("deflector plate", .1f)) return;
        RunInventory.Ping(UpgradeKind.DeflectorPlate);
        ItemSpark.Emit(new Vector3(point.x, point.y, -2), new Color(.66f, .79f, .9f), 3, .08f);
    }

    public void StoreOverflow(float amount)
    {
        if (Count(UpgradeKind.Reserve) > 0 && amount > 0)
        {
            float previous = reserve;
            reserve = Mathf.Min(12, reserve + amount * .5f);
            if (reserve > previous)
            {
                RunInventory.Ping(UpgradeKind.Reserve);
                ItemProcVfx.Emit(ItemProcShape.Convert, transform.position, new Color(.45f, .65f, .86f), .35f);
                ItemSound.Cue("itemReserve", volume: .08f, cooldown: .3f);
            }
        }
    }

    public float SpendReserve(float amount = float.PositiveInfinity)
    {
        float value = Mathf.Min(reserve, amount);
        reserve -= value;
        return value;
    }

    public void Killed(AEnemy enemy)
    {
        int punches = Count(UpgradeKind.TicketPunch);
        if (punches > 0 && ++punchedKills >= 5)
        {
            punchedKills = 0;
            for (int i = 0; i < punches; i++)
                enemy.DropVendingCoin();
            RunInventory.Ping(UpgradeKind.TicketPunch);
        }
        MatchStrike.Spread(enemy);
        if (Count(UpgradeKind.Compost) > 0)
            CompostHeap.Leave(enemy, Count(UpgradeKind.Compost));
        MidasTouch.Touch(enemy, Count(UpgradeKind.Midas));
        if (Count(UpgradeKind.Hive) > 0)
            HiveBee.Release(enemy, Count(UpgradeKind.Hive));
        TrophyShelf.Claim(enemy);
        if (Count(UpgradeKind.Ink) > 0)
        {
            InkPuddle.Spill(enemy.transform.position, Count(UpgradeKind.Ink));
            RunInventory.Ping(UpgradeKind.Ink);
        }
        int chains = Count(UpgradeKind.ChainReactor) > 0 && enemy.GetComponent<BurningHull>() != null ? Proc("chain", .3f, enemy.transform.position) : 0;
        if (chains > 0)
        {
            for (int i = 0; i < chains; i++)
                Fragments(enemy.transform.position, 8, 6, 60, i * 30);
            ItemProcVfx.Emit(ItemProcShape.Detonate, enemy.transform.position, PlayerShotPalette.Fire, Mathf.Max(.55f, enemy.size));
            ItemSound.Cue("itemChain", volume: .13f, cooldown: .25f);
            RunInventory.Ping(UpgradeKind.ChainReactor);
        }
    }

    // Kept for authored item/probe callers; all targeting shares one query.
    public static AEnemy Nearest(Vector3 p, HashSet<AEnemy> excluded, float radius) => CombatTargets.Nearest(p, excluded, radius);
}
