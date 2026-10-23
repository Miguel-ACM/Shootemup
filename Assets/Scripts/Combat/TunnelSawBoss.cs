using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class TunnelSawBoss : DistrictBoss
{
    public bool miniature;
    public override bool UsesHealthBar=>!miniature;
    Vector2 velocity=new Vector2(1,.72f).normalized;
    float haltUntil;
    Rect arena;
    float Radius=>Hull.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x*Hull.localScale.x;
    bool entered;
    protected override void Configure(){hull=20;hullScale=1.25f;health=2800;attackInterval=1.6f;}
    protected override void Start()
    {
        base.Start();GetComponent<CombatHullVisual>().spin=miniature ? 900 : 720;
        var camera=UnityEngine.Camera.main;float h=camera.orthographicSize;
        arena=new Rect(camera.transform.position.x-h*405f/720f,camera.transform.position.y-h,h*810f/720f,h*2);
        if(miniature){isBoss=false;bar=null;lifeDropChance=manaDropChance=moneyDropChance=0;scoreOnKill=100;}
    }
    protected override void MoveBoss(float dt)
    {
        if(!entered)
        {
            transform.position=Vector3.MoveTowards(transform.position,new Vector3(0,Camera.UpLimit-2,1),dt*3);
            if(transform.position.y<Camera.UpLimit-1.9f)
            {
                entered=true;
                velocity=new Vector2(arena.xMax-Radius-transform.position.x,arena.yMin+Radius-transform.position.y).normalized;
            }
            return;
        }
        if(age<haltUntil)return;
        float speed=(2.0f+(1-HealthFraction)*3)*(phaseTwo ? 1.25f : 1)*(miniature ? 1.15f : 1);
        var p=transform.position+(Vector3)velocity*speed*dt;float r=Radius;
        bool bounce=false;
        if((p.x<arena.xMin+r && velocity.x<0)||(p.x>arena.xMax-r && velocity.x>0)){velocity.x=-velocity.x;bounce=true;}
        if((p.y<arena.yMin+r && velocity.y<0)||(p.y>arena.yMax-r && velocity.y>0)){velocity.y=-velocity.y;bounce=true;}
        p.x=Mathf.Clamp(p.x,arena.xMin+r,arena.xMax-r);p.y=Mathf.Clamp(p.y,arena.yMin+r,arena.yMax-r);transform.position=p;
        if(bounce)ImpactBurst();
        if(player!=null && Vector2.Distance(player.position,p)<r)player.SendMessage("ReceiveHazardDamage",14f,SendMessageOptions.DontRequireReceiver);
    }
    void ImpactBurst()
    {
        Radial<SimpleRound>(miniature ? 24 : phaseTwo ? 64 : 48,5.2f,.11f,age*20);
        Radial<RicochetBolt>(miniature ? 12 : 24,2.3f,.23f,age*20+7);
    }
    protected override void Attack(){} // Only physical wall impacts emit projectiles.
    protected override void OnPhaseTwo()
    {
        if(miniature)return;haltUntil=age+2.2f;
        PSXFireballs.Shockwave(transform.position,2,1.8f,Color.yellow,2,.1f);
        foreach(int side in new[]{-1,1})
        {
            var child=(TunnelSawBoss)Spawn(typeof(TunnelSawBoss),transform.position+Vector3.right*side*1.6f);
            child.miniature=true;child.health=480*GameRules.enemyHealth*LevelRun.EnemyHealthScale;child.hullScale=.65f;child.Hull.localScale=Vector3.one*.65f;
            child.GetComponent<CircleCollider2D>().radius=.6f;child.entered=true;child.velocity=new Vector2(side,.6f).normalized;
        }
    }
}
