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
            else yield return LevelProjectilePalette.VoidRoute switch
            {
                0 => EmberTalons(stanza), 1 => JacquardEngine(stanza),
                2 => Inkblot(stanza), _ => SolarChoir(stanza)
            };
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

    // Cinder claws rake past each other. Short hot shots catch the preceding slow fingers.
    IEnumerator EmberTalons(int stanza)
    {
        int beats=Mathf.Max(16,Dense(32));
        for(int k=0;k<beats;k++)
        {
            float u=k/(beats-1f);
            for(int claw=0;claw<4;claw++)
            {
                int side=(claw&1)*2-1;
                UseTone(claw<2 ? HostileTone.Dark : HostileTone.Bright);
                float cut=side*(30+73*Mathf.Cos(u*Mathf.PI*2+claw/2f))+stanza*11;
                for(int tine=0;tine<3;tine++)
                    Fire<KunaiRound>(Port(claw),Down(cut+tine*11+(claw<2?0:160)),
                        2.6f+tine*.42f,.18f,tine==0).Bounce(1);
            }
            yield return Wait(3.75f/beats);
        }
    }

    // Four shuttles lay down a sheet of fabric. Green stitches flip ninety degrees
    // on staggered beats, turning the weft into drifting diagonals.
    IEnumerator JacquardEngine(int stanza)
    {
        for(int row=0;row<5;row++)
        {
            int n=Mathf.Max(8,Dense(18));
            for(int port=0;port<4;port++)
            {
                int side=(port&1)*2-1;
                UseTone(port<2 ? HostileTone.Bright : HostileTone.Dark);
                for(int j=0;j<n;j++)
                {
                    float u=j/(n-1f);
                    Vector3 lip=Port(port);
                    Vector2 mark=new Vector2(Mathf.Lerp(-4.3f,4.3f,u),lip.y+side*(u-.5f)*2.1f+(port<2?.66f:-.66f));
                    Vector2 delta=mark-(Vector2)lip;
                    float speed=WorldSpeed(Mathf.Max(.2f,delta.magnitude/.65f));
                    Fire<NeedleRound>(lip,delta,speed,.17f,j==0).Pause(.8f,.24f+.40f*(side<0?u:1-u),
                        CombatRound.Turn.Down,side*(18+6*Mathf.Sin(stanza+row)),4.4f/speed).ShowTurn();
                }
            }
            yield return Wait(Rest(.63f));
        }
    }

    // Two lobes of ink unfold around each nib. Their mirrored outlines exchange
    // concavities on each impression, leaving smaller comet trails through the centre.
    IEnumerator Inkblot(int stanza)
    {
        for(int impression=0;impression<6;impression++)
        {
            int n=Mathf.Max(18,Dense(38));
            for(int p=0;p<4;p++)
            {
                UseTone(p<2 ? HostileTone.Dark : HostileTone.Bright);
                int side=(p&1)*2-1;
                for(int i=0;i<n;i++)
                {
                    float a=i*Mathf.PI*2/n;
                    float r=2+.8f*Mathf.Cos(3*a+impression*.62f);
                    Vector2 v=CombatVisuals.Rotate(new Vector2(side*Mathf.Sin(a)*r,Mathf.Cos(a)*r*.72f),
                        side*(stanza*8+p*21))+new Vector2(-side*.45f,-1.55f);
                    if(p<2) Fire<SimpleRound>(Port(p),v,v.magnitude,.17f,i==0);
                    else Fire<CrescentRound>(Port(p),v,v.magnitude,.20f,i==0);
                }
            }
            yield return Wait(Rest(.65f));
        }
    }

    // Bright voice and dark echo. Five rotating bundles sound successively from
    // the crown, with a faster answer between each pair of sustained notes.
    IEnumerator SolarChoir(int stanza)
    {
        int beats=Mathf.Max(17,Dense(35));
        for(int k=0;k<beats;k++)
        {
            float u=k/(beats-1f);
            for(int p=0;p<4;p++)
            {
                int side=(p&1)*2-1;
                UseTone(p<2 ? HostileTone.Bright : HostileTone.Dark);
                for(int ray=0;ray<5;ray++)
                {
                    float a=ray*72+side*(u*91+12*Mathf.Sin(u*6))+p*18+stanza*13;
                    if(p<2) Fire<KunaiRound>(Port(p),Down(a),3.6f,.17f,ray==0);
                    else Fire<SimpleRound>(Port(p),Down(a),2.2f,.18f,ray==0)
                        .Ramp(1.55f,.8f,.75f);
                }
            }
            yield return Wait(4.25f/beats);
        }
    }
}
