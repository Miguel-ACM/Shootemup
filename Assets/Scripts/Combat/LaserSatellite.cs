using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class LaserSatellite : SpaceSatellite
{
    protected override void Configure(){hull=42;hullScale=.56f;health=52;attackInterval=2.0f;Mover<TopTracker>();}
    protected override void Attack()
    {
        var beam=CombatRound.Launch<PulseLaser>(Hull.TransformPoint(new Vector3(0,-1.05f,-.25f)),Vector2.down,0);
        if(beam!=null){beam.Bind(Hull,new Vector3(0,-1.05f,-.25f));beam.warning=1.1f;beam.activeTime=.65f;beam.width=.22f;}
    }
}
