using System.Collections.Generic;
using UnityEngine;

// Architectural routes anchored to the scrolling tunnel rather than the camera.
[DefaultExecutionOrder(-200)]
public sealed class TunnelPassage : MonoBehaviour
{
    static TunnelPassage current;
    CombatWall[] walls;
    float travel;
    public const int LayoutCount=6;
    const float Step=9.5f;
    // (lane centre, half-width). Sloping joins give advance sight of each bend.
    static readonly Vector2[][] routes={
        new[]{new Vector2(0,2.5f),new Vector2(0,2.1f),new Vector2(.4f,1.85f),new Vector2(.4f,2.7f),new Vector2(-.4f,2.7f),new Vector2(-.4f,1.85f),new Vector2(0,2.5f)},
        new[]{new Vector2(0,2.5f),new Vector2(-1.35f,2.05f),new Vector2(-1.35f,2.05f),new Vector2(1.35f,2.05f),new Vector2(1.35f,2.05f),new Vector2(-1.35f,2.05f),new Vector2(-1.35f,2.05f),new Vector2(0,2.5f)},
        new[]{new Vector2(0,2.5f),new Vector2(1.45f,2.1f),new Vector2(1.45f,1.9f),new Vector2(-1.45f,2.1f),new Vector2(-1.45f,2.7f),new Vector2(1.45f,2.7f),new Vector2(1.45f,1.9f),new Vector2(-1.45f,2.1f),new Vector2(-1.45f,2.1f),new Vector2(0,2.5f)},
        new[]{new Vector2(0,2.5f),new Vector2(-1.1f,2.2f),new Vector2(-1.1f,2.2f),new Vector2(1.1f,2.2f),new Vector2(1.1f,2.2f),new Vector2(-1.1f,2.2f),new Vector2(-1.1f,2.2f),new Vector2(0,2.5f)},
        new[]{new Vector2(0,2.5f),new Vector2(1.45f,1.9f),new Vector2(1.45f,1.9f),new Vector2(0,3.3f),new Vector2(-1.45f,1.9f),new Vector2(-1.45f,1.9f),new Vector2(0,3.3f),new Vector2(1.45f,1.9f),new Vector2(0,2.5f)},
        new[]{new Vector2(0,2.5f),new Vector2(-1.5f,2.1f),new Vector2(-1.5f,1.85f),new Vector2(1.5f,2.1f),new Vector2(1.5f,2.8f),new Vector2(-1.5f,2.8f),new Vector2(-1.5f,1.85f),new Vector2(1.5f,2.1f),new Vector2(1.5f,2.1f),new Vector2(-1.5f,2.1f),new Vector2(0,2.5f)}
    };
    public static bool Active=>current!=null;
    public static int Layout { get; private set; }
    public static float Length(int layout)=>(routes[layout-1].Length-1)*Step;
    public static Vector2 Opening(int layout,float y)
    {
        var route=routes[layout-1];float at=Mathf.Clamp((y-Camera.UpLimit-2)/Step,0,route.Length-1);
        int i=Mathf.Min(route.Length-2,Mathf.FloorToInt(at));return Vector2.Lerp(route[i],route[i+1],at-i);
    }
    public static float CenterAt(float screenY)=>current!=null?Opening(Layout,screenY+current.travel).x:0;
    public static CombatWall[] Sections(int layout)
    {
        var result=new List<CombatWall>();var route=routes[layout-1];
        float left=Camera.LeftLimit-1,right=Camera.RightLimit+1,bottom=Camera.UpLimit+2;
        for(int i=0;i<route.Length-1;i++)
        {
            var a=route[i];var b=route[i+1];
            if(layout!=4||i%4<2)result.Add(new CombatWall(bottom+i*Step,bottom+(i+1)*Step,left,left,a.x-a.y,b.x-b.y));
            if(layout!=4||i%4>=2)result.Add(new CombatWall(bottom+i*Step,bottom+(i+1)*Step,a.x+a.y,b.x+b.y,right,right));
        }
        return result.ToArray();
    }
    public static void Clear()
    {
        CombatWalls.Solid.Clear();Layout=0;
        if(current!=null){var old=current;current=null;Destroy(old.gameObject);}
    }
    public static void Begin(int layout)
    {
        Clear();Layout=layout;
        var go=new GameObject("Solid tunnel passage");current=go.AddComponent<TunnelPassage>();
        current.walls=Sections(layout);
        go.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.TunnelPassageMesh(layout);
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=CombatMaterials.Fleet;renderer.sortingLayerName="Enemy";
        current.Publish();
    }
    void Publish()
    {
        CombatWalls.Solid.Clear();
        foreach(var wall in walls)
        {
            var moved=wall.Translated(-travel);
            if(moved.Top>=Camera.DownLimit-4&&moved.Bottom<=Camera.UpLimit+6)CombatWalls.Solid.Add(moved);
        }
    }
    void Update()
    {
        if(LevelRun.Current!=LevelId.Tunnel||CloudTransition.Busy||GameOverState.Active||CombatDecision.Active){Clear();return;}
        if(WorldTimeStop.Frozen||GameRules.playerDisabled)return;
        travel+=LevelEnvironment.TunnelSpeed*Time.deltaTime;transform.position=Vector3.down*travel;Publish();
        if(Camera.UpLimit+2+Length(Layout)-travel<Camera.DownLimit-1)Clear();
    }
    void OnDestroy(){if(current==this){current=null;CombatWalls.Solid.Clear();Layout=0;}}
}

