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
            CombatRound.Launch<NeedleRound>(transform.position,CombatVisuals.Rotate(aim,i*7),6.8f-Mathf.Abs(i)*.45f,.13f);
    }
}
