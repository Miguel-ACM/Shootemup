using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public abstract class AEnemy : MonoBehaviour
{

    public float health = 20f;

    bool dying = false;
    public float size = 0.65f;

    public int scoreOnKill;
    public float lifeDropChance = 0.1f;
    public float manaDropChance = 0.1f;
    public float moneyDropChance = 0.1f;
    public int[] minmaxLife = new int[] { 1, 1 };
    public int[] minmaxMoney = new int[] { 1, 1 };
    public int[] minmaxMana = new int[] { 1, 1 };
    protected bool isBoss = false;

    Shipcontroller s;

    WaveOrchestrer orchestrer = null;
    int orchesterWaveNumber = 0;


    bool isBlinking = false;
    new SpriteRenderer renderer;
    float blinkSeconds = 0.12f;
    float blinkTimeout = 0f;
    float blinkInterval = 0.03f;

    public void SetOrchestrer(WaveOrchestrer o, int waveNumber)
    {
        orchestrer = o;
        orchesterWaveNumber = waveNumber;
    }


    protected virtual void Start()
    {
        s = GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();
        renderer = GetComponent<SpriteRenderer>();
    }

    protected void SetHealth(float h)
    {
        health = h * GameRules.enemyHealth;
    }

    /*protected virtual void Move()
    {
        rigidBody.velocity = direction * speed * GameRules.enemySpeed;
    }*/

    void InstantitatePickables(string pickable, int num)
    {
        for (int i = 0; i<num; i++)
        {
            Vector3 pos = Utils.RandomVectorInCircle(size, size);
            Instantiate(Resources.Load<GameObject>("Pickables/" + pickable), transform.position + pos, Quaternion.identity);

        }
    }

    void DropItems()
    {
        int numDrops = 0;
        if (Random.Range(0f, 1f) < (GameRules.itemLuck * lifeDropChance * s.GetHealthChanceMultiplier()))
        {
            numDrops = minmaxLife[0];
            if (minmaxLife[0] != minmaxLife[1])
            {
                numDrops = Random.Range(minmaxLife[0], minmaxLife[1] + 1);
            }
            numDrops += GameRules.morePickables;
            InstantitatePickables("life", numDrops);

        }
        if (Random.Range(0f, 1f) < (GameRules.itemLuck * manaDropChance * s.GetManaChanceMultiplier()))
        {
            numDrops = minmaxMana [0];
            if (minmaxMana[0] != minmaxMana[1])
            {
                numDrops = Random.Range(minmaxMana[0], minmaxMana[1] + 1);
            }
            numDrops += GameRules.morePickables;
            InstantitatePickables("mana", numDrops);
        }
        if (Random.Range(0f, 1f) < (GameRules.itemLuck * moneyDropChance))
        {
            numDrops = minmaxMoney[0];
            if (minmaxMoney[0] != minmaxMoney[1])
            {
                numDrops = Random.Range(minmaxMoney[0], minmaxMoney[1] + 1);
            }
            numDrops += GameRules.morePickables;
            InstantitatePickables("money", numDrops);
        }
    }

    protected virtual void Die()
    {
        SoundManager.PlaySound("enemyExplosion1", 0.04f);
        Score.IncreaseScore(scoreOnKill);
        DropItems();
        if (orchestrer != null)
            orchestrer.NotifyEnemyKilled(orchesterWaveNumber, isBoss: isBoss);
        Destroy(gameObject);
    }

    IEnumerator Blink()
    {
        Color tmp = renderer.color;
        bool inv = false;
        while (!inv || Time.time < blinkTimeout)
        {
            // Alternate between 0 and 1 scale to simulate flashing
            if (inv)
            {
                inv = false;
                tmp.a = 1f;
                renderer.color = tmp;
            }
            else
            {
                inv = true;
                tmp.a = 0.15f;
                renderer.color = tmp;
            }
            yield return new WaitForSeconds(blinkInterval);
        }
        tmp.a = 1f;
        renderer.color = tmp;
        isBlinking = false;
    }

    public virtual void damage(float damage)
    {
        health = health - damage * GameRules.enemyDamageReceived;
        blinkTimeout = Time.time + blinkSeconds;
        if (!isBlinking)
        {
            isBlinking = true;
            StartCoroutine("Blink");
        }
        if (health <= 0 && !dying)
        {
            dying = true;
            Die();
        }
    }

    void OnBecameInvisible()
    {
        if (!dying)
        {
            Destroy(gameObject);
            if (orchestrer  != null)
                orchestrer.NotifyEnemyKilled(orchesterWaveNumber, isBoss: isBoss);
        }
    }

    public void NotifyBossPhaseChange(int phase)
    {
        orchestrer.NotifyBossPhaseChange(phase);
    }


}