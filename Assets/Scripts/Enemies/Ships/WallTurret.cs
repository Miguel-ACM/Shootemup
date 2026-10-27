using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class WallTurret : DistrictEnemy
{
    protected override void Configure()
    {
        hull = 3;
        health = 35;
        hullScale = .55f;
        attackInterval = 2.3f;
        Mover<DriftDownPattern>();
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        Cue(Muzzle((volley + 1) % 2), seconds);
    }

    protected override void Attack()
    {
        var port = Muzzle(volley % 2);
        var axis = new Vector2(transform.position.x > 0 ? -1 : 1, -1.2f).normalized;
        int n = 3;
        for (int i = 0; i < n; i++)
            Fire<RicochetBolt>(port, CombatVisuals.Rotate(axis, (i - (n - 1) * .5f) * 23), 3.8f, .22f);
    }
}
