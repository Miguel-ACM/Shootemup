using System.Collections;
using UnityEngine;

// Proposed Desert worm book (ArtSource/BossPhaseProposals), flown only while
// DistrictBoss.ProposedCards is set so it can be recorded beside the shipping cards.
// Desert grammar throughout: fire spreads out from the body, mines stop then lunge or
// carry on, heat haze quickens, nothing fades and very little falls.
// Phase one
//   Desert Lotus: the head charges and blooms a mirrored lotus around its own line of
//     travel. Each petal is an arrow tip over a pair of circles; the petals brake, hang
//     like heat haze, then swing open away from the worm and quicken. The back of the
//     bloom is left to the body, whose reactors breathe a little rice haze.
//   Mine Caravan: head and tail sow a staggered double zipper of mines along both sides
//     of the worm's own track while the middle of the body breathes rice haze. After a
//     visible charge at the head the whole track wakes on one beat: mines carry on
//     outward and every fifth lunges at the pilot.
// Phase two
//   Simoom: a sandstorm. Two gust waves run along the body, tail to head and head to
//     tail, each reactor puffing a pair of arrows downwind; the bands sway like dunes
//     and quicken. After a charge the wind swings to the other side. Mines dropped in
//     the lee stop, then carry on with the wind or lunge.
//   Mirage Lattice: alternate reactors cross slow diagonal streams from both flanks,
//     so the worm weaves a diamond net along its path that quickens outward. The head
//     drops a few mines between the strands.
// Phase three (Hard)
//   Fata Morgana: the simoom blows while the head keeps blooming lotuses and the tail
//     sows mines that wake in turn.
// Tones: the dominant tone flips card to card.
//   Desert Lotus: bright petals, dark circle hearts and dark haze.
//   Mine Caravan: dark mines that carry on, bright lungers and bright haze.
//   Simoom: bright gusts, dark lee mines (bright lungers).
//   Mirage Lattice: a dark net with bright head mines.
//   Fata Morgana: bright gusts and petals, dark mines.
public sealed partial class DesertWormBoss
{
    protected override IEnumerator Proposed()
    {
        while (true)
        {
            if (phaseThree)
                yield return FataMorgana();
            else if (!phaseTwo)
            {
                yield return DesertLotus(7);
                yield return Wait(.8f);
                yield return MineCaravan();
                yield return Wait(.8f);
            }
            else
            {
                yield return Simoom();
                yield return Wait(.8f);
                yield return MirageLattice();
                yield return Wait(.8f);
            }
        }
    }

    // The head's hull points down the worm's own path.
    Vector2 HeadForward => -(Vector2)Hull.up;

    // Desert Lotus: bloom after bloom from the head, each one charged.
    IEnumerator DesertLotus(float seconds)
    {
        for (float t = 0; t < seconds;)
        {
            yield return Charge(cores[0], Vector3.zero, .35f);
            Bloom();
            float rest = Rest(.65f);
            yield return Wait(rest);
            t += .35f + rest;
        }
    }

    IEnumerator ChargedBloom()
    {
        yield return Charge(cores[0], Vector3.zero, .35f);
        Bloom();
    }

    // Mirrored petals across 300 degrees around the head's line; each half swings open
    // away from that line after the hang, so the bloom spreads instead of converging.
    void Bloom()
    {
        if (!CanShoot(cores[0]))
            return;
        Vector3 p = cores[0].position;
        Vector2 forward = HeadForward;
        int petals = Dense(12);
        if (petals % 2 == 1)
            petals++;
        float step = 300f / petals;
        for (int k = 0; k < petals; k++)
        {
            float angle = (k - (petals - 1) * .5f) * step;
            int side = angle < 0 ? -1 : 1;
            UseTone(HostileTone.Bright);
            for (int lane = -1; lane <= 1; lane++)
                Fire<NeedleRound>(p, CombatVisuals.Rotate(forward, angle + lane * 5), lane == 0 ? 3.1f : 2.7f, .19f)
                    .Pause(.55f, .45f, CombatRound.Turn.Rotate, side * 38, 1)
                    .Ramp(1.6f, .9f, 1.1f);
            UseTone(HostileTone.Dark);
            for (int lane = -1; lane <= 1; lane += 2)
                Fire<SimpleRound>(p, CombatVisuals.Rotate(forward, angle + lane * 3), 2.0f, .2f)
                    .Pause(.55f, .6f, CombatRound.Turn.Rotate, side * 38, 1)
                    .Ramp(1.6f, .9f, 1.25f);
        }
        BodyHaze(volley, HostileTone.Dark);
    }

