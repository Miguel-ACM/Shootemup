using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SandEmitter : DistrictEnemy
{
    Transform ripple;
    static Sprite sandRipple;
    bool exposed;
    public override bool CanBeTargeted=>health>0 && exposed && Hull!=null && Hull.localScale.x>hullScale*.65f;
    protected override void Configure(){hull=30;hullScale=.63f;health=40;attackInterval=1.3f;}
    protected override void Start()
    {
        base.Start();
        var go=new GameObject("Sand disturbance");go.transform.SetParent(transform,false);ripple=go.transform;
        var r=go.AddComponent<SpriteRenderer>();r.sprite=Ripple();r.color=new Color(.88f,.65f,.35f,.8f);
        r.sortingLayerName="Enemy";r.sortingOrder=-1;
        Hull.localScale=Vector3.zero;GetComponent<Collider2D>().enabled=false;
    }
    protected override void Update()
    {
        var flight=movement as FormationFlight;
        float progress=flight!=null ? flight.Progress : age/6;
        bool close=player!=null && Vector2.Distance(player.position,transform.position)<2.4f;
        exposed=age>.8f && progress>.24f && progress<.80f && !close;
        float scale=exposed ? Mathf.SmoothStep(0,1,(progress-.24f)/.04f) : 0;
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
    static Sprite Ripple()
    {
        if(sandRipple!=null)return sandRipple;
        var texture=new Texture2D(64,64,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};var pixels=new Color[4096];
        for(int y=0;y<64;y++)for(int x=0;x<64;x++)
        {
            float dx=x-31.5f,dy=y-31.5f,a=Mathf.Atan2(dy,dx),r=Mathf.Sqrt(dx*dx+dy*dy)+Mathf.Sin(a*9)*1.4f;
            bool ridge=Mathf.Abs(r-26)<1.5f || Mathf.Abs(r-19)<1;
            pixels[y*64+x]=ridge && (x*7+y*11)%9!=0 ? new Color(1,1,1,.7f) : Color.clear;
        }
        texture.SetPixels(pixels);texture.Apply();return sandRipple=Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(.5f,.5f),64);
    }
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
