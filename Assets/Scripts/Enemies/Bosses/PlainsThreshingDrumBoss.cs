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
            int marks = Mathf.Max(5, Dense(phaseThree ? 18 : phaseTwo ? 15 : 12));
            for (int mark = 0; mark < marks; mark++)
            {
                float fraction = mark / (marks - 1f);
                float x = Mathf.Lerp(start, station.x, fraction);
                while (side * (x - transform.position.x) > .09f) yield return null;
                int gap = side > 0 ? marks / 3 : marks * 2 / 3;
                if (Mathf.Abs(mark - gap) <= 1) continue;
                float travelLeft = Mathf.Abs(station.x - transform.position.x) / (3.5f * MovementScale * LevelRun.EnemyTempoScale);
                // The motion program runs in scaled game time independently of the card clock.
                for (int drum = 1; drum <= 2; drum++)
                {
                    var tooth = Fire<NeedleRound>(Muzzle(drum), Vector2.down, 2.5f, .18f);
                    tooth.Pause(.19f, .25f + travelLeft + (phaseTwo && drum == 2 ? .32f : 0), CombatRound.Turn.Down, speed: 1.35f);
                }
            }
            yield return Wait(Rest(.68f));
            // Winnower: pods from the middle chute open sideways over the advancing rows.
            yield return Charge(Hull, WeaponPort(), .44f);
            int pods = phaseThree ? 3 : phaseTwo ? 2 : 1;
            for (int pod = 0; pod < pods; pod++)
            {
                float tilt = -side * (22 + pod * 12);
                Fire<SimpleRound>(Muzzle(), CombatVisuals.Rotate(Vector2.down, tilt), 2.7f, .33f)
                    .SplitAt(.75f + .12f * pod, Dense(11), 13, 2.5f, .16f);
                yield return Wait(Rest(.34f));
            }
            if (phaseThree)
            {
                // Return stroke: the two drums counter-turn, combing the open furrow from opposite edges.
                yield return PrimePair(.35f);
                for (int tooth = 0; tooth < Dense(8); tooth++)
                {
                    float spread = 44 - 7 * tooth;
                    Fire<NeedleRound>(Muzzle(1), CombatVisuals.Rotate(Vector2.down, spread), 3.3f);
                    Fire<NeedleRound>(Muzzle(2), CombatVisuals.Rotate(Vector2.down, -spread), 3.3f);
                    yield return Wait(Rest(.11f));
                }
            }
            yield return Wait(Rest(.8f));
        }
    }
}
