using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class CityCommandCarrier : DistrictMiniboss
{
    static readonly Vector3[] ports = { new Vector3(-1.04f, -1.33f, -.18f), new Vector3(1.04f, -1.33f, -.18f) };
    protected override void Configure()
    {
        hull = 28;
        health = 480;
        hullScale = 1.1f;
        attackInterval = Timing(1.2f, 0.8f);
        Mover<TopTracker>();
    }

    protected override void Attack()
    {
        if (volley % 2 == 0)
        {
            FanFrom<ReturningDisc>(Shots(3, 5), 28, 3.0f, .28f, ports);
        }
        else
        {
            FanFrom<NeedleRound>(Shots(6, 9), 14, 4.8f, .19f, ports);
        }
    }
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount)
    {
        base.damage(amount * (movement != null && !movement.CanFire ? .75f : 1.2f));
    }
}
