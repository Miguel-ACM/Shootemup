using UnityEngine;

/// <summary>One shared, bounded particle buffer for hits and pickup bursts; sphere explosions on death.</summary>
public static class PSXImpactEffects
{
    static ParticleSystem buffer;
    static Material material;
    static Material auraMaterial;
    public static int BurstCount { get; private set; }
    public static int ExplosionCount { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){buffer=null;material=null;auraMaterial=null;BurstCount=0;ExplosionCount=0;}
    public static ParticleSystem Create(Transform parent,string name,int budget,bool aura=false)
    {
        var go=new GameObject(name);if(parent!=null)go.transform.SetParent(parent,false);
        var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.playOnAwake=false;main.loop=true;main.maxParticles=budget;
        main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=.35f;
        main.startSize3D=true;main.startSize=.055f;main.startSpeed=0;main.gravityModifier=0;
        var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
        var fade=ps.colorOverLifetime;fade.enabled=true;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.9f,0),new GradientAlphaKey(0,1)});fade.color=gradient;
        var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
        var renderer=ps.GetComponent<ParticleSystemRenderer>();
        if(material==null)material=Resources.Load<Material>("Effects/PSXParticles");
        if(auraMaterial==null)auraMaterial=Resources.Load<Material>("Effects/PSXAura");
        renderer.sharedMaterial=aura ? auraMaterial : material;renderer.sortingLayerName=aura ? "Objects" : "Bullet";renderer.sortingOrder=aura ? -1 : 30;
        ps.Play();return ps;
    }
    public static void Burst(Vector3 position,Color color,bool pickup=false,Vector2 incoming=default)
    {
        if(buffer==null)buffer=Create(null,"PSX shared impacts",512);
        BurstCount++;
        int count=pickup ? 22 : 9;
        for(int i=0;i<count;i++)
        {
            Vector2 direction=incoming.sqrMagnitude>.001f ? incoming.normalized : Vector2.up;
            float angle=pickup ? i*Mathf.PI*2/count+Random.Range(-.18f,.18f) : Mathf.Atan2(direction.y,direction.x)+Random.Range(-.48f,.48f);
            var e=new ParticleSystem.EmitParams();e.position=position;
            e.velocity=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*Random.Range(pickup ? 1f : 1.6f,pickup ? 3.5f : 4.8f);
            e.startColor=i%4==0 ? Color.Lerp(color,Color.white,.65f) : color;
            float size=Random.Range(.035f,pickup ? .10f : .055f);
            e.startSize3D=pickup ? Vector3.one*size : new Vector3(size,size*2.8f,size);
            e.rotation3D=new Vector3(0,0,90-angle*Mathf.Rad2Deg);
            e.startLifetime=Random.Range(.16f,pickup ? .55f : .34f);
            buffer.Emit(e,1);
        }
    }

    /// <summary>Growing fire spheres scaled to the destroyed object: sphere size, sphere count, spread
    /// and chain length all grow with the hull, so a gunboat or the boss goes up in a long, wide blast.</summary>
    public static void Explosion(Vector3 position,float size)
    {
        ExplosionCount++;
        size=Mathf.Max(.6f,size);
        float sphere=.6f+size*.75f;
        int count=Mathf.Clamp(3+Mathf.RoundToInt(size*2),4,18);
        PSXFireballs.Spawn(position,sphere,count,size*.45f,Mathf.Clamp(size*.15f,.1f,1.1f));
        int sparks=Mathf.Clamp(Mathf.RoundToInt(size),1,6);
        for(int i=0;i<sparks;i++)Burst(position+(Vector3)(Random.insideUnitCircle*size*.4f),new Color(1,.68f,.30f));
    }

    public static void Explosion(GameObject source)
    {
        var collider=source.GetComponent<Collider2D>();
        float size=collider!=null ? Mathf.Max(collider.bounds.size.x,collider.bounds.size.y) : 1;
        Explosion(collider!=null ? collider.bounds.center : source.transform.position,size);
    }

    /// <summary>Small forward cone of hot sparks from a gun muzzle (shares the impact buffer).</summary>
    public static void Muzzle(Vector3 position,Vector2 direction)
    {
        if(buffer==null)buffer=Create(null,"PSX shared impacts",512);
        float baseAngle=Mathf.Atan2(direction.y,direction.x);
        for(int i=0;i<4;i++)
        {
            float angle=baseAngle+Random.Range(-.35f,.35f);
            var e=new ParticleSystem.EmitParams();e.position=position+Vector3.back*.3f;
            e.velocity=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*Random.Range(2.5f,5f);
            e.startColor=i==0 ? new Color(1,.97f,.85f) : new Color(1,.8f,.45f);
            float size=Random.Range(.04f,.07f);
            e.startSize3D=new Vector3(size,size*2.6f,size);
            e.rotation3D=new Vector3(0,0,90-angle*Mathf.Rad2Deg);
            e.startLifetime=Random.Range(.08f,.16f);
            buffer.Emit(e,1);
        }
    }

    public static Color PickupColor(Component pickup)
    {
        string kind=pickup.GetType().Name.ToLowerInvariant();
        return kind.Contains("life") ? new Color(.85f,.25f,.20f) : kind.Contains("mana") ? new Color(.20f,.55f,.95f) : new Color(.85f,.68f,.32f);
    }
}
