using UnityEngine;
using System.Collections;
[UnityEngine.Scripting.Preserve]
public sealed class BeamEmitter : AWeaponAutomatic
{
    bool busy;
    LineRenderer[] beams;
    void Awake(){shootSpeedModifier=.12f;}
    public override void Shoot(){if(!KeepFiring() || busy)return;StartCoroutine(Fire());}
    public override void StopFiring()
    {
        base.StopFiring();busy=false;
        if(beams!=null)foreach(var b in beams)if(b!=null)Destroy(b.gameObject);beams=null;
    }
    IEnumerator Fire()
    {
        busy=true;
        if(beams!=null)foreach(var b in beams)if(b!=null)Destroy(b.gameObject);
        int count=Mathf.Min(6,GameRules.playerShootNumber);beams=new LineRenderer[count];
        for(int i=0;i<count;i++)beams[i]=CombatVisuals.Line(transform,new Color(.2f,1,.8f,.4f),.04f);
        float charge=CombatUpgrades.Current!=null ? CombatUpgrades.Current.Volley() : 1;
        float warning=.25f/Mathf.Max(1,GameRules.playerShootSpeed/10),end=warning+.65f,t=0,nextTick=warning;
        while(t<end && !GameRules.playerDisabled)
        {
            t+=Time.deltaTime;bool active=t>=warning;
            for(int i=0;i<count;i++)
            {
                Vector3 p=transform.position+spawnPoints[i];
                beams[i].SetPosition(0,p);beams[i].SetPosition(1,p+Vector3.up*20);
                beams[i].startWidth=beams[i].endWidth=active ? .15f*Mathf.Sqrt(GameRules.playerShootSize) : .035f;
                beams[i].startColor=beams[i].endColor=active ? new Color(.4f,1,.85f) : new Color(.4f,1,.85f,.35f);
            }
            if(active && t>=nextTick)
            {
                nextTick=t+.12f;
                foreach(var enemy in new System.Collections.Generic.List<AEnemy>(AEnemy.Live))
                {
                    if(enemy==null || enemy.health<=0)continue;
                    bool hit=false;
                    for(int i=0;i<count;i++)
                    {
                        var delta=enemy.transform.position-(transform.position+spawnPoints[i]);
                        if(delta.y>=0 && delta.y<20 && Mathf.Abs(delta.x)<.22f+enemy.size*.6f){hit=true;break;}
                    }
                    if(!hit)continue;
                    // Multiple emitters cover more lanes; a target is charged once per tick.
                    float amount=GameRules.playerShootDamage*1.2f*charge;
                    enemy.damage(CombatUpgrades.Current!=null ? CombatUpgrades.Current.DirectHit(enemy,amount) : amount);
                    enemy.HitAt(enemy.transform.position,Vector2.up);
                }
            }
            yield return null;
        }
        foreach(var b in beams)if(b!=null)Destroy(b.gameObject);beams=null;busy=false;
    }
}
