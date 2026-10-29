using System.Collections;
using UnityEngine;

// Hell crab: the final exam, flown as spell cards. Every round leaves a pincer, an
// eye or the mouth, and the top of the arena is never safe.
// Phase one
//   Pincer Scissors: both pincers stream arrows whose aim lines cross over the
//     player and close, answered by widening arrow chevrons from the mouth and a
//     brimstone canopy: mouth arrows thrown up that rebound off the ceiling.
//   Soul Procession: the eyes loose mirrored arcs of slow souls that stop, stare and
//     dart at the player one by one, while braided arrow threads weave down and the
//     canopy keeps the ceiling covered.
// Phase two (one eye lost speeds up the claws)
//   Pincer Inferno: scissors from both pincers twice as often, with chevrons and canopy.
//   Inferno Lace: a three-arm mirrored lace pours from the two pincers themselves,
//     following them as they sweep, while the surviving eye's souls stare.
public sealed partial class HellCrabBoss
{
    static readonly Vector3 Pincer = new Vector3(0, -.32f, -.4f);
    static readonly Vector3 Mouth = new Vector3(0, -.9f, -.4f);
    int BurningEye()
    {
        if (eyes[0] == null)
            return -1;
        int first = (volley / 16) % 2;
        return eyes[first].health > 0 ? first : eyes[1 - first].health > 0 ? 1 - first : -1;
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
            if (!phaseTwo)
            {
                yield return Every(8, 1.6f, n =>
                {
                    Phrase(Scissors(n % 2 == 0 ? 1 : -1));
                    if (n % 2 == 1)
                        Phrase(Chevrons(Hull, Mouth, Dense(6), 4.4f, .11f));
                    else
                        Canopy(n);
                });
                yield return Wait(.8f);
                Phrase(BraidedThreads(90));
                yield return Every(8, 1.6f, n =>
                {
                    for (int e = 0; e < 2; e++)
                        SoulSnipe(e);
                    Canopy(n);
                });
                yield return Wait(.8f);
            }
            else
            {
                yield return Every(7, .9f, n =>
                {
                    Phrase(Scissors(n % 2 == 0 ? 1 : -1));
                    if (n % 2 == 1)
                        Phrase(Chevrons(Hull, Mouth, Dense(7), 4.6f, .1f));
                    else
                        Canopy(n);
                });
                yield return Wait(.7f);
                Phrase(Every(8, 2f, n => SoulSnipe(BurningEye())));
                yield return MirrorLace(claws[0], Pincer, claws[1], Pincer, Dense(110), 3, 75, 18, 3.6f, .065f);
                yield return Wait(.7f);
            }
        }
    }

    // Brimstone canopy: arrows thrown up from the mouth that rebound off the ceiling.
    void Canopy(int n)
    {
        int count = Dense(9);
        var mouth = Hull.TransformPoint(Mouth);
        for (int i = 0; i < count; i++)
            for (int mirror = -1; mirror <= 1; mirror += 2)
                Fire<NeedleRound>(mouth, CombatVisuals.Rotate(Vector2.up, mirror * (6 + i * 9 + (n % 2) * 4)), 3.6f, .19f).Bounce(1);
    }

    // Each pincer sweeps its stream across the player's position, so the two lines cross and close.
    IEnumerator Scissors(int sign)
    {
        int shots = Dense(phaseTwo ? 15 : 12);
        for (int i = 0; i < shots; i++)
        {
            float t = i / (shots - 1f);
            for (int side = 0; side < 2; side++)
            {
                if (claws[side] == null)
                    continue;
                var port = claws[side].TransformPoint(Pincer);
                if (player != null && Vector2.Distance(port, player.position) < 1.8f)
                    continue;
                float sweep = Mathf.Lerp(22, -22, t) * (side == 0 ? 1 : -1) * sign;
                Fire<NeedleRound>(port, CombatVisuals.Rotate(CombatVisuals.Aim(port), sweep), 4.3f, .19f);
            }
            yield return Wait(Rest(.075f));
        }
    }

    // Slow souls that stop, stare, then dart at the player one after another.
    void SoulSnipe(int eye)
    {
        if (eye < 0 || eyes[eye] == null || eyes[eye].health <= 0)
            return;
        int souls = Dense(phaseTwo ? 9 : 7);
        var port = eyes[eye].transform.position;
        for (int i = 0; i < souls; i++)
        {
            float lane = i - (souls - 1) * .5f;
            Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, lane * 120f / Mathf.Max(1, souls - 1)), 1.7f, .36f)
                .Pause(.85f, .45f + i * .24f, CombatRound.Turn.Aim, 0, 2.5f);
        }
    }

    // Two eyes stream in antiphase: their threads braid around each other on the way down.
    IEnumerator BraidedThreads(int beads)
    {
        beads = Dense(beads);
        for (int i = 0; i < beads; i++)
        {
            for (int e = 0; e < 2; e++)
                if (eyes[e] != null && eyes[e].health > 0)
                {
                    var p = eyes[e].transform.position;
                    Fire<NeedleRound>(p, CombatVisuals.Rotate(Vector2.down, (e == 0 ? 1 : -1) * 9), 2.8f, .19f)
                        .Sway(e == 0 ? .7f : -.7f, .7f);
                }
            yield return Wait(Rest(.085f));
        }
    }
}
