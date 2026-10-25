using UnityEngine;
using System.Collections.Generic;

public sealed class StaticCharge : MonoBehaviour
{
    public float Charge {get;private set;}
    Vector3 previous;
    float spark;
    SpriteRenderer[] buildup;
    void OnEnable(){previous=transform.position;}
    void LateUpdate()
    {
        float distance=Vector2.Distance(previous,transform.position);previous=transform.position;
        if(GameRules.playerDisabled || Menu.GameplayPaused || WorldTimeStop.PlayerDelta<=0)return;
        float old=Charge;Charge=Mathf.Min(1,Charge+Mathf.Min(distance,GameRules.playerMoveSpeed*WorldTimeStop.PlayerDelta*1.5f)/24f);
        if(buildup==null)
        {
            buildup=new SpriteRenderer[3];
            for(int i=0;i<3;i++){buildup[i]=ItemSpark.Sprite(transform,"Static charge spark","standard",new Color(.7f,.5f,.95f),.075f);buildup[i].transform.localPosition=new Vector3((i-1)*.18f,-.48f,-2);}
        }
        for(int i=0;i<3;i++)buildup[i].enabled=Charge>=(i+1)*.25f;
        if(old<1 && Charge>=1)ItemSound.Play("ready",.12f,1.4f);
        if(Charge<1 || Time.time<spark)return;spark=Time.time+.5f;
        var p=transform.position+new Vector3(.25f,-.25f,-2);
        StaticArc.Draw(p,p+new Vector3(-.5f,.1f,0),.15f);
    }
    public void Discharge(AEnemy first,float damage)
    {
        if(Charge<1)return;Charge=0;
        var used=new HashSet<AEnemy>{first};var point=first.transform.position;
        for(int i=0;i<3;i++)
        {
            var next=CombatUpgrades.Nearest(point,used,4.5f);if(next==null)break;
            used.Add(next);StaticArc.Draw(point,next.transform.position,.22f);
            float dealt=CombatUpgrades.Current.DirectHit(next,damage*.7f*Mathf.Pow(.8f,i),1);
            next.HitAt(next.transform.position,((Vector2)(next.transform.position-point)).normalized);
            next.damage(dealt);Score.IncreaseScore(Mathf.RoundToInt(dealt));point=next.transform.position;
        }
        ItemSpark.Emit(first.transform.position,new Color(.7f,.5f,.95f),5,.12f);
        ItemSound.Play("static",.2f);
    }
    void OnDestroy(){if(buildup!=null)foreach(var sprite in buildup)if(sprite!=null)Destroy(sprite.gameObject);}
}
public sealed class StaticArc : MonoBehaviour
{
    float age,duration;
    LineRenderer line;
    Color fromColor,toColor;
    public static void Draw(Vector3 from,Vector3 to,float duration)
    {
        var go=new GameObject("Static discharge");var arc=go.AddComponent<StaticArc>();arc.duration=duration;
        arc.line=CombatVisuals.Line(go.transform,new Color(.65f,.55f,.95f),.055f);arc.line.positionCount=7;
        bool fire=PlayerShotPalette.Burning,poison=PlayerShotPalette.Toxic;
        arc.fromColor=poison ? PlayerShotPalette.Poison : fire ? PlayerShotPalette.Fire : new Color(.65f,.55f,.95f);
        arc.toColor=fire ? PlayerShotPalette.Fire : arc.fromColor;
        var side=Vector3.Cross(to-from,Vector3.forward).normalized;
        for(int i=0;i<7;i++){var p=Vector3.Lerp(from,to,i/6f)+side*(i==0 || i==6 ? 0 : (i%2==0 ? -.1f : .1f));p.z=-2;arc.line.SetPosition(i,p);}
    }
    void Update(){if(WorldTimeStop.Frozen)return;age+=Time.deltaTime;fromColor.a=toColor.a=1-age/duration;line.startColor=fromColor;line.endColor=toColor;if(age>=duration)Destroy(gameObject);}
}
