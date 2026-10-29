using System.Collections;
using UnityEngine;

// Desert worm: after one dive it keeps rising from the bottom edge to the top in a
// steady S, lane by lane, attacking on the way up. Everything comes off its body.
// Phase one
//   Quicksand Field: reactor after reactor, head to tail, sows mirrored rows of mines
//     on both flanks; they brake and glint, odd mines sink away, even mines carry on
//     outward, so the field spreads across the arena instead of piling on the player.
//   Sandblast: twin mirrored arrow streams fan either side of the player from the
//     jaw, with a little ordered heat haze from the body.
// Phase two
//   Sidewinder: every surviving reactor fires arrows straight out of both flanks as
//     the body rises, drawing ribbons of arrows along its whole path.
//   Dune Collapse: reactors in turn throw small geysers of arrows that fall back.
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

    protected override void Attack()
    {
        OpenCards();
    }

    protected override IEnumerator Cards()
    {
        while (true)
        {
            if (!phaseTwo)
            {
                yield return Every(7, .45f, n => QuicksandMines(WindingCore(1 + n % 7)));
                yield return Wait(.8f);
                yield return Sandblast();
                yield return Wait(.8f);
            }
            else
            {
                yield return Every(7, .2f, n => Sidewinder(n));
                yield return Wait(.8f);
                yield return Every(6, .6f, n => Geyser(WindingCore(n % cores.Length)));
                yield return Wait(.8f);
            }
        }
    }

    // Two flanking rows that brake and glint; even mines carry on outward, odd ones sink.
    void QuicksandMines(int core)
    {
        if (core < 0)
            return;
        var t = cores[core].transform;
        Vector2 flank = t.right;
        int mines = Dense(7);
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < mines; i++)
            {
                var axis = CombatVisuals.Rotate(flank * side, (i - (mines - 1) * .5f) * 14);
                float hold = 1.0f + i * .08f;
                var mine = Fire<SimpleRound>(t.position, axis, 2.3f + (i % 2) * .5f, .22f).Pause(.5f, hold, CombatRound.Turn.Keep, 0, 1.2f);
                if (i % 2 == 1)
                    mine.FadeAt(.5f + hold);
            }
    }

    // Twin mirrored arrow streams from the jaw, sweeping apart and back together.
    IEnumerator Sandblast()
    {
        Phrase(Every(6, 2.5f, n => Mirage(n % 2)));
        int shots = Dense(90);
        for (int i = 0; i < shots && CanShoot(cores[0]); i++)
        {
            Vector2 heading = CombatVisuals.Aim(Muzzle());
            float open = 12 + Mathf.Abs(Mathf.Sin(i / 30f * Mathf.PI * 2)) * 34;
            foreach (int mirror in new[] { -1, 1 })
                foreach (float arm in new[] { 0f, 14f })
                    Fire<NeedleRound>(Muzzle(), CombatVisuals.Rotate(heading, mirror * (open + arm)), 3.4f, .19f);
            yield return Wait(Rest(.065f));
        }
    }

    // Heat haze: slow mirrored rounds that shimmer and fade out at staggered depths.
    void Mirage(int parity)
    {
        int count = Dense(3);
        for (int i = 1; i < cores.Length; i++)
            if (i % 2 == parity && CanShoot(cores[i]))
                for (int j = 0; j < count; j++)
                    Fire<SimpleRound>(cores[i].transform.position, CombatVisuals.Rotate(Vector2.down, (j - (count - 1) * .5f) * 24), 2.0f, .24f)
                        .Sway((j % 2 == 0 ? 1 : -1) * .5f, .55f)
                        .FadeAt(2.2f + Mathf.Abs(j - (count - 1) * .5f) * .4f);
    }

    // Arrows straight out of both flanks of every reactor: the body draws ribbons.
    void Sidewinder(int n)
    {
        for (int i = n % 2; i < cores.Length; i += 2)
        {
            if (!CanShoot(cores[i]))
                continue;
            var t = cores[i].transform;
            for (int side = -1; side <= 1; side += 2)
                Fire<NeedleRound>(t.position, (Vector2)t.right * side, 2.6f, .19f);
        }
    }

    // A mirrored fountain of arrows thrown up from one reactor, falling back under gravity.
    void Geyser(int core)
    {
        if (core < 0)
            return;
        var p = cores[core].transform.position;
        int lanes = Dense(9);
        for (int layer = 0; layer < 1; layer++)
            for (int i = 0; i < lanes; i++)
            {
                float lane = i - (lanes - 1) * .5f;
                Fire<NeedleRound>(p, CombatVisuals.Rotate(Vector2.up, lane * 100f / (lanes - 1)), 3.6f + layer * 1.1f, .19f)
                    .Fall(new Vector2(0, -2.8f));
            }
    }
}
