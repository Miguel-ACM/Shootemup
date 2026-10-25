using UnityEngine;
using System.Collections.Generic;

public sealed class HotSauceTrail : MonoBehaviour
{
    static int live;
    float age,next;
    AEnemy source;
    ParticleSystem flames;
    readonly HashSet<AEnemy> touched=new HashSet<AEnemy>();
    public static void Leave(AEnemy source)
    {
        if(live>=80)return;
        var go=new GameObject("Hot sauce fire trail");go.transform.position=source.transform.position;
        var patch=go.AddComponent<HotSauceTrail>();patch.source=source;
        patch.flames=PSXImpactEffects.Create(go.transform,"Small lingering flames",16,false);
        var fade=patch.flames.colorOverLifetime;fade.enabled=true;
        var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(new Color(.95f,.35f,.08f),0),new GradientColorKey(new Color(.6f,.08f,.02f),1)},new[]{new GradientAlphaKey(.8f,0),new GradientAlphaKey(0,1)});fade.color=g;
    }
    void OnEnable(){live++;}
    void OnDisable(){live--;}
    void Update()
    {
        if(Time.deltaTime<=0 || WorldTimeStop.Frozen)return;
        if(GameRules.playerDisabled){Destroy(gameObject);return;}
        age+=Time.deltaTime;if(age>1.35f){Destroy(gameObject);return;}
        if(age<next)return;next=age+.12f;
        var e=new ParticleSystem.EmitParams{position=transform.position+new Vector3(Random.Range(-.2f,.2f),0,-1.5f),velocity=Vector3.up*.6f,startLifetime=.4f,startSize3D=new Vector3(.1f,.28f,.1f),startColor=new Color(.95f,.38f,.08f)};flames.Emit(e,2);
        foreach(var target in AEnemy.Live)
        {
            if(target==null || target==source || !target.CanBeTargeted || touched.Contains(target))continue;
            var collider=target.GetComponent<Collider2D>();
            Vector2 closest=collider!=null ? collider.ClosestPoint(transform.position) : (Vector2)target.transform.position;
            if(Vector2.Distance(closest,transform.position)>.32f)continue;
            touched.Add(target);
            (target.GetComponent<BurningHull>() ?? target.gameObject.AddComponent<BurningHull>()).Ignite(6,false);
        }
    }
}
