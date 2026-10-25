using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class InfernalFerryman : DistrictMiniboss
{
    protected override void Configure(){hull=59;hullScale=1;health=650;attackInterval=.46f;nextAttack=1.0f;}
    protected override void Attack()
    {
        int beat=(volley-1)%10;
        if(beat<3 || beat==6 || beat==7)
        {
            for(int side=-1;side<=1;side+=2)
            {
                var p=Hull.TransformPoint(new Vector3(side*.88f,.45f-(beat%3)*.55f,-.3f));
                for(int i=-2;i<=2;i++)Fire<SimpleRound>(p,CombatVisuals.Rotate(CombatVisuals.Aim(p),i*15+(beat%3-1)*6),4.7f,.15f);
            }
        }
        else if(beat==4)Fan<NeedleRound>(9,10,6,.14f);
    }
}
