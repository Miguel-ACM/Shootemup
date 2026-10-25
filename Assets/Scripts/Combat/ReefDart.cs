using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class ReefDart : OceanEnemy
{
    protected override void Configure(){hull=50;hullScale=.49f;health=33;attackInterval=1.8f;Mover<Slalom>();}
    protected override void Attack()
    {
        var axis=Aim;
        for(int s=-1;s<=1;s+=2)Fire<NeedleRound>(Hull.TransformPoint(new Vector3(s*.17f,-.55f,-.2f)),CombatVisuals.Rotate(axis,s*5),4.1f,.15f);
    }
}
