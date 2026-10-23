using UnityEngine;
public sealed class ScatterCarrier : CombatRound
{
    int releases;
    protected override void Initialize(){releases=0;visual.color=new Color(1,.7f,.3f);}
    protected override void Tick(float dt)
    {
        base.Tick(dt);
        if(releases<3 && age>(releases+1)*.65f)
        {
            releases++;var side=new Vector2(-heading.y,heading.x);
            Launch<NeedleRound>(transform.position,side,3,.11f);Launch<NeedleRound>(transform.position,-side,3,.11f);
        }
    }
}
