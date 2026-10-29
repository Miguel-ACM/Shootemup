using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class TunnelDefensePlatform : DistrictMiniboss
{
    static readonly Vector3[] ports = { new Vector3(-.20f, -1.09f, -.48f), new Vector3(0, -1.09f, -.48f), new Vector3(.20f, -1.09f, -.48f) };
    protected override void Configure()
    {
        hull = 25;
        health = 480;
        hullScale = 1.1f;
        attackInterval = 0.95f;
        Mover<TopTracker>();
    }

    // Tunnel platform: everything from its centre. Full 360 bursts of fast arrows with
    // slow bouncing teeth threaded between them, and rounds that run along the walls.
    protected override void Attack()
    {
        if (volley % 3 != 2)
        {
            int rays = Dense(Enraged ? 20 : 16);
            for (int i = 0; i < rays; i++)
            {
                var axis = CombatVisuals.Rotate(Vector2.down, (i + (volley % 2) * .5f) * 360f / rays);
                Fire<NeedleRound>(transform.position, axis, 4.4f, .19f);
                Fire<SimpleRound>(transform.position, CombatVisuals.Rotate(axis, 180f / rays), 1.6f, .28f).Bounce(2);
            }
        }
        else
        {
            int runners = Dense(8);
            for (int i = 0; i < runners; i++)
                Fire<SimpleRound>(transform.position, CombatVisuals.Rotate(Vector2.down, i * 360f / runners), 4.2f, .23f)
                    .RunWalls(6, i % 2 == 0 ? 1 : -1);
        }
    }
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount)
    {
        base.damage(amount * (movement != null && !movement.CanFire ? .75f : 1.2f));
    }
}
