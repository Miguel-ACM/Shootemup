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
        attackInterval = 1.15f;
        shellTravelDistance = 7.2f;
        Mover<TopTracker>();
    }

    // Snow siege craft: it hurls a spray of shards straight up and out of sight, then
    // the snow falls back from the top of the screen across the whole width; between
    // storms it fires mirrored arrow fans that freeze and re-aim.
    protected override void Attack()
    {
        if (volley % 2 == 0)
        {
            int flakes = Dense(Enraged ? 22 : 18);
            var p = Hull.TransformPoint(ports[0]);
            for (int i = 0; i < flakes; i++)
            {
                float lane = i - (flakes - 1) * .5f;
                Fire<SimpleRound>(p, CombatVisuals.Rotate(Vector2.up, lane * 4), 9 + (i % 3), .16f).FadeAt(.45f);
                float x = Mathf.Lerp(Camera.LeftLimit + .3f, Camera.RightLimit - .3f, (i + (volley / 2 % 2) * .5f) / flakes);
                CombatRound.Launch<SimpleRound>(new Vector3(x, Camera.UpLimit + .4f), Vector2.down, 1.5f + (i % 3) * .3f, .19f)
                    .Pause(.05f, .6f + (i % 4) * .15f, CombatRound.Turn.Down, lane, 1)
                    .Sway(i % 2 == 0 ? .35f : -.35f, .45f);
            }
        }
        else
        {
            int count = Dense(Enraged ? 13 : 11);
            for (int mirror = -1; mirror <= 1; mirror += 2)
                for (int i = 0; i < count; i++)
                {
                    float lane = i - (count - 1) * .5f;
                    Fire<NeedleRound>(Hull.TransformPoint(ports[0]), CombatVisuals.Rotate(Vector2.down, mirror * (30 + lane * 5)), 4.2f, .19f)
                        .Pause(.6f, .6f + Mathf.Abs(lane) * .04f, CombatRound.Turn.Aim, mirror * lane * 3, .25f)
                        .Ramp(4.5f, 1.2f, 1.25f);
                }
        }
    }
    // The bays are exposed during firing and reload; armor still accepts every weapon.
    public override void damage(float amount)
    {
        base.damage(amount * (movement != null && !movement.CanFire ? .75f : 1.2f));
    }
}
