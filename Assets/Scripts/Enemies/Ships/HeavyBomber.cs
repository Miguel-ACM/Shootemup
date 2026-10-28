using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class HeavyBomber : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 2;
        health = 80;
        hullScale = .9f;
        attackInterval = (1.8f);
        shellTravelDistance = 5.2f;
        Mover<Slalom>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        firingCue.Warn(seconds);
    }

    // Bombs that brake, glint and burst into a flat sideways fan; needles between drops.
    protected override void Attack()
    {
        if (volley % 2 == 1)
        {
            int shards = Dense(5);
            for (int i = 0; i < 2; i++)
                Fire<SimpleRound>(Muzzle(i), CombatVisuals.Rotate(Vector2.down, i == 0 ? -10 : 10), 2.4f, .38f)
                    .Pause(.85f + i * .3f, .35f)
                    .SplitAt(1.2f + i * .3f, shards, 170f / (shards - 1), 2.3f, .15f);
        }
        else
            Fan<NeedleRound>(5, 18, 3.8f, .19f);
    }
}
