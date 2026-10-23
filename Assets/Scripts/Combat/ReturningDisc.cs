using UnityEngine;
public sealed class ReturningDisc : CombatRound
{
    Vector2 initial;
    protected override void Initialize(){initial=heading;visual.color=new Color(1,.35f,.85f);lifetime=6;}
    protected override void Tick(float dt)
    {
        if(age>1.4f)heading=Vector2.Lerp(heading,((Vector2)(origin-transform.position)).normalized,dt*2).normalized;
        if(age>2 && Vector2.Distance(transform.position,origin)<.4f){Release();return;}
        base.Tick(dt);transform.Rotate(0,0,age*200);
    }
}
