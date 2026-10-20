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
    static Mesh sphere;
    Material material;
    readonly List<Ball> active=new List<Ball>();
    readonly Stack<Ball> pool=new Stack<Ball>();
    MaterialPropertyBlock properties;
    static readonly int ColorId=Shader.PropertyToID("_Color"),FadeId=Shader.PropertyToID("_Fade");
    public static int Spawned { get; private set; }

    sealed class Ball
    {
        public Transform transform;public MeshRenderer renderer;
        public float start,life,size;public Vector3 axis,drift;public float spin;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){instance=null;Spawned=0;}

    /// <summary>A burst of count spheres spread over radius, the largest about size across.</summary>
    public static void Spawn(Vector3 position,float size,int count,float radius,float stagger)
    {
        if(instance==null)
        {
            var material=Resources.Load<Material>("Effects/PSXv2Fireball");
            if(material==null)return;
            instance=new GameObject("PSXv2 fireballs").AddComponent<PSXFireballs>();
            instance.material=material;instance.properties=new MaterialPropertyBlock();
        }
        for(int i=0;i<count;i++)
        {
            if(instance.active.Count>=PoolLimit)return;
            var offset=i==0 ? Vector2.zero : Random.insideUnitCircle*radius;
            float s=size*(i==0 ? 1f : Random.Range(.45f,.85f));
            instance.Add(position+(Vector3)offset,s,i==0 ? 0 : Random.Range(0,stagger),Random.Range(.42f,.6f)+s*.04f);
        }
    }

    void Add(Vector3 position,float size,float delay,float life)
    {
        var ball=pool.Count>0 ? pool.Pop() : Create();
        ball.transform.position=position+Vector3.back*.5f;
        ball.start=Time.time+delay;ball.life=life;ball.size=size;
        ball.axis=Random.onUnitSphere;ball.spin=Random.Range(-220f,220f);
        ball.drift=(Vector3)(Random.insideUnitCircle*.4f);
        ball.transform.localScale=Vector3.zero;ball.renderer.enabled=false;
        active.Add(ball);Spawned++;
    }

    Ball Create()
    {
        var go=new GameObject("Fireball");go.transform.SetParent(transform,false);
        go.AddComponent<MeshFilter>().sharedMesh=Sphere();
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        renderer.sortingLayerName="Bullet";renderer.sortingOrder=32;
        return new Ball{transform=go.transform,renderer=renderer};
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
            // Fast ease-out growth, stepped at 30 Hz for the PS1 cadence.
            float stepped=Mathf.Floor(age*ball.life*30)/(ball.life*30);
            float grow=1-Mathf.Pow(1-stepped,3);
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
