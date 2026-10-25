using UnityEngine;
public sealed class ScatterCarrier : CombatRound
{
    int releases;
    protected override void Initialize(){releases=0;Orient();}
    protected override void Tick(float dt)
    {
        base.Tick(dt);
        if(age>(releases+1)*.65f)
        {
            releases++;var side=new Vector2(-heading.y,heading.x);
            float muzzle=transform.localScale.x*.435f;
            var center=transform.position-(Vector3)heading*transform.localScale.y*.022f;
            Launch<NeedleRound>(center+(Vector3)side*muzzle,side,3,.11f);
            Launch<NeedleRound>(center-(Vector3)side*muzzle,-side,3,.11f);
        }
    }
}
