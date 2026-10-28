using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public abstract class DistrictBoss : DistrictEnemy
{
    protected float initialHealth;
    protected bool phaseTwo;
    protected ResourceBar bar;
    protected Vector3 station;
    protected float motionAge;
    float nextPressure;
    float phaseEntryUntil;
    BossPhaseEntry phaseEntry;
    protected bool PhaseTransitioning => age < phaseEntryUntil;
    protected virtual bool PatternEmissionAllowed => true;
    public virtual bool UsesHealthBar => true;
    public virtual float DisplayFraction => HealthFraction;
    public float HealthFraction => health / Mathf.Max(1, initialHealth);
    protected override void Start()
    {
        base.Start();
        initialHealth = health;
        isBoss = true;
        scoreOnKill = 1600;
        lifeDropChance = manaDropChance = moneyDropChance = 1;
        minmaxLife = new[] { 4, 6 };
        minmaxMana = new[] { 6, 9 };
        minmaxMoney = new[] { 10, 15 };
        var ui = GameObject.Find("UI/ItemsRight/BossHealth");
        if (ui != null && UsesHealthBar)
        {
            bar = ui.GetComponent<ResourceBar>();
            bar.SetMax(health);
            bar.SetVisible(true);
        }
        station = new Vector3(0, Camera.UpLimit - 2.3f, 1);
        nextPressure = nextAttack;
        phaseEntry = gameObject.AddComponent<BossPhaseEntry>();
        phaseEntry.Build(this);
    }

    protected override void Update()
    {
        if (GameRules.playerDisabled)
            return;
        age += Time.deltaTime * LevelRun.EnemyTempoScale;
        float warning = (nextAttack - age) / LevelRun.EnemyTempoScale;
        // One authored score on both difficulties; Easy thins the counts.
        base.PrepareFire(warning);
        firingCue.ClearWarning();
        PrepareScore(warning);
        if (!phaseTwo && PhaseTwoReady)
        {
            phaseTwo = true;
            NotifyBossPhaseChange(2);
            OnPhaseTwo();
            PresentPhaseTwo();
        }
        float motionDelta = Time.deltaTime * LevelRun.EnemyTempoScale * MovementScale;
        motionAge += motionDelta;
        MoveBoss(motionDelta);
        if (!enteredFight && transform.position.y < Camera.UpLimit - 1)
        {
            enteredFight = true;
            nextAttack = Mathf.Max(nextAttack, age + .35f);
            nextPressure = Mathf.Max(nextPressure, age + .35f);
        }
        if (!PhaseTransitioning && transform.position.y < Camera.UpLimit - 1 && age >= nextAttack)
        {
            volley++;
            Attack();
            nextAttack = age + attackInterval * AttackPace / Mathf.Max(.5f, GameRules.enemyShootSpeed);
        }
        if (!PhaseTransitioning && transform.position.y < Camera.UpLimit - 1 && age >= nextPressure)
        {
            PressureAttack();
            nextPressure = age + PressureInterval / Mathf.Max(.5f, GameRules.enemyShootSpeed);
        }
        UpdateBar();
    }

    protected virtual void MoveBoss(float dt)
    {
        transform.position = Vector3.MoveTowards(transform.position, station, dt * 2);
    }

    protected virtual bool PhaseTwoReady => HealthFraction <= .5f;
    protected virtual float AttackPace => phaseTwo ? (.87f) : 1;
    protected void PresentPhaseTwo()
    {
        phaseEntryUntil = age + 1.5f * LevelRun.EnemyTempoScale;
        nextAttack = Mathf.Max(nextAttack, phaseEntryUntil);
        nextPressure = Mathf.Max(nextPressure, phaseEntryUntil);
        firingCue.ClearWarning();
        StopPhrases();
        CombatRound.ClearAll();
        phaseEntry.Begin();
    }

    protected virtual float PressureInterval => phaseTwo ? .55f : .85f;
    protected virtual void PressureAttack()
    {
    }

    protected virtual void OnPhaseTwo()
    {
    }

    protected virtual void PrepareScore(float seconds)
    {
    }

    // Multi-frame phrases: coroutines on the boss that wait in enemy tempo,
    // hold through phase entries and stop with the phase or the boss.
    readonly List<Coroutine> phrases = new List<Coroutine>();
    protected bool Holding => GameRules.playerDisabled || WorldTimeStop.Frozen || PhaseTransitioning || !PatternEmissionAllowed;
    protected void Phrase(IEnumerator routine)
    {
        if (isActiveAndEnabled)
            phrases.Add(StartCoroutine(routine));
    }

    protected void StopPhrases()
    {
        foreach (var phrase in phrases)
            if (phrase != null)
                StopCoroutine(phrase);
        phrases.Clear();
    }

    protected IEnumerator Wait(float seconds)
    {
        for (float t = 0; t < seconds;)
        {
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            yield return null;
        }
    }
    // A visible charge at a moving emitter for the whole wait.
    protected IEnumerator Charge(Transform anchor, Vector3 local, float seconds)
    {
        for (float t = 0; t < seconds;)
        {
            if (anchor == null)
                yield break;
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            Cue(anchor.TransformPoint(local), seconds - t);
            yield return null;
        }
    }

    protected Vector3 WeaponPort(int index = 0)
    {
        var ports = EnemyWeaponLayout.Ports(hull);
        return ports[Mathf.Abs(index) % ports.Length];
    }

    protected virtual void UpdateBar()
    {
        if (bar != null)
            bar.Set(health);
    }

    protected override void Die()
    {
        StopPhrases();
        if (bar != null)
            bar.SetVisible(false);
        base.Die();
    }
}
