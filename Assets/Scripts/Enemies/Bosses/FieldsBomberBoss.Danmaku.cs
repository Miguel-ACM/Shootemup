using System.Collections;
using UnityEngine;

// Plains bomber: a working aircraft. Wingtip guns stream aimed fire that the
// player must lead away, bomb bays drop payloads that stop mid-air and burst
// sideways, and orange carriers drift through the gaps. Phase two strings a
// contrail curtain across the arena that hangs, then falls all at once.
public sealed partial class FieldsBomberBoss
{
    protected override void Attack()
    {
        int beat = (volley - 1) % 12;
        if (beat == 0 || beat == 8)
            Phrase(WingStream(beat == 0 ? 1 : -1));
        else if (beat == 3)
            CarpetBombs();
        else if (beat == 5 || phaseTwo && beat == 11)
            Fire<ScatterCarrier>(Muzzle(4), CombatVisuals.Rotate(Vector2.down, Random.Range(-18f, 18f)), 2.2f, .33f);
        else if (beat == 6 && phaseTwo)
            Phrase(ContrailCurtain());
        else if (beat == 6)
            CarpetBombs();
    }

    // Streaming: each round re-aims, so standing still is fatal and a steady sweep is safe.
    IEnumerator WingStream(int lead)
    {
        int shots = Dense(phaseTwo ? 9 : 7);
        for (int i = 0; i < shots; i++)
        {
            for (int side = 0; side < 2; side++)
            {
                var port = Muzzle(side);
                var axis = CombatVisuals.Rotate(CombatVisuals.Aim(port), lead * (side == 0 ? 4 : -4));
                Fire<NeedleRound>(port, axis, 4.4f, .19f);
            }
            yield return Wait(Rest(.11f));
        }
    }

    // Each bomb brakes at its own height, glints, then bursts into a flat sideways fan.
    void CarpetBombs()
    {
        int bombs = Dense(phaseTwo ? 6 : 4), shards = Dense(7);
        for (int i = 0; i < bombs; i++)
        {
            var port = Hull.TransformPoint(bays[i % bays.Length]);
            float stop = .9f + (i % 3) * .35f + Random.Range(0, .15f);
            Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, (i - (bombs - 1) * .5f) * 7), 2.4f, .42f)
                .Pause(stop, .45f)
                .SplitAt(stop + .45f, shards, 170f / (shards - 1), 2.3f, .15f);
        }
    }

    // A line strung from wingtip to wingtip across the arena, with a gap where the
    // player was. It hangs as a fence, then drops with alternating speeds.
    IEnumerator ContrailCurtain()
    {
        int lanes = Dense(16);
        float y = Camera.UpLimit - 4.2f;
        float gap = player != null ? player.position.x : 0;
        for (int i = 0; i < lanes; i++)
        {
            float x = Mathf.Lerp(Camera.LeftLimit + .4f, Camera.RightLimit - .4f, i / (lanes - 1f));
            if (Mathf.Abs(x - gap) < .8f)
                continue;
            var port = Muzzle(x < transform.position.x ? 0 : 1);
            Vector2 travel = new Vector2(x, y) - (Vector2)port;
            // Braking over the last .35 s covers half that time's distance.
            float arrive = .75f, launch = travel.magnitude / (arrive - .175f);
            float fall = i % 2 == 0 ? 1.1f : 1.8f;
            Fire<SimpleRound>(port, travel, WorldSpeed(launch), .24f)
                .Pause(arrive, 1.1f - i * .02f, CombatRound.Turn.Down, 0, fall / launch)
                .Ramp(1.8f, 1.2f, arrive + 1.1f);
            yield return Wait(.02f);
        }
    }

    protected override void PrepareScore(float seconds)
    {
        int beat = volley % 12;
        if (beat == 0 || beat == 8 || beat == 6 && phaseTwo)
        {
            Cue(Muzzle(0), seconds);
            Cue(Muzzle(1), seconds);
        }
        else if (beat == 3 || beat == 6)
            for (int i = 0; i < 3; i++)
                Cue(Hull.TransformPoint(bays[i * 2]), seconds);
        else if (beat == 5 || phaseTwo && beat == 11)
            Cue(Muzzle(4), seconds);
    }

    protected override void OnPhaseTwo()
    {
        volley = 0;
        nextAttack = age + 1;
    }
}
