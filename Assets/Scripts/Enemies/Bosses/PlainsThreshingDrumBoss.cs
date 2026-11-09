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

    // Faster traverses than the original 3.5 (Miguel).
    const float DrumSpeed = 5.2f;
    // Windrow column spacing on every difficulty: too narrow to slip between.
    const float ColumnStep = .27f;
    protected override void MoveBoss(float dt) => transform.position = Vector3.MoveTowards(transform.position, station, dt * DrumSpeed);

    // The traverse carries the outer drum right to the visible edge, so no column
    // of open air is left along either side of the windrows.
    float Reach(int side)
    {
        float drum = Mathf.Abs(Muzzle(2).x - Muzzle(1).x) * .5f;
        return side * Mathf.Max(.5f, CombatRound.Border.xMax - .12f - drum);
    }

    protected override IEnumerator Cards()
    {
        int side = -1;
        while (true)
        {
            station.x = Reach(side);
            station.y = Camera.UpLimit - 2.25f;
            while (Vector2.Distance(transform.position, station) > .08f) yield return null;
            yield return PrimePair(.52f);
            // Windrows: traverse the field, depositing teeth at physical intervals. All the
            // teeth in a row release on the same beat, with a broad unplanted furrow.
            float start = station.x;
            side = -side;
            station.x = Reach(side);
            // Density changes the depth of the rows, never the spacing between columns.
            int marks = Mathf.Max(5, Mathf.CeilToInt(Mathf.Abs(station.x - start) / ColumnStep) + 1);
            int rows = Dense(phaseThree ? 12 : 10);
            const float PlantStep = .24f, FirstPlant = .35f;
            float lastPlant = FirstPlant + (rows - 1) * PlantStep;
            float gapX = Lane(-side * .38f);
            for (int mark = 0; mark < marks; mark++)
            {
                float fraction = mark / (marks - 1f);
                float x = Mathf.Lerp(start, station.x, fraction);
                while (side * (x - transform.position.x) > .09f) yield return null;
                float travelLeft = Mathf.Abs(station.x - transform.position.x) / (DrumSpeed * MovementScale * LevelRun.EnemyTempoScale);
                // The motion program runs in scaled game time independently of the card clock.
                for (int drum = 1; drum <= 2; drum++)
                {
                    Vector3 port = Muzzle(drum);
                    if (Mathf.Abs(port.x - gapX) < .80f) continue;
                    // Both banks share one field. The first bank covers the left
                    // half and the second the right, avoiding superimposed columns.
                    if (drum == 1 ? port.x > gapX : port.x < gapX) continue;
                    for (int row = 0; row < rows; row++)
                    {
                        // Equal planting intervals past the fixed braking ramp give
                        // equal row depths. Even the last planted row finishes before
                        // the shared release; no reversals or faster rows catching up.
                        float plant = FirstPlant + row * PlantStep;
                        ScoredShot(row%3==0,port, Vector2.down, 3.2f, .18f)
                            .Pause(plant, travelLeft + lastPlant + .30f - plant,
                                CombatRound.Turn.Down, speed: 1.3f);
                    }
                }
            }
            yield return Wait(Rest(.26f));
            // Winnower: pods from the middle chute open sideways over the advancing rows.
            yield return Charge(Hull, WeaponPort(), .44f);
            int pods = Dense(phaseThree ? 7 : phaseTwo ? 6 : 5);
            for (int pod = 0; pod < pods; pod++)
            {
                // The chute's kernels cross the old furrow between windrow releases.
                if(pod%2==0)Fire<SimpleRound>(Muzzle(),CombatVisuals.Aim(Muzzle()),3.1f,.22f);
                for (int mirror = -1; mirror <= 1; mirror += 2)
                    // The chute throws chaff sideways. Alternating pods turn into
                    // ascending and descending diagonal combs rather than another rain.
                    Fire<SimpleRound>(Muzzle(), new Vector2(mirror, 0), 2.7f, .33f)
                        .Pause(.62f, .24f, CombatRound.Turn.Rotate,
                            mirror * (pod % 2 == 0 ? -55 : 55), .85f)
                        .SplitAt(.92f, Dense(17), 10, 2.5f, .16f);
                yield return Wait(Rest(.2f));
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
                        // Three side-wall bounces each, rather than one (Miguel).
                        ScoredShot(tooth%3==0,Muzzle(1), CombatVisuals.Rotate(Vector2.right, spread + offset), 3.3f).Bounce(3, false);
                        ScoredShot(tooth%3==0,Muzzle(2), CombatVisuals.Rotate(Vector2.left, -spread - offset), 3.3f).Bounce(3, false);
                    }
                    yield return Wait(Rest(.09f));
                }
            }
            yield return Wait(Rest(.34f));
        }
    }
}
