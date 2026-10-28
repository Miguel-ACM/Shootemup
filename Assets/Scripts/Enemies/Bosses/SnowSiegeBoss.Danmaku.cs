using System.Collections;
using UnityEngine;

// Snow fortress: time freezes around it. Fans fly out, stop dead and glint,
// then crawl back toward the player; comb pillars start almost still and
// accelerate; a blizzard flings rounds sideways that halt and change course.
// Its exploding shells remain, and their fragments now hang and fall as snow.
public sealed partial class SnowSiegeBoss
{
    Vector3 Mouth => Hull.TransformPoint(new Vector3(0, -.65f, -.25f));
    protected override void Attack()
    {
        int beat = (volley - 1) % 14;
        if (beat == 0 || phaseTwo && beat == 12)
            FreezeFrame(beat == 0 ? 1 : -1);
        else if (beat == 2)
            CrystalShells();
        else if (beat == 4)
            DeployComb();
        else if (beat == 5 || beat == 7)
            CombPillars(beat == 7);
        else if (beat == 6)
        {
            deployment = 0;
            for (int i = 0; i < combCount; i++)
            {
                launch[i] = comb[i].position;
                lanes[i].x += (Camera.RightLimit - Camera.LeftLimit - .7f) / combCount * .5f;
            }
        }
        else if (beat == 8)
        {
            deployment = 0;
            returning = true;
            for (int i = 0; i < combCount; i++)
                launch[i] = comb[i].position;
        }
        else if (beat == 9)
            Phrase(Blizzard());
        else if (beat == 11 && phaseTwo)
            CrystalShells();
    }

    // Fast fan, full stop, then every round re-aims slowly at the player and accelerates.
    // The re-aimed fan keeps its spacing, so the escape is a short sidestep, not a dash.
    void FreezeFrame(int sign)
    {
        int count = Dense(phaseTwo ? 17 : 13);
        float spread = 150f / (count - 1);
        for (int i = 0; i < count; i++)
        {
            float lane = i - (count - 1) * .5f;
            Fire<SimpleRound>(Mouth, CombatVisuals.Rotate(Vector2.down, lane * spread + sign * 4), 4.2f - Mathf.Abs(lane) * .08f, .2f)
                .Pause(.7f, .75f + Mathf.Abs(lane) * .03f, CombatRound.Turn.Aim, lane * 5.5f, .22f)
                .Ramp(4, 1.6f, 1.5f);
        }
    }

    // Shells keep Snow's explosive identity; BurstShell turns their fragments into falling snow.
    void CrystalShells()
    {
        int count = Dense(phaseTwo ? 4 : 3);
        var axis = Aim;
        for (int i = 0; i < count; i++)
        {
            var shell = Fire<BurstShell>(Mouth, CombatVisuals.Rotate(axis, (i - (count - 1) * .5f) * 34), 2.7f, .5f);
            if (shell != null)
                shell.SetTravelDistance(i % 2 == 0 ? 4.4f : 6.2f);
        }
    }

    // Almost static pillars that accelerate downward: lanes that close from slow to fast.
    void CombPillars(bool second)
    {
        for (int i = 0; i < combCount; i++)
        {
            if (Difficulty.Easy && i % 3 == 2)
                continue;
            var p = comb[i].TransformPoint(new Vector3(0, -.26f, -.55f));
            for (int k = 0; k < (phaseTwo ? 3 : 2); k++)
                Fire<SimpleRound>(p, Vector2.down, .25f, .2f).Ramp(second ? 14 : 11, 1.8f, .4f + k * .45f + (i % 2) * .25f);
            releases[i] = .1f;
        }
    }

    // Rounds thrown out sideways, braking to a halt, then turning down and inward.
    IEnumerator Blizzard()
    {
        int gusts = Dense(phaseTwo ? 12 : 9);
        for (int g = 0; g < gusts; g++)
        {
            int side = g % 2 == 0 ? -1 : 1;
            var port = Hull.TransformPoint(new Vector3(side * .9f, -.3f, -.25f));
            var axis = CombatVisuals.Rotate(Vector2.right * side, -side * Random.Range(5f, 25f));
            Fire<SimpleRound>(port, axis, Random.Range(2.6f, 3.6f), .19f)
                .Pause(.6f, .35f, CombatRound.Turn.Rotate, -side * Random.Range(95f, 125f), .75f)
                .Sway(.25f, .6f);
            yield return Wait(Rest(.13f));
        }
    }

    protected override void PrepareScore(float seconds)
    {
        int beat = volley % 14;
        if (beat == 0 || beat == 2 || phaseTwo && (beat == 11 || beat == 12))
            Cue(Mouth, seconds);
        else if (beat == 5 || beat == 7)
            for (int i = 0; i < combCount; i++)
                Cue(comb[i].TransformPoint(new Vector3(0, -.26f, -.55f)), seconds);
        else if (beat == 9)
        {
            Cue(Hull.TransformPoint(new Vector3(-.9f, -.3f, -.25f)), seconds);
            Cue(Hull.TransformPoint(new Vector3(.9f, -.3f, -.25f)), seconds);
        }
    }
}
