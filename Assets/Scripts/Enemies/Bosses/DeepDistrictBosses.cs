using System.Collections;
using UnityEngine;

public sealed class CisternHeart : DistrictBoss
{
    public static float LakeCenter => Mathf.Lerp(Camera.DownLimit, Camera.UpLimit, .64f);
    protected override bool RepositionsOnPhase => false;
    protected override bool AtOpeningStation => (transform.position - station).sqrMagnitude < .0025f;
    protected override void Configure() { health = 2375; hullScale = 1.02f; attackInterval = .24f; nextAttack = 1.4f; }

    protected override void Start()
    {
        base.Start();
        station = new Vector3(0, LakeCenter, 1);
        LevelEnvironment.RevealCistern(station.y);
    }

    protected override void MoveBoss(float dt)
    {
        if (!openingComplete)
            transform.position = Vector3.MoveTowards(transform.position, station, dt * 5.3f);
    }

    protected override void Attack()
    {
        if (openingComplete)
            OpenCards();
    }

    protected override IEnumerator Cards() => phaseThree ? Spillway() : phaseTwo ? Undertow() : PressureFront();

    IEnumerator PrimePair(int pair, float seconds)
    {
        for (float t = 0; t < seconds;)
        {
            Cue(Muzzle(pair), seconds - t);
            Cue(Muzzle(pair + 3), seconds - t);
            if (!Holding) t += Time.deltaTime * LevelRun.EnemyTempoScale;
            yield return null;
        }
    }

    // The outlet pair punches a scalloped bow wave into the lake. The slower,
    // darker backwash comes from the same mouths in the opposite direction.
    IEnumerator PressureFront()
    {
        for (int beat = 0; ; beat++)
        {
            int pair = beat % 3;
            yield return PrimePair(pair, .48f);
            float cant = 18 * Mathf.Sin(beat * .85f);
            for (int echo = 0; echo < 2; echo++)
            {
                int count = Mathf.Max(5, Dense(echo == 0 ? 15 : 10));
                UseTone(echo == 0 ? HostileTone.Bright : HostileTone.Dark);
                for (int h = 0; h < 2; h++)
                {
                    int side = h * 2 - 1;
                    Vector3 port = Muzzle(pair + 3 * h);
                    for (int i = 0; i < count; i++)
                    {
                        float t = (i + .5f) / count;
                        float angle = side * Mathf.Lerp(-37, 101, t) + cant;
                        float speed = 2.32f + .81f * Mathf.Sin(t * Mathf.PI) + .29f * Mathf.Cos(t * 4 * Mathf.PI);
                        var axis = CombatVisuals.Rotate(echo == 0 ? Vector2.down : Vector2.up, angle);
                        Fire<GiantOrb>(port, axis, speed, i % 4 == 0 ? .63f : .38f, i == 0)
                            .Ramp(echo == 0 ? 1.32f : .84f, .84f, .53f);
                    }
                }
                if (echo == 0) yield return Wait(Rest(.33f));
            }
            yield return Wait(Rest(.89f));
        }
    }

