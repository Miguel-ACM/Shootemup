using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class TunnelSawBoss : DistrictBoss
{
    public bool miniature;
    Vector2 velocity=new Vector2(1,.72f).normalized;
    float haltUntil, nextImpact;
    bool entered;
    protected override void Configure(){hull=20;hullScale=1.25f;health=1250;attackInterval=2.8f;}
    protected override void Start()
    {
        base.Start();GetComponent<CombatHullVisual>().spin=110;
        if(miniature){isBoss=false;bar=null;lifeDropChance=manaDropChance=moneyDropChance=0;scoreOnKill=100;}
    }
    protected override void MoveBoss(float dt)
    {
        if(!entered)
        {
            transform.position=Vector3.MoveTowards(transform.position,new Vector3(0,Camera.UpLimit-2,1),dt*3);
            if(transform.position.y<Camera.UpLimit-1.9f)entered=true;
            return;
        }
        if(age<haltUntil)return;
        float speed=(2.0f+(1-HealthFraction)*3)*(phaseTwo ? 1.25f : 1)*(miniature ? 1.15f : 1);
        var p=transform.position+(Vector3)velocity*speed*dt;float r=miniature ? .75f : 1.45f;
        bool bounce=false;
        if((p.x<Camera.LeftLimit+r && velocity.x<0)||(p.x>Camera.RightLimit-r && velocity.x>0)){velocity.x=-velocity.x;bounce=true;}
        if((p.y<Camera.DownLimit+r+1 && velocity.y<0)||(p.y>Camera.UpLimit-r && velocity.y>0)){velocity.y=-velocity.y;bounce=true;}
        p.x=Mathf.Clamp(p.x,Camera.LeftLimit+r,Camera.RightLimit-r);p.y=Mathf.Clamp(p.y,Camera.DownLimit+r+1,Camera.UpLimit-r);transform.position=p;
        if(bounce)ImpactBurst();
        if(player!=null && Vector2.Distance(player.position,p)<r)player.SendMessage("ReceiveHazardDamage",14f,SendMessageOptions.DontRequireReceiver);
    }
    void ImpactBurst()
    {
        if(age<nextImpact)return;nextImpact=age+.65f;
        Radial<SimpleRound>(miniature ? 6 : 10,4.5f,.12f,age*20);
        Radial<RicochetBolt>(miniature ? 4 : 6,2.2f,.28f,age*20+20);
    }
    public override void damage(float amount){base.damage(amount);if(health>0 && entered)ImpactBurst();}
    protected override void Attack(){if(age>=haltUntil)Fan<RicochetBolt>(miniature ? 2 : 3,28,4);}
    protected override void OnPhaseTwo()
    {
        if(miniature)return;haltUntil=age+2.2f;
        PSXFireballs.Shockwave(transform.position,2,1.8f,Color.yellow,2,.1f);
        foreach(int side in new[]{-1,1})
        {
            var child=(TunnelSawBoss)Spawn(typeof(TunnelSawBoss),transform.position+Vector3.right*side*1.6f);
            child.miniature=true;child.health=240*GameRules.enemyHealth;child.hullScale=.65f;child.Hull.localScale=Vector3.one*.65f;
            child.GetComponent<CircleCollider2D>().radius=.6f;child.entered=true;child.velocity=new Vector2(side,.6f).normalized;
        }
    }
}
