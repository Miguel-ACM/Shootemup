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
    bool stopingWave = false;
    GameObject ship;
    Dialog dialog;
    [SerializeField] Warning warning;
    [SerializeField] private GameObject boss;

    List<GameObject> items = new List<GameObject>();
    bool isShopUp = false;
    int currShowing = -1;


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
            while (currentUnit != null && waveTime > currentUnit.time)
            {
                GameObject enemy = currentUnit.GetUnit();
                Vector3 pos = new Vector3(currentUnit.position.x * limits[3], currentUnit.position.y * limits[0], 1);
                enemy = Instantiate(enemy, pos, Quaternion.identity);
                enemy.GetComponent<AEnemy>().SetOrchestrer(this, currentWaveNumber);
                currentUnit = currentWave.getNext();
            }
            if (waveTime > currentWave.duration)
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
        BGMManager.PlayBGM("stage1bossOST");
        GameObject bossGO = Instantiate(boss);
        bossGO.GetComponent<AEnemy>().SetOrchestrer(this, currentWaveNumber);
        bossGO.transform.position = new Vector3(0f, 11.52f, 1f);
        EraseAllBullets();
    }

    void GenerateWave()
    {
        
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
            
            StartCoroutine(BGMManager.FadeOutSong(2.9f));
            SoundManager.PlaySound("bossWarning", 0.24f);
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


    public void NotifyItemPicked(GameObject item)
    {

        foreach (GameObject i in items)
        {
            if (i != item)
            {
                try
                {
                    Destroy(i);
                }
                catch { }
            }
        }
        items.Clear();
        isShopUp = false;
        dialog.Hide();

        GenerateWave();
    }

    public void NotifyEnemyKilled(int waveNumber, bool isBoss = false)
    {
        if (isBoss)
        {
            Camera.WhiteScreenJitter(0.5f);
            EraseAllBullets();
            BGMManager.SetLoop(false);
            StartCoroutine(BGMManager.FadeOutSongAndPlay(1.4f, "stageComplete", pitch: 1.0f));
        }
        else if (currentWaveNumber == waveNumber)
        {
            currentWaveKilledEnemies += 1;
            if (currentWaveKilledEnemies == currentWaveEnemies)
            {
                //Early wave termination when all enemies are killed
                if (waveTime + 1.5f < currentWave.duration)
                {
                    waveTime = currentWave.duration - 1.5f;
                }
            }
        }
    }

    public void NotifyBossPhaseChange(int phase)
    {
        SoundManager.PlaySound("enemyExplosion1", 0.12f);
        BGMManager.ChangePitch(1.1f);
        //BGMManager.RestartSong();
    }
}
