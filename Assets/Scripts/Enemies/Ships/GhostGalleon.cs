using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class GhostGalleon : InfernalEnemy
{
    protected override void Configure()
    {
        hull = 47;
        hullScale = .64f;
        health = 95;
        attackInterval = Timing(2.3f,1.8f);
        Mover<TopTracker>();
    }

    protected override void Attack()
    {
        for(int side=-1;side<=1;side+=2)
            for(int i=0;i<Shots(2,3);i++) {
                var port=Hull.TransformPoint(new Vector3(side*.61f,.43f-i*.42f,-.22f));
                Fire<SimpleRound>(port,CombatVisuals.Rotate(Vector2.down,side*(35+i*17+(volley%2)*7)),2.9f+i*.16f,.25f);
            }
        if(volley%2==0) Fan<NeedleRound>(Shots(2,3),17,4.5f,.18f);
    }
}
