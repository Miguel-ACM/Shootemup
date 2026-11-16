using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class Wasp : AItem
{
    void Awake() => SetData("Wasp", "Brief visit", "Darts out to sting a nearby enemy, then flies back.", ItemType.partner);
    public override void OnBegin() { }

    public override void OnPickup()
    {
        RoamingCompanion.Attach<WaspFlight>(s, nameof(Wasp), .43f, new Vector2(-.47f, .62f));
        base.OnPickup();
    }
}

public sealed class WaspFlight : RoamingCompanion
{
    const float Reach = 5.5f;
    static readonly Color Amber = new Color(1, .75f, .27f);
    AEnemy target;
    bool returning;
    float rest;

    protected override void Step(float dt)
    {
        if (target != null)
        {
            if (!target.CanReceiveDamage || !target.ClosestDamagePoint(Position, out var point) ||
                (point - Pilot).sqrMagnitude > Reach * Reach * 1.6f ||
                CombatWalls.Sweep(Position, point, .09f, out _, out _))
                Return();
            else
            {
                var incoming = (point - Position).normalized;
                Fly(point, 15, dt);
                if ((point - Position).sqrMagnitude <= .12f * .12f)
                {
                    CombatHit.Apply(target, GameRules.playerShootDamage * 1.65f, point, incoming, generation: 1, companion: true);
                    ItemSpark.Emit(transform.position, Amber, 5, .085f);
                    ItemSound.Cue("shotCompanion", volume: .052f, pitch: 1.43f);
                    RunInventory.Ping(this);
                    Return();
                }
            }
            return;
        }

        Fly(Home, 13, dt);
        if (returning && (Home - Position).sqrMagnitude > .14f * .14f) return;
        returning = false;
        rest -= dt;
        if (rest > 0 || !Shipcontroller.Firing) return;
        rest = .18f;
        var next = CombatTargets.Nearest(Pilot, null, Reach);
        if (next != null && next.CanReceiveDamage && next.ClosestDamagePoint(Position, out var contact) &&
            !CombatWalls.Sweep(Position, contact, .09f, out _, out _))
            target = next;
    }

    void Return()
    {
        target = null;
        returning = true;
        rest = .78f;
    }

    protected override void Park()
    {
        target = null;
        returning = false;
    }
}
