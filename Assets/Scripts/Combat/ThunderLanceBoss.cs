using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ThunderLanceBoss : DistrictBoss
{
    float nextBarrage;
    protected override void Configure(){hull=22;hullScale=1.25f;health=1250;attackInterval=3.2f;}
    protected override void MoveBoss(float dt)
    {
        station.x=phaseTwo ? 0 : Mathf.Sin(age*.7f)*2.1f;base.MoveBoss(dt);
        if(phaseTwo && age>nextBarrage && transform.position.y<Camera.UpLimit-1)
        {nextBarrage=age+.85f;Fan<NeedleRound>(3,10,6);}
    }
    protected override void Attack()
    {
        if(!phaseTwo)
        {
            if(volley%2==0)
            {
                var laser=CombatRound.Launch<PulseLaser>(transform.position,Aim,0);
                if(laser!=null){laser.sideShots=true;laser.activeTime=1.7f;laser.warning=1.1f;}
            }
            else Fan<NeedleRound>(5,7,7);
        }
        else
        {
            attackInterval=7.5f;
            int sign=volley%2==0 ? 1 : -1;
            for(int i=0;i<7;i++)
            {
                var beam=CombatRound.Launch<PulseLaser>(transform.position,CombatVisuals.Rotate(Vector2.down,(i-3)*18-sign*18),0);
                if(beam!=null){beam.warning=1.25f;beam.activeTime=3.6f;beam.width=.17f;beam.sweep=sign*10;}
            }
        }
    }
    protected override void OnPhaseTwo(){nextAttack=age+1.5f;nextBarrage=age+2.8f;}
}
