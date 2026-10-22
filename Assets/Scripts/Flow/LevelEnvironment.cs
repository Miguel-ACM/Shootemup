using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Composed scrolling districts with the same ship-driven lateral parallax as Fields.</summary>
public sealed class LevelEnvironment : MonoBehaviour
{
    [Serializable] sealed class Kit { public MeshData[] meshes; }
    [Serializable] sealed class MeshData { public string name;public float[] vertices,colors;public int[] triangles; }
    sealed class Atmosphere { public Transform transform;public Material material;public float parallax,speed; }
    sealed class Traffic { public Transform transform;public float x,y,speed; }
    struct Flake { public Vector2 position;public float speed,size,phase,depth; }
    const float SectionLength=20,LoopLength=80;
    static Dictionary<string,Mesh> kit;
    static LevelEnvironment active;
    [Range(0,.5f)] public float sideParallax=.12f;
    public float HorizontalOffset { get; private set; }
    Material material;
    Transform player,weatherTransform,lightningRoot;
    readonly List<Transform> sections=new List<Transform>();
    readonly List<Transform> foreground=new List<Transform>();
    readonly List<Atmosphere> atmosphere=new List<Atmosphere>();
    readonly List<Traffic> traffic=new List<Traffic>();
    readonly List<Mesh> ownedMeshes=new List<Mesh>();
    readonly List<AudioClip> ownedAudio=new List<AudioClip>();
    Mesh weather;
    Vector3[] weatherVertices;
    Flake[] flakes;
    LevelId level;
    float travel,clock,stormAt,flash;
    AudioSource thunder;
    readonly LineRenderer[] bolts=new LineRenderer[3];
    System.Random random;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRuntime() { kit=null;active=null; }

