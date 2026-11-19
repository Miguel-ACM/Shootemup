using System.Collections;
using UnityEngine;

public sealed partial class VoidSovereignBoss
{
    public int CardIndex { get; private set; }
    PulseLaser VoidRay(int port,Vector2 axis,float width,float hold,float sweep=0)
    {
        var beam=HeavenArmory.Lance(body.Emitter(port),Vector3.zero,axis,width,hold,1.25f);
        if(beam!=null)beam.sweep=sweep;
        return beam;
    }
    static float Across(int i,int count)=>count<=1?0:i/(count-1f)*2-1;
    // Tones (Miguel, 9 November 2026). Each card's body takes one tone; its replies
    // and giant crowns take the other, and the body flips card to card:
    //   Witnesses bright, Funeral Psalm dark; Severing bright, Cut the Firmament dark;
    //   Hunger bright, False Stars dark (its comets and their shrapnel).
    //   The final form mixes densely: its opposed pairs alternate tones beat by beat,
    //   diagonal banks dark and vertical banks bright in World Without Edges, the
    //   reverse in Last Equation. Rays stay bright.
    static HostileTone Other(HostileTone shade)=>shade==HostileTone.Bright?HostileTone.Dark:HostileTone.Bright;
    // Void's mix (Miguel, November 2026: more moons, kunai and giant circles): the
    // circular lanes are moons, the arrow lanes kunai or chevrons, and lone giant orbs
    // take the crowns.
    CombatRound CrownedShot(bool giant,bool circular,Vector3 port,Vector2 axis,float speed,float radius=.17f,float crown=.60f,bool chevron=false)
        =>giant?(CombatRound)Fire<GiantOrb>(port,axis,speed*.9f,crown):
            circular?(CombatRound)Fire<CrescentRound>(port,axis,speed,radius+.03f):
            chevron?Fire<ChevronRound>(port,axis,speed,radius):Fire<KunaiRound>(port,axis,speed,radius);
    // No Void round is aimed (Miguel, November 2026). A reply is a mirrored moon fan from
    // both eyes that crosses beneath the body on a heading sweeping slowly with the beat,
    // so a resting pilot is crossed on a schedule rather than tracked.
    void SweepingReply(int beat,int count,float speed,float radius)
    {
        float angle=10+22*Mathf.Abs(Mathf.Sin(beat*.37f));
        for(int port=0;port<2;port++)
        {
            int side=port==0?-1:1;
            var axis=CombatVisuals.Rotate(Vector2.down,-side*angle);
            for(int i=0;i<count;i++)
                Fire<CrescentRound>(Port(port),CombatVisuals.Rotate(axis,Across(i,count)*6),speed,radius);
        }
    }
    static int BankCount(int count,int bank)
    {
        int upper=Mathf.Max(1,Mathf.RoundToInt(count*.55f));
        return bank>0?upper:2*count-upper;
    }
    protected override IEnumerator Cards()
    {
        if (Memory != null)
        {
            yield return MemoryCard();
            yield break;
        }
        for (int stanza = 0; ; stanza++)
        {
            CardIndex = stanza & 1;
            yield return Teleport();
            yield return Prime(.42f);
            if (Form == 2) yield return ScissorScript(stanza);
            else if (Form == 3) yield return SwallowingSky(stanza);
            else yield return WorldWithoutEdges();
            yield return Wait(Rest(.36f));
        }
    }

    // Two hands write opposite sweeping blades. A blade thickens at its tip,
    // and the reflected teeth overtake it on the return stroke.
    IEnumerator ScissorScript(int stanza)
    {
        int cuts = Mathf.Max(18, Dense(40));
        for (int i=0;i<cuts;i++)
        {
            float u=i/(cuts-1f);
            for(int hand=0;hand<2;hand++)
            {
                int sign=hand*2-1;
                UseTone(hand==0 ? HostileTone.Bright : HostileTone.Dark);
                float turn=sign*(stanza*17 + 115*Mathf.Cos(u*Mathf.PI*2));
                for(int arm=0;arm<3;arm++)
                {
                    var axis=Down(turn+arm*120);
                    Fire<KunaiRound>(Port(hand),axis,2.3f+.7f*u,.18f,arm==0);
                    if(i%2==0)
                        Fire<SimpleRound>(Port(hand),CombatVisuals.Rotate(axis,sign*8),3.5f,.16f).Bounce(1);
                }
            }
            yield return Wait(3.8f/cuts);
        }
    }

