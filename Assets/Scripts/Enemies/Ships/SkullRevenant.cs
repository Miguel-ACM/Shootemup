using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SkullRevenant : InfernalEnemy
{
    float reveal;
    bool appeared;
    LineRenderer portal;
    protected override void Configure(){hull=46;hullScale=.47f;health=40;attackInterval=1.25f;Mover<Slalom>();}
    protected override void Update()
    {
        if(!appeared && transform.position.y<Camera.UpLimit-.65f)
        {
            appeared=true;portal=CombatVisuals.Line(transform,LevelProjectilePalette.Accent(LevelId.Hell),.045f);
            PSXImpactEffects.Burst(transform.position,new Color(.45f,.94f,.78f),true);
        }
        if(appeared && reveal<.6f)
        {
            if(!GameRules.playerDisabled)reveal+=Time.deltaTime;
            Hull.localScale=Vector3.one*hullScale*Mathf.SmoothStep(.25f,1,reveal/.6f);
            CombatVisuals.Ring(portal,transform.position,.65f*(1-reveal/.6f));
            if(reveal>=.6f)Destroy(portal.gameObject);
        }
        base.Update();
    }
    protected override void Attack()
    {
        if(reveal<.6f)return;
        var port=Hull.TransformPoint(new Vector3(0,-.48f,-.4f));
        for(int i=-1;i<=1;i++)Fire<NeedleRound>(port,CombatVisuals.Rotate(Aim,i*13),5.1f+((volley&1)==0 ? .4f : 0),.15f);
        for(int side=-1;side<=1;side+=2)Fire<SimpleRound>(port,CombatVisuals.Rotate(Vector2.down,side*(40+volley%3*11)),3.5f,.18f);
    }
}
