using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public abstract class AEnemy : MonoBehaviour, ICombatDamageable
{
    readonly List<Collider2D> damageShapes = new List<Collider2D>(1);
    int damageShapesFrame = -1;
    public IReadOnlyList<Collider2D> DamageShapes
    {
        get
        {
            if (damageShapesFrame != Time.frameCount)
            {
                GetComponents(damageShapes);
                damageShapesFrame = Time.frameCount;
            }
            return damageShapes;
        }
    }
    public bool ClosestDamagePoint(Vector2 origin, out Vector2 point)
    {
        point = transform.position;
        float nearest = float.PositiveInfinity;
        var shapes = DamageShapes;
        for (int i = 0; i < shapes.Count; i++)
        {
            var shape = shapes[i];
            if (shape == null || !shape.enabled) continue;
            Vector2 candidate = shape.ClosestPoint(origin);
            float distance = (candidate-origin).sqrMagnitude;
            if (distance >= nearest) continue;
            nearest = distance; point = candidate;
        }
        return !float.IsPositiveInfinity(nearest);
    }

    public static readonly HashSet<AEnemy> Live = new HashSet<AEnemy>();
    // District hazards (rising mines, falling candy) never hold a wave open.
    public virtual bool HoldsWave => true;
    public static int HoldingCount
    {
        get
        {
            int count = 0;
            foreach (var enemy in Live)
                if (enemy != null && enemy.HoldsWave)
                    count++;
            return count;
        }
    }

    protected virtual void OnEnable()
    {
        Live.Add(this);
    }

    protected virtual void OnDisable()
    {
        Live.Remove(this);
        // Explicit cleanup can remove an ordinary ship without calling Depart.
        if (!dying && !isBoss && orchestrer != null)
        {
            dying = true;
            orchestrer.NotifyEnemyKilled(orchesterWaveNumber, defeated: false);
        }
    }

    [EnemyTunable("Base health", 1, 50000)] public float health = 20f;
    public virtual bool CanBeTargeted => health > 0;
    public virtual bool CanReceiveDamage => CanBeTargeted && !dying;
    // Set by CombatHit.Affliction around a burn or poison tick: no impact, sound or flash.
    [System.NonSerialized] public bool QuietDamage;
    public virtual DamageResult ReceiveDamage(float amount)
    {
        if (!CanReceiveDamage || amount <= 0 || !float.IsFinite(amount))
            return default;
        float before = Mathf.Max(0, health);
        damage(amount);
        float applied = Mathf.Clamp(before - health, 0, before);
        return new DamageResult(applied > 0, amount, applied);
    }

    bool dying = false;
    public float size = 0.65f;

    public int scoreOnKill;
    public float lifeDropChance = 0.1f;
    // Minibosses also drop health, and coins that grow with the run's stage like a boss's.
    protected bool dropsLife;
    protected int coinsPerStage;
    // Elites burst into this many coins when destroyed.
    protected int eliteCoins;
    public float manaDropChance = 0.1f;
    public float moneyDropChance = 0.1f;
    public int[] minmaxLife = new int[] { 1, 3 };
    public int[] minmaxMoney = new int[] { 1, 1 };
    public int[] minmaxMana = new int[] { 1, 1 };
    protected bool isBoss = false;

    Shipcontroller s;

    WaveOrchestrer orchestrer = null;
    int orchesterWaveNumber = 0;


    bool isBlinking = false;
    SpriteRenderer renderer;
    float blinkSeconds = 0.12f;
    float blinkTimeout = 0f;
    float blinkInterval = 0.03f;

    public void SetOrchestrer(WaveOrchestrer o, int waveNumber)
    {
        orchestrer = o;
        orchesterWaveNumber = waveNumber;
    }

    protected void ShareEncounterWith(AEnemy other)
    {
        if (other != null)
            other.SetOrchestrer(orchestrer, orchesterWaveNumber);
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
        for (int i = 0; i < num; i++)
        {
            Vector3 pos = Utils.RandomVectorInCircle(size, size);
            Instantiate(Resources.Load<GameObject>("Pickables/" + pickable), transform.position + pos, Quaternion.identity);

        }
    }

    internal void DropVendingCoin()
    {
        var position = transform.position;
        position.x = Mathf.Clamp(position.x, Camera.LeftLimit + .25f, Camera.RightLimit - .25f);
        position.y = Mathf.Clamp(position.y, Camera.DownLimit + .25f, Camera.UpLimit - .25f);
        var coin = Instantiate(Resources.Load<GameObject>("Pickables/money"), position, Quaternion.identity).GetComponent<Money>();
        coin.Launch(Vector2.down * 1.3f, .15f);
    }

    int DropItems()
    {
        int numDrops = 0;
        int bossCoins = 0;
        // Health is precious now: only boss and miniboss kills drop it, always, whatever the ship's health.
        if ((isBoss || dropsLife) && Random.Range(0f, 1f) < lifeDropChance)
        {
            numDrops = Random.Range(minmaxLife[0], minmaxLife[1] + 1) + GameRules.morePickables;
            InstantitatePickables("life", numDrops);
        }
        if (Random.Range(0f, 1f) < (GameRules.itemLuck * manaDropChance * s.GetManaChanceMultiplier()))
        {
            numDrops = minmaxMana[0];
            if (minmaxMana[0] != minmaxMana[1])
            {
                numDrops = Random.Range(minmaxMana[0], minmaxMana[1] + 1);
            }
            numDrops += GameRules.morePickables;
            InstantitatePickables("mana", numDrops);
        }
        if (Random.Range(0f, 1f) < (GameRules.itemLuck * moneyDropChance * LevelModifiers.CoinChance))
        {
            numDrops = minmaxMoney[0];
            if (minmaxMoney[0] != minmaxMoney[1])
            {
                numDrops = Random.Range(minmaxMoney[0], minmaxMoney[1] + 1);
            }
            numDrops += GameRules.morePickables + LevelModifiers.ExtraCoins;
            if (isBoss)
                bossCoins = numDrops;
            else
                InstantitatePickables("money", numDrops);
        }
        return bossCoins;
    }

    public void Depart()
    {
        if (dying)
            return;
        dying = true;
        if (orchestrer != null)
            orchestrer.NotifyEnemyKilled(orchesterWaveNumber, isBoss: isBoss, defeated: false);
        Destroy(gameObject);
    }

    protected virtual void Die()
    {
        if (isBoss)
        {
            BossDestruction.Play(this, position => CompleteDeath(false, position));
            return;
        }
        CompleteDeath(true);
    }

    void CompleteDeath(bool explode, Vector3? bossPosition = null)
    {
        // The explosion plays its own sound for every fireball it spawns.
        if (CombatUpgrades.Current != null)
            CombatUpgrades.Current.Killed(this);
        if (explode)
            PSXImpactEffects.Explosion(gameObject);
        Score.IncreaseScore(scoreOnKill);
        int bonusCoins = DropItems();
        // Each boss of the run pays more: 40 coins for the first, 80 for the second, and so on.
        if (isBoss)
            BossCoinBurst.Spawn(bossPosition ?? transform.position, Mathf.RoundToInt((40 * LevelRun.Stage + bonusCoins) * LevelModifiers.CoinBurst));
        else if (coinsPerStage > 0)
            InstantitatePickables("money", Mathf.RoundToInt(coinsPerStage * LevelRun.Stage * LevelModifiers.CoinBurst));
        if (eliteCoins > 0 && !isBoss)
            BossCoinBurst.Spawn(transform.position, Mathf.RoundToInt(eliteCoins * LevelModifiers.CoinBurst), .45f);
        if (orchestrer != null)
            orchestrer.NotifyEnemyKilled(orchesterWaveNumber, isBoss: isBoss, bossPosition: bossPosition);
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

    float nextImpact;
    bool impactProvided;
    PartHitFeedback localFeedback;
    public void HitAt(Vector3 point, Vector2 incoming = default)
    {
        impactProvided = true;
        if (localFeedback == null)
            localFeedback = GetComponentInChildren<PartHitFeedback>();
        if (localFeedback != null)
            localFeedback.Pulse(incoming);
        if (Time.time < nextImpact)
            return;
        nextImpact = Time.time + .025f;
        SoundManager.PlayGated(isBoss ? "bossHit" : "enemyHit", .1f, isBoss ? .085f : .06f);
        PSXImpactEffects.Burst(point, new Color(1, .68f, .30f), false, incoming.sqrMagnitude > .001f ? incoming : Vector2.up);
    }

    public virtual void damage(float damage)
    {
        if (dying || health <= 0)
            return;
        if (!impactProvided && !QuietDamage)
            HitAt(transform.position);
        impactProvided = false;
        health = health - damage * GameRules.enemyDamageReceived;
        if (!QuietDamage)
            blinkTimeout = Time.time + blinkSeconds;
        if (localFeedback == null && !isBlinking && !QuietDamage)
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

    protected virtual void OnBecameInvisible()
    {
        Depart();
    }

    public void NotifyBossPhaseChange(int phase)
    {
        if (orchestrer != null)
            orchestrer.NotifyBossPhaseChange(phase);
    }


}
