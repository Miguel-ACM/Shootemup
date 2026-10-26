using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SkullRevenant : InfernalEnemy
{
    float reveal;
    bool appeared;
    LineRenderer portal;
    protected override void Configure()
    {
        hull = 46;
        hullScale = .47f;
        health = 40;
        attackInterval = Timing(2.1f,1.5f);
        Mover<Slalom>();
    }

    protected override void Update()
    {
        if (!appeared && transform.position.y < Camera.UpLimit - .65f)
        {
            appeared = true;
            portal = CombatVisuals.Line(transform, LevelProjectilePalette.Accent(LevelId.Hell), .045f);
            PSXImpactEffects.Burst(transform.position, new Color(.45f, .94f, .78f), true);
        }
        if (appeared && reveal < .6f)
        {
            if (!GameRules.playerDisabled)
                reveal += Time.deltaTime;
            Hull.localScale = Vector3.one * hullScale * Mathf.SmoothStep(.25f, 1, reveal / .6f);
            CombatVisuals.Ring(portal, transform.position, .65f * (1 - reveal / .6f));
            if (reveal >= .6f)
                Destroy(portal.gameObject);
        }
        base.Update();
    }

    protected override void Attack()
    {
        if(reveal<.6f) return;
        Fan<NeedleRound>(Shots(2,3),19,4.2f,.2f);
        if(volley%2==0) for(int s=-1;s<=1;s+=2) Fire<SimpleRound>(Muzzle(),CombatVisuals.Rotate(Vector2.down,s*52),2.8f,.25f);
    }
}
