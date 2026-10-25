using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class CinderImp : InfernalEnemy
{
    protected override void Configure(){hull=48;hullScale=.40f;health=27;attackInterval=1.0f;Mover<Slalom>();}
    protected override void Attack()
    {
        for(int i=0;i<5;i++)Fire<HelixRound>(transform.position,CombatVisuals.Rotate(Aim,(i-2)*17+(volley%2==0 ? 5 : -5)),4.8f,.14f);
    }
}
