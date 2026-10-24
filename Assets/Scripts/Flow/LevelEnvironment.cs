using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Composed scrolling districts with the same ship-driven lateral parallax as Fields.</summary>
public sealed class LevelEnvironment : MonoBehaviour
{
    [Serializable] sealed class Kit { public MeshData[] meshes; }
    [Serializable] sealed class MeshData { public string name;public float[] vertices,colors;public int[] triangles; }
    sealed class Atmosphere { public Transform transform;public Material material;public float parallax,speed; }
    sealed class Traffic { public Transform transform;public float x,y,speed,direction;public bool horizontal; }
    struct Flake { public Vector2 position;public float speed,size,phase,depth; }
    const float SectionLength=20,LoopLength=80;
    static Dictionary<string,Mesh> kit;
    static bool preparing,sourceReady;
    public static bool SourceReady => sourceReady || kit!=null;
    static LevelEnvironment active;
    static readonly Dictionary<LevelId,LevelEnvironment> preparedRoutes=new Dictionary<LevelId,LevelEnvironment>();
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
    AudioSource rainAudio;
    Transform stormAudioRoot;
    bool stormAudioReleased;
    bool titleBackdrop;
    const float StormFadeSeconds=3.5f;
    readonly LineRenderer[] bolts=new LineRenderer[3];
    System.Random random;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRuntime() { kit=null;active=null;preparing=sourceReady=false;preparedRoutes.Clear(); }

    public static IEnumerator CreateTitleBackdrop()
    {
        yield return Prepare();
        var root=new GameObject("Title tunnel backdrop");
        var environment=root.AddComponent<LevelEnvironment>();
        environment.level=LevelId.Tunnel;environment.titleBackdrop=true;
        var build=environment.BuildSteps(true);
        while(build.MoveNext())yield return build.Current;
    }

    /// <summary>Build an offered route while the player is still flying in the current level.</summary>
    public static IEnumerator PrepareRoute(LevelId next)
    {
        if(next==LevelId.Fields)yield break; // The valley is already present in the gameplay scene.
        if(preparedRoutes.TryGetValue(next,out var ready) && ready!=null)yield break;
        yield return Prepare();
        var root=new GameObject(next+" prepared environment");
        root.SetActive(false);
        var environment=root.AddComponent<LevelEnvironment>();
        environment.level=next;
        bool complete=false;
        try
        {
            var steps=environment.BuildSteps(true);
            while(steps.MoveNext())yield return steps.Current;
            // Only fully built routes may be activated. Cancellation discards partial builds.
            preparedRoutes[next]=environment;
            complete=true;
        }
        finally { if(!complete && root!=null)Destroy(root); }
    }

    public static void Apply()
    {
        FadeOutStorm();
        if(active!=null){active.gameObject.SetActive(false);Destroy(active.gameObject);}
        float parallax=.12f;
        foreach(var field in FindObjectsByType<PSXGrassField>(FindObjectsInactive.Include))
        {
            parallax=field.sideParallax;
            field.gameObject.SetActive(LevelRun.Current==LevelId.Fields);
        }
        // Retain the existing camera/compositor setup without its old disconnected scenery.
        foreach(var industrial in FindObjectsByType<PSXIndustrialField>())industrial.steelMaterial=null;
        if(LevelRun.Current!=LevelId.Fields)
        {
            if(preparedRoutes.TryGetValue(LevelRun.Current,out var ready) && ready!=null)
                active=ready;
            else
            {
                active=new GameObject(LevelRun.CurrentName+" environment").AddComponent<LevelEnvironment>();
                active.level=LevelRun.Current;active.Build();
            }
            active.sideParallax=parallax;
            active.gameObject.name=LevelRun.CurrentName+" environment";
            active.gameObject.SetActive(true);
            active.ApplyParallax();
            foreach(var sound in active.GetComponentsInChildren<AudioSource>())
                if(sound.loop && !sound.isPlaying)sound.Play();
        }
        foreach(var route in preparedRoutes.Values)
            if(route!=null && route!=active)Destroy(route.gameObject);
        preparedRoutes.Clear();
        WavePreparation.Begin();
    }