    // Paired jaws throw scalloped fronts with opposing peaks, then close the bite.
    // The slow dots pivot away while the sharp teeth pass between them.
    IEnumerator SwallowingSky(int stanza)
    {
        for(int bite=0;bite<6;bite++)
        {
            int marks=Mathf.Max(16,Dense(42));
            for(int jaw=0;jaw<2;jaw++)
            {
                int sign=jaw*2-1;
                UseTone(jaw==0 ? HostileTone.Dark : HostileTone.Bright);
                for(int i=0;i<marks;i++)
                {
                    float a=360f*i/marks;
                    float ripple=Mathf.Cos((5*a + sign*(stanza*19+bite*27))*Mathf.Deg2Rad);
                    var axis=Down(a + sign*bite*5);
                    if ((bite&1)==0)
                        Fire<SimpleRound>(Port(jaw),axis,2.6f+.87f*ripple,.19f,i==0)
                            .Pause(.78f,.22f,CombatRound.Turn.Rotate,sign*38,1.28f);
                    else Fire<KunaiRound>(Port(jaw),axis,3.15f+.94f*ripple,.18f,i==0);
                }
            }
            yield return Wait(Rest(.52f));
        }
    }

    IEnumerator WorldWithoutEdges()=>Every(6.2f,.52f,beat=>
    {
        if(beat%6==5)return;
        if(beat%5==0)Phrase(CrossedGaze(HostileTone.Bright));
        int count=Dense(9);bool crown=beat%3==0;
        for(int port=0;port<4;port++)
        {
            if((port<2?0:1)!=beat%2)continue;
            var bankTone=port<2?HostileTone.Dark:HostileTone.Bright;
            int rows=FractureCount(count,port);
            for(int i=0;i<rows;i++)
            {
                Vector2 axis=FractureAxis(port);
                float u=Across(i,rows),sign=port%2==0?-1:1;
                UseTone(crown&&i==rows-1?Other(bankTone):bankTone);
                CrownedShot(crown&&i==rows-1,beat%3==0,Port(port),CombatVisuals.Rotate(axis,u*36+sign*Mathf.Sin(beat*.25f)*19),
                    2.0f+.48f*(1-u*u),.17f,.64f).Pause(.95f,.22f,CombatRound.Turn.Rotate,-sign*24,1.0f).FadeAt(5.6f);
            }
        }
    });

    IEnumerator LastEquation()=>Every(6.2f,.9f,beat=>
    {
        if(beat%5==4)return;
        if(beat%3==0)Phrase(CrossedGaze(HostileTone.Dark));
        int count=Dense(11);bool crown=beat%3==0;
        for(int port=0;port<4;port++)
        {
            if((port<2?0:1)!=beat%2)continue;
            var bankTone=port<2?HostileTone.Bright:HostileTone.Dark;
            int rows=FractureCount(count,port);
            for(int i=0;i<rows;i++)
            {
                Vector2 axis=FractureAxis(port);
                float u=Across(i,rows),sign=port%2==0?-1:1;
                // Ricochets cross the earlier bank after a rest. Giant tips travel
                // straight out, so they cannot rebound into the next giant front.
                var direction=CombatVisuals.Rotate(axis,u*33+sign*(beat%2==0?9:-9));
                float speed=1.8f+.8f*Mathf.Abs(u);
                UseTone(crown&&i==rows-1?Other(bankTone):bankTone);
                if(crown&&i==rows-1)
                    Fire<GiantOrb>(Port(port),direction,speed*.9f,.66f).Ramp(1.3f,1.8f,1.4f).FadeAt(5.6f);
                else
                    Fire<RicochetBolt>(Port(port),direction,speed,.18f).Bounce(1).Ramp(1.3f,1.8f,1.4f).FadeAt(5.6f);
            }
        }
    });

    static int FractureCount(int count,int port)=>port<2?count:BankCount(count,port==2?1:-1);
    static Vector2 FractureAxis(int port)=>port==0?CombatVisuals.Rotate(Vector2.down,-42):
        port==1?CombatVisuals.Rotate(Vector2.down,42):port==2?Vector2.up:Vector2.down;

    // Fixed crossfire for the final form (no aiming): the side tips charge visibly, then
    // cross kunai, a moon at the heart of each chord, through a fixed point above the
    // star that steps left, centre and right in turn. It closes the space above and on
    // the body on a schedule, never by tracking the pilot.
    int crossings;
    IEnumerator CrossedGaze(HostileTone shade)
    {
        int step=crossings++%3;
        for(float t=0;t<.45f;)
        {
            if(!Holding)t+=Time.deltaTime*LevelRun.EnemyTempoScale;
            Cue(Port(0),.45f-t);Cue(Port(1),.45f-t);
            yield return null;
        }
        Vector3 cross=transform.position+new Vector3((step-1)*1.6f,step==1?2.6f:1.7f,0);
        int count=Dense(3)|1;
        UseTone(shade);
        for(int port=0;port<2;port++)
        {
            Vector3 p=Port(port);
            Vector2 line=((Vector2)(cross-p)).normalized;
            for(int i=0;i<count;i++)
            {
                if(i==count/2)
                    Fire<CrescentRound>(p,line,2.6f,.22f).FadeAt(6);
                else
                    Fire<KunaiRound>(p,CombatVisuals.Rotate(line,Across(i,count)*8),2.8f,.19f).FadeAt(6);
            }
        }
    }
}
