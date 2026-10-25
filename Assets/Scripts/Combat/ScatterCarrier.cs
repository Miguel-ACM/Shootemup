using UnityEngine;
public sealed class ScatterCarrier : CombatRound
{
    int releases;
    protected override void Initialize(){releases=0;}
    protected override void Tick(float dt)
    {
        base.Tick(dt);
        if(age>(releases+1)*.65f)
        {
            releases++;var side=new Vector2(-heading.y,heading.x);
            Launch<NeedleRound>(transform.position,side,3,.11f);Launch<NeedleRound>(transform.position,-side,3,.11f);
        }
    }
}
