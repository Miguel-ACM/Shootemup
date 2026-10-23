using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class SnowSiegeBoss : DistrictBoss
{
    protected override void Configure(){hull=21;hullScale=1.6f;health=1500;attackInterval=2.1f;}
    protected override void MoveBoss(float dt)
    {
        station.x=Mathf.Sin(age*.45f)*1.8f;base.MoveBoss(dt);GetComponent<CombatHullVisual>().spin=12;
    }
    protected override void Attack()
    {
        switch(volley%4)
        {
            case 0: Fan<BurstShell>(phaseTwo ? 7 : 4,17,4,.48f);break;
            case 1:
                for(int i=-2;i<=2;i++)CombatRound.Launch<SuspendedRound>(transform.position+Vector3.right*i*.8f,Vector2.down,5,.25f);
                Fan<BurstShell>(2,65,3,.5f);break;
            case 2: Fan<PlasmaOrb>(phaseTwo ? 4 : 2,28,2,.62f);break;
            default:Radial<BurstShell>(phaseTwo ? 8 : 5,3.5f,.42f,volley*15);break;
        }
        if(phaseTwo)Fan<NeedleRound>(3,14,5);
    }
}
