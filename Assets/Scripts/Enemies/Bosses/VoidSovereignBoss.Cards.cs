using System.Collections;
using UnityEngine;

public sealed partial class VoidSovereignBoss
{
    public int CardIndex { get; private set; }
    PulseLaser VoidRay(int port,Vector2 axis,float width,float hold,float sweep=0)
    {
        var beam=HeavenArmory.Lance(Hull,LocalPort(port),axis,width,hold,1.25f);
        if(beam!=null)beam.sweep=sweep;
        return beam;
    }
    protected override IEnumerator Cards()
    {
        while(true)
        {
            CardIndex=0;yield return Prime(.85f);
            if(Form==1)yield return Witnesses();
            else if(Form==2)yield return Severing();
            else if(Form==3)yield return Hunger();
            else yield return WorldWithoutEdges();
            yield return Wait(Rest(1.25f));CardIndex=1;yield return Prime(.85f);
            if(Form==1)yield return FuneralPsalm();
            else if(Form==2)yield return CutTheFirmament();
            else if(Form==3)yield return FalseStars();
            else yield return LastEquation();
            yield return Wait(Rest(1.4f));
        }
    }
    IEnumerator Witnesses()=>Every(7.2f,.72f,n=>
    {
        // Alternating upward/downward witnesses; rising shells open across the ceiling.
        for(int p=0;p<2;p++)
        {
            int sign=p==0?-1:1,count=Dense(9);var eye=Port(p);
            for(int i=0;i<count;i++)
            {
                float a=(i-(count-1)*.5f)*14+sign*(n%2)*7;
                Fire<NeedleRound>(eye,CombatVisuals.Rotate(n%2==0?Vector2.up:Vector2.down,a),2.5f,.18f).Ramp(1.4f,.8f,1).FadeAt(7);
                if(i%2==0)Fire<SimpleRound>(eye,CombatVisuals.Rotate(Vector2.down,a+sign*12),1.45f,.23f).Pause(.85f,.45f,CombatRound.Turn.Rotate,sign*25,1.5f).FadeAt(8);
            }
            if(n%2==0)
            {
                var shell=Fire<BurstShell>(eye,CombatVisuals.Rotate(Vector2.up,sign*(24+n%3*15)),2.5f,.32f);
                if(shell!=null)shell.SetTravelDistance(1.9f);
            }
        }
    });
    IEnumerator FuneralPsalm()=>Every(7.5f,.94f,n=>
    {
        // Hanging pearls accelerate into crossed lanes; huge tears rebound from above.
        for(int p=0;p<2;p++)
        {
            int s=p==0?-1:1,count=Dense(7);var eye=Port(p);
            for(int i=0;i<count;i++)
                Fire<SuspendedRound>(eye,CombatVisuals.Rotate(n%2==0?Vector2.up:Vector2.down,s*(16+i*13)),3.0f,.22f).Bounce(1).FadeAt(7.8f);
            if(n%2==0)Fire<GiantOrb>(eye,CombatVisuals.Rotate(Vector2.up,s*(30+n%3*12)),1.6f,.52f).Bounce(1).SplitAt(3.0f,Dense(11),110,2.5f,.15f).FadeAt(8);
        }
    });
    IEnumerator Severing()=>Every(7.2f,.72f,n=>
    {
        // Hooked returning blades shear the lower arena; ceiling-bound ricochets answer them.
        for(int p=0;p<2;p++)
        {
            int s=p==0?-1:1;var blade=Port(p);
            for(int i=0;i<Dense(4);i++)
                Fire<ReturningDisc>(blade,CombatVisuals.Rotate(Vector2.down,-s*(22+i*16+n%3*8)),2.1f,.25f).FadeAt(5.5f);
            if(n%2==0)for(int i=0;i<Dense(7);i++)
                Fire<RicochetBolt>(blade,CombatVisuals.Rotate(Vector2.up,s*(22+i*10)),2.8f,.18f).Bounce(1).FadeAt(7.5f);
        }
    });
    IEnumerator CutTheFirmament()=>Every(7.2f,1.8f,n=>
    {
        // The cut alternates above and below the scythes, leaving a charged, readable crossing.
        for(int p=0;p<2;p++)
        {
            int s=p==0?-1:1;Vector2 axis=n%2==0?Vector2.up:Vector2.down;
            VoidRay(p,CombatVisuals.Rotate(axis,s*28),.46f,1.25f,-s*9);
            for(int i=0;i<Dense(4);i++)
            {
                var direction=CombatVisuals.Rotate(-axis,s*(18+i*18));
                var first=Fire<HelixRound>(Port(p),direction,2.4f,.17f).FadeAt(6.5f);
                var second=Fire<HelixRound>(Port(p),direction,2.4f,.17f).FadeAt(6.5f);
                if(first!=null)first.phase=0;if(second!=null)second.phase=Mathf.PI;
            }
        }
    });
    IEnumerator Hunger()=>Every(7.2f,.90f,n=>
    {
        // Breakable pressure masses, committed hunters and upward teeth demand different responses.
        for(int p=0;p<2;p++)
        {
            int s=p==0?-1:1;var jaw=Port(p);
            Fire<PlasmaOrb>(jaw,CombatVisuals.Rotate(Vector2.down,s*(20+n%3*17)),1.3f,.38f).Ramp(1.7f,1.2f,1.0f).FadeAt(8);
            if(n%2==0)for(int i=0;i<Dense(3);i++)
                Fire<GuidedMissile>(jaw,CombatVisuals.Rotate(Vector2.up,s*(12+i*28)),2.2f,.18f).FadeAt(7);
            for(int i=0;i<Dense(8);i++)
                Fire<NeedleRound>(jaw,CombatVisuals.Rotate(Vector2.up,s*(5+i*12+n%2*6)),2.8f,.19f).Fall(Vector2.down*.52f).FadeAt(8);
        }
    });
    IEnumerator FalseStars()=>Every(7.2f,1.2f,n=>
    {
        // The mouth spits mines toward the ceiling and carriers through the lower lanes.
        for(int p=0;p<2;p++)
        {
            int s=p==0?-1:1;var jaw=Port(p);
            Fire<ProximityMine>(jaw,CombatVisuals.Rotate(Vector2.up,s*(18+n%3*16)),4.8f,.24f);
            Fire<ScatterCarrier>(jaw,CombatVisuals.Rotate(Vector2.down,s*(20+n%2*24)),1.55f,.31f).FadeAt(7);
            if(n%2==0)Fire<GiantOrb>(jaw,CombatVisuals.Rotate(Vector2.up,s*(50+n*8)),1.4f,.48f).Pause(.95f,.55f,CombatRound.Turn.Rotate,s*80,2.0f).SplitAt(3.2f,Dense(9),120,2.4f,.16f);
        }
    });
    IEnumerator WorldWithoutEdges()=>Every(7.2f,.72f,n=>
    {
        // Four outward-facing fracture tips write folding sheets into every quadrant.
        for(int p=0;p<4;p++)
        {
            Vector2 axis=p==0?Vector2.left:p==1?Vector2.right:p==2?Vector2.up:Vector2.down;
            int sign=p%2==0?-1:1,count=Dense(11);
            for(int i=0;i<count;i++)
            {
                float angle=(i-(count-1)*.5f)*11+Mathf.Sin(n*.7f)*11;
                Fire<NeedleRound>(Port(p),CombatVisuals.Rotate(axis,angle),2.0f,.18f).Pause(.85f,.30f,CombatRound.Turn.Rotate,sign*52,1.55f).Bounce(1).FadeAt(7.5f);
                if(n%2==0&&i%3==0)Fire<SimpleRound>(Port(p),CombatVisuals.Rotate(axis,angle+5),1.2f,.26f).Zigzag(.85f,18).FadeAt(7);
            }
        }
        if(n==1||n==6){VoidRay(0,CombatVisuals.Rotate(Vector2.up,-42),.38f,1.6f);VoidRay(1,CombatVisuals.Rotate(Vector2.up,42),.38f,1.6f);}
    });
    IEnumerator LastEquation()=>Every(7.2f,1.02f,n=>
    {
        // Returning blades cross fast reflected teeth and slow, splitting stars.
        for(int p=0;p<4;p++)
        {
            Vector2 axis=p==0?Vector2.left:p==1?Vector2.right:p==2?Vector2.up:Vector2.down;
            int sign=p%2==0?-1:1;
            for(int i=0;i<Dense(6);i++)
                Fire<RicochetBolt>(Port(p),CombatVisuals.Rotate(axis,(i-2.5f)*15+sign*n%3*7),2.5f,.19f).Bounce(1).FadeAt(7);
            if(n%2==0)Fire<GiantOrb>(Port(p),axis,1.35f,.46f).Pause(1.0f,.5f,CombatRound.Turn.Rotate,sign*65,1.5f).SplitAt(3.0f,Dense(12),140,2.7f,.15f);
            else Fire<ReturningDisc>(Port(p),CombatVisuals.Rotate(axis,sign*25),2.7f,.30f).FadeAt(5.5f);
            if(p<2&&n%2==0)Fire<PlasmaOrb>(Port(p),CombatVisuals.Rotate(Vector2.up,sign*25),1.8f,.32f).Bounce(1).FadeAt(7);
        }
    });
}
