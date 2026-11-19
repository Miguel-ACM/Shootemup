using System.Collections;
using UnityEngine;

public sealed partial class CandyRegentBoss
{
    protected override void Attack() { if (arrived) OpenCards(); }

    protected override IEnumerator Cards()
    {
        for (int verse = 0; ; verse++)
        {
            int side = (verse & 1) == 0 ? 1 : -1;
            Fixate(new Vector3(side * 2.8f, Camera.DownLimit + 2));
            yield return ChargeWands(.32f);
            if (phaseThree) yield return SpunSugar(verse);
            else if (phaseTwo) yield return RibbonPress(verse);
            else yield return Valentine(verse);
            LookAway();
            yield return Wait(Rest(phaseThree ? .36f : .52f));
        }
    }

    IEnumerator ChargeWands(float seconds)
    {
        for (float t = 0; t < seconds;)
        {
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            Cue(WandPort(-1), seconds - t);
            Cue(WandPort(1), seconds - t);
            yield return null;
        }
    }

    // Each wand prints a complete heart. The paired prints travel across one another,
    // followed by a thin reply that ricochets off the ceiling.
    IEnumerator Valentine(int verse)
    {
        for (int impression = 0; impression < 3; impression++)
        {
            Beat();
            int dots = Mathf.Max(16, Dense(44));
            for (int hand = -1; hand <= 1; hand += 2)
            {
                UseTone(hand < 0 ? HostileTone.Dark : HostileTone.Bright);
                Vector3 port = WandPort(hand);
                for (int i = 0; i < dots; i++)
                {
                    float t = 2 * Mathf.PI * i / dots;
                    float s = Mathf.Sin(t);
                    Vector2 heart = new Vector2(16*s*s*s, 13*Mathf.Cos(t)-5*Mathf.Cos(2*t)-2*Mathf.Cos(3*t)-Mathf.Cos(4*t)) * .135f;
                    Vector2 drift = new Vector2(-hand * (.55f + .25f*impression), -2.05f);
                    Vector2 v = drift + CombatVisuals.Rotate(heart, hand * (verse%5 - 2) * 9);
                    Fire<SimpleRound>(port,v,v.magnitude,.18f,i==0);
                }
            }
            CrownStitch(verse + impression);
            yield return Wait(Rest(.66f));
        }
    }

    // Ribbon candy is folded over the two wands. The flat ends hang for one beat,
    // then fall through the folds at alternating inclinations.
    IEnumerator RibbonPress(int verse)
    {
        int pleats = Mathf.Max(8, Dense(22));
        for (int fold = 0; fold < 3; fold++)
        {
            for (int i = 0; i < pleats; i++)
            {
                float u = i / (pleats - 1f);
                for (int hand = -1; hand <= 1; hand += 2)
                {
                    Vector3 port = WandPort(hand);
                    Vector2 mark = new Vector2(hand * Mathf.Lerp(-3.9f,4.2f,u),
                        Camera.UpLimit - 2.25f - fold*.72f + .68f*Mathf.Cos(u*2*Mathf.PI + verse*.75f));
                    Vector2 v = mark - (Vector2)port;
                    float speed = WorldSpeed(Mathf.Max(.15f,v.magnitude/.65f));
                    UseTone(fold%2==0 ? HostileTone.Bright : HostileTone.Dark);
                    Fire<NeedleRound>(port,v,speed,.17f,i==0)
                        .Pause(.8f,.25f + .44f*(1-u),CombatRound.Turn.Down,hand*(9+6*fold),4.8f/speed).ShowTurn();
                }
                yield return Wait(.55f/pleats);
            }
            Beat();
            CrownStitch(verse + fold);
        }
    }

    // Pulled sugar winds around five spokes; successive turns wind in opposite
    // directions. Slower droplets advance between the sharp teeth from the pupil.
    IEnumerator SpunSugar(int verse)
    {
        Thrill();
        int accents = Mathf.Max(14,Dense(30));
        for (int k=0;k<accents;k++)
        {
            float u=k/(accents-1f);
            for (int hand=-1;hand<=1;hand+=2)
            {
                UseTone(hand<0 ? HostileTone.Dark : HostileTone.Bright);
                for (int spoke=0;spoke<5;spoke++)
                {
                    float angle=72*spoke + hand*(verse*13 + 89*Mathf.Sin(u*Mathf.PI));
                    Fire<NeedleRound>(WandPort(hand),CombatVisuals.Rotate(Vector2.down,angle),
                        2.55f + .65f*u,.17f,spoke==0).Ramp(1.32f,.8f,.65f);
                }
            }
            if (k%4==0)
            {
                Beat();
                for(int n=0;n<7;n++)
                    Fire<SimpleRound>(EyePort,CombatVisuals.Rotate(Vector2.down,(n-3)*19+12*Mathf.Sin(verse+u*5)),3.85f,.22f,n==3);
            }
            yield return Wait(2.65f/accents);
        }
    }

    void CrownStitch(int beatIndex)
    {
        for (int hand=-1;hand<=1;hand+=2)
        {
            UseTone(HostileTone.Bright);
            for (int tooth=0;tooth<Dense(6);tooth++)
                Fire<NeedleRound>(CrownPort(hand),CombatVisuals.Rotate(Vector2.up,hand*(16+9*tooth+6*Mathf.Sin(beatIndex))),
                    3.8f,.17f,tooth==0).Bounce(1);
        }
    }
}
