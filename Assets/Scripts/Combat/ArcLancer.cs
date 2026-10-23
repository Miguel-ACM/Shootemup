using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ArcLancer : DistrictEnemy
{
    protected override void Configure(){hull=18;health=34;hullScale=.62f;attackInterval=3.5f;Mover<StrafeAndBrake>();}
    protected override void Attack()
    {
        int side=transform.position.x<0 ? 1 : -1;
        var laser=CombatRound.Launch<PulseLaser>(transform.position,CombatVisuals.Rotate(Aim,-side*9),0);
        if(laser==null)return;
        laser.Bind(Hull,new Vector3(.17f,-.95f,-.3f));
        laser.warning=.85f;laser.activeTime=.72f;laser.width=.22f;laser.sweep=side*25;
    }
}
