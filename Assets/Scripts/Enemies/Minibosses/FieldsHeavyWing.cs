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
        attackInterval = Timing(1.5f, 1.0f);
        shellTravelDistance = 5.4f;
        Mover<TopTracker>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        if (Difficulty.Easy && (volley + 1) % 2 == 0)
            Cue(Muzzle(volley % 2), seconds);
        else
            firingCue.Warn(seconds);
    }

    protected override void Attack()
    {
        if (volley % 2 == 0)
        {
            FanFrom<BurstShell>(Shots(1, 2), 24, 3.2f, .4f, ports);
        }
        else
        {
            FanFrom<NeedleRound>(Shots(5, 7), 18, 4.2f, .19f, ports);
        }
    }
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount)
    {
        base.damage(amount * (movement != null && !movement.CanFire ? .75f : 1.2f));
    }
}
