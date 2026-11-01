using UnityEngine;
public sealed class RicochetBolt : CombatRound
{
    int wallHits;
    protected override bool Erratic => true;
    protected override void Initialize()
    {
        wallHits = 0;
    }

    protected override void Tick(float dt)
    {
        base.Tick(dt);
        var p = transform.position;
        var border = Border;
        float reach = BorderReach;
        if ((p.x < border.xMin + reach && heading.x < 0) || (p.x > border.xMax - reach && heading.x > 0))
        {
            if (wallHits++ >= 1)
            {
                Release();
                return;
            }
            heading.x = -heading.x;
            p.x = Mathf.Clamp(p.x, border.xMin + reach, border.xMax - reach);
            transform.position = p;
            PSXImpactEffects.Vanish(p, Accent, .3f);
            Orient();
        }
    }
}
