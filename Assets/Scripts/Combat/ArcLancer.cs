using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ArcLancer : DistrictEnemy
{
    PulseLaser beam;
    protected override void Configure(){hull=18;health=34;hullScale=.62f;attackInterval=3.5f;Mover<StrafeAndBrake>();}
    protected override void Attack()
    {
        if(beam!=null && beam.IsAttachedTo(Hull))return;
        int side=transform.position.x<0 ? 1 : -1;
        var laser=CombatRound.Launch<PulseLaser>(transform.position,CombatVisuals.Rotate(Aim,-side*9),0);
        if(laser==null)return;
        beam=laser;laser.Bind(Hull,new Vector3(.52f,-1.45f,-.2f));
        laser.warning=.85f;laser.activeTime=.72f;laser.width=.22f;laser.sweep=side*25;
    }
}
