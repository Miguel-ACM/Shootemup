using UnityEngine;
using UnityEngine.Rendering;

// Sculpted ink curtains frame the playable darkness. No scenery projectiles or ordinary enemies.
public sealed class VoidBackdrop : MonoBehaviour
{
    Material abyss, liquid;
    Mesh backdrop, falls;
    float clock;
    void Start()
    {
        abyss=new Material(Resources.Load<Shader>("VoidFalls"));
        liquid=new Material(abyss); liquid.SetFloat("_Curtain",1);
        liquid.SetFloat("_Seed",LevelRun.Seed%791*.01f);
        backdrop=new Mesh{name="Unbroken Void"};
        backdrop.vertices=new[]{new Vector3(-18,-24),new Vector3(18,-24),new Vector3(18,24),new Vector3(-18,24)};
        backdrop.triangles=new[]{0,2,1,0,3,2}; backdrop.RecalculateBounds();
        Add("Black distance",backdrop,abyss,110);
        const int rows=100,cols=8;
        var v=new Vector3[(rows+1)*(cols+1)*2]; var uv=new Vector2[v.Length];
        var tris=new int[rows*cols*12];int k=0;
        for(int side=0;side<2;side++)
        {
            int start=side*(rows+1)*(cols+1);float sign=side==0?-1:1;
            for(int y=0;y<=rows;y++) for(int x=0;x<=cols;x++)
            {
                float yy=-24+y*.48f,u=x/(float)cols;
                float edge=3.9f+.30f*Mathf.Sin(yy*.44f+side*2)+.15f*Mathf.Sin(yy*1.18f);
                int id=start+y*(cols+1)+x;
                v[id]=new Vector3(sign*(edge+u*5),yy,Mathf.Sin(u*9+yy*.4f)*.35f);
                uv[id]=new Vector2(u,yy);
                if(y<rows&&x<cols){int a=id,b=id+1,c=id+cols+1,d=c+1;tris[k++]=a;tris[k++]=c;tris[k++]=b;tris[k++]=b;tris[k++]=c;tris[k++]=d;}
            }
        }
        falls=new Mesh{name="Sculpted falling ink / shared seams"};falls.vertices=v;falls.uv=uv;falls.triangles=tris;falls.RecalculateBounds();
        Add("Obsidian liquid falls",falls,liquid,84);
    }
    void Add(string label,Mesh mesh,Material mat,float depth)
    {
        var go=new GameObject(label);go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3(0,0,depth);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=mat;
        r.sortingLayerName="Background";r.sortingOrder=-45;r.shadowCastingMode=ShadowCastingMode.Off;
    }
    void Update(){clock+=Time.deltaTime;if(liquid!=null)liquid.SetFloat("_Clock",clock);if(abyss!=null)abyss.SetFloat("_Clock",clock);}
    void OnDestroy(){if(abyss!=null)Destroy(abyss);if(liquid!=null)Destroy(liquid);if(backdrop!=null)Destroy(backdrop);if(falls!=null)Destroy(falls);}
}
