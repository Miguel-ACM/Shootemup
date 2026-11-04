using UnityEngine;
using UnityEngine.Rendering;

public sealed class HeavenBackdrop : MonoBehaviour
{
    readonly Transform[] islands = new Transform[16];
    Material sky;
    Mesh plane;
    float clock;
    void Start()
    {
        plane = new Mesh { name="Continuous Heaven cloud sky" };
        plane.vertices=new[]{new Vector3(-18,-24),new Vector3(18,-24),new Vector3(18,24),new Vector3(-18,24)};
        plane.triangles=new[]{0,2,1,0,3,2}; plane.RecalculateBounds();
        sky=new Material(Resources.Load<Shader>("HeavenClouds"));
        sky.SetFloat("_Seed",(LevelRun.Seed%997)*.037f);
        Add("White cloud ocean",plane,sky,new Vector3(0,0,110),-60);
        var random=new System.Random(LevelRun.Seed+19191);
        for(int i=0;i<islands.Length;i++)
        {
            float side=i%2==0?-1:1, x=side*Mathf.Lerp(3.15f,6.7f,(float)random.NextDouble());
            var root=new GameObject("Cloud redoubt / broken palisade "+i).transform;
            root.SetParent(transform,false); root.localPosition=new Vector3(x,-23+i*3.05f,79);
            root.localScale=Vector3.one*Mathf.Lerp(.72f,1.15f,(float)random.NextDouble());
            root.localRotation=Quaternion.Euler(12,side*9,side*(8+i%3*7));
            islands[i]=root;
            var prop=Add("Scalloped cloud and planted lances",IndustrialMeshFactory.HeavenRedoubt(i%4),HeavenMaterials.Ivory,Vector3.zero,-35);
            prop.SetParent(root,false);
        }
    }
    Transform Add(string label,Mesh mesh,Material material,Vector3 p,int order)
    {
        var go=new GameObject(label); go.transform.SetParent(transform,false); go.transform.localPosition=p;
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var r=go.AddComponent<MeshRenderer>(); r.sharedMaterial=material; r.sortingLayerName="Background"; r.sortingOrder=order;
        r.shadowCastingMode=ShadowCastingMode.Off;
        return go.transform;
    }
    void Update()
    {
        clock+=Time.deltaTime; if(sky!=null) sky.SetFloat("_Clock",clock);
        foreach(var island in islands)
        {
            if(island==null) continue;
            var p=island.localPosition; p.y-=Time.deltaTime*.95f;
            if(p.y < -25) p.y+=50;
            island.localPosition=p;
        }
    }
    void OnDestroy(){ if(sky!=null) Destroy(sky); if(plane!=null) Destroy(plane); }
}

public static partial class IndustrialMeshFactory
{
    static readonly Mesh[] redoubts=new Mesh[4];
    public static Mesh HeavenRedoubt(int variant)
    {
        if(redoubts[variant]!=null) return redoubts[variant];
        var b=new Builder();
        // Broad overlapping billows form one cloud foundation, with deep blue-grey undersides.
        b.Ball(new Vector3(0,0,.35f),new Vector3(3.8f,2.3f,.9f),0);
        for(int i=0;i<7;i++)
        {
            float a=i*Mathf.PI*2/7;
            b.Ball(new Vector3(Mathf.Cos(a)*1.35f,Mathf.Sin(a)*.75f,.08f),new Vector3(1.45f,1.2f,.72f),15);
        }
        // Marble fragments, bent palisades and spearheads embedded in the clouds.
        b.Prism(new[]{V(-1.3f,-.4f),V(1.4f,-.35f),V(1.1f,.5f),V(-1,.65f)},.18f,1,new Vector3(0,0,-.45f),14);
        for(int i=0;i<6;i++)
        {
            if(i==variant+1) continue;
            float x=-1.2f+i*.48f, y=.25f+(i%2)*.12f;
            b.Box(new Vector3(x,y,-.9f),new Vector3(.07f,.09f,.9f),7,2);
            b.Prism(new[]{V(-.13f,0),V(0,.36f),V(.13f,0)},.07f,3,new Vector3(x,y+.12f,-1.34f),7);
            if(i<4) b.Box(new Vector3(x+.22f,y,-1.03f),new Vector3(.5f,.065f,.065f),3,7);
        }
        for(int i=0;i<3;i++)
        {
            float x=-.8f+i*.77f, y=-.45f+i*.13f;
            b.Box(new Vector3(x,y,-.85f),new Vector3(.055f,.9f,.06f),7,2);
            b.Prism(new[]{V(-.12f,0),V(0,-.43f),V(.12f,0)},.06f,8,new Vector3(x,y-.43f,-.85f),2);
        }
        return redoubts[variant]=b.Finish("War in Heaven / cloud redoubt "+variant);
    }
}
