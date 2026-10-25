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
    const float ClearBreather = 0.65f;
    public const float HandoffLead = .9f;
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
    public int ChallengeWavesRemaining=>challengeWavesRemaining;
    public EncounterKind[] EncounterOptions {get;private set;}
    public const float OpeningBreather = .8f;
    bool openingWavePending;
    float openingStartsAt;


    // Start is called before the first frame update
    void Start()
    {
        dialog = GameObject.FindGameObjectWithTag("Dialog").GetComponent<Dialog>();
        limits = Camera.getLimits();
        ship = GameObject.FindGameObjectWithTag("Player");
        wavePool = GetComponent<WavePool>();
        waves=DistrictWaves.Sequence();
        if(GetComponent<TunnelReflectors>()==null)gameObject.AddComponent<TunnelReflectors>();
        BeginWaves();
    }

    // Update is called once per frame
    void Update()
    {
        if(openingWavePending)
        {
            // Keep the first phrase clock at zero until the whole arena is visible.
            // This also covers route changes and the loop back to Plains.
            if(CloudTransition.Busy || !WavePreparation.Ready){openingStartsAt=-1;return;}
            if(GameRules.playerDisabled)return;
            if(openingStartsAt<0)openingStartsAt=Time.time+OpeningBreather;
            if(Time.time<openingStartsAt)return;
            openingWavePending=false;
            GenerateWave();
        }
        if (isWaveComingOut)
        {
            waveTime += Time.deltaTime;
            while (currentUnit != null && waveTime >= currentUnit.time)
            {
                Vector3 pos = new Vector3(currentUnit.position.x * limits[3], currentUnit.position.y * limits[0], 1);
                var type=DistrictWaves.EnemyType(currentUnit.enemy);
                GameObject enemy=type!=null ? DistrictEnemy.Spawn(type,pos).gameObject : Instantiate(currentUnit.GetUnit(),pos,Quaternion.identity);
                if(type!=null)enemy.GetComponent<DistrictEnemy>().FlyPhrase(currentUnit);
                if(challengeWave && type!=null)enemy.GetComponent<DistrictEnemy>().MakeChallenging();
                enemy.GetComponent<AEnemy>().SetOrchestrer(this, currentWaveNumber);
                currentUnit = currentWave.getNext();
            }
            // Consecutive phrases overlap their exits: old bullets are part of the rhythm.
            // Decisions and bosses wait for enemies to finish, then explicitly clear hazards.
            string next=currentWaveNumber<waves.Length ? waves[currentWaveNumber] : "";
            bool nextCombat=(int.TryParse(next,out _) || next=="miniboss") && !(challengeWave && challengeWavesRemaining==1);
            bool empty=currentUnit==null && AEnemy.Live.Count==0;
            clearTime=empty ? clearTime+Time.deltaTime : 0;
            bool miniboss=currentWave.units.Exists(unit=>unit.miniboss);
            bool phraseComplete=currentUnit==null && !miniboss && waveTime>=Mathf.Max(0,currentWave.duration-HandoffLead) && AEnemy.Live.Count<22;
            if (currentUnit == null && ((nextCombat && phraseComplete) || clearTime>=.12f))
            {

                if (stopingWave)
                {
                    isWaveComingOut = false;
                    stopingWave = false;
                } else
                {
                    FinishCombatWave();
                }
            }
        }
    }
    void FinishCombatWave()
    {
        isWaveComingOut=false;
        if(challengeWave)
        {
            challengeWave=false;challengeWavesRemaining--;
            if(challengeWavesRemaining==0)
            {
                // The third wave must be cleared, then pay out before any later combat starts.
                if(currentWaveNumber<waves.Length && waves[currentWaveNumber]=="encounterChoice")currentWaveNumber++;
                EraseAllBullets();OpenDecision("reward");return;
            }
        }
        GenerateWave();
    }

    public void BeginWaves()
    {
        if(currentWaveNumber==0)
        {
            isWaveComingOut=false;
            openingWavePending=true;
            openingStartsAt=-1;
        }
        else GenerateWave();
    }

    private void SpawnBoss()
    {
        // Camera.WhiteScreenJitter(0.5f);
        BGMManager.PlayBGM("boss");
        GameObject bossGO = DistrictEnemy.Spawn(DistrictWaves.BossType,new Vector3(0,Camera.UpLimit+3,1)).gameObject;
        bossGO.GetComponent<AEnemy>().SetOrchestrer(this, currentWaveNumber);
        bossGO.transform.position = new Vector3(0f, 11.52f, 1f);
        EraseAllBullets();
    }

    void GenerateWave()
    {
        if(GameOverState.Active)return;
        if (currentWaveNumber >= waves.Length)
        {
            isWaveComingOut = false;
            return;
        }
        string currWaveType = waves[currentWaveNumber];
        if(currWaveType=="encounterChoice")
        {
            EraseAllBullets();isWaveComingOut=false;OpenEncounterChoice();
        }
        else if (currWaveType == "coinShop" || currWaveType == "scoreShop" || currWaveType == "multiplierShop" || currWaveType == "reward")
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
            challengeWave=challengeWavesRemaining>0;
            string planned=challengeWave && int.TryParse(currWaveType,out int budget) ? Mathf.Min(20,budget+3).ToString() : currWaveType;
            currentWave = wavePool.getWave(planned);
            currentWaveEnemies = currentWave.units.Count;
            currentWaveKilledEnemies = 0;
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
    void OpenEncounterChoice()
    {
        int remaining=0;
        for(int i=currentWaveNumber+1;i<waves.Length;i++)if(int.TryParse(waves[i],out _) || waves[i]=="miniboss")remaining++;
        EncounterOptions=EncounterChoice.Draw(unchecked(LevelRun.Seed+LevelRun.Stage*971+currentWaveNumber*7919),remaining>=3 && challengeWavesRemaining==0);
        CombatDecision.Begin();ship.GetComponent<ShipLoadout>().ResetSelection();
        foreach(var weapon in ship.GetComponentsInChildren<AWeapon>())weapon.StopFiring();
        routeChoice=LevelChoice.OpenEncounter(ship.transform,Array.ConvertAll(EncounterOptions,EncounterChoice.Label),Array.ConvertAll(EncounterOptions,EncounterChoice.Accent),index=>ResolveEncounter(EncounterOptions[index]),Array.ConvertAll(EncounterOptions,EncounterChoice.Icon));
    }
    void ResolveEncounter(EncounterKind kind)
    {
        if(GameOverState.Active)return;
        if(routeChoice!=null)Destroy(routeChoice.gameObject);routeChoice=null;EncounterOptions=null;
        if(kind==EncounterKind.Shop || kind==EncounterKind.MultShop || kind==EncounterKind.Item)
        {
            OpenDecision(kind==EncounterKind.Shop ? "coinShop" : kind==EncounterKind.MultShop ? "multiplierShop" : "reward");return;
        }
        CombatDecision.End();
        if(kind==EncounterKind.Challenge)challengeWavesRemaining=3;
        else
        {
            var pickup=Instantiate(Resources.Load<GameObject>("Pickables/life"),ship.transform.position+Vector3.up*2.4f,Quaternion.identity);
            pickup.GetComponent<Life>().maxHealthFraction=.5f;
        }
        Invoke(nameof(GenerateWave),ClearBreather);
    }
    void OpenDecision(string kind)
    {
        foreach(var enemy in new List<AEnemy>(AEnemy.Live))if(enemy!=null)Destroy(enemy.gameObject);
        decision=gameObject.AddComponent<DecisionShop>();
        decision.Open(this,ship.GetComponent<Shipcontroller>(),kind,GenerateWave);
    }

    private void EraseAllBullets()
    {
        CombatRound.ClearAll();
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
        foreach (GameObject i in bullets)
        {
            var bullet=i.GetComponent<AEnemyBullet>();if(bullet!=null)Destroy(i);
        }
    }


    // Claim the closest eligible option once; rewards are applied only on arrival.
    public bool TryClaimItem(AItem candidate)=>decision!=null && decision.TryClaim(candidate);
    public void NotifyItemPicked(GameObject item){if(decision!=null)decision.Picked(item);}

    public void NotifyEnemyKilled(int waveNumber,bool isBoss=false,Vector3? bossPosition=null)
    {
        if (isBoss)
        {
            if(choosingRoute || GameOverState.Active)return;
            choosingRoute=true;
            isWaveComingOut=false;
            EraseAllBullets();
            StartCoroutine(BossReward(bossPosition ?? new Vector3(0,Camera.UpLimit*.5f,1)));
        }
        else if (currentWaveNumber == waveNumber)
        {
            currentWaveKilledEnemies += 1;
        }
    }
    IEnumerator BossReward(Vector3 position)
    {
        // The music has cut on the breakup; let its main blast open before the item falls.
        CombatDecision.Begin();yield return new WaitForSeconds(.8f);
        if(GameOverState.Active)yield break;
        foreach(var enemy in FindObjectsByType<AEnemy>())Destroy(enemy.gameObject);
        EraseAllBullets();
        position=new Vector3(Mathf.Clamp(position.x,Camera.LeftLimit+.8f,Camera.RightLimit-.8f),
            Mathf.Clamp(position.y,Camera.DownLimit+2,Camera.UpLimit-1.5f),1);
        decision=gameObject.AddComponent<DecisionShop>();
        decision.Open(this,ship.GetComponent<Shipcontroller>(),"bossReward",()=>
        {decision=null;StartCoroutine(ChooseNextRoute());},position);
    }

    IEnumerator ChooseNextRoute()
    {
        CombatDecision.Begin();
        // This coroutine is reached only through the mandatory item's actual pickup callback.
        yield return new WaitForSeconds(.2f);
        foreach(var enemy in FindObjectsByType<AEnemy>())Destroy(enemy.gameObject);
        EraseAllBullets();
        CombatDecision.Begin(); // Freeze multiplier and fire while steering to the next route.
        if(LevelRun.ReturnsToPlains){TravelToLevel(LevelId.Fields);yield break;}
        routeChoice=LevelChoice.Open(ship.transform,TravelToLevel);
    }

    void TravelToLevel(LevelId next)
    {
        BGMManager.PlayLevelMusic(next);
        CombatDecision.End();
        CloudTransition.Change(()=>
        {
            if(GameOverState.Active)return;
            StopAllCoroutines();CancelInvoke();
            if(routeChoice!=null)Destroy(routeChoice.gameObject);
            routeChoice=null;
            LevelRun.Advance(next);
            LevelEnvironment.Apply();
            foreach(var enemy in FindObjectsByType<AEnemy>())Destroy(enemy.gameObject);
            EraseAllBullets();
            foreach(var bullet in FindObjectsByType<ABullet>())Destroy(bullet.gameObject);
            foreach(var pickup in FindObjectsByType<APickable>())Destroy(pickup.gameObject);
            foreach(var item in items)if(item!=null)Destroy(item);
            items.Clear();dialog.Hide();
            currentWaveNumber=0;currentWaveEnemies=0;currentWaveKilledEnemies=0;
            challengeWavesRemaining=0;challengeWave=false;EncounterOptions=null;
            waveTime=clearTime=0;currentUnit=null;currentWave=null;stopingWave=false;
            choosingRoute=false;CombatDecision.End();
            waves=DistrictWaves.Sequence();
            if(GetComponent<TunnelReflectors>()==null)gameObject.AddComponent<TunnelReflectors>();
            BeginWaves();
        });
    }

    public void NotifyBossPhaseChange(int phase)
    {
        SoundManager.PlaySound("enemyExplosion1", 0.12f);
        BGMManager.ChangeMood(2);
        //BGMManager.RestartSong();
    }
}