    // Heat haze: alternate body reactors breathe slow rice to both flanks that shimmers,
    // then quickens.
    void BodyHaze(int parity, HostileTone shade)
    {
        UseTone(shade);
        for (int i = 2 + parity % 2; i < cores.Length - 1; i += 2)
        {
            if (!CanShoot(cores[i]))
                continue;
            var t = cores[i];
            for (int side = -1; side <= 1; side += 2)
                Fire<RiceRound>(t.position, (Vector2)t.right * side, 1.3f, .19f)
                    .Sway(side * .3f, .6f)
                    .Ramp(1.9f, .9f, 1.4f);
        }
    }

    // Mine Caravan: two passes of sowing, each released on one charged beat.
    IEnumerator MineCaravan()
    {
        const int steps = 15;
        const float charge = .5f;
        for (int pass = 0; pass < 2; pass++)
        {
            float interval = Rest(.15f);
            float tempo = Mathf.Max(.01f, LevelRun.EnemyTempoScale);
            for (int step = 0; step < steps; step++)
            {
                // Real seconds until the shared wake-up beat.
                float release = ((steps - step) * interval + charge) / tempo;
                SowMine(0, step, release);
                int tail = TailToHead(0, false);
                if (tail > 0)
                    SowMine(tail, step + 1, release);
                if (step % 2 == 0)
                    BodyHaze(step / 2, HostileTone.Bright);
                yield return Wait(interval);
            }
            yield return Charge(cores[0], Vector3.zero, charge);
            yield return Wait(Rest(.7f));
        }
    }

    // One mine to each flank: a near one and a far one, swapping sides every step, so the
    // two rows on either side of the track stagger like a zipper.
    void SowMine(int core, int step, float release)
    {
        if (core < 0 || !CanShoot(cores[core]))
            return;
        var t = cores[core];
        for (int side = -1; side <= 1; side += 2)
        {
            bool far = (step + (side > 0 ? 1 : 0)) % 2 == 0;
            bool lunges = (step * 2 + (side > 0 ? 1 : 0)) % 5 == 0;
            float speed = far ? 3.6f : 2.2f;
            UseTone(lunges ? HostileTone.Bright : HostileTone.Dark);
            Fire<SimpleRound>(t.position, (Vector2)t.right * side, speed, .22f)
                .Pause(.5f, Mathf.Max(.1f, release - .5f), lunges ? CombatRound.Turn.Aim : CombatRound.Turn.Keep, 0, (lunges ? 2.7f : 2.1f) / speed);
        }
    }

    // Simoom: two gusts, the wind swinging to the other side after a charge.
    IEnumerator Simoom()
    {
        int wind = transform.position.x > 0 ? -1 : 1;
        for (int gust = 0; gust < 2; gust++)
        {
            yield return Charge(cores[0], Vector3.zero, .45f);
            int blowing = wind;
            yield return Every(3.4f, .1f, n => Gust(n, blowing, true));
            wind = -wind;
        }
    }

    // Two waves along the body, tail to head and head to tail, crossing in the middle.
    void Gust(int n, int wind, bool bothWaves)
    {
        int up = cores.Length - 1 - n % cores.Length;
        Puff(up, wind);
        int down = n % cores.Length;
        if (bothWaves && down != up)
            Puff(down, wind);
        if (n % 8 == 4)
            LeeMines(TailToHead(n / 8, false), wind, n / 8);
    }

