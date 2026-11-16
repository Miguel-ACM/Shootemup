using System.Collections.Generic;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class CryoVent : OffensiveActiveItem
{
    void Awake()
    {
        SetData("Cryo Vent", "Cold front", "A frost pulse slows ships, their volleys and nearby rounds. 25 energy.", ItemType.active);
        cost = 25;
        cd = 6;
    }

    protected override OffensiveCast Launch() => CreateCast<CryoPulse>("Cryo Vent pulse");
}

public sealed class CryoPulse : OffensiveCast
{
    const float Radius = 4.4f;
    const float Expansion = .36f;
    readonly HashSet<AEnemy> reached = new HashSet<AEnemy>();

    protected override void Begin()
    {
        var origin = transform.position + Vector3.back * 2;
        var color = new Color(.56f, .85f, 1);
        PSXFireballs.Shockwave(origin, Radius, .56f, color, 2, Expansion / .56f);
        ItemSpark.Emit(origin, color, 12, .14f, false, "needle");
        ItemSound.Cue("frost", volume: .28f, pitch: .8f, cooldown: 0);
    }

    protected override void Step(float dt)
    {
        float radius = Radius * Mathf.Clamp01(Age / Expansion);
        float square = radius * radius;
        Vector2 origin = transform.position;
        foreach (var round in CombatRound.Live)
            if (round != null && ((Vector2)round.transform.position - origin).sqrMagnitude <= square)
                round.ChillInFlight(1.1f);
        foreach (var enemy in AEnemy.Live)
            if (enemy != null && enemy.CanReceiveDamage && !reached.Contains(enemy) &&
                enemy.ClosestDamagePoint(origin, out var point) && (point - origin).sqrMagnitude <= square)
            {
                reached.Add(enemy);
                ChilledHull.Apply(enemy, 3.5f, vent: true);
            }
        if (Age >= Expansion) Finish();
    }
}
