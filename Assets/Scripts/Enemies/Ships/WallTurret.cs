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

    protected override void Attack()
    {
        var port = Hull.TransformPoint(new Vector3(volley % 2 == 0 ? .22f : -.22f, -1.115f, -.26f));
        var axis = new Vector2(transform.position.x > 0 ? -1 : 1, -.8f).normalized;
        for (int i = -1; i <= 1; i++)
            Fire<RicochetBolt>(port, CombatVisuals.Rotate(axis, i * 15), 4.8f, .18f);
    }
}
