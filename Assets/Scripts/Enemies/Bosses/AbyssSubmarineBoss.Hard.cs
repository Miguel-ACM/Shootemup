using UnityEngine;

public sealed partial class AbyssSubmarineBoss
{

    protected override bool PatternEmissionAllowed=>!Submerged&&Mathf.Repeat(cycleClock,cycleSeconds)<3.5f;
    void AttackHard()
    {
        if(Submerged||Mathf.Repeat(cycleClock,cycleSeconds)>=3.5f)return;
        if(volley%4==1)
            for(int side=-1;side<=1;side+=2)HeavyTrail(Muzzle(side<0?1:2),CombatVisuals.Rotate(Vector2.down,side*22),.68f,1.55f);
        else if(volley%4==2)Patterns.Ribbon(Hull,WeaponPort(),Aim,3,14,24,3.0f,.09f);
    }
    void HardCrown(float offset,float speed)
    {
        EmergenceBursts++;
        int count=EmergenceBursts%3==1?8:phaseTwo?28:24;
        for(int i=0;i<count;i++)
        {
            int side=i%2==0?-1:1,lane=(i/2)%3-1;
            var p=Hull.TransformPoint(new Vector3(side*1.02f,.3f+lane*.35f,-.3f));
            var shot=Fire<SimpleRound>(p,CombatVisuals.Rotate(Vector2.down,offset+i*360f/count),speed,count==8?.52f:.18f);
            if(shot==null)continue;
            if(count==8)shot.LeaveTrail(.4f,2.5f);
            else shot.Curl((i%2==0?1:-1)*(EmergenceBursts%3==2?16:-16),2.3f);
        }
    }
    void HardWake(Vector3 port,Vector2 direction,float speed,int side)
    {
        var shot=Fire<SimpleRound>(port,direction,speed,.18f);
        if(shot!=null)shot.Curl(-side*26,1.6f);
    }
    protected override void PrepareHardFire(float seconds)
    {
        // Emergence and charge keep their existing physical warnings and launcher cues.
        float t=Mathf.Repeat(cycleClock,cycleSeconds);
        if(t<3.5f){if((volley+1)%4==1){Cue(Muzzle(1),seconds);Cue(Muzzle(2),seconds);}else if((volley+1)%4==2)Cue(Muzzle(),seconds);}
        else PrepareFire(seconds);
    }
}
