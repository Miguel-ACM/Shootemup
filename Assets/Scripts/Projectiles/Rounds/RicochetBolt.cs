using UnityEngine;
public sealed class RicochetBolt : CombatRound
{
    int wallHits;
    protected override void Initialize()
    {
        wallHits = 0;
    }

    protected override void Tick(float dt)
    {
        base.Tick(dt);
        var p = transform.position;
        if ((p.x < Camera.LeftLimit + .15f && heading.x < 0) || (p.x > Camera.RightLimit - .15f && heading.x > 0))
        {
            if (wallHits++ >= 1)
            {
                Release();
                return;
            }
            heading.x = -heading.x;
            p.x = Mathf.Clamp(p.x, Camera.LeftLimit + .15f, Camera.RightLimit - .15f);
            transform.position = p;
            PSXImpactEffects.Vanish(p, Accent, .3f);
            Orient();
        }
    }
}
