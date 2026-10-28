using System;
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
        cardsRunning = false;
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

    // Spell cards: a boss that overrides Cards() runs one named attack at a time,
    // in order, instead of mixing unrelated volleys on a beat. A phase change stops
    // the book and the next attack opens the phase-two cards.
    bool cardsRunning;
    protected virtual IEnumerator Cards() => null;
    protected bool OpenCards()
    {
        if (cardsRunning)
            return true;
        var book = Cards();
        if (book == null)
            return false;
        cardsRunning = true;
        Phrase(book);
        return true;
    }

    // Repeats a volley at a fixed cadence for the length of a card.
    protected IEnumerator Every(float seconds, float interval, Action<int> volley)
    {
        int n = 0;
        for (float t = 0; t < seconds; t += interval)
        {
            volley(n++);
            yield return Wait(Rest(interval));
        }
    }

    // Shared symmetric geometry. Every helper fires mirrored arrows around a centre
    // line, so a boss's own mechanics stay distinct while its fire stays orderly.

    // Mirrored pendulum streams from two ports: a lace that opens and folds.
    protected IEnumerator MirrorLace(Transform anchor, Vector3 leftLocal, Vector3 rightLocal, int steps, int arms, float swing, float armGap, float speed, float interval, int sign = 1)
    {
        for (int i = 0; i < steps && anchor != null; i++)
        {
            float angle = sign * swing * Mathf.Sin(i / (float)steps * Mathf.PI * 2);
            for (int arm = 0; arm < arms; arm++)
            {
                float a = angle + (arm - (arms - 1) * .5f) * armGap;
                Fire<NeedleRound>(anchor.TransformPoint(leftLocal), CombatVisuals.Rotate(Vector2.down, -a), speed, .19f);
                Fire<NeedleRound>(anchor.TransformPoint(rightLocal), CombatVisuals.Rotate(Vector2.down, a), speed, .19f);
            }
            yield return Wait(Rest(interval));
        }
    }

    // A symmetric arrow fan released at several speeds: nested arcs that separate.
    protected void ArrowWavefront(Vector3 port, Vector2 axis, int lanes, float arc, int layers, float speed, float layerStep)
    {
        float step = arc / Mathf.Max(1, lanes - 1);
        for (int layer = 0; layer < layers; layer++)
            for (int i = 0; i < lanes; i++)
                Fire<NeedleRound>(port, CombatVisuals.Rotate(axis, (i - (lanes - 1) * .5f) * step + (layer % 2) * step * .5f), speed + layer * layerStep, .19f);
    }

    // Rows of arrow chevrons aimed at the player, each row a wider V.
    protected IEnumerator Chevrons(Transform anchor, Vector3 local, int rows, float speed, float interval)
    {
        var aim = CombatVisuals.Aim(anchor.TransformPoint(local));
        for (int row = 0; row < rows && anchor != null; row++)
        {
            var port = anchor.TransformPoint(local);
            Fire<NeedleRound>(port, aim, speed, .19f);
            for (int k = 1; k <= 2 + row; k++)
                for (int mirror = -1; mirror <= 1; mirror += 2)
                    Fire<NeedleRound>(port, CombatVisuals.Rotate(aim, mirror * k * 7), speed - k * .12f, .19f);
            yield return Wait(Rest(interval));
        }
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
