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
    Vector2 phraseAxis=Vector2.down;
    protected override void Configure(){hull=23;hullScale=1.4f;health=1440;attackInterval=.55f;shellTravelDistance=4.6f;}
    protected override void MoveBoss(float dt){station.x=Mathf.Sin(age*.5f)*2;base.MoveBoss(dt);}
    protected override void PressureAttack(){} // One score owns every battery and its rests.
    protected override void Attack()
    {
        int beat=(volley-1)%12;
        if(beat==0 || beat==4 || beat==8)phraseAxis=Aim;
        if(beat<3)for(int side=-1;side<=1;side+=2)
        {
            var port=Hull.TransformPoint(new Vector3(side*1.34f,.32f,-.36f));
            var axis=CombatVisuals.Rotate(Vector2.down,side*(32+beat*12));
            PSXImpactEffects.Muzzle(port,axis);
            PetalFan<SimpleRound>(port,axis,phaseTwo ? 9 : 7,12,phaseTwo ? 3.9f : 3.3f,.13f,.18f);
        }
        else if(beat==4)FanFrom<BurstShell>(phaseTwo ? 6 : 4,19,4.8f,.4f,bays);
        else if(beat==5)FanFrom<ScatterCarrier>(phaseTwo ? 6 : 4,22,3.7f,.3f,bays);
        else if(beat>=8 && beat<=10)for(int side=-1;side<=1;side+=2)
        {
            var port=Hull.TransformPoint(new Vector3(side*1.4f,-.68f,-.16f));
            var axis=CombatVisuals.Rotate(phraseAxis,side*(16-(beat-8)*8));
            PSXImpactEffects.Muzzle(port,axis);
            PetalFan<NeedleRound>(port,axis,phaseTwo ? 11 : 7,9,phaseTwo ? 6.3f : 5.2f,.14f);
        }
        // Shoulder petals, shell bloom, then converging wing ribbons; 3/6/7/11 breathe.
    }
    protected override void OnPhaseTwo(){volley=0;nextAttack=age+1;}
}
