using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class BeamDrone : DistrictEnemy
{
    protected override void Configure(){hull=9;health=25;hullScale=.55f;attackInterval=3;Mover<FreeReposition>();}
    protected override void Attack()
    {
        var laser=CombatRound.Launch<PulseLaser>(transform.position,Aim,0);
        if(laser!=null){laser.Bind(Hull,new Vector3(0,-.72f,-.3f));laser.warning=.7f;laser.activeTime=.48f;laser.width=.20f;}
    }
}