    // Two fountains cross overhead. Gravity pulls the strings of bubbles back
    // through the arena; moving from one side of a falling string to the other is the dodge.
    IEnumerator Undertow()
    {
        for (int beat = 0; ; beat++)
        {
            int pair = 1 + beat % 2;
            yield return PrimePair(pair, .48f);
            int marks = Mathf.Max(7, Dense(17));
            float tilt = Mathf.Sin(beat * 1.18f) * .54f;
            for (int i = 0; i < marks; i++)
            {
                float t = i / (marks - 1f);
                for (int h = 0; h < 2; h++)
                {
                    int side = h * 2 - 1;
                    var port = Muzzle(pair + 3 * h);
                    for (int stream = 0; stream < 2; stream++)
                    {
                        UseTone(stream == 0 ? HostileTone.Bright : HostileTone.Dark);
                        Vector2 v = new Vector2(side * (-1.6f + 2.08f * t + stream * .68f) + tilt,
                            2.72f + 1.48f * Mathf.Sin(t * Mathf.PI) - .89f * stream);
                        Fire<GiantOrb>(port, v, v.magnitude, stream == 0 ? .46f : .34f, stream == 0 && i % 4 == 0)
                            .Fall(new Vector2(-side * .12f, -1.82f - .23f * stream));
                    }
                }
                yield return Wait(1.12f / marks);
            }
            yield return Charge(Hull, WeaponPort(6), .32f);
            UseTone(HostileTone.Dark);
            int crests = Mathf.Max(3, Dense(6));
            for (int i=0; i<crests; i++)
            {
                float t=i/(crests-1f);
                for (int side=-1; side<=1; side+=2)
                    Fire<GiantOrb>(Muzzle(6), new Vector2(side*(.28f+.57f*t),1),
                        WorldSpeed(5.3f+.52f*t), .37f, i==0 && side<0)
                        .Fall(new Vector2(-side*.52f,-1.76f));
            }
            yield return Wait(Rest(.52f));
        }
    }

    // A lock gate builds up head from both banks. Its teeth stop along a
    // chevron, then the breach travels out from the centre, row after row.
    IEnumerator Spillway()
    {
        for (int beat = 0; ; beat++)
        {
            float gate = 1.93f * Mathf.Sin(beat * .88f);
            int count = Mathf.Max(8, Dense(20));
            for (int row = 0; row < 3; row++)
            {
                yield return PrimePair(row, row == 0 ? .43f : .16f);
                UseTone(row == 1 ? HostileTone.Dark : HostileTone.Bright);
                for (int i = 0; i < count; i++)
                {
                    float x = Mathf.Lerp(Camera.LeftLimit + .18f, Camera.RightLimit - .18f, (i + .5f) / count);
                    int h = x < gate ? 0 : 1;
                    Vector3 port = Muzzle(row + h * 3);
                    float bank = Mathf.Abs(x - gate) / (h == 0 ? gate - Camera.LeftLimit : Camera.RightLimit - gate);
                    Vector2 aim = new Vector2(x, LakeCenter + 1.36f - .89f * row + 1.16f * bank) - (Vector2)port;
                    float speed = WorldSpeed(Mathf.Max(.22f, aim.magnitude / .585f));
                    Fire<GiantOrb>(port, aim, speed, i % 4 == 0 ? .59f : .35f, i == 0 || i == count - 1)
                        .Pause(.76f, .43f + .42f * bank, CombatRound.Turn.Down, 5 * Mathf.Cos(beat * .88f), 4.65f / speed);
                }
            }
            yield return Charge(Hull, WeaponPort(6), .38f);
            int spray = Mathf.Max(3, Dense(8));
            for (int i = 0; i < spray; i++)
            {
                float angle = Mathf.Lerp(-67, 67, i / (spray - 1f));
                Fire<GiantOrb>(Muzzle(6), CombatVisuals.Rotate(Vector2.up, angle), 3.65f, .43f, i == 0)
                    .Fall(Vector2.down * 1.39f);
            }
            yield return Wait(Rest(.86f));
        }
    }
}

public sealed class CoreGantry : DistrictBoss
{
    static readonly Vector3 CoreMuzzle = new Vector3(0, -.77f, -.28f);
    Transform carriage;
    protected override bool RepositionsOnPhase => false;
    protected override void Configure() { health = 3025; hullScale = 1f; attackInterval = .22f; nextAttack = 1.45f; }

    protected override void Start()
    {
        base.Start();
        station = new Vector3(0, Camera.UpLimit - 3.45f, 1);
        carriage = new GameObject("Sliding reactor carriage").transform;
        carriage.SetParent(Hull, false);
        carriage.gameObject.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.GantryCarriage();
        var renderer = carriage.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CombatMaterials.Fleet;
        renderer.sortingLayerName = "Enemy";
        firingCue.Register(carriage, CoreMuzzle);
    }

