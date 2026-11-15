using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WaveOrchestrer : MonoBehaviour
{

    WavePool wavePool;
    public RunPhase Phase { get; private set; }
    public static WaveOrchestrer Current { get; private set; }
    // District event hazards run only while an ordinary combat wave is in play.
    public bool HazardWindow => Phase == RunPhase.Combat && currentWave != null && !currentWave.units.Exists(unit => unit.miniboss) &&
        rewardOffer == null && GameClock.ActivePlay && WavePreparation.Ready && !GameOverState.Active;
    bool isWaveComingOut { get => Phase == RunPhase.Combat; set => Phase = value ? RunPhase.Combat : RunPhase.Idle; }
    readonly RunPlan plan = new RunPlan();
    // Stable textual keys remain at the authoring/debug boundary.
    public string[] waves { get => plan.Export(); set => plan.Load(value); }
    int currentWaveNumber = 0;
    int currentWaveEnemies = 0;
    int currentWaveKilledEnemies = 0;
    readonly WaveKillLedger killLedger = new WaveKillLedger();
    float[] limits;
    Wave currentWave;
    WaveUnit currentUnit;
    //float waveStartTime = 0;
    float waveTime = 0f;
    float clearTime = 0f;
    float handoffAt;
    bool stopingWave = false;
    GameObject ship;
    Dialog dialog;
    [SerializeField] Warning warning;
    [SerializeField] private GameObject boss;

    List<GameObject> items = new List<GameObject>();
    bool choosingRoute;
    LevelChoice routeChoice;
    readonly ChallengeProgress challenge = new ChallengeProgress();
    int challengeWavesRemaining { get => challenge.Remaining; set => challenge.Remaining = value; }
    bool challengeWave { get => challenge.CurrentWave; set => challenge.CurrentWave = value; }
    public int ChallengeWavesRemaining => challengeWavesRemaining;
    // ELITE HUNT: the marked elite of the current hunt wave, the pips for the margin row, and
    // the reward rarity promised by the hexagon when it was offered.
    HuntMark huntTarget;
    string challengeRarity;
    public int HuntPip(int index) => challenge.Pips[index];
    public bool Hunting => challenge.Active;
    public EncounterKind[] EncounterOptions
    {
        get; private set;
    }

    // Encounter slots are offered half-way through the following combat wave, without pausing it.
    bool offerPending;
    float offerAt;
    // The first hexagon offer of each district also carries SKIP, straight to the boss.
    bool skipOffered;
    RewardHexes rewardOffer;
    public RewardHexes RewardOffer => rewardOffer;
    CargoEscort cargo;
    CargoEscort cargoDelivery;
    // Cargo items already earned, waiting for a clear moment: each opens its own pickup in turn,
    // so an item decision or a boss reward on the same boundary can never swallow one.
    readonly Queue<(string key, string rarity)> pendingCargo = new Queue<(string key, string rarity)>();
    public CargoEscort Cargo => cargo;
    readonly float[] encounterExpiry = new float[Enum.GetValues(typeof(EncounterKind)).Length];
    public bool ShowingEncounter(EncounterKind kind) => encounterExpiry[(int)kind] > Time.time &&
        (kind != EncounterKind.Cargo || cargo != null);

    public const float OpeningBreather = .8f;
    bool openingWavePending { get => Phase == RunPhase.Opening; set => Phase = value ? RunPhase.Opening : RunPhase.Idle; }
    float openingStartsAt;
    DistrictMiniboss activeMiniboss;
    float minibossDeadline = -1;
    bool minibossVictory;
    SimulatorFlight simulatorFlight;
    /// <summary>Every enemy of an ordinary wave resolved, in wave order: (total, destroyed).</summary>
    public static event Action<int, int> CombatWaveFinished;
    public float MinibossRemaining => minibossDeadline < 0 ? 20 : Mathf.Max(0, minibossDeadline - Time.time);
    public bool MinibossVictory => minibossVictory;


    // Start is called before the first frame update
    void OnDestroy()
    {
        if (Current == this)
            Current = null;
    }

    void Start()
    {
        Current = this;
        gameObject.AddComponent<DistrictHazards>();
        dialog = GameObject.FindGameObjectWithTag("Dialog").GetComponent<Dialog>();
        // This orchestrator lives in the additive Run scene; the warning banner belongs to the core's UI.
        if (warning == null)
            warning = FindAnyObjectByType<Warning>(FindObjectsInactive.Include);
        limits = Camera.getLimits();
        ship = GameObject.FindGameObjectWithTag("Player");
        wavePool = GetComponent<WavePool>();
        if (FlightTrials.Approach != SimulatorApproach.Standard)
            simulatorFlight = gameObject.AddComponent<SimulatorFlight>();
        waves = FlightTrials.Active?.Sequence() ?? DistrictWaves.Sequence();
        BeginWaves();
    }

    // Update is called once per frame
    void Update()
    {
        if (openingWavePending)
        {
            // Keep the first phrase clock at zero until the whole arena is visible.
            // This also covers route changes and the loop back to Plains.
            if (CloudTransition.Busy || !WavePreparation.Ready)
            {
                openingStartsAt = -1;
                return;
            }
            if (GameRules.playerDisabled)
                return;
            if (openingStartsAt < 0)
                openingStartsAt = Time.time + OpeningBreather;
            if (Time.time < openingStartsAt)
                return;
            openingWavePending = false;
            if (FlightTrials.EscortsCargo) cargo = CargoEscort.Create(ship.GetComponent<Shipcontroller>());
            GenerateWave();
        }
        if (isWaveComingOut)
        {
            waveTime += Time.deltaTime;
            while (currentUnit != null && waveTime >= currentUnit.time)
            {
                Vector3 pos = new Vector3(currentUnit.position.x * limits[3], currentUnit.position.y * limits[0], 1);
                var type = DistrictWaves.EnemyType(currentUnit.enemy);
                GameObject enemy = type != null ? DistrictEnemy.Spawn(type, pos).gameObject : Instantiate(currentUnit.GetUnit(), pos, Quaternion.identity);
                if (type != null)
                {
                    var districtEnemy = enemy.GetComponent<DistrictEnemy>();
                    if (currentUnit.elite || currentUnit.huntTarget)
                        districtEnemy.MakeElite();
                    if (currentUnit.huntTarget)
                        huntTarget = HuntMark.Attach(districtEnemy, this);
                    districtEnemy.FlyPhrase(currentUnit);
                }
                if (enemy.TryGetComponent<DistrictMiniboss>(out var mini))
                    activeMiniboss = mini;
                enemy.GetComponent<AEnemy>().SetOrchestrer(this, currentWaveNumber);
                currentUnit = currentWave.getNext();
            }
            if (offerPending && !TunnelPassage.Active && (waveTime >= offerAt || currentUnit == null && AEnemy.HoldingCount == 0))
                OpenEncounterChoice();
            // Consecutive phrases overlap their exits: old bullets are part of the rhythm.
            // Decisions and bosses wait for enemies to finish, then explicitly clear hazards.
            var next = plan.Following(currentWaveNumber);
            bool nextCombat = next.Kind == RunSlotKind.Combat && !(challengeWave && challengeWavesRemaining == 1) &&
                !(FlightTrials.EscortsCargo && cargo != null && cargo.WavesRemaining == 1);
            bool empty = currentUnit == null && AEnemy.HoldingCount == 0 && !TunnelPassage.Active;
            clearTime = empty ? clearTime + Time.deltaTime : 0;
            bool miniboss = currentWave.units.Exists(unit => unit.miniboss);
            if (miniboss && activeMiniboss != null && activeMiniboss.Engaged && !activeMiniboss.Retreating)
            {
                if (minibossDeadline < 0)
                    minibossDeadline = Time.time + 20;
                activeMiniboss.SetRemaining(MinibossRemaining);
                if (MinibossRemaining <= 0)
                    activeMiniboss.Retreat();
            }
            // DEEP PASSAGES chain one walled wave straight into the next: the next passage
            // joins the current walls just before they end.
            bool chainPassage = TunnelPassage.EndingSoon && next.Kind == RunSlotKind.Combat && LevelRun.Current == LevelId.Tunnel &&
                TunnelPassageWaves.HasPassage(next.Budget) && TunnelPassageWaves.Chained;
            // A hunt wave holds its handoff until the marked elite is caught or escapes.
            bool phraseComplete = currentUnit == null && !miniboss && huntTarget == null && waveTime >= handoffAt && (AEnemy.HoldingCount < 22 || chainPassage) && (!TunnelPassage.Active || chainPassage);
            if (currentUnit == null && ((nextCombat && phraseComplete) || clearTime >= .12f) && (nextCombat || rewardOffer == null))
            {

                if (stopingWave)
                {
                    isWaveComingOut = false;
                    stopingWave = false;
                }
                else
                {
                    FinishCombatWave();
                }
            }
        }
    }

    void FinishCombatWave()
    {
        isWaveComingOut = false;
        if (currentWave.units.Exists(unit => unit.miniboss))
        {
            minibossVictory = currentWaveKilledEnemies >= currentWaveEnemies;
            activeMiniboss = null;
            minibossDeadline = -1;
        }
        else
        {
            killLedger.HandOff(currentWaveNumber);
            killLedger.Drain(CombatWaveFinished);
            FinishCargoWave();
        }
        if (challenge.FinishWave())
        {
            // The third wave is clear before its single reward opens.
            if (plan[currentWaveNumber].Kind == RunSlotKind.EncounterChoice)
                currentWaveNumber++;
            EraseAllBullets();
            OpenDecision(RunSlotKind.ChallengeReward);
            return;
        }
        GenerateWave();
    }

    // One ship a little way into the wave carries the hunt's mark. Gates, mines and moonlets cannot.
    void MarkHuntTarget()
    {
        var eligible = currentWave.units.FindAll(unit => !unit.miniboss && unit.enemy != nameof(FaradayPylon) &&
            unit.enemy != nameof(DepthMine) && unit.enemy != "OrbitalMoonlet");
        if (eligible.Count == 0)
        {
            challenge.SkipWave();
            return;
        }
        var random = new System.Random(unchecked(LevelRun.Seed + LevelRun.Stage * 313 + currentWaveNumber * 7919));
        int index = Mathf.Clamp(Mathf.FloorToInt(eligible.Count * (.25f + .3f * (float)random.NextDouble())), 0, eligible.Count - 1);
        eligible[index].huntTarget = true;
    }

    public void HuntResolved(HuntMark mark, bool caught)
    {
        if (mark != huntTarget)
            return;
        huntTarget = null;
        if (caught)
        {
            challenge.Catch();
            ItemSound.Play("ready", .14f);
            PSXImpactEffects.Burst(mark.transform.position, EncounterChoice.Accent(EncounterKind.Challenge), true);
        }
        else
        {
            challenge.Escape();
            ItemSound.Play("shatter", .14f);
            // The cracked pip stays up a moment, then the hunt leaves the margin.
            encounterExpiry[(int)EncounterKind.Challenge] = Time.time + 3;
            challengeRarity = null;
            EncounterChoice.ChallengeRarity = null;
        }
    }

    void FinishCargoWave()
    {
        if (cargo == null || !cargo.FinishWave())
            return;
        // The simulator still requires its full two-wave delivery, with no early claim.
        RunCoins.Add(EncounterChoice.SalvageCoins(LevelRun.Tier(LevelRun.Current), GameRules.luck) * 2);
        cargo.Deliver();
        cargo = null;
        GameOverState.Show(true);
    }

    void CashInCargo(CargoEscort delivery)
    {
        if (delivery == null || delivery != cargo || !GameClock.ActivePlay)
            return;
        var quality = delivery.Quality;
        string key = ItemPool.GetCargoReward(out var rarity, quality);
        // An exhausted rarity falls back to the nearest one that still has items: lower first.
        for (int step = 1; key == null && step <= 3; step++)
            foreach (int tier in new[] { (int)quality - step, (int)quality + step })
                if (key == null && tier >= 0 && tier <= 3)
                    key = ItemPool.GetCargoReward(out rarity, (ItemRarity)tier);
        if (!delivery.Deliver(key)) return;
        cargo = null;
        RunCoins.Add(EncounterChoice.SalvageCoins(LevelRun.Tier(LevelRun.Current), GameRules.luck) * 2);
        if (key == null)
        {
            // An exhausted rarity pays salvage instead of silently substituting another tier.
            RunCoins.Add(8 * (1 + (int)quality));
            encounterExpiry[(int)EncounterKind.Salvage] = Time.time + 5;
            return;
        }
        cargoDelivery = delivery;
        pendingCargo.Enqueue((key, rarity));
        // The item rises immediately. Like an ITEM encounter, its pickup screen waits for
        // a clear phrase boundary; no live enemies or unfinished score are discarded.
        // A claim during a boss is paid before that boss's mandatory reward instead.
        if (Phase != RunPhase.Boss)
            plan.Insert(currentWaveNumber, RunSlotKind.CargoReward);
    }

    public void BeginWaves()
    {
        if (currentWaveNumber == 0)
        {
            WarpCoreLoop.MarkLevelStart(ship != null ? ship.GetComponent<Shipcontroller>() : null);
            isWaveComingOut = false;
            openingWavePending = true;
            openingStartsAt = -1;
        }
        else
            GenerateWave();
    }

    private void SpawnBoss()
    {
        var bossType = MetaProgress.EngageBoss();
        Phase = RunPhase.Boss;
        BGMManager.QueueBoss(0); // Also covers direct simulator and review entry.
        GameObject bossGO = DistrictEnemy.Spawn(bossType, new Vector3(0, Camera.UpLimit + 3, 1)).gameObject;
        bossGO.GetComponent<AEnemy>().SetOrchestrer(this, currentWaveNumber);
        bossGO.transform.position = new Vector3(0f, 11.52f, 1f);
        EraseAllBullets();
    }

    void GenerateWave()
    {
        if (GameOverState.Active)
            return;
        if (currentWaveNumber >= plan.Count)
        {
            isWaveComingOut = false;
            return;
        }
        var slot = plan[currentWaveNumber];
        var kind = slot.Kind;
        if (kind == RunSlotKind.MinibossChoice && !minibossVictory)
        {
            currentWaveNumber++;
            GenerateWave();
            return;
        }
        if (kind == RunSlotKind.MinibossChoice)
        {
            currentWaveNumber++;
            EraseAllBullets();
            OpenDecision(RunSlotKind.Reward);
            return;
        }
        if (kind == RunSlotKind.EncounterChoice)
        {
            offerPending = true;
            currentWaveNumber++;
            GenerateWave();
            return;
        }
        if (kind == RunSlotKind.End && FlightTrials.Active?.Advanced == true)
        {
            StartCoroutine(SimulatorHandoff(false));
            return;
        }
        if (kind != RunSlotKind.Combat)
            DismissOffer();
        if (slot.IsDecision)
        {
            EraseAllBullets();
            isWaveComingOut = false;
            OpenDecision(kind);
        }
        else if (kind == RunSlotKind.Boss)
        {
            // Hold the wave timer while the boss is up; the next wave starts when it dies.
            FlightTrials.BeginBoss();
            simulatorFlight?.End();
            isWaveComingOut = false;
            Phase = RunPhase.BossWarning;
            StartCoroutine(BossEntrance());
        }
        else
        {
            isWaveComingOut = true;
            // A simulator approach rule covers the run-up; the miniboss and the rest fly normally.
            if (kind == RunSlotKind.Miniboss)
            {
                simulatorFlight?.End();
                FlightTrials.EndApproach();
            }
            else
                simulatorFlight?.Begin(ship.GetComponent<Shipcontroller>());
            challenge.BeginWave(kind == RunSlotKind.Combat);
            currentWave = wavePool.getWave(slot.Key);
            if (challengeWave)
                MarkHuntTarget();
            var following = plan.Following(currentWaveNumber + 1);
            handoffAt = following.Kind == RunSlotKind.Combat ? WaveHandoff.At(currentWave, wavePool.getWave(following.Key)) : currentWave.duration;
            offerAt = handoffAt * .5f;
            currentWaveEnemies = 0;
            foreach (var unit in currentWave.units)
                currentWaveEnemies += unit.HullCount;
            currentWaveKilledEnemies = 0;
            if (!currentWave.units.Exists(unit => unit.miniboss))
                killLedger.Begin(currentWaveNumber + 1, currentWaveEnemies);
            if (kind == RunSlotKind.Miniboss)
            {
                minibossVictory = false;
                minibossDeadline = -1;
            }
            waveTime = 0;
            clearTime = 0;
            currentUnit = currentWave.getNext();
        }
        currentWaveNumber += 1;
        if (currentWaveNumber >= plan.Count)
        {
            stopingWave = true;
        }
    }

    IEnumerator BossEntrance()
    {
        CombatDecision.Begin();
        try
        {
            BGMManager.QueueBoss(Warning.Duration - .4f);
            SoundManager.PlaySound("bossWarning", .18f);
            warning.ShowFor(Warning.Duration);
            yield return new WaitForSeconds(Warning.Duration);
        }
        finally { CombatDecision.End(); }
        if (GameOverState.Active) yield break;
        SpawnBoss();
    }

    DecisionShop decision;
    void OpenEncounterChoice()
    {
        offerPending = false;
        // The wave in progress counts, because a challenge starts at once.
        int remaining = 1 + plan.RemainingCombat(currentWaveNumber);
        var player = ship.GetComponent<Shipcontroller>();
        int seed = unchecked(LevelRun.Seed + LevelRun.Stage * 971 + currentWaveNumber * 7919);
        // Every second district offers one pact hexagon, at its first reward break.
        bool pactAvailable = PactPool.Due && PactPool.HexStage != LevelRun.Stage && PactPool.HasEligible(player);
        var tradeable = RunInventory.For(player).Tradeable();
        bool canReforge = tradeable.Exists(entry => ItemPool.ReforgeAlternatives(entry.key, entry.rarity).Count >= 2);
        bool offerSkip = !skipOffered && FlightTrials.Active == null && plan.IndexOf(RunSlotKind.Boss, currentWaveNumber) >= 0;
        skipOffered = true;
        // The hunt needs three full waves after the one in progress.
        var options = EncounterChoice.Draw(seed, remaining >= 4 && !challenge.Active, player.health, player.maxHealth,
            pactAvailable, GameRules.luck, tradeable.Count > 0, canReforge,
            cargo == null && pendingCargo.Count == 0 && remaining >= CargoEscort.MinimumOfferWaves, !PactPool.GlassHull, offerSkip);
        if (Array.IndexOf(options, EncounterKind.Pact) >= 0)
            PactPool.HexStage = LevelRun.Stage;
        EncounterOptions = options;
        // The hunt's reward rarity is promised up front: the hexagon wears its colour.
        if (Array.IndexOf(options, EncounterKind.Challenge) >= 0)
        {
            double roll = new System.Random(seed ^ 0x4f1bbcdd).NextDouble() - Mathf.Clamp(GameRules.luck, 0, 2) * .08f;
            challengeRarity = roll < .1f ? "legendary" : roll < .45f ? "epic" : "rare";
            EncounterChoice.ChallengeRarity = challengeRarity;
        }
        rewardOffer = RewardHexes.Open(options, ship.transform, unchecked(seed * 31 + 17), index =>
        {
            rewardOffer = null;
            EncounterOptions = null;
            if (index >= 0)
                ResolveEncounter(options[index]);
        });
    }

    // Bosses, minibosses and decisions silently withdraw an offer that is still open.
    void DismissOffer()
    {
        offerPending = false;
        if (rewardOffer != null)
            rewardOffer.Dismiss();
        rewardOffer = null;
        EncounterOptions = null;
    }

    void ResolveEncounter(EncounterKind kind)
    {
        if (GameOverState.Active)
            return;
        DismissOffer();
        encounterExpiry[(int)kind] = kind == EncounterKind.Health || kind == EncounterKind.Salvage || kind == EncounterKind.Skip ||
            EncounterChoice.IsStatReward(kind) ? Time.time + 5 : float.PositiveInfinity;
        if (kind == EncounterKind.Skip)
        {
            SkipToBoss();
            return;
        }
        if (EncounterChoice.IsStatReward(kind))
        {
            EncounterChoice.ApplyStatReward(kind, ship.GetComponent<Shipcontroller>());
            ItemSound.Play("ready", .18f);
            PSXImpactEffects.Burst(ship.transform.position, EncounterChoice.Accent(kind));
            return;
        }
        if (kind == EncounterKind.Salvage)
        {
            // Salvage rains its coins over the arena; later districts rain more.
            CoinRain.Begin(CoinRain.Coins(LevelRun.Stage, GameRules.luck));
            ItemSound.Play("ready", .18f);
            PSXImpactEffects.Burst(ship.transform.position, EncounterChoice.Accent(kind));
            return;
        }
        if (kind == EncounterKind.Cargo)
        {
            if (cargo == null)
                cargo = CargoEscort.Create(ship.GetComponent<Shipcontroller>(), CashInCargo);
            return;
        }
        if (kind == EncounterKind.HealthTrade || kind == EncounterKind.Reforge)
        {
            plan.Insert(currentWaveNumber, kind == EncounterKind.HealthTrade ? RunSlotKind.HealthTrade : RunSlotKind.Reforge);
            return;
        }
        if (kind == EncounterKind.Shop || kind == EncounterKind.MultShop || kind == EncounterKind.Item || kind == EncounterKind.Pact)
        {
            // Items, shops and pacts open at the next wave break, after the current phrase is cleared.
            plan.Insert(currentWaveNumber, kind == EncounterKind.Shop ? RunSlotKind.CoinShop :
                kind == EncounterKind.MultShop ? RunSlotKind.MultiplierShop : kind == EncounterKind.Pact ? RunSlotKind.Pact : RunSlotKind.Reward);
            return;
        }
        if (kind == EncounterKind.Challenge)
        {
            // The hunt begins with the next wave, so every target arrives from its start.
            challenge.Start(false);
        }
        else
            HealthRecovery.Begin(ship.GetComponent<Shipcontroller>(), .5f);
    }

    // SKIP abandons the district's remaining waves, minibosses and decisions; only a
    // claimed cargo item still opens before the boss, so an earned reward is never lost.
    void SkipToBoss()
    {
        int boss = plan.IndexOf(RunSlotKind.Boss, currentWaveNumber);
        if (boss < 0)
            return;
        bool cargoPending = false;
        for (int i = currentWaveNumber; i < boss; i++)
            cargoPending |= plan[i].Kind == RunSlotKind.CargoReward;
        MetaProgress.SkipDistrict();
        foreach (var enemy in new List<AEnemy>(AEnemy.Live))
            if (enemy != null)
                Destroy(enemy.gameObject);
        EraseAllBullets();
        TunnelPassage.Clear();
        isWaveComingOut = false;
        stopingWave = false;
        currentUnit = null;
        activeMiniboss = null;
        minibossDeadline = -1;
        minibossVictory = false;
        challenge.Reset();
        huntTarget = null;
        killLedger.Clear();
        ItemSound.Play("ready", .18f);
        PSXImpactEffects.Burst(ship.transform.position, EncounterChoice.Accent(EncounterKind.Skip));
        currentWaveNumber = boss;
        if (cargoPending)
            plan.Insert(boss, RunSlotKind.CargoReward);
        GenerateWave();
    }

    void OpenDecision(RunSlotKind kind)
    {
        // A cargo slot whose item was already handed out (at a boss, say) simply passes.
        if (kind == RunSlotKind.CargoReward && pendingCargo.Count == 0)
        {
            GenerateWave();
            return;
        }
        Phase = RunPhase.Decision;
        DismissOffer();
        foreach (var enemy in new List<AEnemy>(AEnemy.Live))
            if (enemy != null)
                Destroy(enemy.gameObject);
        if (kind == RunSlotKind.CargoReward)
        {
            StartCoroutine(OpenCargoReward(GenerateWave));
            return;
        }
        string reservedKey = null, reservedRarity = null;
        if (kind == RunSlotKind.ChallengeReward && challengeRarity != null)
        {
            // Deliver exactly the promised rarity, or the nearest one that still has items.
            int promised = challengeRarity == "legendary" ? 3 : challengeRarity == "epic" ? 2 : 1;
            for (int step = 0; reservedKey == null && step <= 3; step++)
                foreach (int tier in new[] { promised - step, promised + step })
                    if (reservedKey == null && tier >= 0 && tier <= 3)
                        reservedKey = ItemPool.GetCargoReward(out reservedRarity, (ItemRarity)tier);
            challengeRarity = null;
            EncounterChoice.ChallengeRarity = null;
        }
        decision = gameObject.AddComponent<DecisionShop>();
        decision.Open(this, ship.GetComponent<Shipcontroller>(), kind, () =>
        {
            decision = null;
            switch (kind)
            {
                case RunSlotKind.CoinShop: Clear(EncounterKind.Shop); break;
                case RunSlotKind.MultiplierShop: Clear(EncounterKind.MultShop); break;
                case RunSlotKind.Reward: Clear(EncounterKind.Item); break;
                case RunSlotKind.Pact: Clear(EncounterKind.Pact); break;
                case RunSlotKind.HealthTrade: Clear(EncounterKind.HealthTrade); break;
                case RunSlotKind.Reforge: Clear(EncounterKind.Reforge); break;
                case RunSlotKind.ChallengeReward: Clear(EncounterKind.Challenge); break;
            }
            GenerateWave();
        }, reservedKey: reservedKey, reservedRarity: reservedKey != null ? reservedRarity : null,
            reason: kind == RunSlotKind.ChallengeReward ? "ELITE HUNT COMPLETE" : null);
        void Clear(EncounterKind encounter) => encounterExpiry[(int)encounter] = 0;
    }

    IEnumerator OpenCargoReward(Action completed)
    {
        Phase = RunPhase.Decision;
        CombatDecision.Begin();
        EraseAllBullets();
        while (cargoDelivery != null && !GameOverState.Active) yield return null;
        if (GameOverState.Active) yield break;
        cargoDelivery = null;
        // Every earned cargo item opens in turn, each saying where it came from.
        while (pendingCargo.Count > 0 && !GameOverState.Active)
        {
            var (key, rarity) = pendingCargo.Dequeue();
            bool picked = false;
            decision = gameObject.AddComponent<DecisionShop>();
            decision.Open(this, ship.GetComponent<Shipcontroller>(), RunSlotKind.CargoReward, () =>
            {
                decision = null;
                picked = true;
            }, reservedKey: key, reservedRarity: rarity, reason: "CARGO DELIVERY · " + rarity.ToUpperInvariant());
            while (!picked && !GameOverState.Active) yield return null;
        }
        if (!GameOverState.Active)
            completed?.Invoke();
    }

    private void EraseAllBullets()
    {
        CombatRound.ClearAll();
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
        foreach (GameObject i in bullets)
        {
            var bullet = i.GetComponent<AEnemyBullet>();
            if (bullet != null)
                bullet.Vanish();
        }
    }


    // Claim the closest eligible option once; rewards are applied only on arrival.
    public bool TryClaimItem(AItem candidate) => decision != null && decision.TryClaim(candidate);
    public bool CommitItem(AItem candidate) => decision != null && decision.CommitItem(candidate);
    public void NotifyItemPicked(GameObject item)
    {
        if (decision != null)
            decision.Picked(item);
    }

    public void NotifyEnemyKilled(int waveNumber, bool isBoss = false, Vector3? bossPosition = null, bool defeated = true)
    {
        if (isBoss)
        {
            DismissOffer();
            if (choosingRoute || GameOverState.Active)
                return;
            if (!defeated && FlightTrials.Active?.Advanced == true)
            {
                GameOverState.Show();
                return;
            }
            MetaProgress.DefeatBoss();
            choosingRoute = true;
            isWaveComingOut = false;
            EraseAllBullets();
            if (FlightTrials.Active != null)
            {
                if (FlightTrials.Active.Advanced)
                    StartCoroutine(SimulatorHandoff(true));
                else
                    GameOverState.Show(true);
                return;
            }
            StartCoroutine(BossReward(bossPosition ?? new Vector3(0, Camera.UpLimit * .5f, 1)));
        }
        else
        {
            if (defeated && currentWaveNumber == waveNumber)
                currentWaveKilledEnemies += 1;
            killLedger.Resolve(waveNumber, defeated);
            killLedger.Drain(CombatWaveFinished);
        }
    }

    IEnumerator SimulatorHandoff(bool bossDefeated)
    {
        Phase = RunPhase.Routing;
        simulatorFlight?.End(true);
        CombatDecision.Begin();
        EraseAllBullets();
        // The next tape leg is fixed. Give the normal shatter and payout their beat.
        if (bossDefeated)
        {
            yield return new WaitForSeconds(1.25f);
            if (GameOverState.Active) yield break;
            foreach (var coin in FindObjectsByType<Money>()) coin.Collect();
        }
        var next = FlightTrials.NextLeg;
        if (next == null)
        {
            GameOverState.Show(bossDefeated);
            yield break;
        }
        BGMManager.PlayLevelMusic(next.district);
        CloudTransition.Change(() => ResetDistrict(next.district, true));
    }

    IEnumerator BossReward(Vector3 position)
    {
        Phase = RunPhase.Decision;
        // Let the breakup's main blast open before the item falls.
        CombatDecision.Begin();
        yield return new WaitForSeconds(.8f);
        if (GameOverState.Active)
            yield break;
        foreach (var enemy in FindObjectsByType<AEnemy>())
            Destroy(enemy.gameObject);
        EraseAllBullets();
        if (LevelRun.FinalDistrict)
        {
            // The last shatter ends the campaign; there is no equipment decision after it.
            // Leave a moment for its coin burst to return before banking the run.
            yield return new WaitForSeconds(2);
            foreach (var coin in FindObjectsByType<Money>()) coin.Collect();
            GameOverState.Show(true);
            yield break;
        }
        if (pendingCargo.Count > 0)
        {
            yield return OpenCargoReward(null);
            if (GameOverState.Active) yield break;
        }
        position = new Vector3(Mathf.Clamp(position.x, Camera.LeftLimit + .8f, Camera.RightLimit - .8f),
            Mathf.Clamp(position.y, Camera.DownLimit + 2, Camera.UpLimit - 1.5f), 1);
        decision = gameObject.AddComponent<DecisionShop>();
        decision.Open(this, ship.GetComponent<Shipcontroller>(), RunSlotKind.BossReward, () =>
           {
               decision = null;
               StartCoroutine(ChooseNextRoute());
           }, position);
    }

    /// <summary>Debug: the next wave break opens a pact offer.</summary>
    public void ForcePactNext()
    {
        plan.Insert(currentWaveNumber, RunSlotKind.Pact);
    }

    IEnumerator ChooseNextRoute()
    {
        Phase = RunPhase.Routing;
        // Pickup and skip both complete through DecisionShop.Close.
        CombatDecision.Begin();
        var next = LevelRun.AutomaticNext;
        yield return new WaitForSeconds(next.HasValue ? 1f : .2f);
        if (GameOverState.Active)
            yield break;
        foreach (var enemy in FindObjectsByType<AEnemy>())
            Destroy(enemy.gameObject);
        EraseAllBullets();
        // A boss whose way on is still sealed ends the campaign after its reward.
        if (LevelRun.FinalDistrict || FlightTrials.Active != null ||
            !Array.Exists(LevelRun.GetOptions(), MetaProgress.RouteOpen))
        {
            GameOverState.Show(true);
            yield break;
        }
        if (next.HasValue)
        {
            BGMManager.PlayLevelMusic(next.Value);
            TravelToLevel(next.Value);
            yield break;
        }
        routeChoice = LevelChoice.Open(ship.transform, TravelToLevel, BGMManager.PlayLevelMusic);
    }

    void TravelToLevel(LevelId next)
    {
        CombatDecision.End();
        CloudTransition.Change(() => ResetDistrict(next));
    }

    /// <summary>Warp-Core loop: replay the current district from its first wave, keeping the run.</summary>
    public void RestartLevel()
    {
        BGMManager.PlayLevelMusic(LevelRun.Current);
        CombatDecision.End();
        CloudTransition.Change(() => ResetDistrict(null));
    }

    void ResetDistrict(LevelId? next, bool simulator = false)
    {
        if (GameOverState.Active)
            return;
        StopAllCoroutines();
        CancelInvoke();
        if (routeChoice != null)
            Destroy(routeChoice.gameObject);
        routeChoice = null;
        if (next.HasValue)
        {
            if (simulator)
            {
                var previous = FlightTrials.CurrentLeg.kit ?? FlightTrials.Active.kit;
                FlightTrials.AdvanceLeg();
                LevelRun.AdvanceSimulator(next.Value);
                ShipFrames.SwapSimulatorKit(ship.GetComponent<Shipcontroller>(), previous, FlightTrials.CurrentLeg.kit);
            }
            else
                LevelRun.Advance(next.Value);
            MetaProgress.EnterDistrict();
        }
        LevelEnvironment.Apply();
        foreach (var enemy in FindObjectsByType<AEnemy>())
            Destroy(enemy.gameObject);
        EraseAllBullets();
        foreach (var bullet in FindObjectsByType<ABullet>())
            bullet.Despawn();
        foreach (var pickup in FindObjectsByType<APickable>())
            Destroy(pickup.gameObject);
        foreach (var item in items)
            if (item != null && !(item.GetComponent<AItem>()?.Collected ?? false))
                Destroy(item);
        items.Clear();
        dialog.Hide();
        currentWaveNumber = 0;
        killLedger.Clear();
        currentWaveEnemies = 0;
        currentWaveKilledEnemies = 0;
        challenge.Reset();
        huntTarget = null;
        Array.Clear(encounterExpiry, 0, encounterExpiry.Length);
        skipOffered = false;
        // Unopened cargo follows the pilot across districts until claimed or destroyed.
        if (cargo != null)
            encounterExpiry[(int)EncounterKind.Cargo] = float.PositiveInfinity;
        DismissOffer();
        waveTime = clearTime = 0;
        currentUnit = null;
        currentWave = null;
        stopingWave = false;
        choosingRoute = false;
        CombatDecision.End();
        waves = FlightTrials.Active?.Sequence() ?? DistrictWaves.Sequence();
        activeMiniboss = null;
        minibossDeadline = -1;
        if (pendingCargo.Count > 0)
            plan.Insert(0, RunSlotKind.CargoReward);
        BeginWaves();
    }

    public void NotifyBossPhaseChange(int phase)
    {
        SoundManager.PlaySound(SoundManager.HasSynth("explosionLarge") ? "explosionLarge" : "enemyExplosion1", 0.12f);
    }
}
