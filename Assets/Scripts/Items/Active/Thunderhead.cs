using System.Collections.Generic;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class Thunderhead : OffensiveActiveItem
{
    void Awake()
    {
        SetData("Thunderhead", "Storm in a coil", "Throw a coil that chains lightning. 40 energy.", ItemType.active);
        cost = 40; cd = 7;
    }
    protected override OffensiveCast Launch() => CreateCast<ThunderheadCoil>("Thunderhead coil");
}

public sealed class ThunderheadCoil : OffensiveCast
{
    readonly HashSet<AEnemy> struck = new HashSet<AEnemy>();
    Transform model;
    float nextStrike = .45f;
    protected override void Begin()
    {
        transform.position += new Vector3(0, .8f, -1);
        model = Model(nameof(Thunderhead), .65f);
        ItemSound.Cue("capacitor", volume: .2f, pitch: .8f, cooldown: 0);
    }
    protected override void Step(float dt)
    {
        transform.position += Vector3.up * (Age < .6f ? 5 * dt : .35f * dt);
        model.localRotation = Quaternion.Euler(15, Mathf.Sin(Age * 5) * 18, 0);
        if (Age >= nextStrike)
        {
            nextStrike = Age + .3f;
            struck.Clear();
            Vector3 from = transform.position;
            AEnemy first = null;
            for (int bolt = 0; bolt < 3; bolt++)
            {
                var target = CombatUpgrades.Nearest(from, struck, 5.5f);
                // Unused links return to the first target: a lone boss takes the whole pulse.
                if (target == null && first != null && first.CanBeTargeted && first.CanReceiveDamage) target = first;
                if (target == null) break;
                first ??= target;
                var point = target.transform.position;
                StaticArc.Draw(from == point ? transform.position : from, point, .18f);
                SparkPlug.ArcContact(target);
                CombatHit.Apply(target, GameRules.playerShootDamage * .9f, point, ((Vector2)(point - transform.position)).normalized, generation: 1, energy: true);
                struck.Add(target);
                from = point;
            }
            if (struck.Count > 0)
            {
                SparkPlug.ExtraHop(from, struck, GameRules.playerShootDamage * .9f, score: true);
                ItemSound.Cue("static", volume: .14f, pitch: 1.15f, cooldown: .25f);
            }
        }
        if (Age >= 3.3f) Finish();
    }
}
