using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ThunderLanceBoss : DistrictBoss
{
    Vector2 barrageAxis=Vector2.down;
    protected override void Configure(){hull=22;hullScale=1.25f;health=2800;attackInterval=.55f;}
    protected override void MoveBoss(float dt)
    {
        station.x=phaseTwo ? 0 : Mathf.Sin(age*.7f)*2.1f;base.MoveBoss(dt);
    }
    protected override void PressureAttack(){}
    protected override void Attack()
    {
        if(!phaseTwo)
        {
            int beat=(volley-1)%12;
            if(beat==0 || beat==6)
            {
                int sign=beat==0 ? 1 : -1;
                foreach(int side in new[]{-1,1})
                {
                    var laser=Fire<PulseLaser>(transform.position,CombatVisuals.Rotate(Vector2.down,side*40-sign*10),0,.18f);
                    if(laser!=null){laser.Bind(Hull,new Vector3(side*.71f,.435f,-.3f));laser.activeTime=2.0f;laser.warning=1.05f;laser.sweep=sign*12;laser.width=.22f;}
                }
            }
            else if(beat==2 || beat==3 || beat==8 || beat==9)
            {
                if(beat==2 || beat==8)barrageAxis=Aim;
                for(int i=-3;i<=3;i++)Fire<NeedleRound>(Hull.TransformPoint(new Vector3(0,-1.98f,-.2f)),
                    CombatVisuals.Rotate(barrageAxis,i*8),7,.13f);
            }
        }
        else
        {
            int beat=(volley-1)%14,sign=((volley-1)/14)%2==0 ? 1 : -1;
            if(beat==0)for(int i=0;i<7;i++)
            {
                var beam=Fire<PulseLaser>(transform.position,CombatVisuals.Rotate(Vector2.down,(i-3)*18-sign*18),0,.17f);
                if(beam!=null)
                {
                    var socket=i==3 ? new Vector3(0,-1.98f,-.2f) : new Vector3(i<3 ? -.71f : .71f,.435f-(i<3 ? i : 6-i)*.47f,-.3f);
                    beam.Bind(Hull,socket);beam.warning=1.25f;beam.activeTime=3.6f;beam.width=.17f;beam.sweep=sign*10;
                }
            }
            if(beat==4)barrageAxis=Aim;
            if(beat>=4 && beat<=11)
                for(int i=-4;i<=4;i++)Fire<NeedleRound>(Hull.TransformPoint(new Vector3(0,-1.98f,-.2f)),
                    CombatVisuals.Rotate(barrageAxis,i*6.5f+(beat%2==0 ? -3.25f : 3.25f)),7.4f,.13f);
        }
    }
    protected override void OnPhaseTwo(){volley=0;nextAttack=age+1.5f;}
}