    public static void FadeOutStorm()
    {
        if(active==null || active.stormAudioRoot==null || active.stormAudioReleased)return;
        active.stormAudioReleased=true;
        // Let the existing rain and thunder trail off after their scenery has gone away.
        var audioRoot=active.stormAudioRoot.gameObject;
        audioRoot.transform.SetParent(null,true);
        DontDestroyOnLoad(audioRoot);
        CloudTransition.FadeOutAudio(audioRoot,active.ownedAudio.ToArray(),StormFadeSeconds);
        active.ownedAudio.Clear(); // The fading audio now owns these clips.
    }

    static void LoadKit()
    {
        if(kit!=null)return;
        var data=JsonUtility.FromJson<Kit>(Resources.Load<TextAsset>("LevelKit").text);
        var loaded=new Dictionary<string,Mesh>();
        foreach(var item in data.meshes)loaded.Add(item.name,CreateMesh(item));
        kit=loaded;
    }

    /// <summary>Prepare shared scenery in the background before scene activation needs the meshes.</summary>
    public static IEnumerator Prepare()
    {
        if(kit!=null)yield break;
        if(preparing)
        {
            while(preparing && kit==null)yield return null;
            yield break;
        }
        preparing=true;
        var loaded=new Dictionary<string,Mesh>();
        try
        {
            var request=Resources.LoadAsync<TextAsset>("LevelKit");
            yield return request;
            string json=((TextAsset)request.asset).text;
            // Finish the resource request before parking a scene load at allowSceneActivation=false.
            sourceReady=true;
            // Unity permits JsonUtility.FromJson(string) on a worker; native Mesh APIs stay on main.
            var parsing=Task.Run(()=>JsonUtility.FromJson<Kit>(json));
            while(!parsing.IsCompleted)yield return null;
            var budget=Stopwatch.StartNew();
            foreach(var item in parsing.GetAwaiter().GetResult().meshes)
            {
                // A direct scene entry can have populated the synchronous fallback in the meantime.
                if(kit!=null)yield break;
                loaded.Add(item.name,CreateMesh(item));
                if(budget.ElapsedMilliseconds>=3){yield return null;budget.Restart();}
            }
            kit=loaded;
        }
        finally
        {
            if(kit!=loaded)foreach(var mesh in loaded.Values)Destroy(mesh);
            preparing=false;
        }
    }

    static Mesh CreateMesh(MeshData item)
    {
        int count=item.vertices.Length/3;var vertices=new Vector3[count];var colors=new Color[count];
        for(int i=0;i<count;i++)
        {
            vertices[i]=new Vector3(item.vertices[i*3],item.vertices[i*3+1],item.vertices[i*3+2]);
            colors[i]=new Color(item.colors[i*4],item.colors[i*4+1],item.colors[i*4+2],item.colors[i*4+3]);
        }
        var mesh=new Mesh{name=item.name,indexFormat=IndexFormat.UInt32};
        mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=item.triangles;mesh.RecalculateBounds();return mesh;
    }

    void Build()
    {
        var steps=BuildSteps(false);
        while(steps.MoveNext()) { }
    }

