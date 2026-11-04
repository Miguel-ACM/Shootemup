using UnityEngine;

// Ophanim: three independently turning, eye-covered wheels surrounding one living core.
public sealed class HeavenAngelVisual : MonoBehaviour
{
    public static readonly float[] Radii={2.3f,1.93f,1.55f};
    public static readonly int[] EyeCounts={14,12,10};
    static readonly float[] Speeds={8,-11,15};
    static Material material;
    public static Material Surface=>material!=null?material:material=new Material(Resources.Load<Shader>("HeavenAngel")){name="Living pearl and celestial gold"};
    readonly Transform[] wheels=new Transform[3];
    readonly Transform[][] eyes=new Transform[3][];
    readonly Vector2[] contactPoints=new Vector2[8];
    Transform coreEye;
    PolygonCollider2D hitbox;
    float clock;
    public Transform Wheel(int index)=>wheels[index];
    public int TotalEyes=>37;

    public static Quaternion Orientation(int wheel,float seconds)
    {
        var tilt=wheel==0?new Vector3(25,12,-12):wheel==1?new Vector3(59,-18,55):new Vector3(-49,24,-38);
        // Slow precession makes the overlapping wheels unmistakably three-dimensional.
        tilt.y+=Mathf.Sin(seconds*.12f+wheel*2)*7;
        return Quaternion.Euler(tilt)*Quaternion.AngleAxis(seconds*Speeds[wheel],Vector3.forward);
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
    public void Step(float delta){clock+=delta;Pose(clock);}
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
