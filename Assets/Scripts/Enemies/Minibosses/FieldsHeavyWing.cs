using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class FieldsHeavyWing : DistrictMiniboss
{
    static readonly Vector3[] ports = { new Vector3(-.68f, -1.20f, -.30f), new Vector3(.68f, -1.20f, -.30f) };
    protected override void Configure()
    {
        health = 380;
        hullScale = 1.1f;
        attackInterval = 1.0f;
        shellTravelDistance = 5.4f;
        Mover<TopTracker>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        firingCue.Warn(seconds);
    }

    // Plains heavy wing: mirrored bombs that travel the same distance and explode 360,
    // streams of sharp arrows from both bays, and wide mirrored needle fans.
    protected override void Attack()
    {
        int beat = volley % 3;
        if (beat == 0)
        {
            int shards = Dense(16);
            // Inner bombs burst dark, outer bombs bright: the two rings interleave.
            for (int i = 0; i < 2; i++)
            {
                UseTone(i == 0 ? HostileTone.Dark : HostileTone.Bright);
                for (int mirror = -1; mirror <= 1; mirror += 2)
                    Fire<SimpleRound>(Hull.TransformPoint(ports[mirror < 0 ? 0 : 1]), CombatVisuals.Rotate(Vector2.down, mirror * (10 + i * 22)), 2.6f, .4f)
                        .Pause(.9f, .4f)
                        .SplitAt(1.3f, shards, 360f / shards, 2.6f, .18f);
            }
        }
        else if (beat == 1)
            Stream(Dense(10), .09f, n =>
            {
                // Aimed arrow streams alternate bright and dark volleys.
                UseTone(n % 2 == 0 ? HostileTone.Bright : HostileTone.Dark);
                foreach (var port in ports)
                {
                    var p = Hull.TransformPoint(port);
                    MirrorFan(p, CombatVisuals.Aim(p), 3, 9, 4.8f);
                }
            });
        else
        {
            // One bay fans dark, the other bright; the bays swap every rotation.
            for (int bay = 0; bay < ports.Length; bay++)
            {
                UseTone((bay + volley / 3) % 2 == 0 ? HostileTone.Dark : HostileTone.Bright);
                MirrorFan(Hull.TransformPoint(ports[bay]), Vector2.down, Dense(Enraged ? 11 : 9), 11, 3.9f);
            }
        }
    }
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount)
    {
        base.damage(amount * (movement != null && !movement.CanFire ? .75f : 1.2f));
    }
}
