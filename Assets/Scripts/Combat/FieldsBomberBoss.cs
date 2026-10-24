using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class FieldsBomberBoss : DistrictBoss
{
    // Follow the actual wing cutouts, engine shoulders, barrels and long nose.
    static readonly Vector2[] outline={new Vector2(-1.7f,.5f),new Vector2(-.58f,1.1f),new Vector2(0,.8f),new Vector2(.58f,1.1f),new Vector2(1.7f,.5f),
        new Vector2(1.48f,-.08f),new Vector2(1.52f,-.72f),new Vector2(1.28f,-.72f),new Vector2(1.28f,-.58f),new Vector2(.75f,-.9f),
        new Vector2(.45f,-.62f),new Vector2(.28f,-.95f),new Vector2(0,-1.13f),new Vector2(-.28f,-.95f),new Vector2(-.45f,-.62f),
        new Vector2(-.75f,-.9f),new Vector2(-1.28f,-.58f),new Vector2(-1.28f,-.72f),new Vector2(-1.52f,-.72f),new Vector2(-1.48f,-.08f)};
    PolygonCollider2D hullCollider;
    readonly Vector2[] collisionPoints=new Vector2[outline.Length];
    protected override void Start()
    {
        base.Start();GetComponent<CircleCollider2D>().enabled=false;
        hullCollider=gameObject.AddComponent<PolygonCollider2D>();hullCollider.isTrigger=true;
        UpdateHitbox();
    }
    void LateUpdate(){UpdateHitbox();}
    void UpdateHitbox()
    {
        if(hullCollider==null || Hull==null)return;
        for(int i=0;i<outline.Length;i++)collisionPoints[i]=transform.InverseTransformPoint(Hull.TransformPoint(outline[i]));
        hullCollider.SetPath(0,collisionPoints);
    }
    // 36 seconds of stock-gun fire before dodging/misses; later loops still scale health.
    protected override void Configure(){hull=23;hullScale=1.4f;health=1440;attackInterval=1.6f;}
    protected override void MoveBoss(float dt){station.x=Mathf.Sin(age*.5f)*2;base.MoveBoss(dt);}
    protected override void PressureAttack(){Radial<SimpleRound>(phaseTwo ? 30 : 22,3.3f,.11f,age*24);}
    protected override void Attack()
    {
        if(volley%3==0)Fan<BurstShell>(phaseTwo ? 5 : 3,25,3.4f,.4f);
        else if(volley%3==1)Fan<ScatterCarrier>(phaseTwo ? 4 : 2,35,3,.3f);
        else Fan<NeedleRound>(phaseTwo ? 9 : 5,12,5);
    }
}
