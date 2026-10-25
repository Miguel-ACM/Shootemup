using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 32-bit era explosions: several low-poly spheres with a scrolling fire skin that pop in
/// staggered, grow, spin, cool from white-hot to deep red and dither away. Pooled.
/// </summary>
public sealed class PSXFireballs : MonoBehaviour
{
    const int PoolLimit=96;
    static PSXFireballs instance;
    static Mesh sphere,ring;
    Material material;
    readonly List<Ball> active=new List<Ball>();
    readonly Stack<Ball> pool=new Stack<Ball>();
    MaterialPropertyBlock properties;
    static readonly float[] Pitch={.82f,1.18f};
    static readonly int ColorId=Shader.PropertyToID("_Color"),FadeId=Shader.PropertyToID("_Fade");
    public static int Spawned { get; private set; }

    sealed class Ball
    {
        public Transform transform;public MeshRenderer renderer;
        public float start,life,size;public Vector3 axis,drift;public float spin;
        public bool isRing;public Color tint;public MeshFilter filter;public float expand=1;public bool sounded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){instance=null;Spawned=0;}

    /// <summary>A burst of count spheres spread over radius, the largest about size across.</summary>
    public static void Spawn(Vector3 position,float size,int count,float radius,float stagger)
    {
        if(!Ready())return;
        for(int i=0;i<count;i++)
        {
            if(instance.active.Count>=PoolLimit)return;
            var offset=i==0 ? Vector2.zero : Random.insideUnitCircle*radius;
            float s=size*(i==0 ? 1f : Random.Range(.45f,.85f));
            instance.Add(position+(Vector3)offset,s,i==0 ? 0 : Random.Range(0,stagger),Random.Range(.42f,.6f)+s*.04f);
        }
    }

    /// <summary>Flat expanding shockwave ring (e.g. the blank bullet clearing radius) with the same
    /// scrolling skin, tinted, reaching radius over life seconds while dithering away.</summary>
    /// <param name="expand">Fraction of life spent expanding; the ring then holds its radius (with a slight
    /// pulse) and fades only near the end, so a long-lived ring can show a lasting effect area.</param>
    public static void Shockwave(Vector3 position,float radius,float life,Color tint,int rings=2,float expand=1)
    {
        if(!Ready())return;
        for(int i=0;i<rings;i++)
        {
            if(instance.active.Count>=PoolLimit)break;
            var ball=instance.Add(position,radius*(i==0 ? 1 : .8f),i*.08f,life);
            ball.isRing=true;ball.tint=tint;ball.spin=0;ball.drift=Vector3.zero;ball.expand=Mathf.Clamp(expand,.05f,1);
            ball.filter.sharedMesh=Ring();ball.transform.rotation=Quaternion.identity;
        }
    }

    static bool Ready()
    {
        if(instance!=null)return true;
        var material=Resources.Load<Material>("Effects/PSXv2Fireball");
        if(material==null)return false;
        instance=new GameObject("PSXv2 fireballs").AddComponent<PSXFireballs>();
        instance.material=material;instance.properties=new MaterialPropertyBlock();
        return true;
    }

    Ball Add(Vector3 position,float size,float delay,float life)
    {
        var ball=pool.Count>0 ? pool.Pop() : Create();
        ball.isRing=false;ball.sounded=false;ball.filter.sharedMesh=Sphere();
        ball.transform.position=position+Vector3.back*.5f;
        ball.start=Time.time+delay;ball.life=life;ball.size=size;
        ball.axis=Random.onUnitSphere;ball.spin=Random.Range(-220f,220f);
        ball.drift=(Vector3)(Random.insideUnitCircle*.4f);
        ball.transform.localScale=Vector3.zero;ball.renderer.enabled=false;
        active.Add(ball);Spawned++;
        return ball;
    }

    Ball Create()
    {
        var go=new GameObject("Fireball");go.transform.SetParent(transform,false);
        var filter=go.AddComponent<MeshFilter>();filter.sharedMesh=Sphere();
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        renderer.sortingLayerName="Bullet";renderer.sortingOrder=32;
        return new Ball{transform=go.transform,renderer=renderer,filter=filter};
    }

