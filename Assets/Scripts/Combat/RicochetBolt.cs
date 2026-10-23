using UnityEngine;
public sealed class RicochetBolt : CombatRound
{
    int bounces;
    protected override void Initialize(){bounces=0;visual.color=new Color(1,.8f,.18f);}
    protected override void Tick(float dt)
    {
        base.Tick(dt);var p=transform.position;
        if((p.x<Camera.LeftLimit+.15f && heading.x<0)||(p.x>Camera.RightLimit-.15f && heading.x>0))
        {
            if(bounces++>=1){Release();return;}
            heading.x=-heading.x;p.x=Mathf.Clamp(p.x,Camera.LeftLimit+.15f,Camera.RightLimit-.15f);transform.position=p;
            PSXImpactEffects.Vanish(p,Color.yellow,.3f);Orient();
        }
    }
}
