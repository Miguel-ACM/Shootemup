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
        attackInterval = (1.0f);
        Mover<TopTracker>();
    }

    // Tunnel platform: fast ricochets paired with slow bouncing rounds on the same rays,
    // then rounds that cling to the walls and run around the arena.
    protected override void Attack()
    {
        if (volley % 2 == 0)
        {
            int rays = Dense(Enraged ? 6 : 5);
            var aim = Aim;
            for (int i = 0; i < rays; i++)
            {
                var axis = CombatVisuals.Rotate(aim, (i - (rays - 1) * .5f) * 24);
                Fire<RicochetBolt>(Muzzle(i), axis, 3.9f, .22f);
                Fire<SimpleRound>(Muzzle(i), axis, 1.4f, .3f).Bounce(2);
            }
        }
        else
        {
            int runners = Dense(4);
            for (int i = 0; i < runners; i++)
                Fire<SimpleRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.down, (i - (runners - 1) * .5f) * 40), 4.0f, .23f)
                    .RunWalls(5, i % 2 == 0 ? 1 : -1);
        }
    }
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount)
    {
        base.damage(amount * (movement != null && !movement.CanFire ? .75f : 1.2f));
    }
}
