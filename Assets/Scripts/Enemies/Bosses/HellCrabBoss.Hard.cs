using UnityEngine;

public sealed partial class HellCrabBoss
{

    int BurningEye(){if(eyes[0]==null)return -1;int first=(volley/16)%2;return eyes[first].health>0?first:eyes[1-first].health>0?1-first:-1;}
    void AttackHard()
    {
        if(claws[0]==null)return;
        int beat=(volley-1)%16;
        if(beat==0||beat==4)
        {
            int side=beat==0?0:1;
            Patterns.Spiral(claws[side],new Vector3(0,-.32f,-.4f),phaseTwo?3:2,24,side==0?15:-15,.08f,2.5f,.17f,side==0?25:-25);
        }
        else if(beat==7)
        {
            int eye=BurningEye();if(eye<0)return;
            Patterns.Web(eyes[eye].transform,Vector3.zero,phaseTwo?9:8,4,2.2f,(volley/16%2==0?-30:20),eyes[eye]);
        }
        else if(beat==12)
        {
            int eye=BurningEye();if(eye>=0)HeavyTrail(eyes[eye].transform.position,Vector2.down,.94f,1.45f);
        }
        else if(phaseTwo&&beat==14)
            Patterns.Ribbon(claws[0],new Vector3(0,-.32f,-.4f),Vector2.down,3,14,36,2.8f);
    }
    protected override void PrepareHardFire(float seconds)
    {
        if(claws[0]==null)return;int beat=volley%16;
        if(beat==0||beat==4)Cue(claws[beat==0?0:1].TransformPoint(new Vector3(0,-.32f,-.4f)),seconds);
        else if(beat==7||beat==12){int eye=BurningEye();if(eye>=0)Cue(eyes[eye].transform.position,seconds);}
        else if(phaseTwo&&beat==14)Cue(claws[0].TransformPoint(new Vector3(0,-.32f,-.4f)),seconds);
    }
}
