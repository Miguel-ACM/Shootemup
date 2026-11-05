using UnityEngine;

// Foreground tunnel structures share the backdrop's travel speed. A passage is
// part of one scored wave, and is gone before a decision, miniboss or boss begins.
[DefaultExecutionOrder(-200)]
public sealed class TunnelPassage : MonoBehaviour
{
    static TunnelPassage current;
    readonly System.Collections.Generic.List<Rect> walls = new();
    float travel;
    public static bool Active => current != null;
    public static int Layout { get; private set; }
    public static void Clear()
    {
        CombatWalls.Solid.Clear(); Layout=0;
        if(current!=null){var old=current;current=null;Destroy(old.gameObject);}
    }
    public static Rect[] Sections(int layout)
    {
        float left=Camera.LeftLimit-1,right=Camera.RightLimit+1;
        float center=layout==2 ? -1.0f : layout==3 ? 1.0f : 0;
        const float half=2.35f;
        float bottom=Camera.UpLimit+2;
        return new[]{Rect.MinMaxRect(left,bottom,center-half,bottom+38),Rect.MinMaxRect(center+half,bottom,right,bottom+38)};
    }
    public static void Begin(int layout)
    {
        Clear(); Layout=layout;
        var go=new GameObject("Solid tunnel passage");
        current=go.AddComponent<TunnelPassage>();
        foreach(var r in Sections(layout))
        {
            current.walls.Add(r);
            for(float y=r.yMin;y<r.yMax;y+=4.75f)
            {
                var part=new GameObject("Ribbed masonry"); part.transform.SetParent(go.transform,false);
                part.transform.localPosition=new Vector3(r.center.x,y+2.375f,.4f);
                part.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.TunnelWallMesh(r.width);
                var renderer=part.AddComponent<MeshRenderer>(); renderer.sharedMaterial=CombatMaterials.Fleet;
                renderer.sortingLayerName="Enemy";
            }
        }
        current.Publish();
    }
    void Publish()
    {
        CombatWalls.Solid.Clear();
        foreach(var r in walls)CombatWalls.Solid.Add(new Rect(r.x,r.y-travel,r.width,r.height));
    }
    void Update()
    {
        if(LevelRun.Current!=LevelId.Tunnel||CloudTransition.Busy||GameOverState.Active||CombatDecision.Active){Clear();return;}
        if(WorldTimeStop.Frozen||GameRules.playerDisabled)return;
        travel+=LevelEnvironment.TunnelSpeed*Time.deltaTime;
        transform.position=Vector3.down*travel;
        Publish();
        if(walls.Count>0&&walls[0].yMax-travel<Camera.DownLimit-1)Clear();
    }
    void OnDestroy(){if(current==this){current=null;CombatWalls.Solid.Clear();Layout=0;}}
}
