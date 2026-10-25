using UnityEngine;
public sealed class ReturningDisc : CombatRound
{
    Vector2 initial;
    protected override void Initialize(){initial=heading;lifetime=6;}
    protected override void Tick(float dt)
    {
        if(age>1.4f)
        {
            var target=((Vector2)(origin-transform.position)).normalized;
            float turn=Mathf.DeltaAngle(Vector2.SignedAngle(Vector2.up,heading),Vector2.SignedAngle(Vector2.up,target));
            heading=CombatVisuals.Rotate(heading,Mathf.Clamp(turn,-240*dt,240*dt));
        }
        if(age>2 && Vector2.Distance(transform.position,origin)<.4f){Release();return;}
        base.Tick(dt);transform.Rotate(0,0,age*200);
    }
}
