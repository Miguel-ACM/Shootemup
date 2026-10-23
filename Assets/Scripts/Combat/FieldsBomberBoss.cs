using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class FieldsBomberBoss : DistrictBoss
{
    protected override void Configure(){hull=23;hullScale=1.4f;health=1200;attackInterval=2.2f;}
    protected override void MoveBoss(float dt){station.x=Mathf.Sin(age*.5f)*2;base.MoveBoss(dt);}
    protected override void Attack()
    {
        if(volley%3==0)Fan<BurstShell>(phaseTwo ? 5 : 3,25,3.4f,.4f);
        else if(volley%3==1)Fan<ScatterCarrier>(phaseTwo ? 4 : 2,35,3,.3f);
        else Fan<NeedleRound>(phaseTwo ? 9 : 5,12,5);
    }
}
