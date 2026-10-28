using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class GhostGalleon : InfernalEnemy
{
    protected override void Configure()
    {
        hull = 47;
        hullScale = .64f;
        health = 95;
        attackInterval = (1.8f);
        Mover<TopTracker>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        for (int side = 0; side < 2; side++)
            for (int row = 0; row < (3); row++)
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
            // Lantern souls: they stop, stare, then dart at the player one after another.
            for (int side = -1; side <= 1; side += 2)
                Fire<SimpleRound>(Hull.TransformPoint(new Vector3(side * .61f, .43f, -.22f)), CombatVisuals.Rotate(Vector2.down, side * 20), 1.6f, .3f)
                    .Pause(.7f, side < 0 ? .4f : .7f, CombatRound.Turn.Aim, 0, 2.6f);
    }
}
