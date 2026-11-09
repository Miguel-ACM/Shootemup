using System.Collections;
using UnityEngine;

// Hell crab: the final exam, flown as spell cards. Every round leaves a pincer, an
// eye or the mouth, and the top of the arena is never safe.
// The claws already sweep in from the sides, so every card layers at most two ideas.
// Phase one
//   Pincer Scissors: both pincers stream arrows whose aim lines cross over the
//     player and close, answered by widening arrow chevrons from the mouth.
//   Soul Procession: the eyes loose mirrored arcs of slow souls that stop, stare and
//     dart at the player one by one, under a brimstone canopy: mouth arrows thrown
//     up that rebound off the ceiling.
// Phase two (half the shared head health speeds up the claws)
//   Pincer Inferno: scissors from both pincers more often, alternating with the canopy.
//   Inferno Lace: a three-arm mirrored lace pours from the two pincers themselves,
//     following them as they sweep, while the eyes' souls stare.
// Tones (Miguel, 9 November 2026), the dominant tone flipping card to card:
//   Pincer Scissors: dark scissor streams, bright mouth chevrons.
//   Soul Procession: bright souls under a dark brimstone canopy.
//   Pincer Inferno: bright scissors alternating with a dark canopy.
//   Inferno Lace: a dark pincer lace while bright souls stare.
//   Inferno (Hard): a bright lace crossed by dark scissors and dark souls.
public sealed partial class HellCrabBoss
{
    static readonly Vector3 Pincer = new Vector3(0, -.32f, -.4f);
    static readonly Vector3 Mouth = new Vector3(0, -.9f, -.4f);
    int BurningEye()
    {
        if (eyes[0] == null)
            return -1;
        return (volley / 16) % 2;
    }

    protected override void Attack()
    {
        if (claws[0] != null)
            OpenCards();
    }

    protected override IEnumerator Cards()
    {
        while (true)
        {
            if (phaseThree)
                yield return FinalSpell();
            else if (!phaseTwo)
            {
                yield return Every(8, 1.6f, n =>
                {
                    if (n % 2 == 0)
                        Phrase(Scissors(n % 4 == 0 ? 1 : -1, HostileTone.Dark));
                    else
                        Phrase(Toned(HostileTone.Bright, Chevrons(Hull, Mouth, Dense(6), 4.4f, .11f)));
                });
                yield return Wait(.8f);
                yield return Every(8, 1.6f, n =>
                {
                    if (n % 2 == 0)
                        for (int e = 0; e < 2; e++)
                            SoulSnipe(e, HostileTone.Bright);
                    else
                        Canopy(n);
                });
                yield return Wait(.8f);
            }
            else
            {
                yield return Every(7, 1.1f, n =>
                {
                    if (n % 3 != 2)
                        Phrase(Scissors(n % 2 == 0 ? 1 : -1, HostileTone.Bright));
                    else
                        Canopy(n);
                });
                yield return Wait(.8f);
                Phrase(Every(8, 2.6f, n => SoulSnipe(BurningEye(), HostileTone.Bright)));
                yield return Toned(HostileTone.Dark, MirrorLace(claws[0], Pincer, claws[1], Pincer, Dense(90), 3, 75, 18, 3.6f, .07f));
                yield return Wait(.8f);
            }
        }
    }

    // Runs a shared helper phrase in one tone, re-applied before every step because
    // concurrent phrases share the tone field.
    IEnumerator Toned(HostileTone shade, IEnumerator phrase)
    {
        while (true)
        {
            UseTone(shade);
            if (!phrase.MoveNext())
                yield break;
            yield return phrase.Current;
        }
    }

    // Brimstone canopy: arrows thrown up from the mouth that rebound off the ceiling.
    // Always dark.
    void Canopy(int n)
    {
        UseTone(HostileTone.Dark);
        int count = Dense(9);
        var mouth = Hull.TransformPoint(Mouth);
        for (int i = 0; i < count; i++)
            for (int mirror = -1; mirror <= 1; mirror += 2)
                ScoredShot(i%3==0,mouth, CombatVisuals.Rotate(Vector2.up, mirror * (6 + i * 9 + (n % 2) * 4)), 3.6f).Bounce(1);
    }

    // Each pincer sweeps its stream across the player's position, so the two lines cross and close.
    IEnumerator Scissors(int sign, HostileTone shade)
    {
        int shots = Dense(phaseTwo ? 15 : 12)|1;
        for (int i = 0; i < shots; i++)
        {
            float t = i / (shots - 1f);
            UseTone(shade);
            for (int side = 0; side < 2; side++)
            {
                if (claws[side] == null)
                    continue;
                var port = claws[side].TransformPoint(Pincer);
                if (player != null && Vector2.Distance(port, player.position) < 1.8f)
                    continue;
                float sweep = Mathf.Lerp(22, -22, t) * (side == 0 ? 1 : -1) * sign;
                ScoredShot(i%3==shots/2%3,port, CombatVisuals.Rotate(CombatVisuals.Aim(port), sweep), 4.3f);
            }
            yield return Wait(Rest(.075f));
        }
    }

    // Slow souls that stop, stare, then dart at the player one after another.
    void SoulSnipe(int eye, HostileTone shade)
    {
        if (eye < 0 || eyes[eye] == null || health <= 0)
            return;
        UseTone(shade);
        int souls = Dense(phaseTwo ? 9 : 7);
        var port = eyes[eye].position;
        for (int i = 0; i < souls; i++)
        {
            float lane = i - (souls - 1) * .5f;
            Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, lane * 120f / Mathf.Max(1, souls - 1)), 1.7f, .36f)
                .Pause(.85f, .45f + i * .24f, CombatRound.Turn.Aim, 0, 2.5f);
        }
    }

    // Hard final phase, Inferno: a three-arm lace from the moving pincers with one
    // answer at a time through it, scissors closing, then the firing eye's souls.
    IEnumerator FinalSpell()
    {
        Phrase(Every(9, 1.4f, n =>
        {
            if (n % 2 == 0)
                Phrase(Scissors(n % 4 == 0 ? 1 : -1, HostileTone.Dark));
            else
                SoulSnipe(BurningEye(), HostileTone.Dark);
        }));
        yield return Toned(HostileTone.Bright, MirrorLace(claws[0], Pincer, claws[1], Pincer, Dense(110), 3, 80, 18, 3.8f, .065f));
        yield return Wait(.8f);
    }
}
