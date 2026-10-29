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
        attackInterval = 1.1f;
        Mover<TopTracker>();
    }

    // City command carrier: three distinct moves in turn. A four-arm arrow junction
    // turning round itself, headlight rays of arrows at falling speeds, and a street
    // lattice in front with a rear guard rebounding off the ceiling behind it.
    protected override void Attack()
    {
        int beat = volley % 3;
        if (beat == 0)
            Stream(Dense(24), .07f, n =>
            {
                for (int arm = 0; arm < 4; arm++)
                    Fire<NeedleRound>(transform.position, CombatVisuals.Rotate(Vector2.down, arm * 90 + n * 12 * (volley % 2 == 0 ? 1 : -1)), 3.6f, .19f);
            });
        else if (beat == 1)
        {
            int beads = Dense(8);
            foreach (var port in ports)
            {
                var p = Hull.TransformPoint(port);
                var aim = CombatVisuals.Aim(p);
                foreach (float ray in new[] { -18f, 0f, 18f })
                    for (int b = 0; b < beads; b++)
                        Fire<NeedleRound>(p, CombatVisuals.Rotate(aim, ray), 5.4f - b * .45f, .18f);
            }
        }
        else
        {
            int lanes = Dense(8);
            for (int family = -1; family <= 1; family += 2)
                for (int i = 0; i < lanes; i++)
                {
                    float offset = (i - (lanes - 1) * .5f) * 7;
                    Fire<SimpleRound>(transform.position, CombatVisuals.Rotate(Vector2.down, family * 34 + offset), 2.4f, .2f);
                    Fire<SimpleRound>(transform.position, CombatVisuals.Rotate(Vector2.up, family * 34 + offset), 2.6f, .2f).Bounce(1);
                }
        }
    }
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount)
    {
        base.damage(amount * (movement != null && !movement.CanFire ? .75f : 1.2f));
    }
}
