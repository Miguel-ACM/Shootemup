using UnityEngine;
using UnityEngine.Rendering;

public sealed class HeavenBackdrop : MonoBehaviour
{
    readonly Transform[] islands = new Transform[16];
    Material sky, mist;
    Mesh plane;
    float clock;
    void Start()
    {
        plane = new Mesh { name="Continuous Heaven cloud sky" };
        plane.vertices=new[]{new Vector3(-18,-24),new Vector3(18,-24),new Vector3(18,24),new Vector3(-18,24)};
        plane.triangles=new[]{0,2,1,0,3,2}; plane.RecalculateBounds();
        sky=new Material(Resources.Load<Shader>("HeavenClouds"));
        sky.SetTexture("_CloudAtlas",Resources.Load<Texture2D>("HeavenCloudRelief"));
        sky.SetFloat("_Seed",(LevelRun.Seed%997)*.037f);
        mist=new Material(Resources.Load<Shader>("HeavenMist"));
        Add("White cloud ocean",plane,sky,new Vector3(0,0,110),-60);
        var random=new System.Random(LevelRun.Seed+19191);
        for(int i=0;i<islands.Length;i++)
        {
            float side=i%2==0?-1:1, x=side*Mathf.Lerp(4.1f,7.4f,(float)random.NextDouble());
            var root=new GameObject("Cloud redoubt / broken palisade "+i).transform;
            root.SetParent(transform,false); root.localPosition=new Vector3(x,-39+i*5.2f,79+i%3*3);
            root.localScale=Vector3.one*Mathf.Lerp(.88f,1.24f,(float)random.NextDouble());
            root.localRotation=Quaternion.Euler(28,side*18,side*(3+i%3*4));
            islands[i]=root;
            var cloud=Add("Deep sculpted cumulus",IndustrialMeshFactory.HeavenCloudIsland(i%3),mist,Vector3.zero,-36);
            cloud.SetParent(root,false);
            var prop=Add("Ruined marble bastion and golden palisade",IndustrialMeshFactory.HeavenBastion(i%6),HeavenMaterials.Ivory,Vector3.zero,-35);
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
            if(p.y < -42) p.y+=83.2f;
            island.localPosition=p;
        }
    }
    void OnDestroy(){ if(sky!=null) Destroy(sky); if(mist!=null)Destroy(mist); if(plane!=null) Destroy(plane); }
}

