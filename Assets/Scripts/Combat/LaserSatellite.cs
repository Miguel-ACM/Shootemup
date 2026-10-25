using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class LaserSatellite : SpaceSatellite
{
    protected override void Configure(){hull=42;hullScale=.56f;health=52;attackInterval=2.0f;Mover<TopTracker>();}
    protected override void Attack()
    {
        var port=Hull.TransformPoint(new Vector3(0,-1.05f,-.25f));
        float angle=Mathf.Clamp(Vector2.SignedAngle(Vector2.down,CombatVisuals.Aim(port)),-32,32);
        var beam=CombatRound.Launch<PulseLaser>(port,CombatVisuals.Rotate(Vector2.down,angle),0);
        if(beam!=null){beam.Bind(Hull,new Vector3(0,-1.05f,-.25f));beam.warning=1.0f;beam.activeTime=.70f;beam.width=.22f;beam.sweep=volley%2==0 ? -8 : 8;}
    }
}
