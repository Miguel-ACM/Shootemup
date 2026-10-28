using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class StormReactor : DistrictMiniboss
{
    protected override void Configure()
    {
        hull = 27;
        health = 480;
        hullScale = 1.1f;
        attackInterval = (0.95f);
        Mover<TopTracker>();
    }

    // Thunder reactor: lightning needles that zigzag down, with a charged laser in the rest.
    protected override void Attack()
    {
        if (volley % 6 == 4 || volley % 6 == 5)
            return;
        if (volley % 6 != 1)
        {
            int bolts = Dense(6);
            var aim = Aim;
            for (int i = 0; i < bolts; i++)
                Fire<NeedleRound>(Muzzle(i), CombatVisuals.Rotate(aim, (i - (bolts - 1) * .5f) * 20), 3.8f, .19f).Zigzag(.2f, 24);
            return;
        }
        var laser = Fire<PulseLaser>(Muzzle(), Vector2.down, 0);
        if (laser != null)
        {
            laser.Bind(Hull, new Vector3(0, -.35f, -.4f));
            laser.warning = 1.1f;
            laser.activeTime = 1.0f;
        }
    }
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount)
    {
        base.damage(amount * (movement != null && !movement.CanFire ? .75f : 1.2f));
    }
}
