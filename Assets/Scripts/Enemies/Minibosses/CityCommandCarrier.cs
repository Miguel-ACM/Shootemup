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
        attackInterval = (0.8f);
        Mover<TopTracker>();
    }

    // City command carrier: headlight rays of mixed speed, then a two-family street lattice.
    protected override void Attack()
    {
        if (volley % 2 == 0)
        {
            int beads = Dense(6);
            var aim = Aim;
            for (int r = -1; r <= 1; r += 2)
                for (int b = 0; b < beads; b++)
                    Fire<NeedleRound>(Muzzle(r < 0 ? 0 : 1), CombatVisuals.Rotate(aim, r * 14), 5.0f - b * .55f, .18f);
        }
        else
        {
            int lanes = Dense(Enraged ? 7 : 6);
            for (int family = -1; family <= 1; family += 2)
                for (int i = 0; i < lanes; i++)
                    Fire<SimpleRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.down, family * 32 + (i - (lanes - 1) * .5f) * 8), 2.2f, .2f);
        }
    }
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount)
    {
        base.damage(amount * (movement != null && !movement.CanFire ? .75f : 1.2f));
    }
}
