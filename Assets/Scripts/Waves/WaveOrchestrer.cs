using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class WaveOrchestrer : MonoBehaviour
{

    WavePool wavePool;
    bool isWaveComingOut = false;
    public string[] waves;
    int currentWaveNumber = 0;
    int currentWaveEnemies = 0;
    int currentWaveKilledEnemies = 0;
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
    int challengeWavesRemaining;
    bool challengeWave;
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

    public const float OpeningBreather = .8f;
    bool openingWavePending;
    float openingStartsAt;
    DistrictMiniboss activeMiniboss;
    float minibossDeadline = -1;
    bool minibossVictory;
    /// <summary>An ordinary combat wave ended: (enemies in the wave, enemies destroyed).</summary>
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
        waves = DistrictWaves.Sequence();
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
            if (offerPending && waveTime >= offerAt)
                OpenEncounterChoice();
            // Consecutive phrases overlap their exits: old bullets are part of the rhythm.
            // Decisions and bosses wait for enemies to finish, then explicitly clear hazards.
            string next = FollowingSlot(currentWaveNumber);
            bool nextCombat = int.TryParse(next, out _) && !(challengeWave && challengeWavesRemaining == 1);
            bool empty = currentUnit == null && AEnemy.Live.Count == 0;
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
            bool phraseComplete = currentUnit == null && !miniboss && waveTime >= handoffAt && AEnemy.Live.Count < 22;
            if (currentUnit == null && ((nextCombat && phraseComplete) || clearTime >= .12f))
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
            CombatWaveFinished?.Invoke(currentWaveEnemies, currentWaveKilledEnemies);
        if (challengeWave)
        {
            challengeWave = false;
            challengeWavesRemaining--;
            if (challengeWavesRemaining == 0)
            {
                // The third wave must be cleared, then pay out before any later combat starts.
                if (currentWaveNumber < waves.Length && waves[currentWaveNumber] == "encounterChoice")
                    currentWaveNumber++;
                EraseAllBullets();
                OpenDecision("challengeReward");
                return;
            }
        }
        GenerateWave();
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
        // Camera.WhiteScreenJitter(0.5f);
        BGMManager.PlayBGM("boss");
        GameObject bossGO = DistrictEnemy.Spawn(DistrictWaves.BossType, new Vector3(0, Camera.UpLimit + 3, 1)).gameObject;
        bossGO.GetComponent<AEnemy>().SetOrchestrer(this, currentWaveNumber);
        bossGO.transform.position = new Vector3(0f, 11.52f, 1f);
        EraseAllBullets();
    }

    void GenerateWave()
    {
        if (GameOverState.Active)
            return;
        if (currentWaveNumber >= waves.Length)
        {
            isWaveComingOut = false;
            return;
        }
        string currWaveType = waves[currentWaveNumber];
        if (currWaveType == "minibossChoice" && !minibossVictory)
        {
            currentWaveNumber++;
            GenerateWave();
            return;
        }
        if (currWaveType == "encounterChoice" || currWaveType == "minibossChoice")
        {
            offerPending = true;
            currentWaveNumber++;
            GenerateWave();
            return;
        }
        if (!int.TryParse(currWaveType, out _))
            DismissOffer();
        if (currWaveType == "coinShop" || currWaveType == "scoreShop" || currWaveType == "multiplierShop" || currWaveType == "reward" || currWaveType == "pact")
        {
            EraseAllBullets();
            isWaveComingOut = false;
            OpenDecision(currWaveType);
        }
        else if (currWaveType == "boss")
        {
            // Hold the wave timer while the boss is up; the next wave starts when it dies.
            isWaveComingOut = false;
            StartCoroutine(BGMManager.FadeOutSong(2.9f));
            SoundManager.PlaySound("bossWarning", 0.18f);
            warning.blink();
            Invoke("SpawnBoss", 3f);


        }
        else
        {
            isWaveComingOut = true;
            challengeWave = challengeWavesRemaining > 0 && int.TryParse(currWaveType, out _);
            currentWave = wavePool.getWave(currWaveType);
            string following = FollowingSlot(currentWaveNumber + 1);
            handoffAt = int.TryParse(following, out _) ? WaveHandoff.At(currentWave, wavePool.getWave(following)) : currentWave.duration;
            offerAt = handoffAt * .5f;
            currentWaveEnemies = currentWave.units.Count;
            currentWaveKilledEnemies = 0;
            if (currWaveType == "miniboss")
            {
                minibossVictory = false;
                minibossDeadline = -1;
            }
            waveTime = 0;
            clearTime = 0;
            currentUnit = currentWave.getNext();
        }
        currentWaveNumber += 1;
        if (currentWaveNumber >= waves.Length)
        {
            stopingWave = true;
        }
    }

    DecisionShop decision;
    // Choice slots are transparent to wave handoffs: the offer rides along with the next wave.
    string FollowingSlot(int index)
    {
        while (index < waves.Length && (waves[index] == "encounterChoice" || waves[index] == "minibossChoice"))
            index++;
        return index < waves.Length ? waves[index] : "";
    }

    void OpenEncounterChoice()
    {
        offerPending = false;
        // The wave in progress counts, because a challenge starts at once.
        int remaining = 1;
        for (int i = currentWaveNumber; i < waves.Length; i++)
            if (int.TryParse(waves[i], out _))
                remaining++;
        var player = ship.GetComponent<Shipcontroller>();
        int seed = unchecked(LevelRun.Seed + LevelRun.Stage * 971 + currentWaveNumber * 7919);
        // Every second district offers one pact hexagon, at its first reward break.
        bool pactAvailable = PactPool.Due && PactPool.HexStage != LevelRun.Stage && PactPool.HasEligible(player);
        var options = EncounterChoice.Draw(seed, remaining >= 3 && challengeWavesRemaining == 0, player.health, player.maxHealth, pactAvailable);
        // A pick needs a real choice: with a single option, nothing is offered at this break.
        if (options.Length < 2)
            return;
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
        if (kind == EncounterKind.Shop || kind == EncounterKind.MultShop || kind == EncounterKind.Item || kind == EncounterKind.Pact)
        {
            // Items, shops and pacts open at the next wave break, after the current phrase is cleared.
            var sequence = new List<string>(waves);
            sequence.Insert(Mathf.Min(currentWaveNumber, sequence.Count), kind == EncounterKind.Shop ? "coinShop" : kind == EncounterKind.MultShop ? "multiplierShop" : kind == EncounterKind.Pact ? "pact" : "reward");
            waves = sequence.ToArray();
            return;
        }
        if (kind == EncounterKind.Challenge)
        {
            // The wave in progress is the first of the three.
            challengeWavesRemaining = 3;
            challengeWave = isWaveComingOut && currentWave != null && !currentWave.units.Exists(unit => unit.miniboss);
            if (challengeWave)
                foreach (var enemy in AEnemy.Live)
                    if (enemy is DistrictEnemy districtEnemy && !(enemy is DistrictMiniboss))
                        districtEnemy.MakeChallenging();
        }
        else
            HealthRecovery.Begin(ship.GetComponent<Shipcontroller>(), .5f);
    }

    void OpenDecision(string kind)
    {
        DismissOffer();
        foreach (var enemy in new List<AEnemy>(AEnemy.Live))
            if (enemy != null)
                Destroy(enemy.gameObject);
        decision = gameObject.AddComponent<DecisionShop>();
        decision.Open(this, ship.GetComponent<Shipcontroller>(), kind, GenerateWave);
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
            choosingRoute = true;
            isWaveComingOut = false;
            EraseAllBullets();
            StartCoroutine(BossReward(bossPosition ?? new Vector3(0, Camera.UpLimit * .5f, 1)));
        }
        else if (currentWaveNumber == waveNumber)
        {
            if (defeated)
                currentWaveKilledEnemies += 1;
        }
    }

    IEnumerator BossReward(Vector3 position)
    {
        // The music has cut on the breakup; let its main blast open before the item falls.
        CombatDecision.Begin();
        yield return new WaitForSeconds(.8f);
        if (GameOverState.Active)
            yield break;
        foreach (var enemy in FindObjectsByType<AEnemy>())
            Destroy(enemy.gameObject);
        EraseAllBullets();
        position = new Vector3(Mathf.Clamp(position.x, Camera.LeftLimit + .8f, Camera.RightLimit - .8f),
            Mathf.Clamp(position.y, Camera.DownLimit + 2, Camera.UpLimit - 1.5f), 1);
        decision = gameObject.AddComponent<DecisionShop>();
        decision.Open(this, ship.GetComponent<Shipcontroller>(), "bossReward", () =>
           {
               decision = null;
               StartCoroutine(ChooseNextRoute());
           }, position);
    }

    /// <summary>Debug: the next wave break opens a pact offer.</summary>
    public void ForcePactNext()
    {
        var sequence = new List<string>(waves);
        sequence.Insert(Mathf.Min(currentWaveNumber, sequence.Count), "pact");
        waves = sequence.ToArray();
    }

    IEnumerator ChooseNextRoute()
    {
        CombatDecision.Begin();
        // This coroutine is reached only through the mandatory item's actual pickup callback.
        yield return new WaitForSeconds(.2f);
        foreach (var enemy in FindObjectsByType<AEnemy>())
            Destroy(enemy.gameObject);
        EraseAllBullets();
        CombatDecision.Begin(); // Freeze multiplier and fire while steering to the next route.
        if (LevelRun.AutomaticNext.HasValue)
        {
            TravelToLevel(LevelRun.AutomaticNext.Value);
            yield break;
        }
        routeChoice = LevelChoice.Open(ship.transform, TravelToLevel);
    }

    void TravelToLevel(LevelId next)
    {
        BGMManager.PlayLevelMusic(next);
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
            LevelRun.Advance(next.Value);
        LevelEnvironment.Apply();
        foreach (var enemy in FindObjectsByType<AEnemy>())
            Destroy(enemy.gameObject);
        EraseAllBullets();
        foreach (var bullet in FindObjectsByType<ABullet>())
            Destroy(bullet.gameObject);
        foreach (var pickup in FindObjectsByType<APickable>())
            Destroy(pickup.gameObject);
        foreach (var item in items)
            if (item != null)
                Destroy(item);
        items.Clear();
        dialog.Hide();
        currentWaveNumber = 0;
        currentWaveEnemies = 0;
        currentWaveKilledEnemies = 0;
        challengeWavesRemaining = 0;
        challengeWave = false;
        DismissOffer();
        waveTime = clearTime = 0;
        currentUnit = null;
        currentWave = null;
        stopingWave = false;
        choosingRoute = false;
        CombatDecision.End();
        waves = DistrictWaves.Sequence();
        activeMiniboss = null;
        minibossDeadline = -1;
        BeginWaves();
    }

    public void NotifyBossPhaseChange(int phase)
    {
        SoundManager.PlaySound(SoundManager.HasSynth("explosionLarge") ? "explosionLarge" : "enemyExplosion1", 0.12f);
        BGMManager.ChangeMood(2);
        //BGMManager.RestartSong();
    }
}
