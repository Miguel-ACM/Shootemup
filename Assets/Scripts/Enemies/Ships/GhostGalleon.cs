using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class GhostGalleon : InfernalEnemy
{
    protected override void Configure()
    {
        hull = 47;
        hullScale = .64f;
        health = 95;
        attackInterval = 1.8f;
        Mover<TopTracker>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        for (int side = 0; side < 2; side++)
            for (int row = 0; row < 3; row++)
                Cue(Muzzle(side * 3 + row), seconds);
    }

    protected override void Attack()
    {
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 3; i++)
            {
                var port = Hull.TransformPoint(new Vector3(side * .61f, .43f - i * .42f, -.22f));
                Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, side * (35 + i * 17 + (volley % 2) * 7)), 2.9f + i * .16f, .25f);
            }
        if (volley % 2 == 0)
            FanFrom<NeedleRound>(3, 17, 4.5f, .18f, new Vector3(-.61f, .43f, -.22f), new Vector3(.61f, .43f, -.22f));
    }
}
