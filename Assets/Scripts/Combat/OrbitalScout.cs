using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class OrbitalScout : SpaceSatellite
{
    protected override void Configure(){hull=41;hullScale=.46f;health=27;attackInterval=1.65f;Mover<Slalom>();}
    protected override void Attack()
    {
        for(int s=-2;s<=2;s++)Fire<NeedleRound>(Hull.TransformPoint(new Vector3(s*.095f,-.68f,-.15f)),CombatVisuals.Rotate(Aim,s*10),6.4f,.14f);
    }
}
