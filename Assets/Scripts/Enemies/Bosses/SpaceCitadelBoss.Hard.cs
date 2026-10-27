using UnityEngine;

public sealed partial class SpaceCitadelBoss
{

    void AttackHard()
    {
        if(parts[0]==null)return;
        int beat=(volley-1)%12;
        if(beat==0)
        {
            if(CoreExposed)OrbitalSystem(Muzzle(3),phaseTwo?4:3,10,(volley/12)*35);
            else for(int i=0;i<2;i++)if(parts[i].health>0)
            {
                var p=parts[i].transform.TransformPoint(new Vector3(0,-.3f,-.3f));
                var seed=Fire<SimpleRound>(p,Vector2.down,1.6f,.68f);
                if(seed!=null)seed.IceSeed(1.7f,24,4,1.65f,i==0?38:-38,1.2f,i*35);
            }
        }
        else if(beat==4)
        {
            centralLaser=Fire<PulseLaser>(Muzzle(),Vector2.down,0);
            if(centralLaser!=null){centralLaser.Bind(Hull,WeaponPort());centralLaser.warning=1.3f;centralLaser.activeTime=phaseTwo?1.8f:1.5f;centralLaser.width=phaseTwo?.42f:.34f;}
        }
        else if(beat==6)
        {
            for(int i=2;i<4;i++)if(parts[i].health>0)
                Patterns.Spiral(parts[i].transform,new Vector3(i==2?-.16f:.16f,-.695f,-.08f),2,phaseTwo?24:18,i==2?(phaseTwo?15:20):-(phaseTwo?15:20),.085f,2.6f,.16f,i*20,parts[i]);
        }
        else if(beat==10)OrbitalCrown(Muzzle(3),phaseTwo?32:28,4,2.8f,volley/12%2==0?32:-32,1.15f,volley*11);
    }
    protected override void PrepareHardFire(float seconds)
    {
        if(parts[0]==null)return;int beat=volley%12;
        if(beat==0)
        {
            if(CoreExposed)Cue(Muzzle(3),seconds);
            else for(int i=0;i<2;i++)if(parts[i].health>0)Cue(parts[i].transform.TransformPoint(new Vector3(0,-.3f,-.3f)),seconds);
        }
        else if(beat==4)Cue(Muzzle(),seconds);
        else if(beat==6){for(int i=2;i<4;i++)if(parts[i].health>0)Cue(parts[i].transform.TransformPoint(new Vector3(i==2?-.16f:.16f,-.695f,-.08f)),seconds);}
        else if(beat==10)Cue(Muzzle(3),seconds);
    }
}
