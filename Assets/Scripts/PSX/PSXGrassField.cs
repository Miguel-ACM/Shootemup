using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Level 1 background: a tiled, muted grass texture with Blender-built ponds, rocks, bushes and
/// trees on it, and soft sprite clouds drifting above with faint shadows on the ground. Everything
/// scrolls down and is recycled to the top with a new random position; nothing solid is ever
/// placed on water. Moving right shifts the field left (clouds twice as much).
/// </summary>
public sealed class PSXGrassField : MonoBehaviour
{
    public Mesh[] puddles,rocks,bushes,trees;
    public Material material,groundMaterial,cloudMaterial;
    [Range(0.2f,3f)] public float scrollSpeed=2.2f;
    /// <summary>World units covered by one repeat of the grass texture.</summary>
    public float grassTile=10;
    /// <summary>World units the ground shifts left per unit the player is right of centre (clouds shift double).</summary>
    [Range(0,.5f)] public float sideParallax=.12f;
    Transform player;
    const float GroundHeight=10,GroundWidth=13,Top=16,Bottom=-16,HalfWidth=5.6f;
    const float GroundZ=90,CloudZ=60;
    readonly List<Piece> pieces=new List<Piece>();
    readonly System.Random random=new System.Random(7);
    MaterialPropertyBlock shadowTint;

    enum Kind { Ground, Water, Solid, Tree, Cloud }
    sealed class Piece { public Transform transform; public float speed,baseX,radius; public Kind kind; public Transform shadow; }

    void Start()
    {
        if(material==null || groundMaterial==null)return;
        var ground=Quad(GroundWidth,GroundHeight,true);
        for(int i=0;i<4;i++)Add("Grass ground",ground,groundMaterial,new Vector3(0,-15+i*GroundHeight,GroundZ),0,1,-50).kind=Kind.Ground;
        Scatter("Pond",puddles,8,GroundZ-.05f,-49,Kind.Water);
        Scatter("Rock",rocks,7,GroundZ-.1f,-47,Kind.Solid);
        Scatter("Bush",bushes,10,GroundZ-.1f,-46,Kind.Solid);
        Scatter("Tree",trees,16,GroundZ-.2f,-44,Kind.Tree);
        if(cloudMaterial!=null)
        {
            shadowTint=new MaterialPropertyBlock();shadowTint.SetColor("_Tint",new Color(0,0,0,.22f));
            var cloud=Quad(3.2f,1.6f,false);
            for(int i=0;i<4;i++)
            {
                float scale=Range(.8f,1.4f);
                var piece=Add("Cloud",cloud,cloudMaterial,new Vector3(Range(-HalfWidth,HalfWidth),Range(Bottom,Top),CloudZ),0,1.6f,-20);
                piece.kind=Kind.Cloud;piece.transform.localScale=Vector3.one*scale;
                // The shadow lies on the ground below and behind the cloud, as if lit from the upper left.
                var shadow=Add("Cloud shadow",cloud,cloudMaterial,Vector3.zero,0,0,-48);
                shadow.transform.localScale=Vector3.one*scale;shadow.transform.GetComponent<MeshRenderer>().SetPropertyBlock(shadowTint);
                pieces.Remove(shadow);piece.shadow=shadow.transform;
            }
        }
    }

    float Range(float lo,float hi)=>Mathf.Lerp(lo,hi,(float)random.NextDouble());

    void Scatter(string name,Mesh[] meshes,int count,float z,int order,Kind kind)
    {
        if(meshes==null || meshes.Length==0)return;
        for(int i=0;i<count;i++)
        {
            var mesh=meshes[i%meshes.Length];
            var piece=Add(name,mesh,material,new Vector3(0,0,z),Range(0,360),1,order);
            piece.kind=kind;piece.radius=Mathf.Max(mesh.bounds.extents.x,mesh.bounds.extents.y)*.9f;
            Place(piece,Bottom,Top);
        }
    }

    /// <summary>Random spot in the y band that keeps water and solid pieces apart; trees favour the
    /// playfield edges so the centre lane stays readable.</summary>
    void Place(Piece piece,float yMin,float yMax)
    {
        var p=piece.transform.localPosition;
        for(int attempt=0;attempt<16;attempt++)
        {
            p.y=Range(yMin,yMax);
            piece.baseX=piece.kind==Kind.Tree ? (random.NextDouble()<.5 ? -1 : 1)*Range(1.8f,HalfWidth) : Range(-HalfWidth,HalfWidth);
            if(!OnWater(piece,piece.baseX,p.y))break;
        }
        piece.transform.localPosition=new Vector3(piece.baseX,p.y,p.z);
    }

    bool OnWater(Piece piece,float x,float y)
    {
        if(piece.kind==Kind.Cloud || piece.kind==Kind.Ground)return false;
        foreach(var other in pieces)
        {
            if(other==piece || other.kind==Kind.Ground || other.kind==Kind.Cloud)continue;
            // Water must not touch anything solid, and solid things must not touch water.
            if((piece.kind==Kind.Water)==(other.kind==Kind.Water))continue;
            var o=other.transform.localPosition;
            if(new Vector2(x-other.baseX,y-o.y).magnitude<piece.radius+other.radius+.15f)return true;
        }
        return false;
    }

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
        if(player==null){var go=GameObject.FindGameObjectWithTag("Player");if(go!=null)player=go.transform;}
        float side=player!=null ? -player.position.x*sideParallax : 0;
        foreach(var piece in pieces)
        {
            var p=piece.transform.localPosition;
            p.y-=scrollSpeed*piece.speed*Time.deltaTime;
            if(piece.kind==Kind.Ground){if(p.y<-25)p.y+=4*GroundHeight;}
            else if(p.y<Bottom)
            {
                piece.transform.localPosition=p;
                // Re-enter just above the top, in a short band so recycled pieces stay spread out.
                Place(piece,p.y+(Top-Bottom),p.y+(Top-Bottom)+2);
                if(piece.shadow==null)piece.transform.localRotation=Quaternion.Euler(0,0,Range(0,360));
                p=piece.transform.localPosition;
            }
            p.x=piece.baseX+side*(piece.shadow!=null ? 2 : 1);
            piece.transform.localPosition=p;
            if(piece.shadow!=null)piece.shadow.localPosition=new Vector3(piece.baseX+side+.9f,p.y-1.1f,GroundZ-.3f);
        }
    }
}
