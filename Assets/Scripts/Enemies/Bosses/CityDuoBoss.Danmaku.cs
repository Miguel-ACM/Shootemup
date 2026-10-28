using System.Collections;
using UnityEngine;

// City duo: rush hour. The light interceptor fires "headlight" lines, rays of
// fast and slow rounds stretched along one aim, and drifting curls like cars
// skidding round a corner. The heavy hauler lays a street grid of two crossing
// diagonal families and lobs cargo that bursts toward the player. They take
// turns; a lone survivor inherits its partner's signature.
public sealed partial class CityDuoBoss
{
    protected override void Attack()
    {
        int beat = (volley - 1) % 12;
        if (heavy)
        {
            if (beat == 0 || beat == 6)
                Phrase(Gridlock(beat == 0 ? 1 : -1));
            else if (beat == 3 || enraged && beat == 9)
                CargoLob();
            else if (enraged && (beat == 2 || beat == 10))
                Headlights(Muzzle(1));
        }
        else
        {
            if (beat == 1 || beat == 2 || beat == 7 || beat == 8)
                Headlights(Muzzle());
            else if (beat == 4)
                Phrase(Skid(volley / 12 % 2 == 0 ? 1 : -1));
            else if (enraged && (beat == 5 || beat == 11))
                Phrase(Gridlock(beat == 5 ? 1 : -1));
        }
    }

    // One aim, many speeds: a ray that arrives as a stretched dotted line.
    void Headlights(Vector3 port)
    {
        int rays = Dense(enraged ? 3 : 2), beads = Dense(6);
        var axis = CombatVisuals.Aim(port);
        for (int r = 0; r < rays; r++)
        {
            var ray = CombatVisuals.Rotate(axis, (r - (rays - 1) * .5f) * 24);
            for (int b = 0; b < beads; b++)
                Fire<NeedleRound>(port, ray, 5.2f - b * .58f, .17f);
        }
    }

    // A skidding stream: each round curls harder than the last, fanning into an arc.
    IEnumerator Skid(int sign)
    {
        int shots = Dense(enraged ? 16 : 12);
        for (int i = 0; i < shots; i++)
        {
            var port = Muzzle(i % 3);
            var round = Fire<SimpleRound>(port, CombatVisuals.Rotate(CombatVisuals.Aim(port), -sign * 40), 3.4f, .18f);
            if (round != null)
                round.Curl(sign * (22 + i * 3.5f), 1.9f);
            yield return Wait(Rest(.06f));
        }
    }

    // Two crossing diagonal families; the diamonds between them are the streets.
    IEnumerator Gridlock(int sign)
    {
        int rows = Dense(5), lanes = Dense(enraged ? 9 : 7);
        for (int row = 0; row < rows; row++)
        {
            foreach (int family in new[] { -1, 1 })
            {
                var axis = CombatVisuals.Rotate(Vector2.down, family * 34 + sign * 6);
                for (int i = 0; i < lanes; i++)
                    Fire<SimpleRound>(Muzzle(1), CombatVisuals.Rotate(axis, (i - (lanes - 1) * .5f) * 7), 2.1f, .2f);
            }
            yield return Wait(Rest(.42f));
        }
    }

    // Heavy crates lobbed high, falling under gravity, then bursting toward the player.
    void CargoLob()
    {
        int crates = Dense(enraged ? 4 : 3), shards = Dense(5);
        for (int i = 0; i < crates; i++)
        {
            float lane = i - (crates - 1) * .5f;
            Fire<SimpleRound>(Muzzle(1), CombatVisuals.Rotate(Vector2.up, lane * 38), 2.6f, .42f)
                .Fall(new Vector2(0, -2.7f))
                .SplitAt(1.9f + Mathf.Abs(lane) * .2f, shards, 13, 3.3f, .17f, true);
        }
    }

    protected override void PrepareScore(float seconds)
    {
        int beat = volley % 12;
        if (heavy && (beat == 0 || beat == 6 || beat == 3 || enraged && (beat == 9 || beat == 2 || beat == 10)))
            Cue(Muzzle(1), seconds);
        else if (!heavy && (beat == 1 || beat == 2 || beat == 7 || beat == 8 || beat == 4 || enraged && (beat == 5 || beat == 11)))
            Cue(Muzzle(), seconds);
    }
}
