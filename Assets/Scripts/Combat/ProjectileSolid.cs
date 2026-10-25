using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// One shared mesh per kind, one instanced material, no lights/shadows or per-frame geometry.
// Keep the sprite component as the existing gameplay tint/visibility channel, but never draw it.
[DisallowMultipleComponent]
public sealed class ProjectileSolid : MonoBehaviour
{
    static Material material;
    static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
    static readonly int Tint=Shader.PropertyToID("_Tint"),Stretch=Shader.PropertyToID("_Stretch");
    SpriteRenderer source;
    MeshRenderer model;
    MeshFilter filter;
    MaterialPropertyBlock properties;
    bool circular;
    bool friendly;
    float lastBurn=-1;
    Color lastTint;
    Vector3 lastScale;
    public Mesh Mesh=>filter.sharedMesh;
    public MeshRenderer Renderer=>model;
    public static Material SharedMaterial
    {
        get
        {
            if(material==null)material=Resources.Load<Material>("SolidProjectile");
            return material;
        }
    }
    public static void Hostile(CombatRound round)
    {
        if(round is PulseLaser)return;
        string key=round.GetType().Name;
        if(!meshes.TryGetValue(key,out var mesh))meshes[key]=mesh=ProjectileSolidGeometry.Hostile(round.GetType());
        Attach(round.gameObject,mesh,true,false);
    }
    public static void Friendly(PlayerRound round)
    {
        string key="Friendly "+round.delivery;
        if(!meshes.TryGetValue(key,out var mesh))
        {
            var c=round.GetComponent<CapsuleCollider2D>();
            meshes[key]=mesh=ProjectileSolidGeometry.Friendly(round.delivery,c.size);
        }
        Attach(round.gameObject,mesh,false,true);
    }
    public static void Legacy(GameObject go,bool friendly)
    {
        if(go.GetComponent<ProjectileSolid>()!=null)return;
        var sprite=go.GetComponent<SpriteRenderer>();if(sprite==null)return;
        // The expanding wave is a ring hazard: model the actual collider paths, retaining its gaps.
        string key="Legacy "+go.name.Replace("(Clone)","");
        if(!meshes.TryGetValue(key,out var mesh))meshes[key]=mesh=ProjectileSolidGeometry.Legacy(go,friendly);
        Attach(go,mesh,false,friendly);
        go.GetComponent<ProjectileSolid>().model.gameObject.AddComponent<ProjectileSolidVisibility>();
    }
    static void Attach(GameObject go,Mesh mesh,bool circle,bool friendly)
    {
        var solid=go.AddComponent<ProjectileSolid>();solid.source=go.GetComponent<SpriteRenderer>();
        var child=new GameObject("Solid ordnance");child.layer=go.layer;child.transform.SetParent(go.transform,false);
        solid.filter=child.AddComponent<MeshFilter>();solid.filter.sharedMesh=mesh;
        solid.model=child.AddComponent<MeshRenderer>();solid.model.sharedMaterial=SharedMaterial;
        solid.model.shadowCastingMode=ShadowCastingMode.Off;solid.model.receiveShadows=false;
        solid.model.lightProbeUsage=LightProbeUsage.Off;solid.model.reflectionProbeUsage=ReflectionProbeUsage.Off;
        solid.model.sortingLayerName="Bullet";solid.model.sortingOrder=friendly ? 16 : 18;
        solid.circular=circle;solid.friendly=friendly;solid.properties=new MaterialPropertyBlock();solid.lastTint=Color.clear;
        solid.source.forceRenderingOff=true;solid.Sync();
    }
    void LateUpdate()=>Sync();
    void Sync()
    {
        if(source==null || model==null)return;
        if(model.enabled!=source.enabled)model.enabled=source.enabled;
        var scale=transform.lossyScale;
        float burn=friendly && CombatUpgrades.Current!=null && CombatUpgrades.Current.Count(UpgradeKind.Gasoline)>0 ? 1 : 0;
        if(source.color==lastTint && scale==lastScale && burn==lastBurn)return;
        lastBurn=burn;properties.SetFloat("_Burn",burn);
        lastTint=source.color;lastScale=scale;
        var stretch=Vector3.one;
        // Unity's CircleCollider2D remains circular under nonuniform scale (guided missiles).
        if(circular)
        {
            float max=Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.y));
            stretch.x=max/Mathf.Max(.0001f,Mathf.Abs(scale.x));stretch.y=max/Mathf.Max(.0001f,Mathf.Abs(scale.y));
        }
        properties.SetColor(Tint,lastTint);properties.SetVector(Stretch,new Vector4(stretch.x,stretch.y,1,0));model.SetPropertyBlock(properties);
        var bounds=Mesh.bounds;bounds.center=Vector3.Scale(bounds.center,stretch);bounds.size=Vector3.Scale(bounds.size,stretch);model.localBounds=bounds;
    }
}
