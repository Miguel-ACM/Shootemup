using UnityEngine;
using UnityEngine.Rendering;

public sealed class HeavenBackdrop : MonoBehaviour
{
    struct Island
    {
        public Transform root;
        public Vector3 origin;
        public Quaternion tilt;
        public float speed, phase, loop;
    }
    readonly Island[] islands = new Island[18];
    Material sky, mist, distantMist, ruins, distantRuins;
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
        distantMist=new Material(mist); distantMist.SetFloat("_Distance", .5f);
        ruins=new Material(Resources.Load<Shader>("HeavenRuins"));
        distantRuins=new Material(ruins); distantRuins.SetFloat("_Distance", .6f);
        Add("White cloud ocean",plane,sky,new Vector3(0,0,110),-60);
        var random=new System.Random(LevelRun.Seed+19191);
        for(int i=0;i<islands.Length;i++)
        {
            bool far=i>=12;
            float R(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
            float side=i%2==0?-1:1, x=side*(far?R(3.0f,4.7f):R(4.05f,4.85f));
            var root=new GameObject((far?"Distant sanctuary / ":"Cloud sanctuary / ")+i).transform;
            root.SetParent(transform,false);
            root.localPosition=new Vector3(x,far?-27+(i-12)*9:-30+(i/2)*10+(i%2)*4.5f,far?94:78);
            root.localScale=Vector3.one*(far?R(.42f,.60f):R(.94f,1.14f));
            root.localRotation=Quaternion.Euler(R(17,24),side*R(8,17),side*R(-4,4));
            islands[i]=new Island{root=root,origin=root.localPosition,tilt=root.localRotation,
                speed=far?R(.34f,.46f):R(.86f,1.04f),phase=R(0,6.28f),loop=far?54:60};
            var cloud=Add("Layered wind-sculpted cumulus",IndustrialMeshFactory.HeavenCloudIsland(i%3),far?distantMist:mist,Vector3.zero,far?-48:-36);
            cloud.SetParent(root,false);
            var prop=Add("Gilded sanctuary and broken lances",IndustrialMeshFactory.HeavenBastion((i+i/6)%6),far?distantRuins:ruins,Vector3.zero,far?-47:-35);
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
        clock+=Time.deltaTime; Pose(clock);
    }
    void Pose(float time)
    {
        if(sky!=null) sky.SetFloat("_Clock",time);
        if(ruins!=null) ruins.SetFloat("_Clock",time);
        if(distantRuins!=null) distantRuins.SetFloat("_Clock",time);
        foreach(var island in islands)
        {
            if(island.root==null) continue;
            var p=island.origin;
            p.x+=Mathf.Sin(time*.19f+island.phase)*.09f;
            p.y=Mathf.Repeat(p.y+island.loop*.5f-time*island.speed,island.loop)-island.loop*.5f;
            p.y+=Mathf.Sin(time*.28f+island.phase)*.08f;
            island.root.localPosition=p;
            island.root.localRotation=island.tilt*Quaternion.Euler(0,0,Mathf.Sin(time*.21f+island.phase)*.65f);
        }
    }
    void OnDestroy()
    {
        if(sky!=null)Destroy(sky); if(mist!=null)Destroy(mist); if(distantMist!=null)Destroy(distantMist);
        if(ruins!=null)Destroy(ruins); if(distantRuins!=null)Destroy(distantRuins); if(plane!=null)Destroy(plane);
    }
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
        // Overlapping broad lobes form a deep bank, with smaller wind-torn curls
        // underneath. They have low-poly silhouettes without stone's flat caps.
        void Lobe(Vector3 center,Vector3 radius,float phase)
        {
            b.Surface(14,9,(u,v)=>{
                float a=u*Mathf.PI*2, t=v*Mathf.PI, ring=Mathf.Sin(t);
                float billow=1+.075f*Mathf.Sin(a*3+phase)*ring+.04f*Mathf.Cos(t*4+a*2+phase);
                return center+Vector3.Scale(new Vector3(Mathf.Cos(a)*ring,Mathf.Sin(a)*ring,Mathf.Cos(t))*billow,radius);
            },15);
        }
        Lobe(new Vector3(0,-.52f,.66f),new Vector3(2.25f,.84f,.67f),variant);
        for(int puff=0;puff<12;puff++)
        {
            float a=puff*Mathf.PI*2/12, size=.62f+(float)random.NextDouble()*.35f;
            var center=new Vector3(Mathf.Cos(a)*1.90f,Mathf.Sin(a)*.59f-.55f,.33f+(float)random.NextDouble()*.38f);
            Lobe(center,new Vector3(size,size*.72f,size*.70f),puff*1.7f+variant);
        }
        for(int curl=0;curl<7;curl++)
        {
            float size=.25f+(float)random.NextDouble()*.20f;
            Lobe(new Vector3(-1.9f+curl*.58f,-1.11f-(float)random.NextDouble()*.18f,-.12f),
                new Vector3(size*1.55f,size*.63f,size*.80f),curl);
        }
        return heavenIslands[variant]=b.Finish("Asymmetric deep cumulus bank "+variant);
    }
}
