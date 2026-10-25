using UnityEngine;

// Short-lived marks are stored in terrain coordinates, so tracks never slide with their vehicle.
public sealed class GroundTreadMarks : MonoBehaviour
{
    struct Mark {public Transform tile;public Vector3 a,b,c,d;public float life;}
    readonly Mark[] history=new Mark[72];
    readonly Vector3[] vertices=new Vector3[288];
    readonly Color[] colors=new Color[288];
    static Material material;
    Mesh mesh;
    Transform visual;
    ParticleSystem powder;
    float distance,nextPowder;
    int cursor;
    public bool Snow;
    public int MarkCount {get;private set;}
    void Start()
    {
        if(material==null)material=new Material(Resources.Load<Shader>("LevelScenery")){name="Terrain vehicle tracks"};
        var go=new GameObject("Attached tread furrows");go.transform.SetParent(transform,false);visual=go.transform;
        mesh=new Mesh{name="Rolling ground tread marks"};mesh.MarkDynamic();mesh.vertices=vertices;mesh.colors=colors;
        var triangles=new int[432];for(int i=0;i<72;i++){int k=i*4,t=i*6;triangles[t]=k;triangles[t+1]=k+1;triangles[t+2]=k+2;triangles[t+3]=k;triangles[t+4]=k+2;triangles[t+5]=k+3;}
        mesh.triangles=triangles;mesh.bounds=new Bounds(Vector3.zero,new Vector3(40,80,200));go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerName="Background";renderer.sortingOrder=-34;
        powder=PSXImpactEffects.Create(transform,Snow ? "Auger snow powder" : "Tread dirt",80);
        var particles=powder.GetComponent<ParticleSystemRenderer>();particles.sortingLayerName="Background";particles.sortingOrder=-31;
    }
    public void Step(Transform tile,Vector3 p,Quaternion facing,float travelled)
    {
        if(mesh==null)return;distance+=travelled;
        if(distance>.18f)
        {
            distance=0;var side=facing*Vector3.right;var forward=facing*Vector3.up;
            foreach(int s in new[]{-1,1})
            {
                var centre=p+side*s*(Snow ? .37f : .36f)+Vector3.back*.025f;
                history[cursor]=new Mark{tile=tile,a=tile.InverseTransformPoint(centre-side*.07f-forward*.10f),b=tile.InverseTransformPoint(centre+side*.07f-forward*.10f),
                    c=tile.InverseTransformPoint(centre+side*.07f+forward*.10f),d=tile.InverseTransformPoint(centre-side*.07f+forward*.10f),life=2.6f};
                cursor=(cursor+1)%history.Length;
            }
        }
        if(Time.time>=nextPowder && travelled>.001f)
        {
            nextPowder=Time.time+.10f;
            foreach(int s in new[]{-1,1})
            {
                var e=new ParticleSystem.EmitParams();e.position=p+facing*new Vector3(s*.39f,.52f,-.12f);
                e.velocity=facing*new Vector3(s*.3f,.8f,-.3f);e.startColor=Snow ? new Color(.78f,.86f,.88f,.65f) : new Color(.44f,.40f,.25f,.55f);
                e.startSize3D=Vector3.one*(Snow ? .09f : .055f);e.startLifetime=.35f;powder.Emit(e,2);
            }
        }
    }
    void LateUpdate()
    {
        if(mesh==null)return;visual.position=Vector3.zero;visual.rotation=Quaternion.identity;MarkCount=0;
        for(int i=0;i<history.Length;i++)
        {
            var mark=history[i];mark.life-=Time.deltaTime;history[i]=mark;int k=i*4;
            if(mark.life<=0 || mark.tile==null){for(int j=0;j<4;j++)colors[k+j]=Color.clear;continue;}
            MarkCount++;vertices[k]=mark.tile.TransformPoint(mark.a);vertices[k+1]=mark.tile.TransformPoint(mark.b);vertices[k+2]=mark.tile.TransformPoint(mark.c);vertices[k+3]=mark.tile.TransformPoint(mark.d);
            var c=Snow ? new Color(.36f,.49f,.55f,.22f) : new Color(.10f,.13f,.09f,.32f);c.a*=Mathf.Clamp01(mark.life/.7f);for(int j=0;j<4;j++)colors[k+j]=c;
        }
        mesh.vertices=vertices;mesh.colors=colors;
    }
    void OnDestroy(){if(mesh!=null)Destroy(mesh);}
}
