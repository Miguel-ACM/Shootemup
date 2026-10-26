using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class SnowSiegeCraft : DistrictMiniboss
{
    static readonly Vector3[] ports = { new Vector3(0, -1.46f, -.3f) };
    protected override void Configure()
    {
        hull = 26;
        health = 480;
        hullScale = 1.1f;
        attackInterval = Timing(1.55f,1.1f);
        shellTravelDistance = 7.2f;
        Mover<TopTracker>();
    }

    protected override void Attack()
    {
        if (volley % 2 == 0)
        {
            FanFrom<BurstShell>(Shots(2,3), 32, 2.8f, .46f, ports);
        }
        else
        {
            FanFrom<SuspendedRound>(Shots(5,7), 23, 3.0f, .25f, ports);
        }
    }
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount)
    {
        base.damage(amount * (movement != null && !movement.CanFire ? .75f : 1.2f));
    }
}
