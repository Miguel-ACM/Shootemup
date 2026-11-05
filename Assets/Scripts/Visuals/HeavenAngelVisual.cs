using UnityEngine;

// Ophanim: three independently turning, eye-covered wheels surrounding one living core.
public sealed class HeavenAngelVisual : MonoBehaviour
{
    public static readonly float[] Radii={2.3f,1.93f,1.55f};
    public static readonly int[] EyeCounts={14,12,10};
    static readonly float[] Speeds={24,-31,39};
    static Material material;
    public static Material Surface=>material!=null?material:material=new Material(Resources.Load<Shader>("HeavenAngel")){name="Living pearl and celestial gold"};
    readonly Transform[] wheels=new Transform[3];
    readonly Transform[][] eyes=new Transform[3][];
    readonly Vector2[] contactPoints=new Vector2[8];
    Transform coreEye,iris;
    readonly Transform[] committed=new Transform[5];
    bool praying;
    float prayer,wheelClock;
    PolygonCollider2D hitbox;
    float clock;
    public int Phase { get; set; } = 1;
    public Transform Wheel(int index)=>wheels[index];
    public int TotalEyes=>37;

    public static Quaternion Orientation(int wheel,float seconds)
    {
        var tilt=wheel==0?new Vector3(25,12,-12):wheel==1?new Vector3(59,-18,55):new Vector3(-49,24,-38);
        // Each wheel tumbles through the others as well as turning around its own axle.
        // Opposed world axes prevent three flat discs merely chasing their own eyes.
        Vector3 axis=wheel==0?Vector3.up:wheel==1?Vector3.right:new Vector3(1,1,0).normalized;
        float tumble=seconds*(wheel==1?-19:14+wheel*5);
        return Quaternion.AngleAxis(tumble,axis)*Quaternion.Euler(tilt)*
            Quaternion.AngleAxis(seconds*Speeds[wheel],Vector3.forward);
    }
    public static Vector3 EyePosition(int wheel,int index)
    {
        float angle=index*Mathf.PI*2/EyeCounts[wheel];
        return new Vector3(Mathf.Cos(angle)*Radii[wheel],Mathf.Sin(angle)*Radii[wheel],-.23f);
    }
    public void Build(PolygonCollider2D damageHull=null)
    {
        hitbox=damageHull;
        GetComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.HeavenAngelCore();
        GetComponent<MeshRenderer>().sharedMaterial=Surface;
        coreEye=new GameObject("Central living eye").transform;coreEye.SetParent(transform,false);coreEye.localPosition=new Vector3(0,0,-.8f);
        iris=new GameObject("Tracking iris").transform;iris.SetParent(transform,false);iris.localPosition=new Vector3(0,0,-.77f);
        iris.gameObject.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.HeavenAngelIris();
        var irisRenderer=iris.gameObject.AddComponent<MeshRenderer>();irisRenderer.sharedMaterial=Surface;irisRenderer.sortingLayerName="Enemy";
        for(int ring=0;ring<3;ring++)
        {
            var wheel=new GameObject("Eye-covered golden wheel "+(ring+1));wheel.layer=gameObject.layer;
            wheels[ring]=wheel.transform;wheel.transform.SetParent(transform,false);
            wheel.AddComponent<MeshFilter>().sharedMesh=IndustrialMeshFactory.HeavenAngelWheel(ring);
            var renderer=wheel.AddComponent<MeshRenderer>();renderer.sharedMaterial=Surface;renderer.sortingLayerName="Enemy";
            eyes[ring]=new Transform[EyeCounts[ring]];
            for(int eye=0;eye<eyes[ring].Length;eye++)
            {
                var port=new GameObject("Living eye "+(eye+1)).transform;port.SetParent(wheel.transform,false);port.localPosition=EyePosition(ring,eye);
                port.localRotation=Quaternion.Euler(0,0,eye*360f/EyeCounts[ring]+90);eyes[ring][eye]=port;
            }
        }
        if(hitbox!=null)hitbox.pathCount=TotalEyes;
        Pose(0);
    }
    public void Pray(bool active)
    {
        if(active&&!praying)for(int i=0;i<5;i++)committed[i]=Emitter(i);
        praying=active;
        if(!active)for(int i=0;i<5;i++)committed[i]=null;
    }
    public void Step(float delta)
    {
        clock+=delta;
        prayer=Mathf.MoveTowards(prayer,praying?1:0,delta*2);
        wheelClock+=delta*(1+(Phase-1)*.55f)*Mathf.Lerp(1,.78f,prayer);
        Pose(wheelClock);
        if(iris!=null)
        {
            Vector3 gaze=CombatVisuals.Player!=null?transform.InverseTransformPoint(CombatVisuals.Player.position):Vector3.down;
            var look=Vector2.ClampMagnitude(gaze,.12f);
            iris.localPosition=new Vector3(look.x,look.y*.65f,-.77f);
            float blink=Mathf.Pow(Mathf.Max(0,Mathf.Cos(clock*.88f)),48)*(.8f-prayer*.7f);
            iris.localScale=new Vector3(1+prayer*.12f,1-blink,1);
        }
    }
    public void Pose(float seconds)
    {
        for(int ring=0;ring<3;ring++)wheels[ring].localRotation=Orientation(ring,seconds);
        if(hitbox==null)return;
        int path=1;
        foreach(var bank in eyes)foreach(var eye in bank)
        {
            for(int vertex=0;vertex<contactPoints.Length;vertex++)
            {
                float angle=vertex*Mathf.PI*2/contactPoints.Length;
                contactPoints[vertex]=hitbox.transform.InverseTransformPoint(eye.TransformPoint(new Vector3(Mathf.Cos(angle)*.27f,Mathf.Sin(angle)*.14f,0)));
            }
            hitbox.SetPath(path++,contactPoints);
        }
    }
    public Transform Emitter(int slot)
    {
        if(praying&&committed[slot]!=null)return committed[slot];
        if(slot==0)return coreEye;
        var bank=eyes[slot<=2?1:0];bool left=slot==1||slot==3;
        Transform chosen=bank[0];float best=float.NegativeInfinity;
        foreach(var eye in bank)
        {
            var local=transform.InverseTransformPoint(eye.position);
            float score=(left?-local.x:local.x)+(slot<=2?-local.y:local.y)*.15f;
            if(score>best){best=score;chosen=eye;}
        }
        return chosen;
    }
}
