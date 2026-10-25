using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class BarrelRoll : AActiveItem
{
    void Awake(){SetData("Barrel roll","Take a side","Roll sideways through danger. No energy cost.",ItemType.active);cost=0;cd=2.5f;}
    public override float OnPress(float mana)
    {
        float valid=CheckValidPress(mana);if(valid<0)return valid;
        var roll=s.GetComponent<ShipBarrelRoll>();if(roll==null)roll=s.gameObject.AddComponent<ShipBarrelRoll>();
        if(roll.Active)return -2;
        roll.Begin(Mathf.Abs(s.MoveInput.x)>.1f ? Mathf.Sign(s.MoveInput.x) : ReplacementSlot==0 ? -1 : 1);
        lastCD=0;return mana;
    }
}
[DefaultExecutionOrder(50)]
public sealed class ShipBarrelRoll : MonoBehaviour
{
    public const float Duration=.38f,Distance=2.6f;
    float elapsed=Duration,direction;
    public bool Active=>elapsed<Duration;
    public float Angle=>Active ? direction*360*Mathf.SmoothStep(0,1,elapsed/Duration) : 0;
    public void Begin(float side){direction=side;elapsed=0;ItemSound.Play("roll",.2f);}
    void Update()
    {
        if(!Active || GameRules.playerDisabled)return;
        float old=elapsed;elapsed=Mathf.Min(Duration,elapsed+WorldTimeStop.PlayerDelta);
        float step=(Mathf.SmoothStep(0,1,elapsed/Duration)-Mathf.SmoothStep(0,1,old/Duration))*Distance*direction;
        var p=transform.position;p.x=Mathf.Clamp(p.x+step,Camera.LeftLimit,Camera.RightLimit);transform.position=p;
    }
}
