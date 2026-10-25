using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>A composed river valley, built as four matching sections rather than scattered props.</summary>
public sealed class PSXGrassField : MonoBehaviour
{
    // Retain the installed kit references so existing scenes and the fleet installer stay compatible.
    public Mesh[] puddles, rocks, bushes, trees;
    public Material material, groundMaterial, cloudMaterial;
    [Range(.2f,8f)] public float scrollSpeed=4.4f;
    public float grassTile=10;
    [Range(0,.5f)] public float sideParallax=.12f;
    [Range(.2f,1)] public float brightness=.62f;
    const float SectionLength=24, LoopLength=96;
    readonly List<Transform> sections=new List<Transform>();
    readonly List<Mesh> ownedMeshes=new List<Mesh>();
    readonly List<Transform> clouds=new List<Transform>();
    Transform player;
    float travel;

    void Start()
    {
        var landscape=Resources.Load<Material>("PSXValley");
        if(landscape==null){Debug.LogError("PSX Valley material is missing",this);return;}
        for(int i=0;i<4;i++)
        {
            float origin=(i-1)*SectionLength;
            var go=new GameObject("Valley section "+i);go.transform.SetParent(transform,false);
            go.transform.localPosition=new Vector3(0,origin,88);
            var mesh=PSXValleyGeometry.Build(origin,SectionLength,brightness/.62f,LevelRun.Seed);
            ownedMeshes.Add(mesh);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=landscape;
            renderer.sortingLayerName="Background";renderer.sortingOrder=-40;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            sections.Add(go.transform);
        }
        if(cloudMaterial!=null)
            for(int i=0;i<3;i++)
            {
                var go=new GameObject("High thin cloud "+i);go.transform.SetParent(transform,false);
                go.transform.localPosition=new Vector3(i==1 ? 4.7f : -4.6f,-12+i*14,60);
                var mesh=CloudQuad(i==1 ? 0 : 2);ownedMeshes.Add(mesh);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=cloudMaterial;
                renderer.sortingLayerName="Background";renderer.sortingOrder=-20;
                var tint=new MaterialPropertyBlock();tint.SetColor("_Tint",new Color(.7f,.78f,.79f,.17f));renderer.SetPropertyBlock(tint);
                clouds.Add(go.transform);
            }
    }

    static Mesh CloudQuad(int row)
    {
        float v=1-(row+1)*.25f;
        var mesh=new Mesh{name="Thin valley cloud"};
        mesh.vertices=new[]{new Vector3(-2,-.6f),new Vector3(2,-.6f),new Vector3(2,.6f),new Vector3(-2,.6f)};
        mesh.uv=new[]{new Vector2(0,v),new Vector2(1,v),new Vector2(1,v+.25f),new Vector2(0,v+.25f)};
        mesh.colors=new[]{Color.white,Color.white,Color.white,Color.white};
        mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateBounds();return mesh;
    }

    void Update()
    {
        if(player==null){var go=GameObject.FindGameObjectWithTag("Player");if(go!=null)player=go.transform;}
        float side=player!=null ? -player.position.x*sideParallax : 0;
        travel=Mathf.Repeat(travel+scrollSpeed*Time.deltaTime,LoopLength);
        for(int i=0;i<sections.Count;i++)
        {
            float origin=(i-1)*SectionLength;
            // Wrap entire matching sections; their river, road and scenery stay attached.
            float y=Mathf.Repeat(origin-travel+36,LoopLength)-36;
            sections[i].localPosition=new Vector3(side,y,88);
        }
        foreach(var cloud in clouds)
        {
            var p=cloud.localPosition;p.y-=scrollSpeed*1.12f*Time.deltaTime;
            if(p.y < -20)p.y+=42;
            p.x=(cloud==clouds[1] ? 4.7f : -4.6f)+side*1.7f;
            cloud.localPosition=p;
        }
    }

    void OnDestroy(){foreach(var mesh in ownedMeshes)if(mesh!=null)Destroy(mesh);}
}
