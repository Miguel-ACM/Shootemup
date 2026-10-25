using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class SiegeGunship : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 7;
        health = 95;
        hullScale = .9f;
        attackInterval = 1.2f;
        Mover<TopTracker>();
    }

    protected override void Attack()
    {
        for (int i = -1; i <= 1; i++)
        {
            var muzzle = Hull.TransformPoint(new Vector3(i * .78f, i == 0 ? -1.06f : -.98f, -.34f));
            for (int j = -1; j <= 1; j++)
                Fire<SuspendedRound>(muzzle, CombatVisuals.Rotate(Vector2.down, j * 16), 5, .23f);
        }
    }
}
