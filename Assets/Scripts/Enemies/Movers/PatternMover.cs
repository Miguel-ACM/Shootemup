using UnityEngine;

/// <summary>Shared timing, bounds and shooter integration for the optional enemy movement patterns.</summary>
public abstract class PatternMover : AMover
{
    public const float FlightTempo = .72f;
    public static float DistrictFlightTempo => FlightTempo * (LevelRun.Current == LevelId.City ? 1.35f : 1);
    public float TempoMultiplier = 1;
    [SerializeField, Min(.01f), EnemyTunable("Standalone movement / Travel speed", .1f, 20)] protected float moveSpeed = 3f;
    [SerializeField, Min(0), EnemyTunable("Standalone movement / Edge inset", 0, 3)] protected float edgeInset = .7f;
    [SerializeField, Min(.1f), EnemyTunable("Standalone movement / Maximum lifetime", 1, 60)] private float maximumLifetime = 30f;
    protected Transform player;
    protected Vector3 origin;
    protected float age;
    private AShooter[] controlledShooters;
    private bool initialized, arrived, leaving;

    public Vector3? LockedAim
    {
        get; private set;
    }

    public bool IsLeaving => leaving;
    public bool CanFire
    {
        get; private set;
    }

    public Vector3 Destination
    {
        get; protected set;
    }

    protected virtual void Awake()
    {
        controlledShooters = GetComponentsInChildren<AShooter>(true);
        SetFiring(false);
    }

    protected override void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
            return;
        initialized = true;
        base.Start();
        if (controlledShooters == null)
        {
            controlledShooters = GetComponentsInChildren<AShooter>(true);
            SetFiring(false);
        }
        origin = transform.position;
        Destination = origin;
        var ship = GameObject.FindGameObjectWithTag("Player");
        player = ship != null ? ship.transform : null;
        if (rigidBody != null)
            rigidBody.linearVelocity = Vector2.zero;
        InitializePattern();
    }

    protected virtual void InitializePattern()
    {
    }

    public override void Move()
    {
        Step(Time.deltaTime);
    }

    /// <summary>Uses scaled game time. Also allows deterministic editor verification without Play mode.</summary>
    public void Step(float deltaTime)
    {
        Initialize();
        if (!movementEnabled || !enabled || deltaTime <= 0 || !float.IsFinite(deltaTime))
            return;
        float dt = deltaTime * DistrictFlightTempo * Mathf.Max(0, GameRules.enemySpeed) * LevelRun.EnemyTempoScale * TempoMultiplier;
        if (dt <= 0)
            return;
        age += dt;
        if (age >= maximumLifetime)
            Leave();
        if (leaving)
        {
            SetFiring(false);
            transform.position += Vector3.up * moveSpeed * dt;
            return; // AEnemy's visibility lifecycle handles despawning and wave notification.
        }
        StepPattern(dt);
    }

    protected abstract void StepPattern(float dt);

    protected Vector3 InBounds(Vector3 point)
    {
        float insetX = Mathf.Min(edgeInset, (Camera.RightLimit - Camera.LeftLimit) * .45f);
        float insetY = Mathf.Min(edgeInset, (Camera.UpLimit - Camera.DownLimit) * .45f);
        point.x = Mathf.Clamp(point.x, Camera.LeftLimit + insetX, Camera.RightLimit - insetX);
        point.y = Mathf.Clamp(point.y, Camera.DownLimit + insetY, Camera.UpLimit - insetY);
        point.z = origin.z;
        return point;
    }

    protected bool Travel(Vector3 target, float dt, float speedMultiplier = 1f)
    {
        target.z = origin.z;
        Destination = target;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * speedMultiplier * dt);
        return (transform.position - target).sqrMagnitude < .000001f;
    }

    protected void Arrive()
    {
        if (arrived)
            return;
        arrived = true;
        if (shooter != null)
            shooter.OnArrive();
    }

    protected void SetFiring(bool allowed)
    {
        CanFire = allowed;
        if (controlledShooters == null)
            return;
        foreach (var weapon in controlledShooters)
            if (weapon != null)
                weapon.SetMovementFiringAllowed(allowed);
    }

    protected void LockAim(Vector3? point)
    {
        if (point.HasValue)
            point = SpectralDecoy.AimPosition(point.Value);
        LockedAim = point;
        foreach (var weapon in controlledShooters)
            if (weapon != null)
                weapon.MovementAimPoint = point;
    }

    protected void Leave()
    {
        if (leaving)
            return;
        leaving = true;
        SetFiring(false);
        if (shooter != null)
            shooter.OnLeave();
    }

    protected virtual void OnDisable()
    {
        SetFiring(false);
        if (controlledShooters != null)
            LockAim(null);
    }
}
