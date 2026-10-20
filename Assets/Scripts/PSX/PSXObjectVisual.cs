using System;
using UnityEngine;

/// <summary>Visual-only rotation; the existing 2D body and collision never rotate.</summary>
public sealed class PSXObjectVisual : MonoBehaviour
{
    public Mesh[] itemMeshes;
    public Transform model;
    public MeshRenderer modelRenderer;
    /// <summary>All renderers of a multi-part (animated) model; empty for single meshes.</summary>
    public MeshRenderer[] modelRenderers;
    public SpriteRenderer sourceSprite;
    public bool spin;
    public bool chooseItem;
    public Color accent=Color.white;
    MaterialPropertyBlock properties;
    Vector3 home;
    float phase;
    bool hidden;
    ParticleSystem motes;
    AItem item;
    APickable pickup;
    float nextMote;
    bool companion;
    bool dismissing;
    float dismissTime;
    Vector3 homeScale;
    public bool IsDismissing=>dismissing;

    void Start()
    {
        home=model.localPosition;
        homeScale=model.localScale;
        phase=(transform.position.x+transform.position.y)*.73f;
        if(chooseItem && itemMeshes!=null && itemMeshes.Length>0)
        {
            var item=GetComponent<AItem>();
            string kind=item!=null ? item.GetType().Name : "money";
            int index=Array.IndexOf(IndustrialMeshFactory.ItemKinds,kind);
            model.GetComponent<MeshFilter>().sharedMesh=itemMeshes[Mathf.Max(0,index)];
        }
        if(sourceSprite!=null)sourceSprite.enabled=false;
        // Desynchronise the looping part animation so a wave does not move in lockstep.
        var animation=model.GetComponentInChildren<Animation>();
        if(animation!=null && animation.clip!=null)
        {
            animation.Play();
            var state=animation[animation.clip.name];
            state.wrapMode=WrapMode.Loop;state.time=UnityEngine.Random.Range(0f,state.length);
        }
        item=GetComponent<AItem>();pickup=GetComponent<APickable>();
        companion=GetComponent<ACompanion>()!=null;
        if(spin && (item!=null || pickup!=null))
        {
            motes=PSXImpactEffects.Create(transform,"Rarity aura",64,true);
            var fade=motes.colorOverLifetime;var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.65f,.2f),new GradientAlphaKey(0,1)});fade.color=gradient;
            // Keep aura particles anchored to the item while it is attracted.
            var main=motes.main;main.simulationSpace=ParticleSystemSimulationSpace.Local;
        }
    }

    public void Hide(){hidden=true;if(model!=null)model.gameObject.SetActive(false);if(motes!=null)motes.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
    public void Dismiss(){dismissing=true;dismissTime=0;}

    void LateUpdate()
    {
        if(model==null || hidden)return;
        float t=Time.time+phase;
        float fadeOut=1;
        if(dismissing)
        {
            dismissTime+=Time.deltaTime;float progress=Mathf.Clamp01(dismissTime/.48f);
            float ease=1-Mathf.Pow(1-progress,3);fadeOut=1-progress;
            model.localScale=homeScale*Mathf.Lerp(1,.15f,ease);
            if(progress>=1){Destroy(gameObject);return;}
        }
        if(motes!=null && !dismissing && Time.time>=nextMote)
        {
            nextMote=Time.time+.06f;
            float radius=Mathf.Max(.28f,modelRenderer.bounds.extents.magnitude*.65f);
            Color color=item!=null ? AItem.GetRarityColor(item.rarity) : PSXImpactEffects.PickupColor(pickup);
            float rootScale=Mathf.Max(.01f,Mathf.Abs(transform.lossyScale.x));
            var e=new ParticleSystem.EmitParams();
            e.position=new Vector3(0,0,.20f)/rootScale;e.startColor=new Color(color.r,color.g,color.b,.50f);
            e.startSize=radius*3.6f;e.startLifetime=.28f;e.velocity=Vector3.zero;motes.Emit(e,1);
            for(int i=0;i<3;i++)
            {
                float angle=t*1.2f+i*Mathf.PI*2/3;float orbit=radius*(.85f+.2f*Mathf.Sin(t*3+i));
                e.position=new Vector3(Mathf.Cos(angle)*orbit,Mathf.Sin(angle)*orbit*.85f,.12f)/rootScale;
                e.velocity=new Vector3(-Mathf.Sin(angle),Mathf.Cos(angle),0)*.12f/rootScale;
                e.startColor=new Color(color.r,color.g,color.b,.85f);e.startSize=radius*.5f;e.startLifetime=.5f;motes.Emit(e,1);
            }
        }
        model.localRotation=companion ? Quaternion.Euler(-18,18*Mathf.Sin(t*1.3f),6*Mathf.Sin(t)) : spin ? Quaternion.Euler(-18, t*52, 10) : Quaternion.Euler(7*Mathf.Sin(t),9*Mathf.Sin(t*.7f),0);
        model.localPosition=home+(spin ? Vector3.up*Mathf.Sin(t*2)*.055f : Vector3.zero)+(dismissing ? Vector3.up*dismissTime*.65f : Vector3.zero);
        if(properties==null)properties=new MaterialPropertyBlock();
        Color tint=accent;tint.a=(sourceSprite!=null ? sourceSprite.color.a : 1)*fadeOut;
        properties.SetColor("_Tint",tint);modelRenderer.SetPropertyBlock(properties);
        if(modelRenderers!=null)foreach(var part in modelRenderers)if(part!=null && part!=modelRenderer)part.SetPropertyBlock(properties);
    }
}
