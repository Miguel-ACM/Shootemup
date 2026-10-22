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


    // Start is called before the first frame update
    void Start()
    {
        dialog = GameObject.FindGameObjectWithTag("Dialog").GetComponent<Dialog>();
        limits = Camera.getLimits();
        ship = GameObject.FindGameObjectWithTag("Player");
        wavePool = GetComponent<WavePool>();
        BeginWaves();
    }

    // Update is called once per frame
    void Update()
    {
        if (isWaveComingOut)
        {
            waveTime += Time.deltaTime;
            while (currentUnit != null && waveTime >= currentUnit.time)
            {
                GameObject enemy = currentUnit.GetUnit();
                Vector3 pos = new Vector3(currentUnit.position.x * limits[3], currentUnit.position.y * limits[0], 1);
                enemy = Instantiate(enemy, pos, Quaternion.identity);
                enemy.GetComponent<AEnemy>().SetOrchestrer(this, currentWaveNumber);
                currentUnit = currentWave.getNext();
            }
            // Never skip a scheduled reinforcement, but avoid waiting on an empty field.
            bool cleared = currentUnit == null && currentWaveKilledEnemies >= currentWaveEnemies;
            clearTime = cleared ? clearTime + Time.deltaTime : 0f;
            if (currentUnit == null && (waveTime >= currentWave.duration || clearTime >= ClearBreather))
            {

                if (stopingWave)
                {
                    isWaveComingOut = false;
                    stopingWave = false;
                } else
                {
                    float overflow = Mathf.Max(0f, waveTime - currentWave.duration);
                    GenerateWave();
                    waveTime = overflow;
                }
            }
        }
        if (isShopUp)
        {
            int nearest = 0;
            float distance = Mathf.Infinity;
            for (int i = 0; i < items.Count; i++)
            {
                GameObject item = items[i];
                Vector3 playerDistance = item.transform.position - ship.transform.position;
                float xDistance = Mathf.Abs(playerDistance.x);
                if (ship.transform.position.y > limits[0]*0.1f && distance > xDistance)
                {
                    distance = xDistance;
                    nearest = i;
                }
                
            }
            if (ship.transform.position.y >= limits[0] * 0.1f)
            {
                if (currShowing != nearest)
                {
                    AItem itemScript = items[nearest].GetComponent<AItem>();
                    string[] data = itemScript.GetData();
                    dialog.ShowItemDesc(data[0], data[1], data[2], itemScript.rarity);
                    currShowing = nearest;
                }
            }
            else
            {
                currShowing = -1;
                dialog.Hide();
            }
        }
    }

    public void BeginWaves()
    {
        GenerateWave();
    }

    private void SpawnBoss()
    {
        // Camera.WhiteScreenJitter(0.5f);
        BGMManager.PlayBGM("boss");
        GameObject bossGO = Instantiate(boss);
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
        if (currWaveType == "shop")
        {
            EraseAllBullets();
            isWaveComingOut = false;
            isShopUp = true;
            currShowing = -1;
            GenerateShop();
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

    void GenerateShop()
    {
        Vector3[] spawnPoints;
        if (GameRules.numShopItems == 1)
            spawnPoints = new Vector3[] { new Vector3(0f, 1.4f) };
        else if (GameRules.numShopItems == 2)
            spawnPoints = new Vector3[] { new Vector3(-0.5f, 1.3f), new Vector3(0.5f, 1.3f) };
        else if (GameRules.numShopItems == 3)
            spawnPoints = new Vector3[] { new Vector3(-0.65f, 1.3f), new Vector3(0.65f, 1.3f), new Vector3(0f, 1.4f) };
        else
            spawnPoints = new Vector3[] { new Vector3(-0.75f, 1.3f), new Vector3(0.75f, 1.3f), new Vector3(0.25f, 1.4f), new Vector3(-0.25f, 1.4f) };

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            // GameObject itemObject = ItemPool.GetItem(out string rarity);
            string itemString = ItemPool.GetItem(out string rarity);
            GameObject itemObject = Instantiate(Resources.Load<GameObject>("Items/Item"), new Vector3(spawnPoints[i].x * limits[3], spawnPoints[i].y * limits[0], 1), Quaternion.identity);
            Type type = Type.GetType(itemString);
            AItem item = (AItem) itemObject.AddComponent(type);//itemObject.GetComponent<type>();
            items.Add(itemObject);
            item.SetRarity(rarity);
            item.Register(this);
            item.GoToPosition(new Vector3(itemObject.transform.position.x, (spawnPoints[i].y - 0.5f) * limits[0], itemObject.transform.position.z));
            
            //spawnPoints[i] = new Vector3(currentSpread, -Mathf.Abs(currentSpread * spreadInclination) + spreadOffset, 0);
        }
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
    public bool TryClaimItem(AItem candidate)
    {
        if(!isShopUp || !items.Contains(candidate.gameObject))return false;
        if(claimedItem!=null)return claimedItem==candidate.gameObject;
        GameObject nearest=null;float distance=float.PositiveInfinity;
        foreach(var item in items)
        {
            if(item==null)continue;
            float d=Vector2.SqrMagnitude(item.transform.position-ship.transform.position);
            if(d<distance){distance=d;nearest=item;}
        }
        if(nearest!=candidate.gameObject)return false;
        claimedItem=nearest;return true;
    }

    public void NotifyItemPicked(GameObject item)
    {

        foreach (GameObject i in items)
        {
            if (i != item)
            {
                try
                {
                    if(i!=null)
                    {
                        var option=i.GetComponent<AItem>();
                        if(option!=null)option.Dismiss();else Destroy(i);
                    }
                }
                catch { }
            }
        }
        items.Clear();
        claimedItem=null;
        isShopUp = false;
        dialog.Hide();

        GenerateWave();
    }

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
        GameRules.playerDisabled=true; // Movement remains available; firing is held during the choice.
        routeChoice=LevelChoice.Open(ship.transform,TravelToLevel);
    }

    void TravelToLevel(LevelId next)
    {
        BGMManager.PlayLevelMusic(next);
        GameRules.playerDisabled=false;
        CloudTransition.Change(()=>
        {
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
            choosingRoute=false;GameRules.playerDisabled=false;
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
