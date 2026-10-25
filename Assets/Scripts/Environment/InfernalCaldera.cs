using UnityEngine;
using System.Collections.Generic;

// Modeled basalt shelves over a molten caldera, with a continuous bed under the geometry.
public sealed class InfernalCaldera : MonoBehaviour
{
    readonly List<Transform> tiles=new List<Transform>();
    readonly List<Mesh> meshes=new List<Mesh>();
    Material lava,basalt;
    float clock;
    void Start()
    {
        lava=new Material(Resources.Load<Shader>("CalderaLava"));basalt=new Material(Resources.Load<Shader>("CalderaBasalt"));
        lava.SetTexture("_CrustMap",Resources.Load<Texture2D>("CalderaCrust"));
        lava.SetTexture("_FlowMap",Resources.Load<Texture2D>("CalderaFlow"));
        var bed=new Mesh{name="Continuous molten caldera"};bed.vertices=new[]{new Vector3(-16,-25),new Vector3(-16,25),new Vector3(16,25),new Vector3(16,-25)};bed.triangles=new[]{0,1,2,0,2,3};bed.RecalculateBounds();meshes.Add(bed);
        Add("Molten depth",bed,lava,transform,new Vector3(0,0,125),-80);
        for(int i=0;i<4;i++)
        {
            var root=new GameObject("Basalt shelf "+i).transform;root.SetParent(transform,false);root.localPosition=new Vector3(0,(i-1)*20,83);tiles.Add(root);
            var mesh=Shelf(LevelRun.Seed+i*719);meshes.Add(mesh);Add("Layered volcanic rock",mesh,basalt,root,Vector3.zero,-45);
            int side=i%2==0 ? -1 : 1;
            var vent=Add("Volcano on basalt shelf",IndustrialMeshFactory.Volcano(),CombatMaterials.Fleet,root,new Vector3(side*3.5f,2.4f,-2.3f),-39);
            vent.localScale=Vector3.one*1.05f;vent.gameObject.AddComponent<VolcanicVent>().phase=i*.55f;
        }
    }
    static Transform Add(string label,Mesh mesh,Material material,Transform parent,Vector3 p,int order)
    {
        var go=new GameObject(label);go.transform.SetParent(parent,false);go.transform.localPosition=p;go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerName="Background";renderer.sortingOrder=order;return go.transform;
    }
    void Update()
    {
        clock+=Time.deltaTime;if(lava==null)return;lava.SetFloat("_Flow",clock);
        var pilot=CombatVisuals.Player;float parallax=pilot!=null ? -pilot.position.x*.08f : 0;
        foreach(var tile in tiles){var p=tile.localPosition;p.x=parallax;p.y-=Time.deltaTime*3.7f;if(p.y< -30)p.y+=80;tile.localPosition=p;}
    }
    static Mesh Shelf(int seed)
    {
        var random=new System.Random(seed);float R(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        var points=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
        void Face(Color c,params Vector3[] face){c.a=1;int start=points.Count;foreach(var p in face){points.Add(p);colors.Add(c);}for(int j=1;j<face.Length-1;j++){triangles.Add(start);triangles.Add(start+j);triangles.Add(start+j+1);}}
        void Island(Vector3 p,float rx,float ry,int sides)
        {
            var rim=new Vector3[sides];var crown=new Vector3[sides];
            float height=R(.65f,1.55f),phase=R(0,6.28f);
            for(int i=0;i<sides;i++)
            {
                float a=phase+i*Mathf.PI*2/sides,r=i%4==0 ? R(.64f,.84f) : R(.91f,1.12f);
                rim[i]=p+new Vector3(Mathf.Cos(a)*rx*r,Mathf.Sin(a)*ry*r,0);
                crown[i]=p+new Vector3(Mathf.Cos(a)*rx*r*.94f-height*.13f,Mathf.Sin(a)*ry*r*.94f+height*.28f,-height+R(-.12f,.12f));
            }
            var middle=p+new Vector3(-height*.13f-rx*.05f,height*.28f+ry*.04f,-height-.10f);
            for(int i=0;i<sides;i++)
            {
                int j=(i+1)%sides;float facing=.72f+.16f*Mathf.Sin(phase+i*Mathf.PI*2/sides);
                // Amber reflected undercuts, dark cliffs, and a broad cool-charcoal top.
                var footA=Vector3.Lerp(rim[i],crown[i],.20f);var footB=Vector3.Lerp(rim[j],crown[j],.20f);
                Face(new Color(.31f,.115f,.032f)*facing,rim[i],rim[j],footB,footA);
                Face(new Color(.085f,.067f,.062f)*facing,footA,footB,crown[j],crown[i]);
                Face(new Color(.175f,.151f,.14f)*R(.88f,1.10f),crown[i],crown[j],middle);
            }
        }
        for(int side=-1;side<=1;side+=2)
        {
            for(int row=0;row<3;row++)
            {
                var p=new Vector3(side*R(4.1f,5.5f),-6.4f+row*6.5f+R(-.35f,.35f),0);
                Island(p,R(2.4f,3.1f),R(2.6f,3.0f),9);
                for(int rock=0;rock<3;rock++)Island(p+new Vector3(R(-.8f,.8f),R(-1.4f,1.4f),-1.3f),R(.24f,.48f),R(.28f,.55f),6);
            }
        }
        for(int row=0;row<3;row++)
        {
            float x=(row%2==0 ? -1 : 1)*R(.4f,1.1f);
            Island(new Vector3(x,-6.3f+row*6.25f+R(-.4f,.4f),.10f),R(1.9f,2.45f),R(2.1f,2.65f),10);
            if(row==1)Island(new Vector3(x+1,1.4f,-1.1f),.65f,.90f,7);
        }
        var mesh=new Mesh{name="Beveled basalt islands and broken shelves"};mesh.SetVertices(points);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
    }
    void OnDestroy()
    {
        void Release(Object item){if(item==null)return;if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);}
        Release(lava);Release(basalt);foreach(var mesh in meshes)Release(mesh);
    }
}
