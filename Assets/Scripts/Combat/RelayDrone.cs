using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class RelayDrone : DistrictEnemy
{
    protected override void Configure(){hull=10;health=32;hullScale=.55f;attackInterval=2.7f;Mover<StrafeAndBrake>();}
    protected override void Attack()
    {
        // Capacitor fighters strike on the formation's shared accent; no ship-to-ship tethers.
        Vector2 aim=Aim;
        for(int i=-1;i<=1;i++)
            CombatRound.Launch<NeedleRound>(Hull.TransformPoint(new Vector3(i*.46f,i==0 ? -1.24f : -.995f,i==0 ? -.13f : -.10f)),CombatVisuals.Rotate(aim,i*7),6.8f-Mathf.Abs(i)*.45f,.13f);
    }
}
