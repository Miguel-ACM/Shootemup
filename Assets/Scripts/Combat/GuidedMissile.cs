using UnityEngine;
public sealed class GuidedMissile : CombatRound
{
    bool committed;
    protected override void Initialize(){committed=false;visual.color=new Color(.9f,.4f,1);transform.localScale=Vector3.Scale(transform.localScale,new Vector3(.7f,1.6f,1));}
    protected override void Tick(float dt)
    {
        if(!committed && (age>1 || transform.position.y<0)){committed=true;visual.color=Color.white;}
        if(!committed)heading=Vector3.RotateTowards(heading,CombatVisuals.Aim(transform.position),dt*1.1f,0);
        base.Tick(dt);
    }
}