    protected override void MoveBoss(float dt)
    {
        if (!openingComplete)
        {
            transform.position = Vector3.MoveTowards(transform.position, station, 6f * dt);
            return;
        }
        float travel = phaseThree ? 1.63f : phaseTwo ? 1.32f : .95f;
        Vector3 target = station + Vector3.up * Mathf.Sin(motionAge * travel) * (phaseThree ? 1.75f : 1.22f);
        transform.position = Vector3.MoveTowards(transform.position, target, dt * 4.8f);
        float x = Mathf.Sin(motionAge * (phaseThree ? 1.94f : phaseTwo ? 1.53f : 1.12f) + .65f) * 2.72f;
        carriage.localPosition = Vector3.MoveTowards(carriage.localPosition, new Vector3(x, 0, -.20f), dt * 8f);
    }

    protected override void Attack()
    {
        if (openingComplete)
            OpenCards();
    }

    protected override IEnumerator Cards() => phaseThree ? PunchTape() : phaseTwo ? Escapement() : Crosshatch();

    IEnumerator PrimeRails(float seconds)
    {
        for (float t = 0; t < seconds;)
        {
            Cue(Muzzle(0), seconds - t);
            Cue(Muzzle(1), seconds - t);
            if (!Holding) t += Time.deltaTime * LevelRun.EnemyTempoScale;
            yield return null;
        }
    }

    // A moving engraving head cuts a herringbone. The rails supply the
    // registration marks: two spaced diagonals cross behind the passing teeth.
    IEnumerator Crosshatch()
    {
        for (int pass = 0; ; pass++)
        {
            yield return Charge(carriage, CoreMuzzle, .46f);
            int strokes = Mathf.Max(9, Dense(21));
            for (int i = 0; i < strokes; i++)
            {
                float t = i / (strokes - 1f);
                Vector3 port = carriage.TransformPoint(CoreMuzzle);
                for (int h = -1; h <= 1; h += 2)
                {
                    UseTone(h < 0 ? HostileTone.Bright : HostileTone.Dark);
                    for (int tooth = 0; tooth < 3; tooth++)
                    {
                        float angle = h * (10 + 38 * Mathf.Abs(2 * t - 1) + 9 * tooth);
                        Fire<NeedleRound>(port, CombatVisuals.Rotate(Vector2.down, angle),
                            3.5f + tooth * .41f, .17f, tooth == 0 && i % 4 == 0);
                    }
                }
                yield return Wait(1.56f / strokes);
            }
            yield return PrimeRails(.28f);
            for (int h = 0; h < 2; h++)
            {
                int side = h * 2 - 1;
                int teeth = Mathf.Max(3, Dense(8));
                for (int i = 0; i < teeth; i++)
                {
                    float a = Mathf.Lerp(19, 73, i / (teeth - 1f));
                    Fire<SimpleRound>(Muzzle(h), CombatVisuals.Rotate(Vector2.up, side * a), 3.45f, .22f, i == 0)
                        .Bounce(1);
                }
            }
            yield return Wait(Rest(.56f));
        }
    }

