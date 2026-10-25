using UnityEngine;
using UnityEngine.Rendering;

public sealed class CandylandBackdrop : MonoBehaviour
{
    struct Falling {public Transform model;public float x,y,speed,spin,phase,scale;}
    Falling[] sweets;
    Material clouds,farCandy,nearCandy;
    Mesh plane;
    float clock;
    public int CandyCount=>sweets?.Length ?? 0;
    void Start()
    {
        plane=new Mesh{name="Pink cloud sky"};plane.vertices=new[]{new Vector3(-16,-22),new Vector3(16,-22),new Vector3(16,22),new Vector3(-16,22)};
        plane.triangles=new[]{0,2,1,0,3,2};plane.RecalculateBounds();
        clouds=new Material(Resources.Load<Shader>("CandyClouds"));clouds.SetFloat("_Seed",(LevelRun.Seed%997)*.037f);
        Add("Layered pink clouds",plane,clouds,new Vector3(0,0,110),-60);
        farCandy=new Material(CandyMaterials.Glaze);farCandy.SetColor("_Tint",new Color(.78f,.65f,.76f,1));
        nearCandy=new Material(CandyMaterials.Glaze);nearCandy.SetColor("_Tint",new Color(.94f,.86f,.91f,1));
        var random=new System.Random(LevelRun.Seed+9329);float R(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        sweets=new Falling[30];
        for(int i=0;i<sweets.Length;i++)
        {
            bool near=i%5==0;float scale=near ? R(.95f,1.35f) : R(.45f,.85f);
            float x=near ? (i%2==0 ? -1 : 1)*R(3.3f,4.9f) : R(-8,8),y=-20+i*40f/sweets.Length;
            int kind=(i+i/5)%5;
            var prop=Add(new[]{"Falling spiral lollipop","Falling chocolate bar","Falling wrapped candy","Falling strawberry macaron","Falling candy cane"}[kind],
                IndustrialMeshFactory.CandyProp(kind),near ? nearCandy : farCandy,new Vector3(x,y,near ? 64 : 83),near ? -26 : -42);
            sweets[i]=new Falling{model=prop,x=x,y=y,speed=near ? R(2.2f,3.0f) : R(.9f,1.7f),spin=R(-18,18),phase=R(0,6.28f),scale=scale};
            prop.localScale=Vector3.one*scale;
        }
    }
    Transform Add(string label,Mesh mesh,Material material,Vector3 position,int order)
    {
        var go=new GameObject(label);go.transform.SetParent(transform,false);go.transform.localPosition=position;
        go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;
        r.sortingLayerName="Background";r.sortingOrder=order;r.shadowCastingMode=ShadowCastingMode.Off;return go.transform;
    }
    void Update()
    {
        clock+=Time.deltaTime;if(clouds!=null)clouds.SetFloat("_Clock",clock);
        if(sweets==null)return;
        for(int i=0;i<sweets.Length;i++)
        {
            var sweet=sweets[i];sweet.y-=Time.deltaTime*sweet.speed;if(sweet.y<-21)sweet.y+=42;
            var p=sweet.model.localPosition;p.x=sweet.x+Mathf.Sin(clock*.35f+sweet.phase)*.65f;p.y=sweet.y;sweet.model.localPosition=p;
            sweet.model.localRotation=Quaternion.Euler(18*Mathf.Sin(clock*.45f+sweet.phase),22*Mathf.Cos(clock*.37f+sweet.phase),sweet.phase*57.3f+clock*sweet.spin);sweets[i]=sweet;
        }
    }
    void OnDestroy(){if(plane!=null)Destroy(plane);if(clouds!=null)Destroy(clouds);if(farCandy!=null)Destroy(farCandy);if(nearCandy!=null)Destroy(nearCandy);}
}

public static partial class IndustrialMeshFactory
{
    static readonly Mesh[] candyProps=new Mesh[5];
    public static Mesh CandyProp(int kind)
    {
        if(candyProps[kind]!=null)return candyProps[kind];var b=new Builder();
        if(kind==0)
        {
            b.Box(new Vector3(0,-.95f,.09f),new Vector3(.13f,1.8f,.13f),15,1);
            b.Drum(new Vector3(0,.18f,0),.69f,.22f,15,20,8);
            b.Drum(new Vector3(0,.18f,.04f),.72f,.14f,8,20,0);
            // A real raised spiral winds across the confection's face.
            for(int i=0;i<48;i++)
            {
                float a=i/48f*Mathf.PI*4.8f,c=(i+1)/48f*Mathf.PI*4.8f,r=.055f+i/48f*.55f,t=.055f+(i+1)/48f*.55f;
                Vector3 P(float angle,float radius)=>new Vector3(Mathf.Cos(angle)*radius,.18f+Mathf.Sin(angle)*radius,-.12f);
                b.Face(0,P(c,t-.034f),P(c,t+.034f),P(a,r+.034f),P(a,r-.034f));
            }
        }
        else if(kind==1)
        {
            b.Box(Vector3.zero,new Vector3(1.08f,1.65f,.20f),4,5);
            for(int y=0;y<3;y++)for(int x=0;x<2;x++)
                b.Prism(new[]{V(-.19f,-.20f),V(.19f,-.20f),V(.23f,-.16f),V(.23f,.16f),V(.19f,.20f),V(-.19f,.20f),V(-.23f,.16f),V(-.23f,-.16f)},.12f,5,new Vector3(-.26f+x*.52f,-.52f+y*.52f,-.15f),4);
            b.Box(new Vector3(0,-.56f,-.23f),new Vector3(1.14f,.57f,.045f),7,14);
            b.Box(new Vector3(0,-.32f,-.26f),new Vector3(1.14f,.08f,.03f),10,1);
            b.Prism(new[]{V(-.54f,0),V(-.29f,.16f),V(-.04f,-.02f),V(.20f,.13f),V(.54f,0)},.035f,15,new Vector3(0,-.28f,-.27f),1);
            b.Prism(new[]{V(0,-.12f),V(-.12f,0),V(0,.12f),V(.12f,0)},.035f,10,new Vector3(0,-.58f,-.28f),1);
        }
        else if(kind==2)
        {
            b.Ball(Vector3.zero,new Vector3(.53f,.42f,.27f),2);
            foreach(int s in new[]{-1,1})b.Prism(new[]{V(s*.44f,0),V(s*.91f,-.32f),V(s*.82f,0),V(s*.91f,.32f)},.12f,15,Vector3.zero,2);
        }
        else if(kind==3)
        {
            b.Drum(Vector3.zero,.59f,.18f,1,16,1);
            b.Ball(new Vector3(0,0,-.16f),new Vector3(.64f,.64f,.23f),8);
            b.Ball(new Vector3(0,0,.16f),new Vector3(.64f,.64f,.23f),0);
            for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI/8;var p=new Vector3(Mathf.Cos(a)*.56f,Mathf.Sin(a)*.56f,-.1f);
                b.Ball(p,new Vector3(.10f,.10f,.09f),8);
            }
        }
        else
        {
            // An eight-sided sugar tube bends into a real hooked cane.
            var points=new Vector3[25];
            for(int i=0;i<13;i++)points[i]=new Vector3(.36f,-1.05f+i*.12f,0);
            for(int i=13;i<25;i++){float a=(i-12)/12f*Mathf.PI;points[i]=new Vector3(Mathf.Cos(a)*.36f,.39f+Mathf.Sin(a)*.36f,0);}
            for(int i=0;i<24;i++)
            {
                var tangent=(points[i+1]-points[i]).normalized;var right=new Vector3(-tangent.y,tangent.x,0);
                Vector3 P(int k,bool end){float a=k*Mathf.PI/4;return points[end ? i+1 : i]+(right*Mathf.Cos(a)+Vector3.forward*Mathf.Sin(a))*.12f;}
                for(int j=0;j<8;j++)b.Face(i%4<2 ? 15 : 0,P(j,false),P(j+1,false),P(j+1,true),P(j,true));
            }
            b.Ball(points[0],Vector3.one*.12f,15);b.Ball(points[24],Vector3.one*.12f,0);
        }
        return candyProps[kind]=b.Finish(new[]{"Spiral lollipop","Foil wrapped chocolate bar","Mint wrapper candy","Strawberry cream macaron","Striped hooked candy cane"}[kind]);
    }
}