    // A pair of arrows (circles from every third reactor) downwind, swaying like a dune
    // line and quickening.
    void Puff(int core, int wind)
    {
        if (core < 0 || !CanShoot(cores[core]))
            return;
        Vector3 p = cores[core].position;
        Vector2 downwind = CombatVisuals.Rotate(Vector2.right * wind, -wind * 14);
        UseTone(HostileTone.Bright);
        for (int mirror = -1; mirror <= 1; mirror += 2)
            ScoredShot(core % 3 == 0, p, CombatVisuals.Rotate(downwind, mirror * 6), 2.1f)
                .Sway(wind * .3f, .5f)
                .Ramp(1.6f, 1, .9f);
    }

    // Mines dropped in the lee: they stop, then carry on with the wind; one in three lunges.
    void LeeMines(int core, int wind, int n)
    {
        if (core <= 0 || !CanShoot(cores[core]))
            return;
        var t = cores[core];
        for (int side = -1; side <= 1; side += 2)
        {
            bool lunges = (n + (side > 0 ? 1 : 0)) % 3 == 0;
            UseTone(lunges ? HostileTone.Bright : HostileTone.Dark);
            Fire<SimpleRound>(t.position, (Vector2)t.right * side, 2.4f, .22f)
                .Pause(.5f, .7f, lunges ? CombatRound.Turn.Aim : CombatRound.Turn.Down, lunges ? 0 : wind * 76, 1.05f);
        }
    }

    // Mirage Lattice: each reactor alternates its diagonal every time it fires, so the
    // strands of neighbouring reactors cross into a travelling diamond net.
    IEnumerator MirageLattice()
    {
        yield return Charge(cores[0], Vector3.zero, .4f);
        yield return Every(6.5f, .35f, n =>
        {
            int sign = (n / 2) % 2 == 0 ? 1 : -1;
            UseTone(HostileTone.Dark);
            for (int i = 1 + n % 2; i < cores.Length; i += 2)
            {
                if (!CanShoot(cores[i]))
                    continue;
                var t = cores[i];
                for (int side = -1; side <= 1; side += 2)
                    ScoredShot(i % 3 == 0, t.position, CombatVisuals.Rotate((Vector2)t.right * side, side * sign * 35), 1.6f)
                        .Ramp(1.8f, .8f, 1.1f);
            }
            if (n % 4 == 2)
                HeadMines(n / 4);
        });
    }

    // Four bright mines from the head: the near pair carries on, the far pair lunges.
    void HeadMines(int n)
    {
        if (!CanShoot(cores[0]))
            return;
        var t = cores[0];
        UseTone(HostileTone.Bright);
        for (int side = -1; side <= 1; side += 2)
            for (int far = 0; far < 2; far++)
            {
                float speed = far == 1 ? 3.4f : 2.2f;
                Fire<SimpleRound>(t.position, CombatVisuals.Rotate((Vector2)t.right * side, side * (n % 2 == 0 ? 20 : -20)), speed, .22f)
                    .Pause(.5f, .55f + far * .25f, far == 1 ? CombatRound.Turn.Aim : CombatRound.Turn.Keep, 0, 2.6f / speed);
            }
    }

    // Hard final phase, Fata Morgana: the simoom over lotus blooms and a sown mine trail.
    IEnumerator FataMorgana()
    {
        int wind = transform.position.x > 0 ? -1 : 1;
        for (int gust = 0; gust < 2; gust++)
        {
            yield return Charge(cores[0], Vector3.zero, .4f);
            int blowing = wind;
            yield return Every(4.5f, .2f, n =>
            {
                Gust(n, blowing, true);
                if (n % 10 == 3)
                    Phrase(ChargedBloom());
                if (n % 3 == 0)
                    SowMine(TailToHead(0, false), n / 3, Rest(1.6f) / Mathf.Max(.01f, LevelRun.EnemyTempoScale));
            });
            wind = -wind;
        }
        yield return Wait(.6f);
    }
}
