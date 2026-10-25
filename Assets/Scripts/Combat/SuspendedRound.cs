using UnityEngine;
public sealed class SuspendedRound : CombatRound
{
    protected override void Initialize(){Orient();}
    protected override void Tick(float dt)
    {
        float factor=age<.8f ? 1-age/.8f : age<1.7f ? 0 : Mathf.Min(1.5f,(age-1.7f)*2);
        transform.position+=(Vector3)heading*speed*factor*dt;Orient();
        float light=age<1.7f ? .82f+.16f*Mathf.Sin(age*5) : 1;visual.color=new Color(light,light,light,1);
    }
}
