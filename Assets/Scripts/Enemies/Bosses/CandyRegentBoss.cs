using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class CandyRegentBoss : DistrictBoss
{
    readonly List<DistrictEnemy> brood = new List<DistrictEnemy>();
    float nextBrood = 2.8f, legClock, legDuration, rest;
    int dash;
    Vector3 legStart, legEnd, legControl;
    Vector2 attackAxis = Vector2.down;
    static readonly Vector2[] Dashes ={new Vector2(-.92f,.78f),new Vector2(.8f,.48f),new Vector2(.33f,.88f),
        new Vector2(-.75f,.35f),new Vector2(.96f,.72f),new Vector2(-.2f,.55f),new Vector2(-.95f,.9f),
        new Vector2(.75f,.30f),new Vector2(.93f,.86f),new Vector2(-.88f,.57f),new Vector2(.15f,.38f),new Vector2(-.32f,.92f)};
    bool arrived;
    Transform[] doors;
    public int SpawnedMinions
    {
        get; private set;
    }

    public int LiveMinions
    {
        get
        {
            brood.RemoveAll(e => e == null);
            return brood.Count;
        }
    }

    protected override void Configure()
    {
        hull = 38;
        hullScale = .85f;
        health = 1320;
        attackInterval = .36f;
        nextAttack = 2.0f;
    }

    protected override void Start()
    {
        base.Start();
        doors = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            var go = new GameObject("Mint launch hatch " + i);
            go.transform.SetParent(Hull, false);
            go.transform.localPosition = new Vector3(i == 0 ? -.82f : .82f, -.2f, -.26f);
            go.transform.localScale = Vector3.one * .24f;
            go.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CandyHatch();
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = CandyMaterials.Glaze;
            r.sortingLayerName = "Enemy";
            doors[i] = go.transform;
        }
    }

    protected override void MoveBoss(float dt)
    {
        float half = UnityEngine.Camera.main.orthographicSize * 405f / 720f;
        float width = Mathf.Min(Camera.RightLimit, half) - 1.35f;
        if (!arrived)
        {
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(0, Camera.UpLimit - 2.4f, 1), dt * 7.5f);
            arrived = transform.position.y <= Camera.UpLimit - 2.39f;
            if (arrived)
                BeginDash(width);
            return;
        }
        if (rest > 0)
        {
            rest -= dt;
            if (rest <= 0)
                BeginDash(width);
        }
        else
        {
            legClock += dt;
            float t = Mathf.Clamp01(legClock / legDuration);
            // Fast curved cuts, double-backs and short braking beats; positions remain continuous.
            float s = t * t * (3 - 2 * t), u = 1 - s;
            transform.position = u * u * legStart + 2 * u * s * legControl + s * s * legEnd;
            if (t >= 1)
                rest = phaseTwo ? .10f + (dash % 3) * .035f : .16f + (dash % 3) * .055f;
        }
        if (age >= nextBrood)
        {
            nextBrood = age + (phaseTwo ? 3.25f : 4.1f);
            StartCoroutine(ReleaseBrood());
        }
    }

    void BeginDash(float width)
    {
        var point = Dashes[dash % Dashes.Length];
        legStart = transform.position;
        legEnd = new Vector3(point.x * width, Mathf.Lerp(Camera.DownLimit + 1.7f, Camera.UpLimit - 1.5f, point.y), 1);
        if (player != null && Vector2.Distance(legEnd, player.position) < 2.8f)
            legEnd.y = Mathf.Min(Camera.UpLimit - 1.5f, legEnd.y + 2.8f);
        legControl = (legStart + legEnd) * .5f + Vector3.up * ((dash % 2 == 0 ? 1 : -1) * .7f);
        legControl.y = Mathf.Clamp(legControl.y, Camera.DownLimit + 3, Camera.UpLimit - 1.4f);
        legDuration = Mathf.Max(.32f, Vector3.Distance(legStart, legEnd) / (phaseTwo ? 13f : 10.2f));
        legClock = 0;
        dash++;
    }

    protected override void Attack()
    {
        if (!arrived || (player != null && Vector2.Distance(player.position, transform.position) < 2.5f))
            return;
        int beat = (volley - 1) % 8;
        if (beat == 0 || beat == 4)
            attackAxis = Aim;
        if (beat < 3)
        {
            var port = Hull.TransformPoint(new Vector3(beat % 2 == 0 ? -.27f : .27f, -.95f, -.2f));
            int count = phaseTwo ? 5 : 3;
            PetalFan<NeedleRound>(port, CombatVisuals.Rotate(attackAxis, (beat - 1) * 5), count, 11, phaseTwo ? 9.3f : 8.2f, .14f, .13f);
        }
        else if (beat == 4 || beat == 5)
        {
            // A broad alternating comb leaves lanes between shots while the next brood enters.
            int count = phaseTwo ? 9 : 7;
            PetalFan<SimpleRound>(transform.position, CombatVisuals.Rotate(attackAxis, beat == 4 ? -4.5f : 4.5f), count, 18, 6.2f, .14f, .22f);
        }
    }

    protected override float AttackPace => phaseTwo ? .80f : 1;
    protected override void OnPhaseTwo()
    {
        volley = 0;
        nextAttack = age + .6f;
        nextBrood = Mathf.Min(nextBrood, age + 1);
    }

    IEnumerator ReleaseBrood()
    {
        // Hatch expansion is the warning. Nothing spawns underneath the player's ship.
        for (float t = 0; t < .6f; t += Time.deltaTime)
        {
            foreach (var door in doors)
                door.localScale = Vector3.one * (.24f + .15f * Mathf.Sin(t / .6f * Mathf.PI));
            yield return null;
        }
        for (int row = 0; row < (phaseTwo ? 5 : 4); row++)
        {
            foreach (int side in new[] { -1, 1 })
            {
                if (LiveMinions >= 18)
                    break;
                var p = Hull.TransformPoint(new Vector3(side * .82f, -.35f, -.2f));
                if (player != null && Vector2.Distance(p, player.position) < 2.5f)
                    continue;
                var kind = row % 3 == 1 ? typeof(WaferDart) : row % 3 == 2 ? typeof(MintSpinner) : typeof(BonbonSkirmisher);
                var minion = Spawn(kind, p);
                minion.moneyDropChance = .10f;
                minion.lifeDropChance = minion.manaDropChance = 0;
                minion.scoreOnKill = 15;
                var flight = minion.gameObject.AddComponent<CandyMinionFlight>();
                flight.side = side;
                minion.UseMovement(flight);
                brood.Add(minion);
                SpawnedMinions++;
            }
            yield return new WaitForSeconds(.18f);
        }
        foreach (var door in doors)
            door.localScale = Vector3.one * .24f;
    }

    protected override void Die()
    {
        ClearBrood();
        base.Die();
    }

    void OnDestroy()
    {
        ClearBrood();
    }

    void ClearBrood()
    {
        foreach (var child in brood)
            if (child != null)
                Destroy(child.gameObject);
        brood.Clear();
    }
}

