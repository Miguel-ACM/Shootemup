using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class Moth : AItem
{
    void Awake() => SetData("Moth", "Mind the light", "Chases down and eats a hostile bullet every few seconds.", ItemType.partner);
    public override void OnBegin() { }

    public override void OnPickup()
    {
        RoamingCompanion.Attach<MothFlight>(s, nameof(Moth), .48f, new Vector2(.66f, .34f));
        base.OnPickup();
    }
}

public sealed class MothFlight : RoamingCompanion
{
    const float Reach = 3.35f;
    CombatRound target;
    int serial;
    float cooldown, searchIn;
    static readonly Color Dust = new Color(.86f, .66f, .89f);

    protected override void Step(float dt)
    {
        cooldown -= dt;
        searchIn -= dt;
        if (!CurrentTarget())
        {
            target = null;
            if (cooldown <= 0 && searchIn <= 0)
            {
                FindRound();
                searchIn = .13f;
            }
        }
        Fly(target == null ? Home : (Vector2)target.transform.position, target == null ? 9 : 13, dt);
        float flutter = .86f + Mathf.Sin(Age * 37) * .14f;
        Model.localScale = new Vector3(RestScale.x * flutter, RestScale.y, RestScale.z);
        if (!CurrentTarget() || ((Vector2)target.transform.position - Position).sqrMagnitude > .20f * .20f) return;
        var eaten = target;
        target = null;
        cooldown = 3.6f;
        eaten.Vanish();
        CombatUpgrades.Current?.Block();
        RunInventory.Ping(this);
        ItemSpark.Emit(transform.position, Dust, 4, .072f);
        ItemSound.Cue("itemBlock", "ready", .052f, 1.45f);
    }

    protected override void Park()
    {
        target = null;
        Model.localScale = RestScale;
    }

    bool CurrentTarget() => target != null && target.IsInterceptable && target.Serial == serial &&
        ((Vector2)target.transform.position - Pilot).sqrMagnitude <= (Reach + .85f) * (Reach + .85f);

    void FindRound()
    {
        float nearest = float.PositiveInfinity;
        foreach (var round in CombatRound.Live)
        {
            if (!round.IsInterceptable) continue;
            Vector2 point = round.transform.position;
            if ((point - Pilot).sqrMagnitude > Reach * Reach) continue;
            float distance = (point - Position).sqrMagnitude;
            if (distance >= nearest || CombatWalls.Sweep(Position, point, .1f, out _, out _)) continue;
            nearest = distance;
            target = round;
            serial = round.Serial;
        }
    }
}
