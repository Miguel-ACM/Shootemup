using UnityEngine;
public sealed class MagnifyingLens : MonoBehaviour
{
    public static MagnifyingLens Current {get;private set;}
    Inputs input;
    float charge;
    public float Charge=>input!=null && input.PlayerControls.Focus.IsPressed() && !GameRules.playerDisabled ? charge : 0;
    public static float Rate=>Current!=null ? Mathf.Lerp(1,.5f,Current.Charge) : 1;
    public static float Power=>Current!=null ? Mathf.Lerp(1,3,Current.Charge) : 1;
    void Awake(){Current=this;input=new Inputs();input.Enable();}
    void Update(){charge=input.PlayerControls.Focus.IsPressed() && !GameRules.playerDisabled ? Mathf.Min(1,charge+WorldTimeStop.PlayerDelta/1.5f) : 0;}
    void OnDestroy(){if(Current==this)Current=null;input?.Disable();input?.Dispose();}
    public static void Enlarge(ABullet bullet)
    {
        if(bullet==null || bullet.companionShot || Current==null)return;
        bullet.damage*=Power;bullet.transform.localScale*=Mathf.Lerp(1,1.6f,Current.Charge);
    }
}
