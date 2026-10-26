using UnityEngine;
using System;

public abstract class DistrictEnemy : AEnemy
{
    protected Transform player;
    protected PatternMover movement;
    protected float age;
    [EnemyTunable("Firing / First attack delay", 0, 15)] protected float nextAttack = 1.2f;
    protected int volley;
    protected bool enteredFight;
    Vector2 committedAim = Vector2.down;
    bool aimCommitted;
    float lastRelease = -10;
    public bool IsPreparing { get; private set; }
    public virtual bool NoseAimed => EnemyWeaponLayout.NoseAimed(hull);
    public float VisualHeading => NoseAimed && (IsPreparing || age - lastRelease < .24f) ? Vector2.SignedAngle(Vector2.down, committedAim) : 0;
    protected int Shots(int easy, int hard) => Difficulty.Easy ? easy : hard;
    protected float Timing(float easy, float hard) => Difficulty.Easy ? easy : hard;
    public float hullScale = .7f;
    public int hull;
    [Min(.1f), EnemyTunable("Firing / Shell travel distance", .1f, 16)] public float shellTravelDistance = 3.8f;
    [EnemyTunable("Firing / Attack interval", .05f, 20)] protected float attackInterval = 2;
    public float MovementScale { get; private set; } = 1;
    public float ProjectileSpeedScale { get; private set; } = 1;
    public float ProjectileDamageScale { get; private set; } = 1;
    internal void SetTuning(float movementRate, float speed, float damage)
    {
        MovementScale = movementRate;
        ProjectileSpeedScale = speed;
        ProjectileDamageScale = damage;
        if (movement != null)
            movement.TempoMultiplier *= MovementScale;
    }

    public bool Challenged
    {
        get; private set;
    }

    public bool IsMiniboss
    {
        get; private set;
    }

    void MakeMiniboss()
    {
        if (IsMiniboss)
            return;
        if (this is DistrictMiniboss)
        {
            IsMiniboss = true;
            return;
        }
        IsMiniboss = true;
        health *= .68f;
        attackInterval *= .74f;
        nextAttack = .45f;
        // These minibosses are three-ship guards, rather than one heavy fortress.
        if (this is Bathysphere || this is GhostGalleon || this is SandWorm)
            health *= 3;
        if (this is OrbitalCarrier)
            health *= 2.8f;
        if (movement is TopTracker tracker)
            tracker.BeginAssault();
        if (movement is Slalom slalom)
            slalom.BeginAssault();
    }

    public void MakeChallenging()
    {
        if (Challenged)
            return;
        Challenged = true;
        attackInterval *= .75f;
    }

    public Transform Hull
    {
        get; private set;
    }

    protected EnemyFiringCue firingCue;
    protected abstract void Configure();
    protected abstract void Attack();
    public static DistrictEnemy Spawn(Type type, Vector3 p, bool applyTuning = true, bool scaleForRun = true, bool activate = true)
    {
        var go = new GameObject(type.Name);
        go.SetActive(false);
        if (!activate)
            go.hideFlags = HideFlags.HideAndDontSave;
        go.transform.position = p;
        go.tag = "Enemy";
        go.layer = 8;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CombatVisuals.Disc;
        sr.enabled = false;
        sr.sortingLayerName = "Enemy";
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.bodyType = RigidbodyType2D.Kinematic;
        go.AddComponent<CircleCollider2D>().isTrigger = true;
        var enemy = (DistrictEnemy)go.AddComponent(type);
        enemy.Configure();
        if (applyTuning)
            EnemyTuning.Apply(enemy, enemy.movement);
        if (scaleForRun)
            enemy.health *= GameRules.enemyHealth * LevelRun.EnemyHealthScale;
        // Light wave ships should die during a clean pass with the stock gun.
        if (scaleForRun && !(enemy is DistrictBoss) && !(enemy is DistrictMiniboss))
        {
            enemy.health *= enemy.hullScale < .8f ? .84f : 1.35f;
            enemy.attackInterval *= .78f;
        }
        enemy.size = enemy.hullScale;
        go.GetComponent<CircleCollider2D>().radius = enemy.hullScale * .8f;
        var model = new GameObject("Armored hull");
        model.transform.SetParent(go.transform, false);
        model.transform.localScale = Vector3.one * enemy.hullScale;
        model.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CombatHull(enemy.hull);
        var renderer = model.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = enemy is SpaceSatellite || enemy is SpaceCitadelBoss || enemy.hull == 60 ? SpaceMaterials.Fleet : enemy.hull >= 35 && enemy.hull <= 38 || enemy.hull == 57 ? CandyMaterials.Glaze : enemy is FieldsBomberBoss ? CombatMaterials.Fortress : CombatMaterials.Fleet;
        renderer.sortingLayerName = "Enemy";
        enemy.Hull = model.transform;
        go.AddComponent<CombatHullVisual>().model = model.transform;
        enemy.firingCue = go.AddComponent<EnemyFiringCue>();
        enemy.firingCue.Build(model.transform);
        if (!(enemy is TunnelSawBoss) && !(enemy is GroundVehicle) && !(enemy is SpaceSatellite) && !(enemy is SpaceCitadelBoss) && !(enemy is InfernalEnemy) && !(enemy is OceanEnemy) && !(enemy is HellCrabBoss) && !(enemy is AbyssSubmarineBoss) && enemy.hull != 56 && enemy.hull < 58)
        {
            var exhaust = go.AddComponent<EnemyThrusters>();
            exhaust.visualRoot = model.transform;
            exhaust.sockets = IndustrialMeshFactory.CombatEngineSockets(enemy.hull);
        }
        enemy.scoreOnKill = 40;
        enemy.moneyDropChance = .8f;
        go.SetActive(activate);
        return enemy;
    }

