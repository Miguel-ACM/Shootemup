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
        attackInterval = (1.1f);
        shellTravelDistance = 7.2f;
        Mover<TopTracker>();
    }

    // Snow siege craft: a fan that freezes and re-aims, then shells that burst into falling snow.
    protected override void Attack()
    {
        if (volley % 2 == 0)
        {
            int count = Dense(Enraged ? 11 : 9);
            for (int i = 0; i < count; i++)
            {
                float lane = i - (count - 1) * .5f;
                Fire<SimpleRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.down, lane * 15), 3.8f, .2f)
                    .Pause(.6f, .7f + Mathf.Abs(lane) * .04f, CombatRound.Turn.Aim, lane * 6, .25f)
                    .Ramp(4, 1.4f, 1.3f);
            }
        }
        else
            FanFrom<BurstShell>(Dense(3), 32, 2.8f, .46f, ports);
    }
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount)
    {
        base.damage(amount * (movement != null && !movement.CanFire ? .75f : 1.2f));
    }
}
