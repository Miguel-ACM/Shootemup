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
    int impacts;
    protected override void Configure(){hull=20;hullScale=1.25f;health=1800;attackInterval=1.6f;}
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
        float speed=(2.4f+(1-HealthFraction)*3.3f)*(phaseTwo ? 1.25f : 1)*(miniature ? 1.15f : 1);
        var p=transform.position+(Vector3)velocity*speed*dt;float r=Radius;
        Vector2 inward=Vector2.zero;
        if((p.x<arena.xMin+r && velocity.x<0)||(p.x>arena.xMax-r && velocity.x>0)){velocity.x=-velocity.x;inward.x=Mathf.Sign(velocity.x);}
        if((p.y<arena.yMin+r && velocity.y<0)||(p.y>arena.yMax-r && velocity.y>0)){velocity.y=-velocity.y;inward.y=Mathf.Sign(velocity.y);}
        p.x=Mathf.Clamp(p.x,arena.xMin+r,arena.xMax-r);p.y=Mathf.Clamp(p.y,arena.yMin+r,arena.yMax-r);transform.position=p;
        if(inward.sqrMagnitude>0)ImpactBurst(inward.normalized);
        if(player!=null && Vector2.Distance(player.position,p)<r)player.SendMessage("ReceiveHazardDamage",14f,SendMessageOptions.DontRequireReceiver);
    }
    void ImpactBurst(Vector2 inward)
    {
        impacts++;
        // Full circular crowns on every physical bounce, including the little saws.
        // Alternate half a quick lane; slower heavy teeth form a second nested ring.
        int small=miniature ? 24 : phaseTwo ? 60 : 44;
        int large=miniature ? 9 : phaseTwo ? 21 : 17;
        int available=Mathf.Min(small+large,CombatRound.Limit-CombatRound.Live.Count);
        int quick=Mathf.RoundToInt(available*small/(float)(small+large));
        int heavy=available-quick;
        void Ring<T>(int count,float speed,float radius,float offset) where T:CombatRound
        {
            for(int i=0;i<count;i++)Fire<T>(transform.position,
                CombatVisuals.Rotate(inward,(i+.5f)*360f/count+offset),speed,radius);
        }
        Ring<SimpleRound>(quick,phaseTwo ? 6 : 5.4f,.11f,impacts%2==0 ? 180f/Mathf.Max(1,quick) : 0);
        Ring<RicochetBolt>(heavy,phaseTwo ? 3.2f : 2.8f,.23f,0);
    }
    protected override void Attack(){} // Only physical wall impacts emit projectiles.
    protected override void OnPhaseTwo()
    {
        if(miniature)return;haltUntil=age+2.2f;
        PSXFireballs.Shockwave(transform.position,2,1.8f,Color.yellow,2,.1f);
        foreach(int side in new[]{-1,1})
        {
            var child=(TunnelSawBoss)Spawn(typeof(TunnelSawBoss),transform.position+Vector3.right*side*1.6f);
            child.miniature=true;child.health=360*GameRules.enemyHealth*LevelRun.EnemyHealthScale;child.hullScale=.65f;child.Hull.localScale=Vector3.one*.65f;
            child.GetComponent<CircleCollider2D>().radius=.6f;child.entered=true;child.velocity=new Vector2(side,.6f).normalized;
        }
    }
}