    void Update()
    {
        for(int i=active.Count-1;i>=0;i--)
        {
            var ball=active[i];
            float age=(Time.time-ball.start)/ball.life;
            if(age<0)continue;
            if(age>=1){ball.renderer.enabled=false;active.RemoveAt(i);pool.Push(ball);continue;}
            ball.renderer.enabled=true;
            // Every fireball that pops in gets its own bang, louder for bigger ones.
            if(!ball.isRing && !ball.sounded){ball.sounded=true;SoundManager.PlaySoundRandomPitch("enemyExplosion1",Mathf.Clamp(.02f+ball.size*.006f,.02f,.06f),Pitch);}
            // Fast ease-out growth, stepped at 30 Hz for the PS1 cadence.
            float stepped=Mathf.Floor(age*ball.life*30)/(ball.life*30);
            float grow=1-Mathf.Pow(1-stepped,3);
            if(ball.isRing)
            {
                float reach=Mathf.Clamp01(stepped/ball.expand),ease=1-Mathf.Pow(1-reach,3);
                float pulse=reach>=1 ? 1+.03f*Mathf.Sin(Time.time*12) : 1;
                ball.transform.localScale=Vector3.one*ball.size*(.1f+.9f*ease)*pulse;
                // Short rings fade as they grow; held rings stay bright and fade over the last quarter.
                float fadeStart=ball.expand>=1 ? .35f : .75f;
                properties.SetColor(ColorId,ball.tint*(ball.expand>=1 ? 1.6f-stepped : 1.2f));properties.SetFloat(FadeId,Mathf.Clamp01((stepped-fadeStart)/(1-fadeStart)));
                ball.renderer.SetPropertyBlock(properties);
                continue;
            }
            ball.transform.localScale=Vector3.one*ball.size*(.2f+.8f*grow);
            ball.transform.rotation=Quaternion.AngleAxis(ball.spin*stepped*ball.life,ball.axis);
            ball.transform.position+=ball.drift*Time.deltaTime;
            properties.SetColor(ColorId,Heat(stepped));properties.SetFloat(FadeId,Mathf.Clamp01((stepped-.25f)/.75f));
            ball.renderer.SetPropertyBlock(properties);
        }
    }

    /// <summary>White-hot flash, then yellow, orange and a deep red smoulder.</summary>
    static Color Heat(float t)
    {
        if(t<.12f)return Color.Lerp(new Color(2.2f,2.2f,2f),new Color(1.6f,1.4f,.8f),t/.12f);
        if(t<.45f)return Color.Lerp(new Color(1.6f,1.4f,.8f),new Color(1.3f,.75f,.3f),(t-.12f)/.33f);
        return Color.Lerp(new Color(1.3f,.75f,.3f),new Color(.55f,.12f,.08f),(t-.45f)/.55f);
    }

    /// <summary>Flat 24-segment annulus of unit outer radius facing the camera.</summary>
    static Mesh Ring()
    {
        if(ring!=null)return ring;
        const int seg=24;
        var vertices=new List<Vector3>();var uvs=new List<Vector2>();var tris=new List<int>();
        for(int i=0;i<=seg;i++)
        {
            float a=2*Mathf.PI*i/seg;var d=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
            vertices.Add(d*.82f);vertices.Add(d);uvs.Add(new Vector2((float)i/seg*4,0));uvs.Add(new Vector2((float)i/seg*4,.35f));
        }
        for(int i=0;i<seg;i++){int a=i*2;tris.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
        ring=new Mesh{name="PSXv2 shockwave ring"};
        ring.SetVertices(vertices);ring.SetUVs(0,uvs);ring.SetTriangles(tris,0);ring.RecalculateNormals();ring.RecalculateBounds();
        return ring;
    }

    /// <summary>10 x 6 UV sphere: few enough faces for visible PS1 facets.</summary>
    static Mesh Sphere()
    {
        if(sphere!=null)return sphere;
        const int seg=10,rings=6;
        var vertices=new List<Vector3>();var uvs=new List<Vector2>();var tris=new List<int>();
        for(int j=0;j<=rings;j++)for(int i=0;i<=seg;i++)
        {
            float a=Mathf.PI*j/rings,b=2*Mathf.PI*i/seg;
            vertices.Add(new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b))*.5f);
            uvs.Add(new Vector2((float)i/seg*2,(float)j/rings));
        }
        for(int j=0;j<rings;j++)for(int i=0;i<seg;i++)
        {
            int a=j*(seg+1)+i,b=a+seg+1;
            tris.AddRange(new[]{a,a+1,b,a+1,b+1,b});
        }
        sphere=new Mesh{name="PSXv2 fireball sphere"};
        sphere.SetVertices(vertices);sphere.SetUVs(0,uvs);sphere.SetTriangles(tris,0);sphere.RecalculateNormals();sphere.RecalculateBounds();
        return sphere;
    }
}
