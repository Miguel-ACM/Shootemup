using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WaveOrchestrer : MonoBehaviour
{

    WavePool wavePool;
    public RunPhase Phase { get; private set; }
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
    public EncounterKind[] EncounterOptions
    {
        get; private set;
    }

    // Encounter slots are offered half-way through the following combat wave, without pausing it.
    bool offerPending;
    float offerAt;
    RewardHexes rewardOffer;
    public RewardHexes RewardOffer => rewardOffer;
    CargoEscort cargo;
    CargoEscort cargoDelivery;
    string cargoRewardKey, cargoRewardRarity;
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
    /// <summary>Every enemy of an ordinary wave resolved, in wave order: (total, destroyed).</summary>
    public static event Action<int, int> CombatWaveFinished;
    public float MinibossRemaining => minibossDeadline < 0 ? 20 : Mathf.Max(0, minibossDeadline - Time.time);
    public bool MinibossVictory => minibossVictory;


    // Start is called before the first frame update
    void Start()
    {
        dialog = GameObject.FindGameObjectWithTag("Dialog").GetComponent<Dialog>();
        // This orchestrator lives in the additive Run scene; the warning banner belongs to the core's UI.
        if (warning == null)
            warning = FindAnyObjectByType<Warning>(FindObjectsInactive.Include);
        limits = Camera.getLimits();
        ship = GameObject.FindGameObjectWithTag("Player");
        wavePool = GetComponent<WavePool>();
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
                    if (challengeWave)
                        districtEnemy.MakeChallenging();
                    districtEnemy.FlyPhrase(currentUnit);
                }
                if (enemy.TryGetComponent<DistrictMiniboss>(out var mini))
                    activeMiniboss = mini;
                enemy.GetComponent<AEnemy>().SetOrchestrer(this, currentWaveNumber);
                currentUnit = currentWave.getNext();
            }
            if (offerPending && !TunnelPassage.Active && (waveTime >= offerAt || currentUnit == null && AEnemy.Live.Count == 0))
                OpenEncounterChoice();
            // Consecutive phrases overlap their exits: old bullets are part of the rhythm.
            // Decisions and bosses wait for enemies to finish, then explicitly clear hazards.
            var next = plan.Following(currentWaveNumber);
            bool nextCombat = next.Kind == RunSlotKind.Combat && !(challengeWave && challengeWavesRemaining == 1) &&
                !(FlightTrials.EscortsCargo && cargo != null && cargo.WavesRemaining == 1);
            bool empty = currentUnit == null && AEnemy.Live.Count == 0 && !TunnelPassage.Active;
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
            bool phraseComplete = currentUnit == null && !miniboss && waveTime >= handoffAt && AEnemy.Live.Count < 22 && !TunnelPassage.Active;
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
        cargoRewardKey = key;
        cargoRewardRarity = rarity;
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
            isWaveComingOut = false;
            Phase = RunPhase.BossWarning;
            StartCoroutine(BossEntrance());
        }
        else
        {
            isWaveComingOut = true;
            challenge.BeginWave(kind == RunSlotKind.Combat);
            currentWave = wavePool.getWave(slot.Key);
            var following = plan.Following(currentWaveNumber + 1);
            handoffAt = following.Kind == RunSlotKind.Combat ? WaveHandoff.At(currentWave, wavePool.getWave(following.Key)) : currentWave.duration;
            offerAt = handoffAt * .5f;
            currentWaveEnemies = currentWave.units.Count;
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
        var options = EncounterChoice.Draw(seed, remaining >= 3 && challengeWavesRemaining == 0, player.health, player.maxHealth,
            pactAvailable, GameRules.luck, tradeable.Count > 0, canReforge,
            cargo == null && cargoRewardKey == null && remaining >= CargoEscort.MinimumOfferWaves, !PactPool.GlassHull);
        if (Array.IndexOf(options, EncounterKind.Pact) >= 0)
            PactPool.HexStage = LevelRun.Stage;
        EncounterOptions = options;
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
        encounterExpiry[(int)kind] = kind == EncounterKind.Health || kind == EncounterKind.Salvage || EncounterChoice.IsStatReward(kind)
            ? Time.time + 5 : float.PositiveInfinity;
        if (EncounterChoice.IsStatReward(kind))
        {
            EncounterChoice.ApplyStatReward(kind, ship.GetComponent<Shipcontroller>());
            ItemSound.Play("ready", .18f);
            PSXImpactEffects.Burst(ship.transform.position, EncounterChoice.Accent(kind));
            return;
        }
        if (kind == EncounterKind.Salvage)
        {
            RunCoins.Add(EncounterChoice.SalvageCoins(LevelRun.Tier(LevelRun.Current), GameRules.luck));
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
            // The wave in progress is the first of the three.
            challenge.Start(isWaveComingOut && currentWave != null && !currentWave.units.Exists(unit => unit.miniboss));
            if (challengeWave)
                foreach (var enemy in AEnemy.Live)
                    if (enemy is DistrictEnemy districtEnemy && !(enemy is DistrictMiniboss))
                        districtEnemy.MakeChallenging();
        }
        else
            HealthRecovery.Begin(ship.GetComponent<Shipcontroller>(), .5f);
    }

    void OpenDecision(RunSlotKind kind)
    {
        Phase = RunPhase.Decision;
        DismissOffer();
        foreach (var enemy in new List<AEnemy>(AEnemy.Live))
            if (enemy != null)
                Destroy(enemy.gameObject);
        if (kind == RunSlotKind.CargoReward && cargoRewardKey != null)
        {
            StartCoroutine(OpenCargoReward(GenerateWave));
            return;
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
        });
        void Clear(EncounterKind encounter) => encounterExpiry[(int)encounter] = 0;
    }

    IEnumerator OpenCargoReward(Action completed)
    {
        Phase = RunPhase.Decision;
        CombatDecision.Begin();
        EraseAllBullets();
        while (cargoDelivery != null && !GameOverState.Active) yield return null;
        if (GameOverState.Active) yield break;
        var key = cargoRewardKey;
        var rarity = cargoRewardRarity;
        cargoDelivery = null;
        cargoRewardKey = cargoRewardRarity = null;
        bool picked = false;
        decision = gameObject.AddComponent<DecisionShop>();
        decision.Open(this, ship.GetComponent<Shipcontroller>(), RunSlotKind.CargoReward, () =>
        {
            decision = null;
            picked = true;
            completed?.Invoke();
        }, reservedKey: key, reservedRarity: rarity);
        while (!picked && !GameOverState.Active) yield return null;
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
            MetaProgress.DefeatBoss();
            choosingRoute = true;
            isWaveComingOut = false;
            EraseAllBullets();
            if (FlightTrials.Active != null)
            {
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
        if (cargoRewardKey != null)
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
        CombatDecision.Begin();
        // Non-final districts continue after collecting or skipping their boss reward.
        yield return new WaitForSeconds(.2f);
        foreach (var enemy in FindObjectsByType<AEnemy>())
            Destroy(enemy.gameObject);
        EraseAllBullets();
        CombatDecision.Begin(); // Freeze multiplier and fire while steering to the next route.
        // Until Void opens, Hell's boss reward ends the campaign.
        if (LevelRun.FinalDistrict || FlightTrials.Active != null ||
            LevelRun.AutomaticNext.HasValue && !MetaProgress.RouteOpen(LevelRun.AutomaticNext.Value))
        {
            GameOverState.Show(true);
            yield break;
        }
        if (LevelRun.AutomaticNext.HasValue)
        {
            BGMManager.PlayLevelMusic(LevelRun.AutomaticNext.Value);
            TravelToLevel(LevelRun.AutomaticNext.Value);
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

    void ResetDistrict(LevelId? next)
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
        Array.Clear(encounterExpiry, 0, encounterExpiry.Length);
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
        if (cargoRewardKey != null)
            plan.Insert(0, RunSlotKind.CargoReward);
        BeginWaves();
    }

    public void NotifyBossPhaseChange(int phase)
    {
        SoundManager.PlaySound(SoundManager.HasSynth("explosionLarge") ? "explosionLarge" : "enemyExplosion1", 0.12f);
    }
}
