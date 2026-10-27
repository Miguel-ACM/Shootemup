using UnityEngine;

public sealed partial class CityDuoBoss
{

    void AttackHard()
    {
        int beat=(volley-1)%12;
        if(heavy)
        {
            if(beat==0||beat==4)HeavyTrail(Muzzle(1),CombatVisuals.Rotate(Vector2.down,beat==0?-12:12),.86f,1.95f);
            else if(beat==2||beat==6)
                for(int i=0;i<(enraged?15:11);i++)
                    Fire<SimpleRound>(Muzzle(i%3),CombatVisuals.Rotate(Vector2.down,(i-(enraged?14:10)*.5f)*11+(beat==2?-5:5)),2.2f,.23f);
            else if(enraged&&beat==8)Patterns.Ribbon(Hull,WeaponPort(1),Aim,3,18,26,3.4f);
        }
        else
        {
            if(beat==1||beat==5)Patterns.Ribbon(Hull,WeaponPort(),Aim,enraged?4:3,18,beat==1?26:-26,4.0f,.07f);
            else if(beat==8)Patterns.Spiral(Hull,WeaponPort(1),2,24,enraged?18:15,.065f,2.8f,.16f);
            else if(enraged&&beat==0)HeavyTrail(Muzzle(2),Vector2.down,.72f,2.0f);
        }
    }
    protected override void PrepareHardFire(float seconds)
    {
        int beat=volley%12;
        if(heavy&&(beat==0||beat==2||beat==4||beat==6)||enraged&&beat==8)Cue(Muzzle(1),seconds);
        else if(!heavy&&(beat==1||beat==5))Cue(Muzzle(),seconds);
        else if(!heavy&&beat==8)Cue(Muzzle(1),seconds);
        else if(!heavy&&enraged&&beat==0)Cue(Muzzle(2),seconds);
    }
}
