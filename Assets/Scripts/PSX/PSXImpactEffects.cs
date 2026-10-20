using UnityEngine;

/// <summary>One shared, bounded particle buffer for hits and pickup bursts, plus PSXv2 pixel flipbooks.</summary>
public static class PSXImpactEffects
{
    static ParticleSystem buffer;
    static ParticleSystem sparks,explosions,bigExplosions;
    static Material material;
    static Material auraMaterial;
    public static int BurstCount { get; private set; }
    public static int ExplosionCount { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){buffer=null;sparks=null;explosions=null;bigExplosions=null;material=null;auraMaterial=null;BurstCount=0;ExplosionCount=0;}
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

    /// <summary>Pool for one horizontal pixel flipbook (Resources/Effects/{materialName}); each particle plays it once.</summary>
    static ParticleSystem Flipbook(string materialName,int frames,int budget,int order)
    {
        var flipMaterial=Resources.Load<Material>("Effects/"+materialName);
        if(flipMaterial==null)return null;
        var go=new GameObject("PSXv2 "+materialName);
        var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.playOnAwake=false;main.loop=true;main.maxParticles=budget;
        main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSpeed=0;main.gravityModifier=0;
        var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
        var sheet=ps.textureSheetAnimation;sheet.enabled=true;sheet.numTilesX=frames;sheet.numTilesY=1;
        // Hold each frame for an equal slice of the particle's life (stepped, no blending).
        sheet.frameOverTime=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,1));
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=flipMaterial;
        renderer.sortingLayerName="Bullet";renderer.sortingOrder=order;
        ps.Play();return ps;
    }

    static void EmitFlipbook(ParticleSystem system,Vector3 position,float size,float lifetime,Color color)
    {
        if(system==null)return;
        var e=new ParticleSystem.EmitParams();e.position=position+Vector3.back*.3f;e.velocity=Vector3.zero;
        e.startSize=size;e.startLifetime=lifetime;e.startColor=color;
        // Quarter turns keep the pixel grid square while varying repeated effects.
        e.rotation=Random.Range(0,4)*90;
        system.Emit(e,1);
    }

    public static void Burst(Vector3 position,Color color,bool pickup=false,Vector2 incoming=default)
    {
        if(buffer==null)buffer=Create(null,"PSX shared impacts",512);
        BurstCount++;
        int count=pickup ? 22 : 5;
        if(!pickup)
        {
            if(sparks==null)sparks=Flipbook("PSXv2Spark",4,96,31);
            EmitFlipbook(sparks,position,.5f,.2f,Color.Lerp(color,Color.white,.5f));
        }
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

    /// <summary>Dithered pixel fireball sized to the destroyed object; large hulls get several staggered blasts.</summary>
    public static void Explosion(Vector3 position,float size)
    {
        ExplosionCount++;
        if(explosions==null)explosions=Flipbook("PSXv2Explosion",8,64,32);
        if(bigExplosions==null)bigExplosions=Flipbook("PSXv2ExplosionBig",8,16,33);
        size=Mathf.Max(.6f,size);
        if(size<2.5f){EmitFlipbook(explosions,position,size*1.6f,.5f,Color.white);return;}
        EmitFlipbook(bigExplosions,position,size*.9f,.8f,Color.white);
        int extra=Mathf.Clamp(Mathf.RoundToInt(size),3,10);
        for(int i=0;i<extra;i++)
        {
            var offset=(Vector3)(Random.insideUnitCircle*size*.45f);
            EmitFlipbook(explosions,position+offset,Random.Range(.9f,1.6f),Random.Range(.4f,.65f),Color.white);
        }
    }

    public static void Explosion(GameObject source)
    {
        var collider=source.GetComponent<Collider2D>();
        float size=collider!=null ? Mathf.Max(collider.bounds.size.x,collider.bounds.size.y) : 1;
        Explosion(collider!=null ? collider.bounds.center : source.transform.position,size);
    }

    public static Color PickupColor(Component pickup)
    {
        string kind=pickup.GetType().Name.ToLowerInvariant();
        return kind.Contains("life") ? new Color(.85f,.25f,.20f) : kind.Contains("mana") ? new Color(.20f,.55f,.95f) : new Color(.85f,.68f,.32f);
    }
}
