using UnityEngine;
public sealed class SuspendedRound : CombatRound
{
    protected override void Initialize(){Orient();}
    protected override void Tick(float dt)
    {
        float factor=age<.8f ? 1-age/.8f : age<1.7f ? 0 : Mathf.Min(1.5f,(age-1.7f)*2);
        transform.position+=(Vector3)heading*speed*factor*dt;Orient();
        visual.color=Color.Lerp(new Color(.68f,.73f,1),Color.white,age<1.7f ? .5f+.25f*Mathf.Sin(age*5) : 1);
    }
}