    protected override void Start()
    {
        base.Start();
        var p = GameObject.FindGameObjectWithTag("Player");
        player = p != null ? p.transform : null;
    }

    public virtual void FlyPhrase(WaveUnit unit)
    {
        if (unit.miniboss)
            MakeMiniboss();
        if (unit.phrase == FlightPhrase.None)
            return;
        if (movement != null)
        {
            movement.enabled = false;
            Destroy(movement);
        }
        var flight = gameObject.AddComponent<FormationFlight>();
        flight.phrase = unit.phrase;
        flight.side = unit.side;
        flight.lane = unit.lane;
        flight.duration = unit.flightDuration;
        flight.fireEvery = unit.fireEvery;
        flight.curve = unit.curve;
        flight.syncFire = unit.syncFire;
        flight.firePhase = unit.firePhase;
        flight.fireTimes = unit.fireTimes;
        if (Challenged)
        {
            // Add one shared answer in the widest rest; retain routes, speed and original accents.
            if (unit.fireTimes != null && unit.fireTimes.Length > 1)
            {
                var score = (float[])unit.fireTimes.Clone();
                Array.Sort(score);
                int gap = 0;
                for (int i = 1; i < score.Length - 1; i++)
                    if (score[i + 1] - score[i] > score[gap + 1] - score[gap])
                        gap = i;
                if (score[gap + 1] - score[gap] >= .8f)
                {
                    var denser = new float[score.Length + 1];
                    Array.Copy(score, denser, score.Length);
                    denser[score.Length] = (score[gap] + score[gap + 1]) * .5f;
                    Array.Sort(denser);
                    flight.fireTimes = denser;
                }
            }
            else
                flight.fireEvery *= .75f;
        }
        if (IsMiniboss && unit.fireTimes != null && unit.fireTimes.Length > 0)
        {
            // Keep the phrase's shared accents, adding a quick opening reply and a deliberate rest.
            var score = new float[unit.fireTimes.Length + 1];
            score[0] = Mathf.Max(.65f, unit.fireTimes[0] - .65f);
            score[1] = unit.fireTimes[0] + .2f;
            Array.Copy(unit.fireTimes, 1, score, 2, unit.fireTimes.Length - 1);
            Array.Sort(score);
            flight.fireTimes = score;
        }
        flight.formationDepth = unit.formationDepth;
        flight.fixedDepth = unit.fixedDepth;
        flight.TempoMultiplier = 1;
        movement = flight;
        var p = flight.PositionAt(0);
        transform.position = new Vector3(p.x * Camera.RightLimit, Mathf.LerpUnclamped(Camera.DownLimit, Camera.UpLimit, p.y), transform.position.z);
    }

    public void Escort(Transform leader)
    {
        if (movement != null)
        {
            movement.enabled = false;
            Destroy(movement);
        }
        var escort = gameObject.AddComponent<EscortOrbit>();
        escort.SetAnchor(leader);
        escort.TempoMultiplier = MovementScale;
        movement = escort;
    }

