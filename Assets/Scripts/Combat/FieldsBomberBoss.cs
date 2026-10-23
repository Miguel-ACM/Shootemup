using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class FieldsBomberBoss : DistrictBoss
{
    // 36 seconds of stock-gun fire before dodging/misses; later loops still scale health.
    protected override void Configure(){hull=23;hullScale=1.4f;health=1440;attackInterval=1.6f;}
    protected override void MoveBoss(float dt){station.x=Mathf.Sin(age*.5f)*2;base.MoveBoss(dt);}
    protected override void PressureAttack(){Radial<SimpleRound>(phaseTwo ? 30 : 22,3.3f,.11f,age*24);}
    protected override void Attack()
    {
        if(volley%3==0)Fan<BurstShell>(phaseTwo ? 5 : 3,25,3.4f,.4f);
        else if(volley%3==1)Fan<ScatterCarrier>(phaseTwo ? 4 : 2,35,3,.3f);
        else Fan<NeedleRound>(phaseTwo ? 9 : 5,12,5);
    }
}
