using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class PlainsThreshingDrumBoss : ArmoredHullBoss
{
    static readonly Vector2[] shape = { new(0,-1.10f),new(-.76f,-1.08f),new(-1.21f,-1.03f),new(-1.67f,-.75f),new(-1.78f,-.57f),
        new(-1.61f,-.40f),new(-1.75f,-.28f),new(-1.69f,.25f),new(-1.53f,.71f),new(-1.32f,1.02f),new(1.32f,1.02f),new(1.53f,.71f),
        new(1.69f,.25f),new(1.75f,-.28f),new(1.61f,-.40f),new(1.78f,-.57f),new(1.67f,-.75f),new(1.21f,-1.03f),new(.76f,-1.08f) };
    protected override Vector2[] Outline => shape;
    protected override void Configure() { health = 1320; hullScale = 1.04f; attackInterval = 1.1f; }

    protected override void MoveBoss(float dt) => transform.position = Vector3.MoveTowards(transform.position, station, dt * 3.5f);

    protected override IEnumerator Cards()
    {
        int side = -1;
        while (true)
        {
            station.x = Lane(side);
            station.y = Camera.UpLimit - 2.25f;
            while (Vector2.Distance(transform.position, station) > .08f) yield return null;
            yield return PrimePair(.52f);
            // Windrows: traverse the field, depositing teeth at physical intervals. All the
            // teeth in a row release on the same beat, with a broad unplanted furrow.
            float start = station.x;
            side = -side;
            station.x = Lane(side);
            int marks = Mathf.Max(5, Dense(phaseThree ? 30 : phaseTwo ? 26 : 22));
            int rows = Dense(phaseThree ? 7 : 5);
            float gapX = Lane(-side * .38f);
            for (int mark = 0; mark < marks; mark++)
            {
                float fraction = mark / (marks - 1f);
                float x = Mathf.Lerp(start, station.x, fraction);
                while (side * (x - transform.position.x) > .09f) yield return null;
                float travelLeft = Mathf.Abs(station.x - transform.position.x) / (3.5f * MovementScale * LevelRun.EnemyTempoScale);
                // The motion program runs in scaled game time independently of the card clock.
                for (int drum = 1; drum <= 2; drum++)
                {
                    Vector3 port = Muzzle(drum);
                    if (Mathf.Abs(port.x - gapX) < .80f) continue;
                    for (int row = 0; row < rows; row++)
                    {
                        float plant = .19f + row * .23f;
                        // Counter-feeding drums release alternating rows toward the
                        // ceiling and the floor. Rising stubble rebounds into the next
                        // descending row; the open furrow crosses both directions.
                        var tooth = Fire<NeedleRound>(port, Vector2.down, 3.2f, .18f)
                            .Pause(plant, Mathf.Max(.12f, .70f + travelLeft - plant) +
                                (phaseTwo && drum == 2 ? .32f : 0), CombatRound.Turn.Rotate,
                                row % 2 == 0 ? 0 : 180, 1.05f + row * .07f);
                        if (row % 2 != 0) tooth.Bounce(1);
                    }
                }
            }
            yield return Wait(Rest(.38f));
            // Winnower: pods from the middle chute open sideways over the advancing rows.
            yield return Charge(Hull, WeaponPort(), .44f);
            int pods = Dense(phaseThree ? 7 : phaseTwo ? 6 : 5);
            for (int pod = 0; pod < pods; pod++)
            {
                for (int mirror = -1; mirror <= 1; mirror += 2)
                    // The chute throws chaff sideways. Alternating pods turn into
                    // ascending and descending diagonal combs rather than another rain.
                    Fire<SimpleRound>(Muzzle(), new Vector2(mirror, 0), 2.7f, .33f)
                        .Pause(.62f, .24f, CombatRound.Turn.Rotate,
                            mirror * (pod % 2 == 0 ? -55 : 55), .85f)
                        .SplitAt(.92f, Dense(17), 10, 2.5f, .16f);
                yield return Wait(Rest(.26f));
            }
            if (phaseTwo)
            {
                // Return stroke: the two drums counter-turn, combing the open furrow from opposite edges.
                yield return PrimePair(.35f);
                int teeth = Dense(phaseThree ? 24 : 18);
                for (int tooth = 0; tooth < teeth; tooth++)
                {
                    float spread = Mathf.Lerp(58, -30, tooth / Mathf.Max(1f, teeth - 1));
                    for (int tine = 0; tine < Dense(5); tine++)
                    {
                        float offset = (tine - (Dense(5) - 1) * .5f) * 9;
                        Fire<NeedleRound>(Muzzle(1), CombatVisuals.Rotate(Vector2.right, spread + offset), 3.3f).Bounce(1, false);
                        Fire<NeedleRound>(Muzzle(2), CombatVisuals.Rotate(Vector2.left, -spread - offset), 3.3f).Bounce(1, false);
                    }
                    yield return Wait(Rest(.11f));
                }
            }
            yield return Wait(Rest(.50f));
        }
    }
}