    protected T Mover<T>() where T : PatternMover
    {
        var m = gameObject.AddComponent<T>();
        m.TempoMultiplier = MovementScale;
        movement = m;
        return m;
    }

    internal void UseMovement(PatternMover next)
    {
        if (movement != null && movement != next)
        {
            movement.enabled = false;
            Destroy(movement);
        }
        next.TempoMultiplier = MovementScale;
        movement = next;
    }

    protected virtual void Update()
    {
        if (FrozenHull.Holds(gameObject))
            return;
        age += Time.deltaTime * LevelRun.EnemyTempoScale;
        if (!enteredFight && transform.position.y < Camera.UpLimit - .5f)
        {
            enteredFight = true;
            if (!(movement is FormationFlight))
                nextAttack = Mathf.Max(nextAttack, age + .65f);
        }
        PrepareFire(movement is FormationFlight score ? score.FireWarning : (nextAttack - age) / LevelRun.EnemyTempoScale);
        if (age > 28 || transform.position.y < Camera.DownLimit - 2)
        {
            Depart();
            return;
        }
        if (GameRules.playerDisabled || transform.position.y > Camera.UpLimit - .5f || transform.position.y < Camera.DownLimit + 1)
            return;
        if (movement != null && !movement.CanFire)
            return;
        if (player != null && Vector2.Distance(player.position, transform.position) < 2)
            return;
        if (movement is FormationFlight flight)
        {
            if (flight.TakeFireBeat())
            {
                volley++;
                Attack();
            }
        }
        else if (age >= nextAttack)
        {
            volley++;
            Attack();
            nextAttack = age + attackInterval / Mathf.Max(.5f, GameRules.enemyShootSpeed);
        }
    }

    Vector2 RawAim => movement != null && movement.LockedAim.HasValue ? ((Vector2)(movement.LockedAim.Value - transform.position)).normalized : CombatVisuals.Aim(transform.position);
    protected Vector2 Aim => aimCommitted ? committedAim : RawAim;
    protected virtual void PrepareFire(float seconds)
    {
        IsPreparing = enteredFight && seconds >= 0 && seconds < .48f;
        if (IsPreparing && !aimCommitted) { committedAim = RawAim; aimCommitted = true; }
        else if (!IsPreparing && age - lastRelease > .24f) aimCommitted = false;
        firingCue?.Warn(enteredFight ? seconds : -1);
    }
    protected void Cue(Vector3 world, float seconds) => firingCue?.WarnAt(world, seconds);
    protected Vector3 Muzzle(int index = 0)
    {
        var ports = EnemyWeaponLayout.Ports(hull);
        return ports.Length == 0 ? transform.position : Hull.TransformPoint(ports[Mathf.Abs(index) % ports.Length]);
    }
    protected T Fire<T>(Vector3 position, Vector2 direction, float velocity, float radius = .18f) where T : CombatRound
    {
        // Legacy center-fired volleys now leave the existing modeled weapon.
        if (!(this is TunnelSawBoss) && (position - transform.position).sqrMagnitude < .00001f)
            position = Muzzle(volley);
        lastRelease = age;
        firingCue?.Flash(position);
        var shot = CombatRound.Launch<T>(position, direction, velocity * ProjectileSpeedScale * (IsMiniboss ? 1.14f : 1), radius);
        if (shot != null)
            shot.damage *= ProjectileDamageScale;
        if (shot is BurstShell shell)
            shell.SetTravelDistance(shellTravelDistance);
        return shot;
    }

    protected void Fan<T>(int count, float spread, float velocity, float radius = .14f) where T : CombatRound
    {
        for (int i = 0; i < count; i++)
            Fire<T>(Muzzle(i), CombatVisuals.Rotate(Aim, (i - (count - 1) * .5f) * spread), velocity, radius);
    }

    protected void FanFrom<T>(int count, float spread, float velocity, float radius, params Vector3[] ports) where T : CombatRound
    {
        Vector2 aim = Aim;
        for (int i = 0; i < count; i++)
        {
            int port = count == 1 ? (volley + ports.Length - 1) % ports.Length : Mathf.RoundToInt(i * (ports.Length - 1f) / (count - 1));
            Fire<T>(Hull.TransformPoint(ports[port]), CombatVisuals.Rotate(aim, (i - (count - 1) * .5f) * spread), velocity, radius);
        }
    }

    protected override void OnBecameInvisible()
    {
    }
}
