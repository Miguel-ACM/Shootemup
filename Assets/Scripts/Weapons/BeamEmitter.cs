using UnityEngine;
using System.Collections;
[UnityEngine.Scripting.Preserve]
public sealed class BeamEmitter : AWeaponAutomatic
{
    bool busy;
    BeamRibbon[] beams;
    LaserSound sound;
    Coroutine firing;
    void Awake(){shootSpeedModifier=.12f;}
    public override void Shoot(){if(!KeepFiring() || busy)return;firing=StartCoroutine(Fire());}
    public override void StopFiring()
    {
        base.StopFiring();
        if(firing!=null){StopCoroutine(firing);firing=null;}
        Cleanup();
    }
    void OnDisable(){if(firing!=null)StopCoroutine(firing);firing=null;Cleanup();}
    void Cleanup()
    {
        if(beams!=null)foreach(var b in beams)if(b!=null)Destroy(b.gameObject);
        beams=null;busy=false;if(sound!=null)sound.Stop();
    }
    IEnumerator Fire()
    {
        busy=true;
        int count=Mathf.Min(6,GameRules.playerShootNumber);beams=new BeamRibbon[count];
        for(int i=0;i<count;i++)
        {
            var go=new GameObject("Cannon plasma ribbon");go.transform.SetParent(transform,false);
            beams[i]=go.AddComponent<BeamRibbon>();beams[i].Build(true);
        }
        if(sound==null)sound=gameObject.AddComponent<LaserSound>();
        float charge=CombatUpgrades.Current!=null ? CombatUpgrades.Current.Volley() : 1;
        float warning=.25f/Mathf.Max(1,GameRules.playerShootSpeed/10),end=warning+.65f,t=0,nextTick=warning;
        sound.Charge(warning,true);
        while(t<end+.22f && !GameRules.playerDisabled)
        {
            if(Time.deltaTime<=0){yield return null;continue;}
            t+=Time.deltaTime;bool active=t>=warning && t<end;
            float power=active ? Mathf.Min(Mathf.Clamp01((t-warning)/.055f),Mathf.Lerp(.4f,1,Mathf.Clamp01((end-t)/.09f))) :
                t>=end ? Mathf.Pow(Mathf.Clamp01(1-(t-end)/.22f),2)*.4f : 0;
            sound.Drive(power,active);
            for(int i=0;i<count;i++)
                beams[i].Draw(transform.position+spawnPoints[i],Vector2.up,20,.14f*Mathf.Sqrt(GameRules.playerShootSize),
                    Mathf.Clamp01(t/warning),power,Mathf.Max(0,t-end)/.22f);
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
                    float amount=GameRules.playerShootDamage*1.2f*charge;
                    enemy.damage(CombatUpgrades.Current!=null ? CombatUpgrades.Current.DirectHit(enemy,amount) : amount);
                    enemy.HitAt(enemy.transform.position,Vector2.up);
                }
            }
            yield return null;
        }
        Cleanup();firing=null;
    }
}