    // The two rail shoes toss racks of bolts across the beam. The rack hangs
    // horizontally for a beat, turns its teeth down and falls through the other rack.
    IEnumerator Escapement()
    {
        for (int pass = 0; ; pass++)
        {
            float door = 1.75f * Mathf.Sin(pass * .95f);
            yield return PrimeRails(.48f);
            int teeth = Mathf.Max(8, Dense(20));
            for (int rack = 0; rack < 3; rack++)
            {
                UseTone(rack == 1 ? HostileTone.Dark : HostileTone.Bright);
                for (int h = 0; h < 2; h++)
                {
                    int side = h * 2 - 1;
                    Vector3 port = Muzzle(h);
                    for (int i = 0; i < teeth; i++)
                    {
                        float t = (i + .5f) / teeth;
                        float x = Mathf.Lerp(side * (Camera.RightLimit - .18f), door + side * .58f, t);
                        float y = station.y + .86f - rack * .66f + side * (x - door) * .36f;
                        Vector2 offset = new Vector2(x, y) - (Vector2)port;
                        float speed = WorldSpeed(Mathf.Max(.19f, offset.magnitude / .625f));
                        Fire<NeedleRound>(port, offset, speed, i % 4 == 0 ? .23f : .155f, i == 0)
                            .Pause(.80f, .43f + .35f * (1 - t), CombatRound.Turn.Down,
                                4 * Mathf.Cos(pass * .95f), 5.35f / speed).ShowTurn();
                    }
                }
                yield return Wait(.16f);
            }
            Vector3 lip = carriage.TransformPoint(CoreMuzzle);
            Vector2 bearing = CombatVisuals.Aim(lip);
            PointWeapon(lip, bearing);
            yield return Charge(carriage, CoreMuzzle, .42f);
            int rivets = Mathf.Max(3, Dense(6));
            for (int i = 0; i < rivets; i++)
            {
                Fire<SimpleRound>(carriage.TransformPoint(CoreMuzzle), bearing, 4.4f + .21f * i, .21f);
                yield return Wait(.085f);
            }
            yield return Wait(Rest(.72f));
        }
    }

    // The press punches an actual strip of perforations while the carriage
    // traverses. The rails
    // fold the selvage back in only after the press has lifted.
    IEnumerator PunchTape()
    {
        for (int strip = 0; ; strip++)
        {
            yield return Charge(carriage, CoreMuzzle, .44f);
            Vector2 bearing = CombatVisuals.Aim(carriage.TransformPoint(CoreMuzzle));
            int punches = Mathf.Max(10, Dense(25));
            for (int i = 0; i < punches; i++)
            {
                float x = Mathf.Lerp(-4.48f, 4.48f, (i + .5f) / punches);
                if ((strip & 1) != 0) x = -x;
                Vector3 port = carriage.TransformPoint(CoreMuzzle);
                for (int tooth = 0; tooth < 6; tooth++)
                {
                    // Alternate blocks sit higher along the tape.
                    float y = station.y + 1.68f - .54f * tooth + .22f * ((i / 3) % 2);
                    Vector2 offset = new Vector2(x, y) - (Vector2)port;
                    float speed = WorldSpeed(Mathf.Max(.18f, offset.magnitude / .725f));
                    UseTone(tooth == 1 || tooth == 2 ? HostileTone.Dark : HostileTone.Bright);
                    Fire<NeedleRound>(port, offset, speed, tooth == 0 ? .22f : .16f, tooth == 0)
                        .Pause(.90f, .39f + .79f * (1 - i / (punches - 1f)), CombatRound.Turn.Down,
                            5 * Mathf.Cos(strip * .81f), 5.15f / speed).ShowTurn();
                }
                yield return Wait(.86f / punches);
            }
            yield return PrimeRails(.32f);
            int stitches = Mathf.Max(5, Dense(12));
            for (int i = 0; i < stitches; i++)
            {
                float t = i / (stitches - 1f);
                for (int h = 0; h < 2; h++)
                {
                    int side = h * 2 - 1;
                    UseTone(h == (strip & 1) ? HostileTone.Bright : HostileTone.Dark);
                    Vector2 axis = new Vector2(side * (.35f + .54f * t), .94f - 1.75f * t);
                    Fire<SimpleRound>(Muzzle(h), axis, 4.15f + .65f * t, .18f, i % 4 == 0).Bounce(1);
                }
                yield return Wait(.052f);
            }
            yield return Charge(carriage, CoreMuzzle, .31f);
            int staples = Mathf.Max(3, Dense(7));
            for (int i=0; i<staples; i++)
            {
                float t=i/(staples-1f);
                for (int side=-1; side<=1; side+=2)
                    Fire<SimpleRound>(carriage.TransformPoint(CoreMuzzle),
                        CombatVisuals.Rotate(bearing,side*(8+19*t)), 4.85f-.77f*t, .19f, side<0);
                yield return Wait(.047f);
            }
            yield return Wait(Rest(.31f));
        }
    }
}
