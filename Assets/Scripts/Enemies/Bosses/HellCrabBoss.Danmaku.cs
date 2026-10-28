using System.Collections;
using UnityEngine;

// Hell crab: the final exam. Its pincers stream scissoring needles whose aim
// lines cross over the player and close; its eyes loose slow souls that stop,
// stare and then dart at the player one by one; both eyes weave braided
// threads in antiphase; in phase two the claws hurl embers that hang at the
// top of their arc and fall as a cage of fast and slow columns.
public sealed partial class HellCrabBoss
{
    static readonly Vector3 Pincer = new Vector3(0, -.32f, -.4f);
    int BurningEye()
    {
        if (eyes[0] == null)
            return -1;
        int first = (volley / 16) % 2;
        return eyes[first].health > 0 ? first : eyes[1 - first].health > 0 ? 1 - first : -1;
    }

    protected override void Attack()
    {
        if (claws[0] == null)
            return;
        int beat = (volley - 1) % 16;
        if (beat == 0 || beat == 5)
            Phrase(Scissors(beat == 0 ? 1 : -1));
        else if (beat == 8)
            SoulSnipe(BurningEye());
        else if (beat == 11)
            Phrase(BraidedThreads());
        else if (phaseTwo && (beat == 3 || beat == 14))
            EmberCage();
        else if (phaseTwo && beat == 13)
            SoulSnipe(1 - Mathf.Max(0, BurningEye()));
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
    IEnumerator BraidedThreads()
    {
        int beads = Dense(phaseTwo ? 20 : 15);
        for (int i = 0; i < beads; i++)
        {
            for (int e = 0; e < 2; e++)
                if (eyes[e] != null && eyes[e].health > 0)
                {
                    var p = eyes[e].transform.position;
                    Fire<SimpleRound>(p, CombatVisuals.Rotate(Vector2.down, (e == 0 ? 1 : -1) * 9), 2.6f, .2f)
                        .Sway(e == 0 ? .7f : -.7f, .7f);
                }
            yield return Wait(Rest(.085f));
        }
    }

    // Embers thrown up from the claws stop at their apex, then fall as alternating fast and slow columns.
    void EmberCage()
    {
        int embers = Dense(8);
        for (int side = 0; side < 2; side++)
        {
            var port = claws[side].TransformPoint(Pincer);
            for (int i = 0; i < embers; i++)
            {
                float lane = i - (embers - 1) * .5f;
                Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.up, lane * 11 + (side == 0 ? 20 : -20)), 3.2f, .21f)
                    .Pause(.6f, .35f, CombatRound.Turn.Down, 0, i % 2 == 0 ? .55f : 1.05f);
            }
        }
    }

    protected override void PrepareScore(float seconds)
    {
        if (claws[0] == null)
            return;
        int beat = volley % 16;
        if (beat == 0 || beat == 5 || phaseTwo && (beat == 3 || beat == 14))
        {
            Cue(claws[0].TransformPoint(Pincer), seconds);
            Cue(claws[1].TransformPoint(Pincer), seconds);
        }
        else if (beat == 8 || beat == 11 || phaseTwo && beat == 13)
            for (int e = 0; e < 2; e++)
                if (eyes[e].health > 0)
                    Cue(eyes[e].transform.position, seconds);
    }
}
