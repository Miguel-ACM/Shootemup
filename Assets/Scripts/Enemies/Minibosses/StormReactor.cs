using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class StormReactor : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 27;
        health = 480;
        hullScale = 1.1f;
        attackInterval = 1.05f;
        Mover<TopTracker>();
    }

    // Thunder reactor: mirrored curtains of zigzag lightning from both flanks, an aimed
    // zigzag fan, and a charged laser flanked by more lightning.
    protected override void Attack()
    {
        int beat = volley % 3;
        if (beat == 0)
            Stream(Dense(8), .12f, n =>
            {
                for (int side = -1; side <= 1; side += 2)
                    Fire<NeedleRound>(transform.position + Vector3.right * side * .8f, CombatVisuals.Rotate(Vector2.down, side * (10 + n * 8)), 3.6f, .19f).Zigzag(.2f, 24);
            });
        else if (beat == 1)
        {
            int bolts = Dense(Enraged ? 11 : 9);
            var aim = Aim;
            for (int i = 0; i < bolts; i++)
                Fire<NeedleRound>(Muzzle(i), CombatVisuals.Rotate(aim, (i - (bolts - 1) * .5f) * 13), 3.8f, .19f).Zigzag(.2f, 24);
        }
        else
        {
            var laser = Fire<PulseLaser>(Muzzle(), Vector2.down, 0);
            if (laser != null)
            {
                laser.Bind(Hull, new Vector3(0, -.35f, -.4f));
                laser.warning = 1.0f;
                laser.activeTime = 1.2f;
            }
            for (int side = -1; side <= 1; side += 2)
                MirrorFan(transform.position, CombatVisuals.Rotate(Vector2.down, side * 50), Dense(5), 12, 3.4f);
        }
    }
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount)
    {
        base.damage(amount * (movement != null && !movement.CanFire ? .75f : 1.2f));
    }
}
