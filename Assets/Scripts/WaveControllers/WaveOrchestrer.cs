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
    bool stopingWave = false;
    GameObject ship;
    Dialog dialog;
    [SerializeField] Warning warning;
    [SerializeField] private GameObject boss;

    List<GameObject> items = new List<GameObject>();
    bool isShopUp = false;
    GameObject claimedItem;
    int currShowing = -1;
    bool choosingRoute;
    LevelChoice routeChoice;
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
                enemy.GetComponent<AEnemy>().SetOrchestrer(this, currentWaveNumber);
                currentUnit = currentWave.getNext();
            }
            // Consecutive phrases overlap their exits: old bullets are part of the rhythm.
            // Decisions and bosses wait for enemies to finish, then explicitly clear hazards.
            string next=currentWaveNumber<waves.Length ? waves[currentWaveNumber] : "";
            bool nextCombat=int.TryParse(next,out _) || next=="miniboss";
            bool empty=currentUnit==null && AEnemy.Live.Count==0;
            clearTime=empty ? clearTime+Time.deltaTime : 0;
            bool phraseComplete=currentUnit==null && waveTime>=currentWave.duration && AEnemy.Live.Count<22;
            if (currentUnit == null && ((nextCombat && phraseComplete) || clearTime>=.12f))
            {

                if (stopingWave)
                {
                    isWaveComingOut = false;
                    stopingWave = false;
                } else
                {
                    GenerateWave();
                }
            }
        }
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
        if (currentWaveNumber >= waves.Length)
        {
            isWaveComingOut = false;
            return;
        }
        string currWaveType = waves[currentWaveNumber];
        if (currWaveType == "scoreShop" || currWaveType == "multiplierShop" || currWaveType == "reward")
        {
            EraseAllBullets();
            isWaveComingOut = false;
            isShopUp = true;
            currShowing = -1;
            OpenDecision(currWaveType);
        }
        else if (currWaveType == "boss")
        {
            // Hold the wave timer while the boss is up; the next wave starts when it dies.
            isWaveComingOut = false;
            StartCoroutine(BGMManager.FadeOutSong(2.9f));
            SoundManager.PlayCue("bossWarning");
            warning.blink();
            Invoke("SpawnBoss", 3f);
            
            
        }
        else
        {
            isWaveComingOut = true;
            currentWave = wavePool.getWave(currWaveType);
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
    void OpenDecision(string kind)
    {
        foreach(var enemy in new List<AEnemy>(AEnemy.Live))if(enemy!=null)Destroy(enemy.gameObject);
        decision=gameObject.AddComponent<DecisionShop>();
        decision.Open(this,ship.GetComponent<Shipcontroller>(),kind,()=>{isShopUp=false;GenerateWave();});
    }

    private void EraseAllBullets()
    {
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
        foreach (GameObject i in bullets)
        {
            i.GetComponent<AEnemyBullet>().OnTrigger();
        }
    }


    // Claim the closest eligible option once; rewards are applied only on arrival.
    public bool TryClaimItem(AItem candidate)=>decision!=null && decision.TryClaim(candidate);
    public void NotifyItemPicked(GameObject item){if(decision!=null)decision.Picked(item);}

    public void NotifyEnemyKilled(int waveNumber, bool isBoss = false)
    {
        if (isBoss)
        {
            if(choosingRoute || GameRules.playerDisabled)return;
            choosingRoute=true;
            isWaveComingOut=false;
            Camera.WhiteScreenJitter(0.5f);
            EraseAllBullets();
            StartCoroutine(BGMManager.FadeOutSong(1.4f));
            StartCoroutine(ChooseNextRoute());
        }
        else if (currentWaveNumber == waveNumber)
        {
            currentWaveKilledEnemies += 1;
        }
    }

    IEnumerator ChooseNextRoute()
    {
        // Let the boss explosion settle, then provide a safe space to steer.
        yield return new WaitForSeconds(.65f);
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
            items.Clear();claimedItem=null;isShopUp=false;currShowing=-1;dialog.Hide();
            currentWaveNumber=0;currentWaveEnemies=0;currentWaveKilledEnemies=0;
            waveTime=clearTime=0;currentUnit=null;currentWave=null;stopingWave=false;
            choosingRoute=false;CombatDecision.End();
            waves=DistrictWaves.Sequence();
        if(GetComponent<TunnelReflectors>()==null)gameObject.AddComponent<TunnelReflectors>();
        BeginWaves();
        });
    }

    public void NotifyBossPhaseChange(int phase)
    {
        SoundManager.PlayCue("enemyExplosion1");
        BGMManager.ChangeMood(2);
        //BGMManager.RestartSong();
    }
}
