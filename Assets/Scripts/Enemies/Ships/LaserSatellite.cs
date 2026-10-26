using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class LaserSatellite : SpaceSatellite
{
    protected override void Configure()
    {
        hull = 42;
        hullScale = .56f;
        health = 68;
        attackInterval = Timing(3.0f,2.3f);
        Mover<TopTracker>();
    }

    protected override Vector2 CueAxis=>Vector2.down;

    protected override void Attack()
    {
        var beam=Fire<PulseLaser>(Muzzle(),Vector2.down,0);
        if(beam!=null) { beam.Bind(Hull,new Vector3(0,-1.05f,-.24f)); beam.warning=Timing(1.5f,1.2f); beam.activeTime=Timing(.8f,1.15f); beam.width=.23f; }
    }
}
