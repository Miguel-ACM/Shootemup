using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class BulletTime : AActiveItem
{
    void Awake(){SetData("Bullet time","Hold that thought","Freeze the world for 2 seconds. Keep firing.",ItemType.active);cost=35;cd=12;}
    public override float OnPress(float mana)
    {
        float valid=CheckValidPress(mana);if(valid<0)return valid;
        if(WorldTimeStop.Frozen || Time.timeScale<=0)return -2;
        var stop=s.gameObject.AddComponent<WorldTimeStop>();stop.Begin(2);lastCD=0;return mana-cost;
    }
}
[DefaultExecutionOrder(-200)]
public sealed class WorldTimeStop : MonoBehaviour
{
    public static WorldTimeStop Current {get;private set;}
    public static bool Frozen=>Current!=null && Current.remaining>0;
    public static bool PlayerCanAct=>Frozen && !Menu.GameplayPaused;
    public static float PlayerDelta=>PlayerCanAct ? Time.unscaledDeltaTime : Time.deltaTime;
    float remaining,previousRate=1;
    LineRenderer border;
    public float Remaining=>remaining;
    public void Begin(float duration)
    {
        Current=this;remaining=duration;previousRate=Time.timeScale;Time.timeScale=0;
        var go=new GameObject("Stopped time border");go.transform.SetParent(transform,false);
        border=CombatVisuals.Line(go.transform,new Color(.3f,.75f,.9f,.6f),.04f);border.positionCount=5;
        ItemSound.Play("timeStop",.22f);
    }
    void Update()
    {
        if(Current!=this)return;
        if(GameRules.playerDisabled || GetComponent<Shipcontroller>().health<=0){End();return;}
        if(Menu.GameplayPaused)return;
        remaining-=Time.unscaledDeltaTime;
        if(remaining<=0){End();return;}
        Time.timeScale=0;
        var camera=UnityEngine.Camera.main;
        float halfHeight=camera.orthographicSize,halfWidth=halfHeight*405f/720f;
        var center=camera.transform.position;
        float left=center.x-halfWidth+.04f,right=center.x+halfWidth-.04f,top=center.y+halfHeight-.04f,bottom=center.y-halfHeight+.04f;
        border.SetPositions(new[]{new Vector3(left,bottom,-2),new Vector3(left,top,-2),new Vector3(right,top,-2),new Vector3(right,bottom,-2),new Vector3(left,bottom,-2)});
        border.startColor=border.endColor=new Color(.3f,.75f,.9f,Mathf.Min(.65f,remaining*.8f));
    }
    void End(){ItemSound.Play("powerdown",.16f,1.25f);Restore();Destroy(this);}
    void Restore()
    {
        if(Current!=this)return;
        Current=null;remaining=0;Time.timeScale=Menu.GameplayPaused ? 0 : previousRate;
        if(border!=null)Destroy(border.transform.parent.gameObject);
    }
    void OnDisable(){Restore();}
    void OnDestroy(){Restore();}
}
