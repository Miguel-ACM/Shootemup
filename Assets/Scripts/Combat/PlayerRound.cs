using UnityEngine;
using System.Collections.Generic;

public enum PlayerDelivery { Pulse, Needle, Shell, Disc, Missile, Flame }
public sealed class PlayerRound : ABullet
{
    public static int LiveCount {get;private set;}
    public PlayerDelivery delivery;
    public int generation;
    public bool companion;
    public int pierces;
    public bool returns;
    float age;
    Vector3 origin;
    Vector2 heading;
    bool detonated, returning, rebounded;
    readonly HashSet<AEnemy> hit=new HashSet<AEnemy>();
    SpriteRenderer visual;
    public static PlayerRound Spawn(Vector3 p,Vector2 d,float damage,float speed,PlayerDelivery mode=PlayerDelivery.Pulse,int generation=0)
    {
        if(LiveCount>=180)return null;
        var go=new GameObject("Friendly "+mode);go.tag="PlayerBullet";go.layer=3;go.transform.position=new Vector3(p.x,p.y,-1);
        var rb=go.AddComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Kinematic;rb.gravityScale=0;
        go.AddComponent<CircleCollider2D>().isTrigger=true;
        var sr=go.AddComponent<SpriteRenderer>();sr.sprite=CombatVisuals.Disc;sr.sortingLayerName="Bullet";sr.sortingOrder=16;
        sr.color=mode==PlayerDelivery.Flame ? new Color(1,.65f,.2f) : new Color(.35f,1,.78f);
        var b=go.AddComponent<PlayerRound>();b.visual=sr;b.damage=damage;b.speed=speed;b.delivery=mode;b.generation=generation;
        b.origin=p;b.heading=d.normalized;b.pierces=mode==PlayerDelivery.Needle ? 1 : 0;
        float radius=mode==PlayerDelivery.Shell ? .22f : mode==PlayerDelivery.Disc ? .2f : .11f;
        go.transform.localScale=Vector3.one*radius*2*Mathf.Sqrt(GameRules.playerShootSize);
        if(mode==PlayerDelivery.Needle)go.transform.localScale=Vector3.Scale(go.transform.localScale,new Vector3(.6f,2.6f,1));
        b.returns=generation==0 && CombatUpgrades.Current!=null && CombatUpgrades.Current.ReturnThisVolley;
        return b;
    }
    void OnEnable(){LiveCount++;}
    void OnDisable(){LiveCount--;}
    void Update()
    {
        age+=Time.deltaTime;
        if(age>5 || transform.position.y<Camera.DownLimit-3 || Mathf.Abs(transform.position.x)>Camera.RightLimit+4){Destroy(gameObject);return;}
        if((delivery==PlayerDelivery.Disc || returns) && age>.75f && !returning){returning=true;hit.Clear();}
        if(returning)
        {
            heading=Vector3.RotateTowards(heading,((Vector2)(origin-transform.position)).normalized,Time.deltaTime*5,0);
            if(age>1.3f && Vector2.Distance(transform.position,origin)<.5f){Destroy(gameObject);return;}
        }
        else if(delivery==PlayerDelivery.Missile || GameRules.playerShootFollowness>0)
        {
            var target=CombatUpgrades.Nearest(transform.position,hit,7);
            if(target!=null)heading=Vector3.RotateTowards(heading,((Vector2)(target.transform.position-transform.position)).normalized,
                Time.deltaTime*(delivery==PlayerDelivery.Missile ? 3 : Mathf.Min(3,GameRules.playerShootFollowness)),0);
        }
        float brake=delivery==PlayerDelivery.Shell ? Mathf.Clamp01(1-age/.7f) : 1;
        transform.position+=(Vector3)heading*speed*brake*Time.deltaTime;
        transform.rotation=Quaternion.FromToRotation(Vector3.up,heading);
        if(delivery==PlayerDelivery.Flame){visual.color=Color.Lerp(Color.yellow,new Color(1,.25f,.1f,.1f),age/.55f);if(age>.55f)Destroy(gameObject);}
        if(delivery==PlayerDelivery.Shell && age>1.05f)Detonate();
        if(generation==0 && CombatUpgrades.Current!=null && CombatUpgrades.Current.ProximityReady && age>.1f)
        {
            var e=CombatUpgrades.Nearest(transform.position,null,.8f);
            if(e!=null && CombatUpgrades.Current.SpendProximity())Detonate();
        }
        if(transform.position.y>Camera.UpLimit+3 && !returns && delivery!=PlayerDelivery.Disc)Destroy(gameObject);
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        var enemy=collision.GetComponent<AEnemy>();
        if(enemy==null || enemy.health<=0 || !hit.Add(enemy))return;
        float dealt=CombatUpgrades.Current!=null ? CombatUpgrades.Current.DirectHit(enemy,damage,generation,companion,delivery==PlayerDelivery.Flame) : damage;
        enemy.HitAt(collision.ClosestPoint(transform.position),heading);enemy.damage(dealt);Score.IncreaseScore(Mathf.RoundToInt(dealt));
        if(delivery==PlayerDelivery.Shell){Detonate();return;}
        if(pierces-->0 || delivery==PlayerDelivery.Disc)return;
        if(!rebounded && CombatUpgrades.Current!=null && CombatUpgrades.Current.TryRebound())
        {
            rebounded=true;
            var next=CombatUpgrades.Nearest(transform.position,hit,5);
            if(next!=null){heading=((Vector2)(next.transform.position-transform.position)).normalized;damage*=.75f;generation=1;return;}
        }
        Destroy(gameObject);
    }
    void Detonate()
    {
        if(detonated)return;detonated=true;
        PSXImpactEffects.Vanish(transform.position,new Color(.4f,1,.8f),.7f);
        for(int i=0;i<7;i++)Spawn(transform.position,CombatVisuals.Rotate(heading,(i-3)*24),damage*.45f,10,PlayerDelivery.Pulse,1);
        Destroy(gameObject);
    }
}
