using System.Collections;
using UnityEngine;

// Plains bomber: a working aircraft. Both wingtips stream triple arrows at the
// player in step, bomb bays drop mirrored payloads that stop mid-air and burst
// into flat arrow fans, a wing lace folds open and shut, and orange carriers
// drift through the gaps. Phase two strings a contrail curtain across the arena
// that hangs, then falls all at once.
public sealed partial class FieldsBomberBoss
{
    protected override void Attack()
    {
        int beat = (volley - 1) % 12;
        if (beat == 0 || beat == 7)
            Phrase(WingStream());
        else if (beat == 2 || beat == 9)
            CarpetBombs();
        else if (beat == 4)
            Phrase(MirrorLace(Hull, WeaponPort(0), WeaponPort(1), Dense(phaseTwo ? 30 : 24), phaseTwo ? 3 : 2, 55, 20, 3.4f, .07f, volley / 12 % 2 == 0 ? 1 : -1));
        else if (beat == 5 || phaseTwo && beat == 11)
            Fire<ScatterCarrier>(Muzzle(4), CombatVisuals.Rotate(Vector2.down, Random.Range(-18f, 18f)), 2.2f, .33f);
        else if (beat == 10 && phaseTwo)
            Phrase(ContrailCurtain());
        else if (beat == 10)
            ArrowWavefront(Muzzle(4), Vector2.down, Dense(19), 130, 2, 2.6f, .8f);
    }

    // Streaming: each volley re-aims, so standing still is fatal and a steady sweep is safe.
    IEnumerator WingStream()
    {
        int shots = Dense(phaseTwo ? 14 : 11);
        for (int i = 0; i < shots; i++)
        {
            for (int side = 0; side < 2; side++)
            {
                var port = Muzzle(side);
                var axis = CombatVisuals.Aim(port);
                foreach (float spread in new[] { -9f, 0f, 9f })
                    Fire<NeedleRound>(port, CombatVisuals.Rotate(axis, spread), 4.6f, .19f);
            }
            yield return Wait(Rest(.1f));
        }
    }

    // Mirrored bombs brake at matching heights, glint, then burst into flat arrow fans.
    void CarpetBombs()
    {
        int pairs = Dense(phaseTwo ? 3 : 2), shards = Dense(11);
        for (int i = 0; i < pairs; i++)
            for (int mirror = 0; mirror < 2; mirror++)
            {
                var port = Hull.TransformPoint(bays[mirror == 0 ? i : bays.Length - 1 - i]);
                float stop = .8f + i * .4f;
                Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, (mirror == 0 ? -1 : 1) * (6 + i * 10)), 2.5f, .42f)
                    .Pause(stop, .45f)
                    .SplitAt(stop + .45f, shards, 170f / (shards - 1), 2.6f, .19f);
            }
    }

    // A line strung from wingtip to wingtip across the arena, with a gap where the
    // player was. It hangs as a fence, then drops with alternating speeds.
    IEnumerator ContrailCurtain()
    {
        int lanes = Dense(22);
        float y = Camera.UpLimit - 4.2f;
        float gap = player != null ? player.position.x : 0;
        for (int i = 0; i < lanes; i++)
        {
            float x = Mathf.Lerp(Camera.LeftLimit + .3f, Camera.RightLimit - .3f, i / (lanes - 1f));
            if (Mathf.Abs(x - gap) < .75f)
                continue;
            var port = Muzzle(x < transform.position.x ? 0 : 1);
            Vector2 travel = new Vector2(x, y) - (Vector2)port;
            // Braking over the last .35 s covers half that time's distance.
            float arrive = .75f, launch = travel.magnitude / (arrive - .175f);
            float fall = i % 2 == 0 ? 1.2f : 2.0f;
            Fire<SimpleRound>(port, travel, WorldSpeed(launch), .24f)
                .Pause(arrive, 1.1f - i * .02f, CombatRound.Turn.Down, 0, fall / launch)
                .Ramp(1.8f, 1.2f, arrive + 1.1f);
            yield return Wait(.02f);
        }
    }

    protected override void PrepareScore(float seconds)
    {
        int beat = volley % 12;
        if (beat == 0 || beat == 7 || beat == 4 || beat == 10 && phaseTwo)
        {
            Cue(Muzzle(0), seconds);
            Cue(Muzzle(1), seconds);
        }
        else if (beat == 2 || beat == 9)
            for (int i = 0; i < 3; i++)
                Cue(Hull.TransformPoint(bays[i * 2 + 1 - i % 2]), seconds);
        else if (beat == 5 || beat == 10 || phaseTwo && beat == 11)
            Cue(Muzzle(4), seconds);
    }

    protected override void OnPhaseTwo()
    {
        volley = 0;
        nextAttack = age + 1;
    }
}
