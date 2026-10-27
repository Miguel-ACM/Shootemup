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
    protected BossWeave Patterns { get; private set; }
    public int PatternRounds => Patterns != null ? Patterns.Released : 0;
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
        Patterns = new BossWeave(this);
    }

    protected override void Update()
    {
        if (GameRules.playerDisabled)
            return;
        age += Time.deltaTime * LevelRun.EnemyTempoScale;
        float warning = (nextAttack - age) / LevelRun.EnemyTempoScale;
        if (Difficulty.Hard)
        {
            base.PrepareFire(warning);
            firingCue.ClearWarning();
            PrepareHardFire(warning);
        }
        else PrepareFire(warning);
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
        if (!PhaseTransitioning && PatternEmissionAllowed) Patterns.Tick(Time.deltaTime * LevelRun.EnemyTempoScale);
        else Patterns.Cancel();
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
        Patterns.Cancel();
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
    protected virtual void PrepareHardFire(float seconds) { }
    protected Vector3 WeaponPort(int index = 0)
    {
        var ports = EnemyWeaponLayout.Ports(hull);
        return ports[Mathf.Abs(index) % ports.Length];
    }
    internal void PreparePattern(Vector3 port, float seconds, Vector2 axis)
    { Cue(port, seconds); PointWeapon(port, axis); }
    internal SimpleRound PatternShot(Vector3 port, Vector2 axis, float speed, float radius)
        => Fire<SimpleRound>(port, axis, speed, radius);
    protected SimpleRound HeavyTrail(Vector3 port, Vector2 axis, float radius = .72f, float speed = 1.65f)
    {
        var shot = Fire<SimpleRound>(port, axis, speed, radius);
        if (shot != null) shot.LeaveTrail(.28f, 4.8f);
        return shot;
    }
    protected void OrbitalCrown(Vector3 port, int count, int gap, float radius, float rotation, float drift, float offset)
    {
        if (CombatRound.Live.Count + count - gap > CombatRound.Limit) return;
        for (int i = gap; i < count; i++)
        {
            var shot = Fire<SimpleRound>(port, Vector2.down, drift, .18f);
            if (shot != null) shot.Orbit(radius, offset + i * 360f / count, rotation);
        }
    }
    protected void OrbitalSystem(Vector3 port, int moons, int beads, float offset)
    {
        int gap = 2, count = moons * (beads - gap);
        if (CombatRound.Live.Count + count > CombatRound.Limit) return;
        for (int moon = 0; moon < moons; moon++)
            for (int bead = gap; bead < beads; bead++)
            {
                var shot = Fire<SimpleRound>(port, Vector2.down, 1.05f, .17f);
                if (shot != null) shot.Orbit(2.25f, offset + moon * 360f / moons, 24,
                    14, .74f, bead * 360f / beads, -78, 1.6f);
            }
    }

    protected virtual void UpdateBar()
    {
        if (bar != null)
            bar.Set(health);
    }

    protected override void Die()
    {
        if (bar != null)
            bar.SetVisible(false);
        base.Die();
    }
    // A continuous scalloped front: symmetric speeds bow each fan into a petal.
    // Geometry and shot counts stay deterministic; nothing is added per projectile.
    protected void PetalFan<T>(Vector3 port, Vector2 axis, int count, float spacing, float speed, float radius, float curvature = .12f) where T : CombatRound
    {
        for (int i = 0; i < count; i++)
        {
            float lane = i - (count - 1) * .5f;
            float t = count <= 1 ? 0 : lane / ((count - 1) * .5f);
            Fire<T>(port, CombatVisuals.Rotate(axis, lane * spacing), speed * (1 + curvature * (1 - t * t)), radius);
        }
    }

    protected void Radial<T>(int count, float speed, float radius, float offset = 0) where T : CombatRound
    {
        for (int i = 0; i < count; i++)
            Fire<T>(transform.position, CombatVisuals.Rotate(Vector2.down, i * 360f / count + offset), speed, radius);
    }
}
