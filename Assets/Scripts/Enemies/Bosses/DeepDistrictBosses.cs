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

    // Sluice Crown / Flood Mark / The Cistern Overflows.
    // The stationary caisson floods paired shores, then rolls the opening around the lake.
    protected override IEnumerator Cards()
    {
        int beat = 0;
        while (true)
        {
            int pair = beat % 3;
            int first = pair, second = pair + 3;
            float warning = phaseThree ? .31f : .46f;
            Cue(Muzzle(first), warning);
            Cue(Muzzle(second), warning);
            yield return Charge(Hull, WeaponPort(first), warning);
            int lanes = Dense(phaseThree ? 15 : phaseTwo ? 12 : 9);
            for (int i = 0; i < lanes; i++)
            {
                float t = i / Mathf.Max(1f, lanes - 1);
                float sweep = Mathf.Sin(beat * (phaseThree ? .79f : .53f)) * 22;
                float a = Mathf.Lerp(phaseTwo ? -31 : -12, phaseTwo ? 124 : 104, t) + sweep;
                float size = i % 4 == 0 ? .76f : .53f;
                float speed = 2.05f + .72f * Mathf.Abs(t * 2 - 1);
                for (int side = -1; side <= 1; side += 2)
                {
                    var origin = Muzzle(side < 0 ? first : second);
                    var direction = CombatVisuals.Rotate(Vector2.down, side * a);
                    var orb = Fire<GiantOrb>(origin, direction, speed, size, i == 0);
                    if (phaseThree && (i + beat) % 3 == 0)
                        orb.Pause(.76f, .43f, CombatRound.Turn.Rotate, side * 21, 3.7f);
                    else if (phaseTwo && i % 2 == 0)
                        orb.Fall(new Vector2(-side * .47f, -.29f));
                }
            }
            if (phaseThree || phaseTwo && beat % 2 == 1)
            {
                Cue(Muzzle(6), .23f);
                yield return Wait(Rest(.23f));
                int count = Dense(7);
                for (int i = 0; i < count; i++)
                    Fire<GiantOrb>(Muzzle(6), CombatVisuals.Rotate(Vector2.up, (i - (count - 1) * .5f) * 25), 3.1f, .42f, i == 0)
                        .Fall(new Vector2(0, -1.45f));
            }
            beat++;
            yield return Wait(Rest(phaseThree ? .39f : phaseTwo ? .58f : .79f));
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

    // Cross Travel / Rail Interlock / Runaway Press.
    // The carriage stamps rows while the two rail shoes throw replies along the walls.
    protected override IEnumerator Cards()
    {
        int beat = 0;
        while (true)
        {
            yield return Charge(carriage, CoreMuzzle, phaseThree ? .24f : .42f);
            int strokes = phaseThree ? 6 : phaseTwo ? 4 : 3;
            for (int row = 0; row < strokes; row++)
            {
                var port = carriage.TransformPoint(CoreMuzzle);
                int lanes = Dense(phaseThree ? 13 : phaseTwo ? 11 : 9);
                int skip = Mathf.Clamp(Mathf.RoundToInt((.5f + .34f * Mathf.Sin(beat * .62f + row * .42f)) * (lanes - 1)), 0, lanes - 1);
                for (int i = 0; i < lanes; i++)
                {
                    if (i == skip || phaseThree && i == Mathf.Min(lanes - 1, skip + 1))
                        continue;
                    float angle = Mathf.Lerp(-77, 77, i / Mathf.Max(1f, lanes - 1));
                    float skew = 13 * Mathf.Sin(beat + row * .72f);
                    var shot = Fire<NeedleRound>(port, CombatVisuals.Rotate(Vector2.down, angle + skew), 4.6f, .19f, i == 0);
                    if (phaseTwo && (i + row) % 3 == 0)
                        shot.Pause(.54f, .31f, CombatRound.Turn.Down, 0, 5.25f);
                }
                if (row % 2 == 0)
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        int portIndex = side < 0 ? 0 : 1;
                        for (int i = 0; i < Dense(phaseThree ? 6 : 4); i++)
                        {
                            var direction = new Vector2(side * -.65f, (phaseTwo && (beat + row) % 3 == 0 ? .75f : -.75f));
                            Fire<SimpleRound>(Muzzle(portIndex), CombatVisuals.Rotate(direction, side * (i * 14 - 20)), 4.1f, .24f, i == 0)
                                .Bounce(phaseThree ? 2 : 1);
                        }
                    }
                }
                yield return Wait(Rest(phaseThree ? .16f : .23f));
            }
            beat++;
            yield return Wait(Rest(phaseThree ? .37f : .61f));
        }
    }
}
