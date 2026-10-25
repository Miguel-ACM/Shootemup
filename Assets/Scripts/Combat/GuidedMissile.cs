using UnityEngine;
public sealed class GuidedMissile : CombatRound
{
    bool committed;
    protected override void Initialize(){committed=false;transform.localScale*=1.6f;Orient();}
    protected override void Tick(float dt)
    {
        if(!committed && (age>1 || transform.position.y<0)){committed=true;visual.color=Color.white;}
        if(!committed)heading=Vector3.RotateTowards(heading,CombatVisuals.Aim(transform.position),dt*1.1f,0);
        base.Tick(dt);
    }
}
