using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class TrafficCone : OffensiveActiveItem
{
    void Awake()
    {
        SetData("Traffic Cone", "Lane closed", "Drop a cone that stops three bullets. 18 energy.", ItemType.active);
        cost = 18;
        cd = 3;
    }

    protected override OffensiveCast Launch() => CreateCast<PlacedTrafficCone>("Traffic cone barricade");
}

public sealed class PlacedTrafficCone : OffensiveCast
{
    const float Radius = .45f;
    const float Lifetime = 7f;
    static readonly Color Orange = new Color(.93f, .54f, .2f);
    Collider2D contact;
    Transform model;
    public int Remaining { get; private set; } = 3;

    protected override void Begin()
    {
        Vector2 at = transform.position;
        Vector2 ahead = at + Vector2.up * 1.1f;
        if (ahead.y <= Camera.UpLimit - Radius && !CombatWalls.Sweep(at, ahead, Radius, out _, out _))
            at = ahead;
        transform.position = new Vector3(at.x, at.y, -1);
        gameObject.layer = 7; // Ship and blocker contacts share the hostile projectile collision window.
        var collider = gameObject.AddComponent<CircleCollider2D>();
        collider.radius = Radius;
        collider.isTrigger = true;
        contact = collider;
        ProjectileCollisionWindow.Register(contact);
        model = Model(nameof(TrafficCone), Radius * 2);
        model.localRotation = Quaternion.Euler(-14, -15, 0);
        ItemSound.Cue("itemBlock", "roll", .14f, .8f);
    }

    protected override void Step(float dt)
    {
        if (Age < Lifetime) return;
        Break();
    }

    protected override void Update()
    {
        if (CombatDecision.Active || CloudTransition.Busy || GameRules.playerDisabled)
        {
            Finish();
            return;
        }
        base.Update();
    }

    void OnTriggerEnter2D(Collider2D other) => TryStop(other);
    void OnTriggerStay2D(Collider2D other) => TryStop(other);

    void TryStop(Collider2D other)
    {
        if (Remaining == 0 || !GameClock.ActivePlay || !other.enabled ||
            !other.TryGetComponent<AEnemyBullet>(out var bullet) || !BulletReflection.CanReflect(bullet))
            return;
        if (bullet is CombatRound round && !round.PixelOverlaps(contact)) return;
        // Legacy bullets are destroyed at the end of the frame; prevent a second queued contact.
        if (!(bullet is CombatRound)) other.enabled = false;
        bullet.Vanish();
        CombatUpgrades.Current?.Block();
        Remaining--;
        ItemSpark.Emit(transform.position, Orange, 4, .09f);
        if (Remaining == 0)
            Break();
        else
        {
            model.localRotation *= Quaternion.Euler(0, 0, Remaining == 2 ? 12 : -22);
            ItemSound.Cue("itemBlock", "roll", .085f, 1.35f);
        }
    }

    void Break()
    {
        ItemSpark.Emit(transform.position, Orange, 6, .12f);
        ItemSound.Cue("shatter", volume: .13f);
        Finish();
    }

    void OnDisable()
    {
        ProjectileCollisionWindow.Unregister(contact);
        if (contact != null) contact.enabled = false;
    }
}
