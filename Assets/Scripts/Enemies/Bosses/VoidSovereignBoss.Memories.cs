using System.Collections;
using UnityEngine;

public sealed partial class VoidSovereignBoss
{
    Vector2 sawTravel = new Vector2(4.1f, -3.45f);
    int bounces;
    float memoryAge, dive;
    Vector3 diveStart, diveEnd;

    void MoveMemory(float dt)
    {
        memoryAge += dt;
        float t = memoryAge;
        var district = Memory.district;
        if (district == LevelId.Tunnel)
        {
            var r = CombatRound.Border;
            Vector3 p = transform.position + (Vector3)sawTravel * dt;
            bool x = p.x < r.xMin+1.28f || p.x > r.xMax-1.28f;
            bool y = p.y < r.yMin+1.28f || p.y > r.yMax-1.28f;
            if (x) sawTravel.x *= -1;
            if (y) sawTravel.y *= -1;
            if (x || y) bounces++;
            transform.position = new Vector3(Mathf.Clamp(p.x,r.xMin+1.28f,r.xMax-1.28f),
                Mathf.Clamp(p.y,r.yMin+1.28f,r.yMax-1.28f),1);
            return;
        }
        if (district == LevelId.Underwater && dive > 0)
        {
            dive = Mathf.Min(1, dive + dt*.55f);
            transform.position=Vector3.Lerp(diveStart,diveEnd,Mathf.SmoothStep(0,1,dive));
            return;
        }
        float xOffset = district == LevelId.Core || district == LevelId.Sewers || district == LevelId.Space ? 0 :
            Mathf.Sin(t * (district == LevelId.Candyland ? 1.9f : .71f)) * 1.55f;
        float yOffset = district == LevelId.Sewers ? -1.45f : district == LevelId.Desert ? -2.1f+2.3f*Mathf.Cos(t*.64f) :
            .55f*Mathf.Sin(t*.93f);
        transform.position=Vector3.MoveTowards(transform.position, station+new Vector3(xOffset,yOffset,0),dt*4.2f);
    }

    Vector3 InkPort(int port) => Memory.Port(port).position;
    IEnumerator Trace(int a,int b,float seconds)
    {
        for(float t=0;t<seconds;)
        {
            Cue(InkPort(a),seconds-t);
            if(a!=b) Cue(InkPort(b),seconds-t);
            if(!Holding) t+=Time.deltaTime*LevelRun.EnemyTempoScale;
            yield return null;
        }
    }
    Vector2 Down(float angle) => CombatVisuals.Rotate(Vector2.down,angle);

    IEnumerator MemoryCard()
    {
        var district=Memory.district;
        if (district == LevelId.Thunder)
        {
            Vector2 hub = new Vector2(0,(Camera.UpLimit + Camera.DownLimit)*.5f + .9f);
            for (int i=0;i<6;i++)
            {
                Vector2 ray = Down(i*60);
                var pylon = ThunderLaserBot.Deploy(InkPort(2),hub + ray*1.12f,ray,16,.18f)
                    .OwnedBy(this).Swing(60,Difficulty.Hard ? 27 : 21).ArmAt(1.65f);
                pylon.transform.SetParent(Memory.root,true);
                pylon.GetComponentInChildren<MeshRenderer>().sharedMaterial = VoidEchoVisual.Obsidian;
            }
        }
        if(district==LevelId.Tunnel)
        {
            int previous=bounces;
            while(true)
            {
                while(previous==bounces || Holding) yield return null;
                previous=bounces;
                int n=Dense(34);
                for(int i=0;i<n;i++)
                {
                    float a=i*360f/n+previous*9;
                    if(i%5==0) Fire<RicochetBolt>(InkPort(0),Down(a),3.85f,.23f,i==0).Bounce(1);
                    else Fire<SimpleRound>(InkPort(0),Down(a),i%2==0?3.22f:2.45f,.17f,i==1);
                }
            }
        }
        for(int beat=0;;beat++)
        {
            int sign=(beat&1)==0?1:-1;
            UseTone(sign>0?HostileTone.Bright:HostileTone.Dark);
            yield return district switch
            {
                LevelId.Thunder => PylonLace(beat),
                LevelId.Space => CollapsingHorizon(beat),
                LevelId.Heaven => TurningWheels(beat),
                LevelId.Desert => ReactorProcession(beat),
                LevelId.Sewers => CisternBowWave(beat),
                LevelId.Core => GantryCrosshatch(),
                LevelId.Mushroom => AmanitaEcho(),
                LevelId.Candyland => LollipopSwirl(beat),
                LevelId.Dream => PorcelainPleats(beat),
                LevelId.Underwater => PressureWake(beat),
                LevelId.Hell => PincerLace(),
                _ => throw new System.InvalidOperationException("Unscored Void memory " + district)
            };
            yield return Wait(Rest(.49f));
        }
    }

