using UnityEngine;
public sealed class HelixRound : CombatRound
{
    public float phase;
    protected override void Initialize(){phase=0;}
    protected override void Tick(float dt)
    {
        var side=new Vector2(-heading.y,heading.x);
        var old=transform.position;
        transform.position=origin+(Vector3)(heading*age*speed+side*Mathf.Sin(age*5+phase)*.5f);
        var d=transform.position-old;if(d.sqrMagnitude>.001f)transform.rotation=Quaternion.FromToRotation(Vector3.up,d);
    }
    public static void Pair(Vector3 p,Vector2 d,float speed=3)
    {
        Launch<HelixRound>(p,d,speed,.12f);var b=Launch<HelixRound>(p,d,speed,.12f);if(b!=null)b.phase=Mathf.PI;
    }
}
