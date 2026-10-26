using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class BeamDrone : DistrictEnemy
{
    PulseLaser beam;
    protected override void Configure()
    {
        hull = 9;
        health = 25;
        hullScale = .55f;
        attackInterval = Timing(3.6f,2.9f);
        Mover<FreeReposition>();
    }

    public override bool NoseAimed=>false;
    protected override Vector2 CueAxis=>Vector2.down;

    protected override void Attack()
    {
        if(beam!=null && beam.IsAttachedTo(Hull)) return;
        beam=Fire<PulseLaser>(Muzzle(),Vector2.down,0);
        if(beam!=null) { beam.Bind(Hull,new Vector3(0,-.28f,-.24f)); beam.warning=Timing(1.3f,1.05f); beam.activeTime=Timing(.6f,.85f); beam.width=.21f; }
    }
}