    IEnumerator PylonLace(int beat)
    {
        int sign = (beat & 1) == 0 ? 1 : -1;
        yield return Trace(beat%2,2,.38f);
        for(int accent=0;accent<5;accent++)
        {
            for(int n=-3;n<=3;n++)
                Fire<NeedleRound>(InkPort(2),Down(n*20+sign*accent*3),3.58f,.17f,n==0)
                    .Zigzag(.42f,sign*19);
            yield return Wait(Rest(.40f));
        }
    }

    IEnumerator CollapsingHorizon(int beat)
    {
        int sign = (beat & 1) == 0 ? 1 : -1;
        yield return Trace(0,0,.42f);
        int stars=Dense(54);
        for(int i=0;i<stars;i++)
        {
            float angle=i*360f/stars;
            Fire<CrescentRound>(InkPort(0),Down(angle),3.65f,.20f,i==0)
                .Pause(1.8f,.31f,CombatRound.Turn.Rotate,180+sign*10,1.28f);
        }
        for(int i=0;i<12;i++)
            Fire<SimpleRound>(InkPort(0),Vector2.zero,0,.17f,false)?.Orbit(.3f,i*30,sign*27,5).Expand(1.65f,2.6f);
        yield return Wait(Rest(1.42f));
    }

    IEnumerator TurningWheels(int beat)
    {
        int sign = (beat & 1) == 0 ? 1 : -1;
        yield return Trace(1,2,.37f);
        for(int j=0;j<Dense(20);j++)
        {
            for(int eye=1;eye<=4;eye++)
            {
                float a=j*17+eye*90+beat*23;
                var v=new Vector2(Mathf.Cos(a*Mathf.Deg2Rad)*3.5f,Mathf.Sin(a*Mathf.Deg2Rad)*2.4f-1.65f);
                Fire<SimpleRound>(InkPort(eye),v,v.magnitude,.18f,eye==1);
                if(j%3==0)Fire<KunaiRound>(InkPort(eye),Down((eye-2.5f)*28+sign*j),3.9f,.17f,false);
            }
            yield return Wait(Rest(.072f));
        }
    }

    IEnumerator ReactorProcession(int beat)
    {
        int sign = (beat & 1) == 0 ? 1 : -1;
        yield return Trace(0,4,.42f);
        for(int joint=0;joint<7;joint++)
        {
            for(int k=0;k<Dense(8);k++)
            {
                float a=k*360f/Dense(8)+joint*23+beat*13;
                Fire<KunaiRound>(InkPort(joint),Down(a),2.35f,.19f,k==0)
                    .Pause(.65f,.31f,CombatRound.Turn.Rotate,sign*(20+joint*3),1.8f);
            }
            yield return Wait(Rest(.13f));
        }
    }

    IEnumerator CisternBowWave(int beat)
    {
        int sign = (beat & 1) == 0 ? 1 : -1;
        yield return Trace(beat%3,3+beat%3,.48f);
        for(int layer=0;layer<2;layer++)
        {
            int marks=Dense(13);
            for(int h=0;h<2;h++)
                for(int i=0;i<marks;i++)
                {
                    float t=i/(marks-1f);
                    var axis=Down((h*2-1)*(Mathf.Lerp(-42,97,t)+sign*12+layer*10));
                    Fire<GiantOrb>(InkPort(beat%3+h*3),axis,2.1f+.9f*Mathf.Sin(t*Mathf.PI)+layer*.4f,
                        i%4==0?.54f:.32f,i==0).Ramp(1.37f,.73f,.6f);
                }
            yield return Wait(Rest(.44f));
        }
    }

    IEnumerator GantryCrosshatch()
    {
        yield return Trace(0,1,.32f);
        for(int tooth=0;tooth<Dense(18);tooth++)
        {
            float a=12+tooth*3;
            for(int rail=0;rail<2;rail++)
                Fire<NeedleRound>(InkPort(rail),Down((rail*2-1)*-a),3.9f,.17f,tooth%4==0);
            if(tooth%3==0)
                for(int k=-2;k<=2;k++) Fire<SimpleRound>(InkPort(2),Down(k*12),3.2f,.17f,k==0);
            yield return Wait(Rest(.079f));
        }
    }

