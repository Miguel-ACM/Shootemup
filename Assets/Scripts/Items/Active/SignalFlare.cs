using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SignalFlare : OffensiveActiveItem
{
    void Awake()
    {
        SetData("Signal Flare", "Fire on this mark", "Mark and burn an enemy ahead. Companions and homing fire target it. 25 energy.", ItemType.active);
        cost = 25;
        cd = 5;
    }

    protected override OffensiveCast Launch()
    {
        Vector2 origin = s.transform.position;
        AEnemy target = null;
        float nearest = 14 * 14;
        foreach (var enemy in AEnemy.Live)
        {
            if (enemy == null || !enemy.CanReceiveDamage ||
                !enemy.ClosestDamagePoint(origin, out var point) || point.y > Camera.UpLimit + .2f)
                continue;
            Vector2 delta = point - origin;
            float distance = delta.sqrMagnitude;
            if (distance >= nearest || delta.y < .4f * Mathf.Sqrt(distance) ||
                CombatWalls.Sweep(origin, point, 0, out _, out _))
                continue;
            nearest = distance;
            target = enemy;
        }
        if (target == null)
            return null;
        var mark = CreateCast<FlareSignal>("Signal flare mark");
        mark.Target = target;
        return mark;
    }
}

public sealed class FlareSignal : OffensiveCast
{
    static FlareSignal current;
    static readonly Color Ember = new Color(1, .52f, .21f);
    const float Duration = 5, BurnInterval = .35f;
    LineRenderer ring;
    float nextBurn;
    public AEnemy Target { get; set; }

    public static AEnemy Marked => current != null && current.Target != null &&
        current.Target.CanBeTargeted && current.Target.CanReceiveDamage ? current.Target : null;

    protected override void Begin()
    {
        if (current != null)
            current.Extinguish();
        current = this;
        ring = CombatVisuals.Line(transform, Ember, .038f);
        Draw();
        FeedBurn();
        ItemSound.Cue("itemFocus", volume: .12f, pitch: 1.35f);
    }

    protected override void Step(float dt)
    {
        if (Age >= Duration || Target == null || !Target.CanBeTargeted)
        {
            Extinguish();
            return;
        }
        Draw();
        if (Age >= nextBurn && Target.CanReceiveDamage && GameClock.ActivePlayDelta > 0)
            FeedBurn();
    }

    void Draw()
    {
        Vector3 point = Target.transform.position + Vector3.up * Mathf.Clamp(Target.size * .45f, .28f, 1);
        point.x = Mathf.Clamp(point.x, Camera.LeftLimit + .25f, Camera.RightLimit - .25f);
        point.y = Mathf.Min(point.y, Camera.UpLimit - .22f);
        CombatVisuals.Ring(ring, point, .16f + .035f * Mathf.Cos(Age * 10));
    }

    void FeedBurn()
    {
        var burn = Target.GetComponent<BurningHull>() ?? Target.gameObject.AddComponent<BurningHull>();
        burn.Ignite(5 + GameRules.playerShootDamage);
        nextBurn = Age + BurnInterval;
    }

    void Extinguish()
    {
        if (current == this)
            current = null;
        ring.enabled = false;
        Finish();
    }

    void OnDestroy()
    {
        if (current == this)
            current = null;
    }
}
