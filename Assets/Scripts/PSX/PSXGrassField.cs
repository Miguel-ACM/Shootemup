using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Level 1 background: a tiled, muted grass texture with Blender-built puddles, bushes and trees
/// on it, and soft sprite clouds drifting above with faint shadows on the ground. Everything
/// scrolls down and is recycled to the top with a new random position. Parallax follows the
/// player: moving right shifts the field left (clouds twice as much), climbing speeds the scroll
/// up and backing down slows it.
/// </summary>
public sealed class PSXGrassField : MonoBehaviour
{
    public Mesh[] puddles,bushes,trees;
    public Material material,groundMaterial,cloudMaterial;
    [Range(0.2f,3f)] public float scrollSpeed=2.2f;
    /// <summary>World units covered by one repeat of the grass texture.</summary>
    public float grassTile=10;
    /// <summary>World units the ground shifts left per unit the player is right of centre (clouds shift double).</summary>
    [Range(0,.5f)] public float sideParallax=.12f;
    /// <summary>Scroll gain per unit/s of player vertical speed: faster when climbing, slower when backing off.</summary>
    [Range(0,.5f)] public float climbParallax=.2f;
    Transform player;
    float lastPlayerY,climb;
    const float GroundHeight=10,GroundWidth=13,Top=16,Bottom=-16,HalfWidth=5.6f;
    const float GroundZ=90,CloudZ=60;
    readonly List<Piece> pieces=new List<Piece>();
    readonly System.Random random=new System.Random(7);
    MaterialPropertyBlock shadowTint;

    sealed class Piece { public Transform transform; public float speed; public bool isGround,isTree; public float baseX; public Transform shadow; }

    void Start()
    {
        if(material==null || groundMaterial==null)return;
        var ground=Quad(GroundWidth,GroundHeight,true);
        for(int i=0;i<4;i++)Add("Grass ground",ground,groundMaterial,new Vector3(0,-15+i*GroundHeight,GroundZ),0,1,-50).isGround=true;
        Scatter("Puddle",puddles,5,GroundZ-.05f,-49,false);
        Scatter("Bush",bushes,8,GroundZ-.1f,-46,false);
        Scatter("Tree",trees,9,GroundZ-.2f,-44,true);
        if(cloudMaterial!=null)
        {
            shadowTint=new MaterialPropertyBlock();shadowTint.SetColor("_Tint",new Color(0,0,0,.22f));
            var cloud=Quad(3.2f,1.6f,false);
            for(int i=0;i<4;i++)
            {
                float scale=Range(.8f,1.4f);
                var piece=Add("Cloud",cloud,cloudMaterial,new Vector3(Range(-HalfWidth,HalfWidth),Range(Bottom,Top),CloudZ),0,1.6f,-20);
                piece.transform.localScale=Vector3.one*scale;
                // The shadow lies on the ground below and behind the cloud, as if lit from the upper left.
                var shadow=Add("Cloud shadow",cloud,cloudMaterial,Vector3.zero,0,0,-48);
                shadow.transform.localScale=Vector3.one*scale;shadow.transform.GetComponent<MeshRenderer>().SetPropertyBlock(shadowTint);
                pieces.Remove(shadow);piece.shadow=shadow.transform;
            }
        }
    }

    float Range(float lo,float hi)=>Mathf.Lerp(lo,hi,(float)random.NextDouble());

    void Scatter(string name,Mesh[] meshes,int count,float z,int order,bool isTree)
    {
        if(meshes==null || meshes.Length==0)return;
        for(int i=0;i<count;i++)
        {
            var piece=Add(name,meshes[i%meshes.Length],material,new Vector3(0,Range(Bottom,Top),z),Range(0,360),1,order);
            piece.isTree=isTree;piece.baseX=isTree ? Edge() : Range(-HalfWidth,HalfWidth);
        }
    }

    // Trees favour the playfield edges so the centre lane stays readable.
    float Edge()=>(random.NextDouble()<.5 ? -1 : 1)*Range(1.8f,HalfWidth);

    /// <summary>Camera-facing quad; tiled UVs for the ground, 0..1 for sprites.</summary>
    Mesh Quad(float width,float height,bool tiled)
    {
        float u=tiled ? width/grassTile : 1,v=tiled ? height/grassTile : 1;
        var mesh=new Mesh{name=tiled ? "Grass ground quad" : "Sprite quad"};
        mesh.vertices=new[]{new Vector3(-width/2,-height/2),new Vector3(width/2,-height/2),new Vector3(width/2,height/2),new Vector3(-width/2,height/2)};
        mesh.uv=new[]{Vector2.zero,new Vector2(u,0),new Vector2(u,v),new Vector2(0,v)};
        mesh.colors=new[]{Color.white,Color.white,Color.white,Color.white};
        mesh.normals=new[]{Vector3.back,Vector3.back,Vector3.back,Vector3.back};
        mesh.triangles=new[]{0,2,1,0,3,2};
        mesh.RecalculateBounds();
        return mesh;
    }

    Piece Add(string name,Mesh mesh,Material mat,Vector3 position,float angle,float speed,int order)
    {
        var go=new GameObject(name);go.transform.SetParent(transform,false);
        go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,0,angle);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;
        renderer.sortingLayerName="Background";renderer.sortingOrder=order;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        var piece=new Piece{transform=go.transform,speed=speed,baseX=position.x};
        pieces.Add(piece);return piece;
    }

    void Update()
    {
        if(player==null){var go=GameObject.FindGameObjectWithTag("Player");if(go!=null){player=go.transform;lastPlayerY=player.position.y;}}
        float side=0,gain=1;
        if(player!=null && Time.deltaTime>0)
        {
            side=-player.position.x*sideParallax;
            float vy=(player.position.y-lastPlayerY)/Time.deltaTime;lastPlayerY=player.position.y;
            climb=Mathf.Lerp(climb,vy,1-Mathf.Exp(-6*Time.deltaTime));
            gain=Mathf.Clamp(1+climb*climbParallax,.35f,2.2f);
        }
        foreach(var piece in pieces)
        {
            var p=piece.transform.localPosition;
            p.y-=scrollSpeed*piece.speed*gain*Time.deltaTime;
            if(piece.isGround){if(p.y<-25)p.y+=4*GroundHeight;}
            else if(p.y<Bottom)
            {
                p.y+=Top-Bottom;
                piece.baseX=piece.isTree ? Edge() : Range(-HalfWidth,HalfWidth);
                if(piece.shadow==null)piece.transform.localRotation=Quaternion.Euler(0,0,Range(0,360));
            }
            p.x=piece.baseX+side*(piece.shadow!=null ? 2 : 1);
            piece.transform.localPosition=p;
            if(piece.shadow!=null)piece.shadow.localPosition=new Vector3(piece.baseX+side+.9f,p.y-1.1f,GroundZ-.3f);
        }
    }
}
