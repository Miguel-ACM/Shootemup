using System.Collections;
using UnityEngine;

// Desert worm: everything comes off the sand-coloured body as it winds. Body
// reactors sow quicksand mines on both flanks that stop and glint; half of
// each row then sinks away, the other half lunges at the player. The jaw sweeps
// a wavering sandblast, and the body sheds heat-haze rounds that shimmer and
// vanish mid-screen. With few reactors left, a volley ripples head to tail.
public sealed partial class DesertWormBoss
{
    int WindingCore(int wanted)
    {
        for (int n = 0; n < cores.Length; n++)
        {
            int i = (wanted + n) % cores.Length;
            if (CanShoot(cores[i]))
                return i;
        }
        return -1;
    }

    int MineCore(int beat) => WindingCore(1 + (beat / 2 + volley / 16) * 3 % 7);
    protected override void Attack()
    {
        int beat = (volley - 1) % 16;
        if (beat == 0 || beat == 2 || beat == 4)
            QuicksandMines(MineCore(beat));
        else if (beat == 6 && CanShoot(cores[0]))
            Phrase(Sandblast());
        else if (beat == 9 || beat == 10)
            Mirage(beat % 2);
        else if (phaseTwo && beat == 13)
            Phrase(RippleVolley());
    }

    // Two flanking rows that brake and glint; even mines lunge at the player, odd ones sink.
    void QuicksandMines(int core)
    {
        if (core < 0)
            return;
        var t = cores[core].transform;
        Vector2 flank = t.right;
        int mines = Dense(phaseTwo ? 9 : 7);
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < mines; i++)
            {
                var axis = CombatVisuals.Rotate(flank * side, (i - (mines - 1) * .5f) * 15);
                float hold = 1.2f + i * .09f;
                var mine = Fire<SimpleRound>(t.position, axis, 2.1f + (i % 2) * .5f, .22f).Pause(.55f, hold, CombatRound.Turn.Aim, 0, .85f);
                if (i % 2 == 1)
                    mine.FadeAt(.55f + hold);
            }
    }

    // A wavering stream from the jaw that sweeps across the worm's heading.
    IEnumerator Sandblast()
    {
        int shots = Dense(phaseTwo ? 18 : 14);
        for (int i = 0; i < shots && CanShoot(cores[0]); i++)
        {
            Vector2 heading = Hull.TransformDirection(Vector3.down);
            var axis = CombatVisuals.Rotate(heading, Mathf.Sin(i / (float)shots * Mathf.PI * 2) * 38);
            Fire<SimpleRound>(Muzzle(), axis, 3.1f, .2f).Sway(.35f, 1.1f);
            yield return Wait(Rest(.065f));
        }
    }

    // Heat haze: slow shimmering rounds that fade out at staggered depths.
    void Mirage(int parity)
    {
        int count = Dense(4);
        for (int i = 1; i < cores.Length; i++)
            if (i % 2 == parity && CanShoot(cores[i]))
                for (int j = 0; j < count; j++)
                    Fire<SimpleRound>(cores[i].transform.position, CombatVisuals.Rotate(Vector2.down, (j - (count - 1) * .5f) * 30 + (parity == 0 ? 8 : -8)), 2.0f, .24f)
                        .Sway(.55f, .55f)
                        .FadeAt(Random.Range(2.2f, 3.4f));
    }

    // Each surviving reactor answers in turn from head to tail: a travelling ripple of aimed fire.
    IEnumerator RippleVolley()
    {
        int spread = Dense(3);
        for (int i = 0; i < cores.Length; i++)
        {
            if (!CanShoot(cores[i]))
                continue;
            var p = cores[i].transform.position;
            var aim = CombatVisuals.Aim(p);
            for (int j = 0; j < spread; j++)
                Fire<NeedleRound>(p, CombatVisuals.Rotate(aim, (j - (spread - 1) * .5f) * 14), 4.0f, .19f);
            yield return Wait(.08f);
        }
    }

    protected override void PrepareScore(float seconds)
    {
        int beat = volley % 16;
        if (beat == 0 || beat == 2 || beat == 4)
        {
            int i = WindingCore(1 + (beat / 2 + (volley + 1) / 16) * 3 % 7);
            if (i >= 0)
                Cue(cores[i].transform.position, seconds);
        }
        else if (beat == 6)
            Cue(Muzzle(), seconds);
        else if (beat == 9 || beat == 10 || phaseTwo && beat == 13)
            for (int i = 1; i < cores.Length; i++)
                if (CanShoot(cores[i]) && (beat == 13 || i % 2 == beat % 2))
                    Cue(cores[i].transform.position, seconds);
    }
}
