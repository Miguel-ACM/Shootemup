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
        base.Start();var oldCollider=GetComponent<CircleCollider2D>();oldCollider.enabled=false;Destroy(oldCollider);
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
    static readonly Vector3[] bays={new Vector3(-1.19f,-.24f,-.51f),new Vector3(-.97f,-.36f,-.51f),new Vector3(-.75f,-.48f,-.51f),
        new Vector3(.75f,-.48f,-.51f),new Vector3(.97f,-.36f,-.51f),new Vector3(1.19f,-.24f,-.51f)};
    protected override void Configure(){hull=23;hullScale=1.4f;health=1440;attackInterval=1.25f;shellTravelDistance=4.6f;}
    protected override void MoveBoss(float dt){station.x=Mathf.Sin(age*.5f)*2;base.MoveBoss(dt);}
    protected override float PressureInterval=>phaseTwo ? .95f : 1.45f;
    protected override void PressureAttack()
    {
        // Shoulder batteries sweep outward; the main barrels own the aimed volleys.
        int count=phaseTwo ? 9 : 7;
        foreach(int side in new[]{-1,1})
        {
            Vector3 port=Hull.TransformPoint(new Vector3(side*1.34f,.32f,-.36f));
            Vector2 axis=CombatVisuals.Rotate(Vector2.down,side*(48+18*Mathf.Sin(age*.8f)));
            PSXImpactEffects.Muzzle(port,axis);
            for(int i=0;i<count;i++)CombatRound.Launch<SimpleRound>(port,CombatVisuals.Rotate(axis,(i-(count-1)*.5f)*14),phaseTwo ? 3.9f : 3.3f,.13f);
        }
    }
    protected override void Attack()
    {
        if(volley%4==0)FanFrom<BurstShell>(phaseTwo ? 6 : 4,19,4.8f,.4f,bays);
        else if(volley%4==2)FanFrom<ScatterCarrier>(phaseTwo ? 6 : 4,22,3.7f,.3f,bays);
        else foreach(int side in new[]{-1,1})
        {
            Vector3 port=Hull.TransformPoint(new Vector3(side*1.4f,-.68f,-.16f));
            Vector2 axis=CombatVisuals.Rotate(Aim,volley%4==1 ? side*9 : -side*18);
            PSXImpactEffects.Muzzle(port,axis);
            int count=phaseTwo ? 11 : 7;
            for(int i=0;i<count;i++)CombatRound.Launch<NeedleRound>(port,CombatVisuals.Rotate(axis,(i-(count-1)*.5f)*9),phaseTwo ? 6.3f : 5.2f,.14f);
        }
    }
}