    IEnumerator BuildSteps(bool background)
    {
        LoadKit();random=new System.Random(LevelRun.Seed+810+(int)level);
        material=new Material(Resources.Load<Shader>("LevelScenery"));
        if(level==LevelId.City)material.SetFloat("_Haze",.38f);
        var go=GameObject.FindGameObjectWithTag("Player");if(go!=null)player=go.transform;
        yield return null;
        if(level==LevelId.Thunder)
        {
            var storm=BuildStormSteps(background);
            while(storm.MoveNext())yield return storm.Current;
        }
        else
        {
            for(int i=0;i<4;i++)
            {
                // All four authored sections form one district. Their boundaries match after a full wrap.
                var section=new GameObject(level+" district "+i).transform;section.SetParent(transform,false);
                section.localPosition=new Vector3(0,(i-1)*SectionLength,90);section.localScale=new Vector3(.8f,1,1);
                Add(kit[level+"Ground"+i],section,Vector3.zero,-50);
                if(level==LevelId.City)
                {
                    var buildings=CityDistrictGeometry.Build(LevelRun.Seed,i);ownedMeshes.Add(buildings);Add(buildings,section,Vector3.zero,-40);
                }
                else Add(kit[level+"Detail"+i],section,Vector3.zero,-40);
                sections.Add(section);
                var near=Add(kit[level+"Near"+i],transform,new Vector3(0,(i-1)*SectionLength,76),-25);
                near.localScale=new Vector3(.8f,1,1);foreground.Add(near);
                yield return null;
            }
            Color tint=level==LevelId.Snow ? new Color(.74f,.85f,.89f,.22f) : level==LevelId.City ? new Color(.46f,.54f,.59f,.16f) : new Color(.10f,.16f,.19f,.10f);
            AddAtmosphere("Low drifting haze",0,44,tint,1.9f,level==LevelId.Snow ? .55f : .20f);
            if(level==LevelId.Snow)
            {
                AddAtmosphere("Snowdust over the drifts",3,42,new Color(.83f,.90f,.92f,.38f),1.5f,1.1f);
                AddAtmosphere("Near windblown powder",3,35,new Color(.89f,.94f,.95f,.21f),2.5f,1.8f);
                yield return null;
            }
            if(level==LevelId.City)
            {
                AddAtmosphere("Wisps above the distant city",0,35,new Color(.75f,.80f,.83f,.20f),2.6f,.32f);
                var cars=BuildTrafficSteps();
                while(cars.MoveNext())yield return cars.Current;
            }
        }
        if(level==LevelId.Snow || level==LevelId.Thunder)
        {
            var precipitation=BuildWeatherSteps();
            while(precipitation.MoveNext())yield return precipitation.Current;
        }
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

    IEnumerator BuildTrafficSteps()
    {
        for(int i=0;i<24;i++)
        {
            bool horizontal=i>=16;
            float direction=i%2==0 ? -1 : 1;
            // Avenue pitch 2.6 is scaled by .8; cross streets repeat every 2.5 units.
            float x=horizontal ? Range(-8,8) : (i%5-2)*2.08f+direction*.08f;
            float y=horizontal ? ((i-16)*3+2)*2.5f-direction*.07f : Range(-20,40);
            var car=Add(kit[i%3==0 ? "CityTaxi" : "CitySedan"],transform,new Vector3(x,y,88),-34);
            float angle=horizontal ? (direction>0 ? 90 : 270) : direction<0 ? 0 : 180;
            car.localRotation=Quaternion.Euler(0,0,angle);car.localScale=new Vector3(.18f,.23f,.23f);
            traffic.Add(new Traffic{transform=car,x=x,y=y,speed=Range(.45f,.75f),direction=direction,horizontal=horizontal});
            yield return null;
        }
    }

    IEnumerator BuildStormSteps(bool background)
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
            yield return null;
        }
        stormAudioRoot=new GameObject("Storm ambience").transform;stormAudioRoot.SetParent(transform,false);
        thunder=stormAudioRoot.gameObject.AddComponent<AudioSource>();
        thunder.playOnAwake=false;thunder.volume=0;stormAt=1.7f;
        if(background)
        {
            var noise=CreateNoiseAsync(false,clip=>thunder.clip=clip);
            while(noise.MoveNext())yield return noise.Current;
        }
        else thunder.clip=CreateNoise(false);
        var rain=new GameObject("Continuous rain");rain.transform.SetParent(stormAudioRoot,false);
        var sound=rain.AddComponent<AudioSource>();sound.playOnAwake=false;sound.loop=true;sound.volume=0;
        rainAudio=sound;
        if(background)
        {
            var noise=CreateNoiseAsync(true,clip=>sound.clip=clip);
            while(noise.MoveNext())yield return noise.Current;
        }
        else sound.clip=CreateNoise(true);
    }

    IEnumerator BuildWeatherSteps()
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
            if((i+1)%128==0)yield return null;
        }
        yield return null;
        weather=new Mesh{name=level+" precipitation"};weather.MarkDynamic();weather.vertices=weatherVertices;weather.colors=colors;weather.triangles=triangles;
        weather.bounds=new Bounds(Vector3.zero,new Vector3(32,42,2));ownedMeshes.Add(weather);
        weatherTransform=Add(weather,transform,new Vector3(0,0,32),-5);UpdateWeather(0);
    }

    void Update()
    {
        float dt=titleBackdrop ? Time.unscaledDeltaTime : Time.deltaTime;clock+=dt;
        travel=Mathf.Repeat(travel+dt*(level==LevelId.Tunnel ? 10.2f : level==LevelId.City ? 1.8f : 4.4f),LoopLength);
        for(int i=0;i<sections.Count;i++)
        {
            float y=SectionY(i,travel);
            sections[i].localPosition=new Vector3(HorizontalOffset,y,90);
            foreground[i].localPosition=new Vector3(HorizontalOffset*1.65f,y,76);
        }
        foreach(var car in traffic)
        {
            if(car.horizontal)car.x=Mathf.Repeat(car.x+dt*car.speed*car.direction+11,22)-11;
            else car.y=Mathf.Repeat(car.y+dt*car.speed*car.direction+30,LoopLength)-30;
            float y=Mathf.Repeat(car.y-travel+30,LoopLength)-30;
            car.transform.localPosition=new Vector3(car.x+HorizontalOffset,y,88);
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
        if(!stormAudioReleased)
        {
            float volume=Mathf.SmoothStep(0,1,Mathf.Clamp01(clock/StormFadeSeconds));
            thunder.volume=.24f*volume;
            rainAudio.volume=.035f*volume;
        }
        if(clock>=stormAt)
        {
            stormAt=clock+Range(3.4f,6.7f);flash=1;
            if(!stormAudioReleased){thunder.pitch=Range(.8f,1.1f);thunder.PlayDelayed(.2f);}
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
        return CreateNoiseClip(rain,GenerateNoise(rain,random.Next()));
    }

    IEnumerator CreateNoiseAsync(bool rain,Action<AudioClip> ready)
    {
        int seed=random.Next();
        var task=Task.Run(()=>GenerateNoise(rain,seed));
        while(!task.IsCompleted)yield return null;
        ready(CreateNoiseClip(rain,task.GetAwaiter().GetResult()));
    }

    // Pure managed work: no Unity objects or native audio calls on the worker thread.
    static float[] GenerateNoise(bool rain,int seed)
    {
        const int rate=22050;var samples=new float[rate*4];float low=0;
        var noiseRandom=new System.Random(seed);
        for(int i=0;i<samples.Length;i++)
        {
            float t=i/(float)rate;float noise=(float)noiseRandom.NextDouble()*2-1;
            low+=(noise-low)*(rain ? .12f : .035f);
            float seam=Math.Min(1,Math.Min(t,4-t)*20);
            samples[i]=rain ? (noise*.14f+low*.35f)*seam :
                (low*.85f+(float)Math.Sin(t*62)*.05f)*(float)Math.Exp(-t*1.2f)*Math.Min(1,t*35);
        }
        return samples;
    }

    AudioClip CreateNoiseClip(bool rain,float[] samples)
    {
        var clip=AudioClip.Create(rain ? "Rain wash" : "Storm rumble",samples.Length,1,22050,false);
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
