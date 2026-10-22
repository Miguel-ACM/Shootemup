using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Blender-authored, exactly repeating 20-unit sections, independent of combat and inventory.</summary>
public sealed class LevelEnvironment : MonoBehaviour
{
    [Serializable] sealed class Kit { public MeshData[] meshes; }
    [Serializable] sealed class MeshData { public string name;public float[] vertices,colors;public int[] triangles; }
    static Dictionary<string,Mesh> kit;
    static LevelEnvironment active;
    Material material;
    readonly List<Transform> sections=new List<Transform>();
    readonly List<Transform> clouds=new List<Transform>();
    Mesh weather;
    Vector3[] weatherVertices;
    Color[] weatherColors;
    readonly List<Mesh> ownedMeshes=new List<Mesh>();
    struct Flake { public Vector2 position;public float speed,size,phase; }
    Flake[] flakes;
    LevelId level;
    float travel,clock,stormAt,flash;
    AudioSource thunder;
    AudioClip rumble;
    LineRenderer bolt;
    System.Random random;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRuntime() { kit=null;active=null; }

    public static void Apply()
    {
        if(active!=null)
        {
            // Hide immediately; Destroy is deferred until the end of the frame.
            active.gameObject.SetActive(false);Destroy(active.gameObject);
        }
        foreach(var field in FindObjectsByType<PSXGrassField>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            field.gameObject.SetActive(LevelRun.Current==LevelId.Fields);
        // Keep the existing industrial component's camera/compositor setup, but use the continuous kit here.
        foreach(var industrial in FindObjectsByType<PSXIndustrialField>(FindObjectsSortMode.None)) industrial.steelMaterial=null;
        if(LevelRun.Current==LevelId.Fields)return;
        var root=new GameObject(LevelRun.CurrentName+" environment");
        active=root.AddComponent<LevelEnvironment>();
        active.level=LevelRun.Current;
        active.Build();
    }

    static void LoadKit()
    {
        if(kit!=null)return;
        kit=new Dictionary<string,Mesh>();
        var data=JsonUtility.FromJson<Kit>(Resources.Load<TextAsset>("LevelKit").text);
        foreach(var item in data.meshes)
        {
            int count=item.vertices.Length/3;var vertices=new Vector3[count];var colors=new Color[count];
            for(int i=0;i<count;i++)
            {
                vertices[i]=new Vector3(item.vertices[i*3],item.vertices[i*3+1],item.vertices[i*3+2]);
                colors[i]=new Color(item.colors[i*4],item.colors[i*4+1],item.colors[i*4+2],item.colors[i*4+3]);
            }
            var mesh=new Mesh{name=item.name,indexFormat=IndexFormat.UInt32};
            mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=item.triangles;mesh.RecalculateBounds();
            kit.Add(item.name,mesh);
        }
    }

    void Build()
    {
        LoadKit();random=new System.Random(810+(int)level);
        material=new Material(Resources.Load<Shader>("LevelScenery"));
        var camera=UnityEngine.Camera.main;
        if(camera!=null)camera.backgroundColor=level==LevelId.Snow ? new Color(.31f,.4f,.46f) : new Color(.025f,.035f,.055f);
        for(int i=0;i<4;i++)
        {
            string name=level==LevelId.City ? "CityBlock"+i%3 : level==LevelId.Tunnel ? "TunnelSection" : "SnowSection";
            if(level==LevelId.Thunder)
            {
                var backdrop=Quad("Storm abyss",16,20,new Color(.045f,.065f,.095f));
                sections.Add(Add(backdrop,new Vector3(0,(i-1)*20,94),-45));
            }
            else
            {
                var section=Add(kit[name],new Vector3(0,(i-1)*20,88),-40);
                section.localScale=new Vector3(.65f,1,1);sections.Add(section);
            }
        }
        if(level==LevelId.Snow || level==LevelId.Thunder)BuildWeather();
        if(level==LevelId.Thunder)
        {
            for(int i=0;i<15;i++)
            {
                var cloud=Add(kit["StormCloud"],new Vector3(Range(-7,7),Range(-16,30),65+i*.08f),-25);
                cloud.localScale=Vector3.one*Range(1,1.7f);cloud.localRotation=Quaternion.Euler(0,0,Range(-25,25));clouds.Add(cloud);
            }
            var go=new GameObject("Forked lightning");go.transform.SetParent(transform,false);
            bolt=go.AddComponent<LineRenderer>();bolt.sharedMaterial=material;bolt.startColor=bolt.endColor=new Color(.78f,.87f,1);
            bolt.startWidth=.075f;bolt.endWidth=.025f;bolt.sortingLayerName="Background";bolt.sortingOrder=-10;bolt.enabled=false;
            thunder=gameObject.AddComponent<AudioSource>();thunder.volume=.19f;
            rumble=CreateThunder();thunder.clip=rumble;stormAt=1.6f;
        }
    }

    float Range(float lo,float hi)=>Mathf.Lerp(lo,hi,(float)random.NextDouble());
    Transform Add(Mesh mesh,Vector3 position,int order)
    {
        var go=new GameObject(mesh.name);go.transform.SetParent(transform,false);go.transform.localPosition=position;
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerName="Background";renderer.sortingOrder=order;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return go.transform;
    }
    Mesh Quad(string name,float width,float height,Color color)
    {
        var mesh=new Mesh{name=name};mesh.vertices=new[]{new Vector3(-width/2,-height/2),new Vector3(width/2,-height/2),new Vector3(width/2,height/2),new Vector3(-width/2,height/2)};
        mesh.colors=new[]{color,color,color,color};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateBounds();ownedMeshes.Add(mesh);return mesh;
    }

    void BuildWeather()
    {
        flakes=new Flake[level==LevelId.Snow ? 650 : 1000];
        weatherVertices=new Vector3[flakes.Length*4];weatherColors=new Color[flakes.Length*4];var triangles=new int[flakes.Length*6];
        for(int i=0;i<flakes.Length;i++)
        {
            flakes[i]=new Flake{position=new Vector2(Range(-9,9),Range(-14,16)),speed=Range(.6f,1.6f),size=Range(.025f,.08f),phase=Range(0,6.28f)};
            int k=i*4,t=i*6;triangles[t]=k;triangles[t+1]=k+2;triangles[t+2]=k+1;triangles[t+3]=k;triangles[t+4]=k+3;triangles[t+5]=k+2;
            var color=level==LevelId.Snow ? new Color(.87f,.95f,1,Range(.35f,.85f)) : new Color(.52f,.68f,.83f,Range(.13f,.38f));
            for(int j=0;j<4;j++)weatherColors[k+j]=color;
        }
        weather=new Mesh{name=level+" precipitation"};weather.MarkDynamic();weather.vertices=weatherVertices;weather.colors=weatherColors;weather.triangles=triangles;
        weather.bounds=new Bounds(Vector3.zero,new Vector3(30,40,2));ownedMeshes.Add(weather);Add(weather,new Vector3(0,0,32),-5);
        UpdateWeather(0);
    }

    void Update()
    {
        float dt=Time.deltaTime;clock+=dt;
        travel=Mathf.Repeat(travel+dt*(level==LevelId.Tunnel ? 5.4f : 4.4f),80);
        for(int i=0;i<sections.Count;i++)sections[i].localPosition=new Vector3(0,Mathf.Repeat((i-1)*20-travel+30,80)-30,sections[i].localPosition.z);
        if(flakes!=null)UpdateWeather(dt);
        if(level!=LevelId.Thunder)return;
        for(int i=0;i<clouds.Count;i++)
        {
            var p=clouds[i].localPosition;p.y-=dt*3;p.x+=Mathf.Sin(clock*.3f+i)*dt*.15f;
            if(p.y < -18)p.y+=48;clouds[i].localPosition=p;
        }
        if(clock>=stormAt)
        {
            stormAt=clock+Range(3.8f,7);flash=1;thunder.pitch=Range(.8f,1.1f);thunder.PlayDelayed(.22f);
            bolt.positionCount=9;float x=Range(-4,4);
            for(int i=0;i<9;i++){x+=Range(-.65f,.65f);bolt.SetPosition(i,new Vector3(x,12-i*2.6f,36));}
        }
        flash=Mathf.MoveTowards(flash,0,dt*2.8f);
        // A second sharp flicker follows the first stroke.
        float strength=flash>.65f ? flash*.58f : flash>.4f ? .04f : flash*.8f;
        material.SetFloat("_Flash",strength);bolt.enabled=flash>.7f || (flash<.4f && flash>.28f);
    }

    void UpdateWeather(float dt)
    {
        bool snow=level==LevelId.Snow;
        float wind=snow ? -1.8f+Mathf.Sin(clock*.65f)*1.8f : -4;
        for(int i=0;i<flakes.Length;i++)
        {
            var f=flakes[i];f.position.y-=dt*f.speed*(snow ? 6.5f : 22);
            f.position.x+=dt*(wind+(snow ? Mathf.Sin(clock*1.8f+f.phase)*1.4f : 0));
            f.position.y=Mathf.Repeat(f.position.y+14,30)-14;f.position.x=Mathf.Repeat(f.position.x+9,18)-9;flakes[i]=f;
            float w=snow ? f.size : .012f;float h=snow ? f.size : .45f*f.speed;float slant=snow ? 0 : .11f;
            int k=i*4;var p=(Vector3)f.position;
            weatherVertices[k]=p+new Vector3(-w,-h);weatherVertices[k+1]=p+new Vector3(w,-h);
            weatherVertices[k+2]=p+new Vector3(w+slant,h);weatherVertices[k+3]=p+new Vector3(-w+slant,h);
        }
        weather.vertices=weatherVertices;
    }

    AudioClip CreateThunder()
    {
        const int rate=22050;var samples=new float[rate*3];float low=0;
        for(int i=0;i<samples.Length;i++)
        {
            float t=i/(float)rate;low=Mathf.Lerp(low,Range(-1,1),.035f);
            samples[i]=(low*.85f+Mathf.Sin(t*62)*.05f)*Mathf.Exp(-t*1.4f)*Mathf.Clamp01(t*35);
        }
        var clip=AudioClip.Create("Storm rumble",samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
    }
    void OnDestroy()
    {
        if(material!=null)Destroy(material);
        if(rumble!=null)Destroy(rumble);
        foreach(var mesh in ownedMeshes)if(mesh!=null)Destroy(mesh);
        if(active==this)active=null;
    }
}