    IEnumerator AmanitaEcho()
    {
        yield return Trace(0,1,.34f);
        for(int layer=0;layer<3;layer++)
        {
            int marks=Dense(layer == 1 ? 17 : 34);
            for(int wing=0;wing<2;wing++)
                for(int i=0;i<marks;i++)
                {
                    float a=i*360f/marks;
                    Vector2 v=Down((wing*2-1)*(a+layer*8))*(2.6f+1.14f*Mathf.Cos((4*a+layer*27)*Mathf.Deg2Rad))+Vector2.down*1.65f;
                    Fire<SimpleRound>(InkPort(wing),v,v.magnitude,.18f,i==0);
                }
            yield return Wait(Rest(.24f));
        }
    }

    IEnumerator LollipopSwirl(int beat)
    {
        yield return Trace(0,1,.32f);
        for(int i=0;i<Dense(22);i++)
        {
            for(int h=0;h<2;h++)
            {
                float a=i*22+beat*17+h*180;
                Vector2 v=Down(a)*(2.2f+.65f*Mathf.Cos(a*3*Mathf.Deg2Rad))+Vector2.down*.95f;
                Fire<RicochetBolt>(InkPort(h),v,v.magnitude,.21f,i%4==0).Bounce(1).FadeAt(6);
            }
            yield return Wait(Rest(.066f));
        }
    }

    IEnumerator PorcelainPleats(int beat)
    {
        yield return Trace(0,1,.38f);
        for(int step=0;step<Dense(17);step++)
        {
            for(int finger=0;finger<8;finger++)
            {
                float t=step/(float)Dense(17);
                float a=(finger/2)*90+(finger%2==0?-1:1)*(beat*23+47*Mathf.Sin(t*Mathf.PI));
                Vector2 v=Down(a)*3.45f+Vector2.down*1.23f;
                Fire<CrescentRound>(InkPort(finger),v,v.magnitude,.195f,finger<2);
                if(step%4==0)Fire<NeedleRound>(InkPort(finger),Down(a+12),2.5f,.17f,false).Ramp(1.8f,.65f,.50f);
            }
            yield return Wait(Rest(.086f));
        }
    }

    IEnumerator PressureWake(int beat)
    {
        int sign = (beat & 1) == 0 ? 1 : -1;
        yield return Trace(0,0,.50f);
        int pearls=Dense(34);
        for(int i=0;i<pearls;i++)
            Fire<SimpleRound>(InkPort(0),Down(i*360f/pearls+beat*11),3.15f,.205f,i==0);
        diveStart=transform.position;
        diveEnd=new Vector3(sign*1.9f,Camera.DownLimit+1.55f,1);
        yield return Trace(0,0,.70f);
        dive=.001f;
        for(int wake=0;wake<15;wake++)
        {
            for(int h=-1;h<=1;h+=2)
                for(int row=0;row<3;row++)
                    Fire<SimpleRound>(InkPort(0),new Vector2(h,.12f+row*.2f),1.85f+row*.55f,.16f,row==0);
            yield return Wait(.12f);
        }
        dive=0;
        yield return Wait(2.75f);
    }

    IEnumerator PincerLace()
    {
        yield return Trace(0,1,.38f);
        for(int stitch=0;stitch<Dense(23);stitch++)
        {
            for(int h=0;h<2;h++)
            {
                float angle=(h*2-1)*(26+35*Mathf.Sin(stitch*.26f));
                for(int k=-2;k<=2;k++)
                    Fire<KunaiRound>(InkPort(h),Down(angle+k*13),3.45f+k*.12f,.17f,k==0);
                if(stitch%4==0)Fire<CrescentRound>(InkPort(h),-Down(angle),2.8f,.24f,false).Bounce(1);
            }
            yield return Wait(Rest(.089f));
        }
    }

    // Scythes stitch between their cuts, the maw drops a second row of inward teeth,
    // and the four star tips pass one rotating accent from bank to bank.
    IEnumerator ObsidianCounterpoint()
    {
        for(int beat=0;;beat++)
        {
            yield return Wait(Rest(Form==4?.38f:.48f));
            if (teleporting || (beat%8)>=6) continue;
            int tips=Form==4?4:2;
            for(int i=0;i<tips;i++)
            {
                int side=i%2==0?-1:1;
                float a=Form==2?side*(34+18*Mathf.Sin(beat*.32f)):
                    Form==3?-side*(19+10*Mathf.Cos(beat*.45f)):i*90+beat*12;
                UseTone((beat+i)%2==0?HostileTone.Bright:HostileTone.Dark);
                for(int k=-1;k<=1;k++)
                    Fire<SimpleRound>(Port(i),Down(a+k*12),2.72f+k*.22f,.17f,k==0);
            }
        }
    }
}
