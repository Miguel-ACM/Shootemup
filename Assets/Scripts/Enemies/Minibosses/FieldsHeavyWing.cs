using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class FieldsHeavyWing : DistrictMiniboss
{
    static readonly Vector3[] ports = { new Vector3(-.68f, -1.20f, -.30f), new Vector3(.68f, -1.20f, -.30f) };
    protected override void Configure()
    {
        hull = 24;
        health = 380;
        hullScale = 1.1f;
        attackInterval = (1.0f);
        shellTravelDistance = 5.4f;
        Mover<TopTracker>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        firingCue.Warn(seconds);
    }

    // Plains heavy wing: bombs that stop and burst flat, an aimed needle fan, and a sparse orange carrier.
    protected override void Attack()
    {
        int beat = volley % 3;
        if (beat == 0)
        {
            int shards = Dense(Enraged ? 7 : 5);
            for (int i = 0; i < 2; i++)
            {
                float stop = .8f + i * .4f;
                Fire<SimpleRound>(Hull.TransformPoint(ports[i == 0 ? 0 : ports.Length - 1]), CombatVisuals.Rotate(Vector2.down, i == 0 ? -12 : 12), 2.5f, .4f)
                    .Pause(stop, .4f)
                    .SplitAt(stop + .4f, shards, 170f / (shards - 1), 2.4f, .15f);
            }
        }
        else if (beat == 1)
            FanFrom<NeedleRound>(Dense(7), 18, 4.2f, .19f, ports);
        else
            Fire<ScatterCarrier>(Muzzle(), CombatVisuals.Rotate(Aim, Random.Range(-12f, 12f)), 2.3f, .33f);
    }
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount)
    {
        base.damage(amount * (movement != null && !movement.CanFire ? .75f : 1.2f));
    }
}
