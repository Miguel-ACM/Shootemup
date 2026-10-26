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
        attackInterval = Timing(1.45f, 1.0f);
        Mover<TopTracker>();
    }

    protected override void Attack()
    {
        if (volley % 2 == 0)
        {
            FanFrom<RicochetBolt>(Shots(5, 7), 26, 3.3f, .25f, ports);
        }
        else
        {
            FanFrom<ProximityMine>(Shots(1, 2), 40, 2.5f, .25f, ports);
            FanFrom<NeedleRound>(Shots(3, 5), 21, 3.9f, .19f, ports);
        }
    }
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount)
    {
        base.damage(amount * (movement != null && !movement.CanFire ? .75f : 1.2f));
    }
}