public sealed class CandyMinionFlight : PatternMover
{
    public int side;
    protected override void StepPattern(float dt)
    {
        if (GameRules.playerDisabled)
            return;
        float t = age / 3.8f;
        if (t >= 1)
        {
            GetComponent<AEnemy>().Depart();
            return;
        }
        float u = 1 - t;
        Vector3 a = origin, b = new Vector3(side * Camera.RightLimit * .82f, origin.y - 1, origin.z), c = new Vector3(-side * Camera.RightLimit * .38f, Camera.DownLimit + 4, origin.z), d = new Vector3(-side * Camera.RightLimit * .7f, Camera.DownLimit - 2, origin.z);
        transform.position = u * u * u * a + 3 * u * u * t * b + 3 * u * t * t * c + t * t * t * d;
        SetFiring(t > .24f && t < .72f);
    }
}

public static partial class IndustrialMeshFactory
{
    static Mesh candyHatch;
    public static Mesh CandyHatch()
    {
        if (candyHatch != null)
            return candyHatch;
        var b = new Builder();
        b.Drum(Vector3.zero, 1, .15f, 9, 8, 11);
        return candyHatch = b.Finish("Mint launch aperture");
    }

    static void BuildCandyRegent(Builder b)
    {
        // Split wafer wings, a dark ganache keel and a crown of sugar crystals.
        BevelArmor(b, new[] { V(0, -1.2f), V(-.46f, -.53f), V(-.50f, .55f), V(0, .86f), V(.50f, .55f), V(.46f, -.53f) }, .42f, 4);
        foreach (int s in new[] { -1, 1 })
        {
            b.Prism(new[] { V(s * .34f, -.45f), V(s * 1.34f, -.17f), V(s * 1.51f, .65f), V(s * .90f, .37f), V(s * .48f, .84f) }, .20f, 13, new Vector3(0, 0, -.04f), 5);
            b.Prism(new[] { V(s * .58f, -.20f), V(s * 1.27f, .02f), V(s * 1.34f, .50f), V(s * .93f, .25f) }, .13f, 0, new Vector3(0, 0, -.21f), 14);
            b.Box(new Vector3(s * .65f, .65f, 0), new Vector3(.33f, .28f, .30f), 11, 7);
            b.Drum(new Vector3(s * .82f, -.2f, -.19f), .33f, .16f, 11, 10, 10);
            b.Box(new Vector3(s * .27f, -.86f, -.23f), new Vector3(.15f, .43f, .15f), 11, 4);
        }
        b.Ball(new Vector3(0, -.08f, -.29f), new Vector3(.39f, .50f, .24f), 8);
        b.Prism(new[] { V(-.28f, 0), V(-.22f, .20f), V(.22f, .20f), V(.28f, 0), V(0, -.20f) }, .12f, 11, new Vector3(0, -.1f, -.51f), 14);
        b.Box(new Vector3(0, -.08f, -.59f), new Vector3(.34f, .075f, .03f), 9, 11);
        for (int i = -1; i <= 1; i++)
            b.Prism(new[] { V(-.13f, 0), V(0, .44f + (i == 0 ? .18f : 0)), V(.13f, 0) }, .18f, 10, new Vector3(i * .31f, .53f, -.31f), 3);
    }
}
