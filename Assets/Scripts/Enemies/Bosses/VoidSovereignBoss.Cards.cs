using System.Collections;
using UnityEngine;

public sealed partial class VoidSovereignBoss
{
    public int CardIndex { get; private set; }
    static float Across(int i,int count) => count<=1 ? 0 : i/(count-1f)*2-1;
    static HostileTone Other(HostileTone shade) => shade==HostileTone.Bright ? HostileTone.Dark : HostileTone.Bright;

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

    // The upper nibs print mirrored, scalloped outlines. The lower nibs answer
    // on the next beat with a shallow crescent fan between impressions.
    IEnumerator Inkblot(int stanza)
    {
        for (int beat = 0; beat < 6; beat++)
        {
            bool outline = (beat & 1) == 0;
            int count = Dense(outline ? 24 : 10);
            float tilt = 16 * Mathf.Sin(stanza * .9f + (beat / 2) * .95f);
            UseTone(outline ? HostileTone.Dark : HostileTone.Bright);
            for (int nib = 0; nib < 2; nib++)
            {
                int side = nib * 2 - 1;
                Vector3 lip = Port(outline ? nib : nib + 2);
                for (int i = 0; i < count; i++)
                {
                    if (outline)
                    {
                        float a = i * 2 * Mathf.PI / count;
                        float radius = 1.65f + .34f * Mathf.Cos(3*a);
                        Vector2 lobe = new Vector2(side * Mathf.Sin(a) * radius, Mathf.Cos(a) * radius * .55f);
                        Vector2 velocity = CombatVisuals.Rotate(lobe, side * tilt) + new Vector2(-side * .34f,-3.25f);
                        Fire<SimpleRound>(lip,velocity,velocity.magnitude,.17f,i==0);
                    }
                    else
                    {
                        float angle = side * (tilt + 58 * Across(i,count));
                        Fire<CrescentRound>(lip,Down(angle),2.7f,.20f,i==0);
                    }
                }
            }
            yield return Wait(Rest(outline ? .98f : 1.15f));
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
