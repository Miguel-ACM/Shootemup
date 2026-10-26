using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class StormReactor : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 27;
        health = 480;
        hullScale = 1.1f;
        attackInterval = Timing(1.35f, 0.95f);
        Mover<TopTracker>();
    }

    protected override void Attack()
    {
        if (volley % 6 == 4 || volley % 6 == 5)
            return;
        if (volley % 6 != 1)
        {
            Fan<NeedleRound>(Shots(5, 7), 20, 4.4f, .19f);
            return;
        }
        var laser = Fire<PulseLaser>(Muzzle(), Vector2.down, 0);
        if (laser != null)
        {
            laser.Bind(Hull, new Vector3(0, -.35f, -.4f));
            laser.warning = Timing(1.4f, 1.1f);
            laser.activeTime = 1.0f;
        }
    }
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount)
    {
        base.damage(amount * (movement != null && !movement.CanFire ? .75f : 1.2f));
    }
}
