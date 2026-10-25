using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SandEmitter : DistrictEnemy
{
    Transform ripple;
    bool exposed;
    protected override void Configure(){hull=30;hullScale=.63f;health=40;attackInterval=1.3f;}
    protected override void Start()
    {
        base.Start();
        var go=new GameObject("Sand disturbance");go.transform.SetParent(transform,false);ripple=go.transform;
        var r=go.AddComponent<SpriteRenderer>();r.sprite=CombatVisuals.Disc;r.color=new Color(.63f,.42f,.21f,.6f);
        r.sortingLayerName="Enemy";r.sortingOrder=-1;
        Hull.localScale=Vector3.zero;GetComponent<Collider2D>().enabled=false;
    }
    protected override void Update()
    {
        var flight=movement as FormationFlight;
        float progress=flight!=null ? flight.Progress : age/6;
        bool close=player!=null && Vector2.Distance(player.position,transform.position)<2.4f;
        exposed=age>.8f && progress<.80f && !close;
        float scale=exposed ? Mathf.SmoothStep(0,1,(age-.8f)/.22f) : 0;
        Hull.localScale=Vector3.one*hullScale*scale;
        GetComponent<Collider2D>().enabled=scale>.65f;
        if(ripple!=null)ripple.localScale=new Vector3(1.6f+Mathf.Sin(age*15)*.12f,1.1f,1)*(exposed ? 1.2f : 1);
        base.Update();
        if(flight==null){transform.position+=Vector3.down*Time.deltaTime*1.2f;if(age>6)Depart();}
    }
    protected override void Attack()
    {
        if(!exposed || CombatRound.Live.Count+14>CombatRound.Limit)return;
        for(int i=0;i<14;i++)
        {
            Vector2 direction=CombatVisuals.Rotate(Vector2.down,i*360f/14+volley*12.857f);
            Fire<SimpleRound>(transform.position+(Vector3)direction*.52f,direction,3.7f,.14f);
        }
    }
    public override void damage(float amount){if(exposed)base.damage(amount);}
}

public static partial class IndustrialMeshFactory
{
    static void BuildSandEmitter(Builder b)
    {
        b.Drum(Vector3.zero,.73f,.26f,13,8,11);
        b.Drum(new Vector3(0,0,-.19f),.57f,.20f,6,8,11);
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var n=new Vector2(-d.y,d.x);
            b.Prism(new[]{d*.42f+n*.14f,d*.92f+n*.10f,d*1.02f,d*.92f-n*.10f,d*.42f-n*.14f},.16f,13,new Vector3(0,0,-.12f),6);
            b.Drum((Vector3)(d*.53f)+Vector3.back*.36f,.09f,.12f,11,6);
        }
        b.Drum(new Vector3(0,0,-.34f),.32f,.20f,1,8,6);
        b.Drum(new Vector3(0,0,-.47f),.17f,.06f,3,8,11);
    }
}
