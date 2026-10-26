using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class PursuitFighter : DistrictEnemy
{
    PulseLaser beam;
    protected override void Configure()
    {
        hull = 12;
        health = 20;
        hullScale = .45f;
        attackInterval = Timing(3.5f,2.8f);
        Mover<TopTracker>();
    }

    public override bool NoseAimed=>false;
    protected override Vector2 CueAxis=>Vector2.down;

    protected override void Attack()
    {
        if(beam!=null && beam.IsAttachedTo(Hull)) return;
        beam=Fire<PulseLaser>(Muzzle(),Vector2.down,0);
        if(beam!=null) { beam.Bind(Hull,new Vector3(0,-1.38f,-.15f)); beam.warning=Timing(1.25f,1.0f); beam.activeTime=Timing(.35f,.55f); beam.width=.19f; }
    }
}
