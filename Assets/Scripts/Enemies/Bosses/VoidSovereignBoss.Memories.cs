using System.Collections;
using UnityEngine;

public sealed partial class VoidSovereignBoss
{
    Vector2 sawTravel;
    readonly ThunderLaserBot[] echoPylons = new ThunderLaserBot[6];
    Vector3[] circuit;
    float[] distances;
    float circuitTravel;
    int circuitIndex;
    int bounces;
    float memoryAge;
    bool holdMemory, submergedMemory;

    void MoveMemory(float dt)
    {
        memoryAge += dt;
        float t = memoryAge;
        var district = Memory.district;
        if (district == LevelId.Tunnel)
        {
            var r = CombatRound.Border;
            float radius = Memory.hull.GetComponent<MeshFilter>().sharedMesh.bounds.extents.x * Mathf.Abs(Memory.hull.lossyScale.x);
            if (sawTravel == Vector2.zero)
                sawTravel = (new Vector2(r.xMax-radius,r.yMin+radius)-(Vector2)transform.position).normalized;
            Vector3 p = transform.position + (Vector3)sawTravel * ((2.2f+(1-HealthFraction)*1.8f)*1.12f*dt);
            bool x = p.x < r.xMin+radius && sawTravel.x < 0 || p.x > r.xMax-radius && sawTravel.x > 0;
            bool y = p.y < r.yMin+radius && sawTravel.y < 0 || p.y > r.yMax-radius && sawTravel.y > 0;
            if (x) sawTravel.x *= -1;
            if (y) sawTravel.y *= -1;
            if (x || y) bounces++;
            transform.position = new Vector3(Mathf.Clamp(p.x,r.xMin+radius,r.xMax-radius),
                Mathf.Clamp(p.y,r.yMin+radius,r.yMax-radius),1);
            return;
        }
        if (holdMemory) return;
        if (district == LevelId.Desert)
        {
            if (circuit == null)
            {
                DesertWormBoss.BuildCircuit(Mathf.Max(1.85f,UnityEngine.Camera.main.orthographicSize*405/720),out circuit,out distances);
                float nearest = float.PositiveInfinity;
                for (int i=0;i<circuit.Length-1;i++)
                {
                    float distance = (circuit[i]-transform.position).sqrMagnitude;
                    if (distance >= nearest) continue;
                    nearest = distance; circuitIndex = i; circuitTravel = distances[i];
                }
            }
            Vector3 before = transform.position;
            transform.position = DesertWormBoss.AdvanceCircuit(circuit,distances,ref circuitTravel,ref circuitIndex,dt*3.3f);
            Memory.FollowWorm(transform.position, transform.position-before);
            return;
        }
        float top = Camera.UpLimit, middle = (top+Camera.DownLimit)*.5f;
        float speed = 4.8f;
        Vector3 perch = new Vector3(0,top-2.4f,1);
        switch (district)
        {
            case LevelId.Thunder: perch.x=ThunderLanceBoss.StarFiringLane(echoPylons); speed=3.3f; break;
            case LevelId.Space: perch.Set(.82f*Mathf.Sin(t*.43f),top-3+.32f*Mathf.Sin(t*.31f),1); speed=3.2f; break;
            case LevelId.Heaven: perch.Set(1.35f*Mathf.Sin(t*.4f),top-3.1f+.35f*Mathf.Cos(t*.6f),1); speed=1.2f; break;
            case LevelId.Sewers: perch.y=CisternHeart.LakeCenter; break;
            case LevelId.Core: perch.y=top-3.45f+1.22f*Mathf.Sin(t*.95f); break;
            case LevelId.Dream: perch.Set(.60f*Mathf.Sin(t*.78f),top-3.45f+.27f*Mathf.Sin(t*1.56f),1); speed=6; break;
            case LevelId.Hell: perch.Set(.70f*Mathf.Sin(t*.58f),top-2.55f,1); speed=3.4f; break;
            case LevelId.Underwater: perch.Set(2.1f*Mathf.Sin(t*.65f),top-2.65f,1); speed=5; break;
            case LevelId.Candyland:
                float width=Mathf.Min(Camera.RightLimit,UnityEngine.Camera.main.orthographicSize*405/720)-1.5f;
                perch.Set(Mathf.Sin(t*.23f)*width*.6f,Mathf.Lerp(middle+1.6f,top-1.7f,.55f+.45f*Mathf.Sin(t*.31f+1)),1);
                break;
            case LevelId.Mushroom:
                float glide=Mathf.PingPong(t/1.6f,1);
                perch.Set(Mathf.Lerp(-1.65f,1.65f,Mathf.SmoothStep(0,1,glide)),top-2.95f-.58f*Mathf.Sin(glide*Mathf.PI),1);
                break;
        }
        transform.position=Vector3.MoveTowards(transform.position,perch,dt*speed);
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
                echoPylons[i] = pylon;
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
        holdMemory=true;
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
        holdMemory=false;
    }

    IEnumerator TurningWheels(int beat)
    {
        int sign = (beat & 1) == 0 ? 1 : -1;
        holdMemory=true;
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
        holdMemory=false;
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
        yield return Wait(1.1f);
        holdMemory = submergedMemory = true;
        Vector3 start = transform.position;
        Vector3 emerge = new Vector3(((beat%3)-1)*2.8f,Camera.UpLimit-3.1f,1);
        yield return Swim(start,start+new Vector3(0,-1.6f,28),.9f);
        transform.position = emerge + Vector3.forward*28;
        yield return Wait(1.1f);
        yield return Swim(transform.position,emerge,.9f);
        submergedMemory = false;
        for(int crown=0;crown<3;crown++)
        {
            int pearls=Dense(34);
            for(int i=0;i<pearls;i++)
                Fire<SimpleRound>(InkPort(0),Down((i+crown*.34f)*360/pearls),2.6f+crown*.43f,.205f,i==0);
            yield return Wait(.55f);
        }
        Vector3 launch = new Vector3(emerge.x,Camera.UpLimit-2.3f,1);
        yield return Swim(emerge,launch,.24f);
        yield return Trace(0,0,.65f);
        for(float elapsed=0,nextWake=0; elapsed<1.75f;)
        {
            if (!Holding)
            {
                elapsed += Time.deltaTime*LevelRun.EnemyTempoScale;
                transform.position = launch + Vector3.down * (elapsed*14);
                if(elapsed>=nextWake)
                {
                    nextWake += .12f;
                    for(int h=-1;h<=1;h+=2)
                        for(int row=0;row<3;row++)
                            Fire<SimpleRound>(InkPort(0),new Vector2(h,.12f+row*.2f),1.85f+row*.55f,.16f,row==0);
                }
            }
            yield return null;
        }
        transform.position=new Vector3(-emerge.x,Camera.UpLimit+3.5f,1);
        holdMemory=false;
        yield return Wait(1.2f);
    }

    IEnumerator Swim(Vector3 from,Vector3 to,float seconds)
    {
        for(float age=0;age<seconds;)
        {
            if(!Holding) age+=Time.deltaTime*LevelRun.EnemyTempoScale;
            transform.position=Vector3.Lerp(from,to,age/seconds);
            yield return null;
        }
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

}