    public static void Apply()
    {
        if(active!=null){active.gameObject.SetActive(false);Destroy(active.gameObject);}
        float parallax=.12f;
        foreach(var field in FindObjectsByType<PSXGrassField>(FindObjectsInactive.Include))
        {
            parallax=field.sideParallax;
            field.gameObject.SetActive(LevelRun.Current==LevelId.Fields);
        }
        // Retain the existing camera/compositor setup without its old disconnected scenery.
        foreach(var industrial in FindObjectsByType<PSXIndustrialField>())industrial.steelMaterial=null;
        if(LevelRun.Current==LevelId.Fields)return;
        active=new GameObject(LevelRun.CurrentName+" environment").AddComponent<LevelEnvironment>();
        active.level=LevelRun.Current;active.sideParallax=parallax;active.Build();
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
            mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=item.triangles;mesh.RecalculateBounds();kit.Add(item.name,mesh);
        }
    }

    void Build()
    {
        LoadKit();random=new System.Random(810+(int)level);
        material=new Material(Resources.Load<Shader>("LevelScenery"));
        var go=GameObject.FindGameObjectWithTag("Player");if(go!=null)player=go.transform;
        if(level==LevelId.Thunder)BuildStorm();
        else
        {
            for(int i=0;i<4;i++)
            {
                // All four authored sections form one district. Their boundaries match after a full wrap.
                var section=new GameObject(level+" district "+i).transform;section.SetParent(transform,false);
                section.localPosition=new Vector3(0,(i-1)*SectionLength,90);section.localScale=new Vector3(.8f,1,1);
                Add(kit[level+"Ground"+i],section,Vector3.zero,-50);
                Add(kit[level+"Detail"+i],section,Vector3.zero,-40);sections.Add(section);
                var near=Add(kit[level+"Near"+i],transform,new Vector3(0,(i-1)*SectionLength,76),-25);
                near.localScale=new Vector3(.8f,1,1);foreground.Add(near);
            }
            Color tint=level==LevelId.Snow ? new Color(.74f,.85f,.89f,.22f) : level==LevelId.City ? new Color(.36f,.46f,.52f,.09f) : new Color(.26f,.40f,.43f,.12f);
            AddAtmosphere("Low drifting haze",0,44,tint,1.9f,level==LevelId.Snow ? .55f : .20f);
            if(level==LevelId.Snow)
            {
                AddAtmosphere("Snowdust over the drifts",3,42,new Color(.83f,.90f,.92f,.38f),1.5f,1.1f);
                AddAtmosphere("Near windblown powder",3,35,new Color(.89f,.94f,.95f,.21f),2.5f,1.8f);
            }
            if(level==LevelId.City)BuildTraffic();
        }
        if(level==LevelId.Snow || level==LevelId.Thunder)BuildWeather();
        ApplyParallax();
    }

    Transform Add(Mesh mesh,Transform parent,Vector3 position,int order)
    {
        var go=new GameObject(mesh.name);go.transform.SetParent(parent,false);go.transform.localPosition=position;
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerName="Background";renderer.sortingOrder=order;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return go.transform;
    }

    Mesh Quad(string name,float width,float height)
    {
        var mesh=new Mesh{name=name};mesh.vertices=new[]{new Vector3(-width/2,-height/2),new Vector3(width/2,-height/2),new Vector3(width/2,height/2),new Vector3(-width/2,height/2)};
        mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateBounds();ownedMeshes.Add(mesh);return mesh;
    }

    void AddAtmosphere(string name,int kind,float z,Color tint,float parallax,float speed)
    {
        var plane=Add(Quad(name,26,38),transform,new Vector3(0,0,z),kind==1 ? -60 : -9);
        var mat=new Material(Resources.Load<Shader>("LevelAtmosphere"));mat.SetFloat("_Kind",kind);mat.SetColor("_Tint",tint);
        plane.GetComponent<MeshRenderer>().sharedMaterial=mat;
        atmosphere.Add(new Atmosphere{transform=plane,material=mat,parallax=parallax,speed=speed});
    }

    void BuildTraffic()
    {
        for(int i=0;i<18;i++)
        {
            float lane=(i%3-1)*6.8f+(i%2==0 ? -.22f : .22f);
            var car=Add(kit[i%3==0 ? "CityTaxi" : "CitySedan"],transform,new Vector3(lane,Range(-20,40),88),-34);
            bool oncoming=i%2==0;car.localRotation=Quaternion.Euler(0,0,oncoming ? 180 : 0);car.localScale=new Vector3(.8f,1,1);
            traffic.Add(new Traffic{transform=car,x=lane,y=car.localPosition.y,speed=oncoming ? 3.1f : 6.2f});
        }
    }

    void BuildStorm()
    {
        AddAtmosphere("Deep rolling thunderheads",1,102,new Color(.19f,.24f,.29f,1),.65f,.34f);
        AddAtmosphere("Low torn storm clouds",2,63,new Color(.14f,.19f,.24f,.88f),1.65f,.67f);
        AddAtmosphere("Windblown rain mist",0,44,new Color(.36f,.47f,.58f,.14f),2.1f,1.0f);
        lightningRoot=new GameObject("Lightning in the cloud banks").transform;lightningRoot.SetParent(transform,false);
        for(int i=0;i<bolts.Length;i++)
        {
            var go=new GameObject(i==0 ? "Lightning main stroke" : "Lightning fork "+i);go.transform.SetParent(lightningRoot,false);
            var bolt=go.AddComponent<LineRenderer>();bolt.useWorldSpace=false;bolt.sharedMaterial=material;
            bolt.startColor=bolt.endColor=new Color(.73f,.85f,1);bolt.startWidth=i==0 ? .065f : .035f;bolt.endWidth=.008f;
            bolt.sortingLayerName="Background";bolt.sortingOrder=-11;bolt.enabled=false;bolts[i]=bolt;
        }
        thunder=gameObject.AddComponent<AudioSource>();thunder.volume=.24f;thunder.clip=CreateNoise(false);stormAt=1.7f;
        var rain=new GameObject("Continuous rain");rain.transform.SetParent(transform,false);
        var sound=rain.AddComponent<AudioSource>();sound.clip=CreateNoise(true);sound.loop=true;sound.volume=.035f;sound.Play();
    }

    void BuildWeather()
    {
        bool snow=level==LevelId.Snow;
        flakes=new Flake[snow ? 1600 : 1150];weatherVertices=new Vector3[flakes.Length*4];
        var colors=new Color[weatherVertices.Length];var triangles=new int[flakes.Length*6];
        for(int i=0;i<flakes.Length;i++)
        {
            float depth=Range(.45f,1.5f);
            flakes[i]=new Flake{position=new Vector2(Range(-12,12),Range(-16,18)),speed=Range(.65f,1.4f),size=Range(.012f,.047f)*depth,phase=Range(0,6.28f),depth=depth};
            int k=i*4,t=i*6;triangles[t]=k;triangles[t+1]=k+2;triangles[t+2]=k+1;triangles[t+3]=k;triangles[t+4]=k+3;triangles[t+5]=k+2;
            var color=snow ? new Color(.85f,.93f,.96f,Range(.38f,.85f)) : new Color(.52f,.66f,.81f,Range(.13f,.40f));
            for(int j=0;j<4;j++)colors[k+j]=color;
        }
        weather=new Mesh{name=level+" precipitation"};weather.MarkDynamic();weather.vertices=weatherVertices;weather.colors=colors;weather.triangles=triangles;
        weather.bounds=new Bounds(Vector3.zero,new Vector3(32,42,2));ownedMeshes.Add(weather);
        weatherTransform=Add(weather,transform,new Vector3(0,0,32),-5);UpdateWeather(0);
    }

    void Update()
    {
        float dt=Time.deltaTime;clock+=dt;
        travel=Mathf.Repeat(travel+dt*(level==LevelId.Tunnel ? 5.4f : 4.4f),LoopLength);
        for(int i=0;i<sections.Count;i++)
        {
            float y=SectionY(i,travel);
            sections[i].localPosition=new Vector3(HorizontalOffset,y,90);
            foreground[i].localPosition=new Vector3(HorizontalOffset*1.65f,y,76);
        }
        foreach(var car in traffic)
        {
            car.y=Mathf.Repeat(car.y-dt*car.speed+22,64)-22;
            car.transform.localPosition=new Vector3(car.x+HorizontalOffset,car.y,88);
        }
        if(flakes!=null)UpdateWeather(dt);
        if(level==LevelId.Thunder)UpdateStorm(dt);
        foreach(var layer in atmosphere)
        {
            layer.material.SetVector("_Drift",new Vector4(-clock*layer.speed*.11f,clock*layer.speed*.18f,0,0));
            layer.material.SetFloat("_Flash",flash>.65f ? flash*.62f : flash>.4f ? .02f : flash*.7f);
        }
    }

    public static float SectionY(int index,float distance)=>Mathf.Repeat((index-1)*SectionLength-distance+30,LoopLength)-30;

    void LateUpdate(){ApplyParallax();}
    void ApplyParallax()
    {
        if(player==null){var go=GameObject.FindGameObjectWithTag("Player");if(go!=null)player=go.transform;}
        HorizontalOffset=player!=null ? -player.position.x*sideParallax : 0;
        foreach(var section in sections){var p=section.localPosition;p.x=HorizontalOffset;section.localPosition=p;}
        foreach(var near in foreground){var p=near.localPosition;p.x=HorizontalOffset*1.65f;near.localPosition=p;}
        foreach(var layer in atmosphere)layer.transform.localPosition=new Vector3(HorizontalOffset*layer.parallax,0,layer.transform.localPosition.z);
        foreach(var car in traffic){var p=car.transform.localPosition;p.x=car.x+HorizontalOffset;car.transform.localPosition=p;}
        if(weatherTransform!=null)weatherTransform.localPosition=new Vector3(HorizontalOffset*2.2f,0,32);
        if(lightningRoot!=null)lightningRoot.localPosition=new Vector3(HorizontalOffset*1.65f,0,0);
    }

    void UpdateStorm(float dt)
    {
        if(clock>=stormAt)
        {
            stormAt=clock+Range(3.4f,6.7f);flash=1;thunder.pitch=Range(.8f,1.1f);thunder.PlayDelayed(.2f);
            float x=Range(-3.8f,3.8f);var main=new Vector3[10];
            for(int i=0;i<main.Length;i++){x+=Range(-.6f,.6f);main[i]=new Vector3(x,13-i*2.7f,47);}
            bolts[0].positionCount=main.Length;bolts[0].SetPositions(main);
            for(int b=1;b<bolts.Length;b++)
            {
                var start=main[b*2+1];var points=new Vector3[5];
                for(int i=0;i<points.Length;i++)points[i]=start+new Vector3((b==1 ? -1 : 1)*i*.52f+Range(-.2f,.2f),-i*1.1f,0);
                points[0]=start;bolts[b].positionCount=points.Length;bolts[b].SetPositions(points);
            }
        }
        flash=Mathf.MoveTowards(flash,0,dt*2.8f);
        bool visible=flash>.68f || (flash<.39f && flash>.27f);
        foreach(var bolt in bolts)bolt.enabled=visible;
        material.SetFloat("_Flash",flash>.65f ? flash*.45f : flash>.4f ? .02f : flash*.5f);
    }

    void UpdateWeather(float dt)
    {
        bool snow=level==LevelId.Snow;
        float gust=Mathf.Sin(clock*.53f)*.5f+Mathf.Sin(clock*.21f+1)*.5f;
        float wind=snow ? -2.6f+gust*2.6f : -4.0f+gust*1.8f;
        for(int i=0;i<flakes.Length;i++)
        {
            var f=flakes[i];f.position.y-=dt*f.speed*(snow ? 5.5f : 24);
            f.position.x+=dt*(wind+(snow ? Mathf.Sin(clock*1.5f+f.phase)*f.depth : 0))*f.depth;
            f.position.y=Mathf.Repeat(f.position.y+16,34)-16;f.position.x=Mathf.Repeat(f.position.x+12,24)-12;flakes[i]=f;
            float w=snow ? f.size : .009f*f.depth;float h=snow ? f.size*.72f : .4f*f.speed*f.depth;
            float slant=snow ? Mathf.Sin(clock+f.phase)*w*.6f : -wind/24*h;
            int k=i*4;var p=(Vector3)f.position;
            weatherVertices[k]=p+new Vector3(-w,-h);weatherVertices[k+1]=p+new Vector3(w,-h);
            weatherVertices[k+2]=p+new Vector3(w+slant,h);weatherVertices[k+3]=p+new Vector3(-w+slant,h);
        }
        weather.vertices=weatherVertices;
    }

    AudioClip CreateNoise(bool rain)
    {
        const int rate=22050;var samples=new float[rate*4];float low=0;
        for(int i=0;i<samples.Length;i++)
        {
            float t=i/(float)rate;float noise=Range(-1,1);low=Mathf.Lerp(low,noise,rain ? .12f : .035f);
            float seam=Mathf.Min(1,Mathf.Min(t,4-t)*20);
            samples[i]=rain ? (noise*.14f+low*.35f)*seam : (low*.85f+Mathf.Sin(t*62)*.05f)*Mathf.Exp(-t*1.2f)*Mathf.Clamp01(t*35);
        }
        var clip=AudioClip.Create(rain ? "Rain wash" : "Storm rumble",samples.Length,1,rate,false);
        clip.SetData(samples,0);ownedAudio.Add(clip);return clip;
    }
    float Range(float lo,float hi)=>Mathf.Lerp(lo,hi,(float)random.NextDouble());
    void OnDestroy()
    {
        if(material!=null)Destroy(material);
        foreach(var layer in atmosphere)if(layer.material!=null)Destroy(layer.material);
        foreach(var clip in ownedAudio)if(clip!=null)Destroy(clip);
        foreach(var mesh in ownedMeshes)if(mesh!=null)Destroy(mesh);
        if(active==this)active=null;
    }
}