public static partial class IndustrialMeshFactory
{
    static readonly Mesh[] redoubts=new Mesh[4],heavenIslands=new Mesh[3];
    static void HeavenArch(Builder b,Vector3 foot,float width,float height,bool broken)
    {
        for(int s=-1;s<=1;s+=2)
        {
            var a=foot+Vector3.right*s*width*.5f;var d=foot+Vector3.up*height;
            if(broken&&s>0)d=foot+new Vector3(width*.46f,height*.55f,0);
            b.Sweep(a,a+Vector3.up*height*.65f,foot+new Vector3(s*width*.42f,height*.9f,0),d,.13f,.19f,15,false);
            b.Sweep(a+Vector3.back*.21f,a+new Vector3(0,height*.65f,-.21f),foot+new Vector3(s*width*.39f,height*.86f,-.21f),d+Vector3.back*.21f,.028f,.032f,3,false);
            b.Pearl(a+Vector3.down*.02f,new Vector3(.23f,.13f,.25f),7,20,10);
            b.Halo(a+Vector3.up*.19f,.16f,.025f,Vector3.one,Quaternion.Euler(90,0,0),3,24);
        }
    }
    public static Mesh HeavenRedoubt(int variant)
    {
        if(redoubts[variant]!=null)return redoubts[variant];var b=new Builder();
        // Interlocked marble terraces sit in the clouds; ruins have volume and visible broken ends.
        for(int slab=0;slab<5;slab++)
        {
            var offset=V(-1.3f+slab*.65f,-.2f+(slab%2)*.12f);
            BevelArmor(b,new[]{V(-.38f,-.28f)+offset,V(.35f,-.34f)+offset,V(.4f,.26f)+offset,V(-.36f,.35f)+offset},.24f,slab%2==0?15:1,0);
        }
        b.Pearl(new Vector3(0,-.15f,.38f),new Vector3(2.05f,.70f,.32f),1);
        if(variant==0||variant==1)
        {
            HeavenArch(b,new Vector3(0,.1f,-.10f),2.4f,2.15f,variant==1);
            HeavenArch(b,new Vector3(-1.55f,.12f,.08f),.78f,1.4f,false);
            if(variant==0)HeavenArch(b,new Vector3(1.55f,.12f,.08f),.78f,1.4f,false);
            for(int s=-1;s<=1;s+=2)
                b.Sweep(new Vector3(s*1.8f,.1f,.1f),new Vector3(s*2,.6f,.1f),new Vector3(s*1.5f,1.1f,.2f),new Vector3(s*1.2f,1.35f,.2f),.09f,.12f,7,false);
        }
        else
        {
            for(int pillar=0;pillar<5;pillar++)
            {
                float x=-1.55f+pillar*.78f,h=pillar==variant? .55f:1.15f+(pillar%2)*.4f;
                b.Sweep(new Vector3(x,0,0),new Vector3(x,.4f,0),new Vector3(x,h-.2f,0),new Vector3(x+(pillar==2?.3f:0),h,.04f),.15f,.19f,15,false);
                b.Halo(new Vector3(x,.18f,0),.18f,.036f,Vector3.one,Quaternion.Euler(90,0,0),3,24);
                if(pillar<4&&pillar!=variant)HeavenArch(b,new Vector3(x+.39f,0,-.06f),.64f,.87f,false);
            }
            // A fallen length of carved cornice and exposed ribs.
            b.Sweep(new Vector3(-1.5f,-.33f,-.27f),new Vector3(-.7f,-.58f,-.23f),new Vector3(.3f,-.13f,-.15f),new Vector3(1.4f,-.43f,-.16f),.18f,.16f,1,false);
        }
        for(int spear=0;spear<5;spear++)
        {
            float x=-1.7f+spear*.79f;var root=new Vector3(x,-.25f,-.35f);
            var tip=root+new Vector3((spear%2==0?-.22f:.24f),.82f+(spear%3)*.17f,-.15f);
            b.Sweep(root,Vector3.Lerp(root,tip,.3f),Vector3.Lerp(root,tip,.7f),tip,.027f,.027f,7,false,12);
            Feather(b,tip-Vector3.up*.12f,tip+Vector3.up*.29f,.095f,.02f,3);
        }
        // A torn pennant on each surviving ruin, with a real curled cloth surface.
        float bx=variant%2==0?-1.2f:1.2f;
        b.Surface(10,12,(u,v)=>new Vector3(bx+(u-.5f)*.48f,1.6f-v*(.87f+.13f*Mathf.Cos(u*19)), -.3f+Mathf.Sin(v*5+u*2)*.12f),1);
        return redoubts[variant]=b.Finish("Heaven / ruined gilded sanctuary "+variant);
    }
    public static Mesh HeavenCloudIsland(int variant=0)
    {
        if(heavenIslands[variant]!=null)return heavenIslands[variant];var b=new Builder();
        var random=new System.Random(4711+variant*173);
        CutStone(b,new Vector3(0,-.28f,.48f),new Vector3(2.6f,1.12f,.75f),15,8);
        for(int puff=0;puff<16;puff++)
        {
            float a=puff*Mathf.PI*2/16;
            float size=.55f+(float)random.NextDouble()*.42f;
            var center=new Vector3(Mathf.Cos(a)*(1.7f+(float)random.NextDouble()*.5f),Mathf.Sin(a)*.72f-.35f,.35f+(float)random.NextDouble()*.45f);
            CutStone(b,center,new Vector3(size,size*.65f,size*.74f),15,7,a);
        }
        return heavenIslands[variant]=b.Finish("Asymmetric deep cumulus bank "+variant);
    }
}
