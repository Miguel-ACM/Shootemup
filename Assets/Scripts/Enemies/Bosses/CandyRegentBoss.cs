using UnityEngine;

// The candy regent: a faceted sweetheart on a lace doily, crowned with sugar crystals,
// with one big eye and two lollipop wands. It drifts calmly through the upper arena, but
// its attention does not: the eye snaps from one thing to the next, the wands swing to
// point at it, the crown shivers and the heart throbs in bursts of excitement between
// calm moments. Its attacks leave the wands, the eye and the crown.
[UnityEngine.Scripting.Preserve]
public sealed partial class CandyRegentBoss : DistrictBoss
{
    protected override bool AtOpeningStation => arrived;
    bool arrived;
    Transform pupil, crown;
    readonly Transform[] wands = new Transform[2];
    Vector3 baseScale;
    static readonly Vector3 WandTip = new Vector3(0, -.98f, -.12f);
    static readonly Vector3 EyeCentre = new Vector3(0, .02f, -.56f);

    // Attention: what the eye holds, how excited the regent is and how hard it has crashed.
    Vector3 focus;
    bool focused;
    float excitement, beat, slump;
    Vector2 hop;
    public int AttentionShifts
    {
        get; private set;
    }

    protected override void Configure()
    {
        hullScale = .85f;
        health = 1750;
        attackInterval = .58f;
        nextAttack = 2.0f;
    }

    protected override void Start()
    {
        base.Start();
        baseScale = Hull.localScale;
        pupil = Part("Regent pupil", IndustrialMeshFactory.CandyRegentPupil(), EyeCentre);
        crown = Part("Regent sugar crown", IndustrialMeshFactory.CandyRegentCrown(), new Vector3(0, .55f, -.12f));
        for (int i = 0; i < 2; i++)
            wands[i] = Part(i == 0 ? "Left lollipop wand" : "Right lollipop wand", IndustrialMeshFactory.CandyRegentWand(), new Vector3(i == 0 ? -.98f : .98f, -.05f, -.16f));
    }

    Transform Part(string name, Mesh mesh, Vector3 local)
    {
        var go = new GameObject(name);
        go.transform.SetParent(Hull, false);
        go.transform.localPosition = local;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CandyMaterials.Glaze;
        renderer.sortingLayerName = "Enemy";
        return go.transform;
    }

    // Firing ports on the animated parts.
    Vector3 WandPort(int side) => wands[side < 0 ? 0 : 1].TransformPoint(WandTip);
    Vector3 EyePort => pupil.position;
    Vector3 CrownPort(int side) => crown.TransformPoint(new Vector3(side * .3f, .5f, -.05f));

    // The eye snaps to a new thing; the regent twitches toward it and gets a little excited.
    void Fixate(Vector3 world)
    {
        focus = world;
        focused = true;
        slump = 0;
        AttentionShifts++;
        excitement = Mathf.Min(1, excitement + .3f);
        hop = Vector2.ClampMagnitude((Vector2)(world - transform.position), 1) * .5f;
    }

    void LookAway() => focused = false;

    void Beat() => beat = 1;

    void Thrill() => excitement = 1;

    protected override void MoveBoss(float dt)
    {
        float half = UnityEngine.Camera.main.orthographicSize * 405f / 720f;
        float width = Mathf.Min(Camera.RightLimit, half) - 1.5f;
        float lower = (Camera.UpLimit + Camera.DownLimit) * .5f + 1.1f;
        float upper = Camera.UpLimit - 1.7f;
        if (!arrived)
        {
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(0, Camera.UpLimit - 2.4f, 1), dt * 7.5f);
            arrived = transform.position.y <= Camera.UpLimit - 2.39f;
            return;
        }
        // A slow drift through the upper arena; only a twitch toward each new fixation.
        var target = new Vector3(Mathf.Sin(motionAge * .23f) * width * .6f,
            Mathf.Lerp(lower + .5f, upper, .55f + .45f * Mathf.Sin(motionAge * .31f + 1)), 1);
        hop = Vector2.Lerp(hop, Vector2.zero, 1 - Mathf.Exp(-dt * 3));
        target += (Vector3)hop;
        if (player != null && Vector2.Distance(target, player.position) < 2.6f)
            target.y += 1.2f;
        target.x = Mathf.Clamp(target.x, -width, width);
        target.y = Mathf.Clamp(target.y, lower, upper + .3f);
        transform.position = Vector3.MoveTowards(transform.position, target, dt * (2.2f + excitement * 4));
    }

    protected override float AttackPace => phaseTwo ? (.88f) : 1;
    protected override void OnPhaseTwo()
    {
        volley = 0;
        nextAttack = age + .6f;
        LookAway();
        slump = 0;
    }

    protected override void OnPhaseThree()
    {
        LookAway();
        slump = 0;
    }

    // The regent's body language: the eye, the wands, the crown and the heartbeat.
    void LateUpdate()
    {
        if (Hull == null || pupil == null || health <= 0 || GameRules.playerDisabled || WorldTimeStop.Frozen)
            return;
        float dt = Time.deltaTime * LevelRun.EnemyTempoScale;
        float t = age;
        excitement = Mathf.MoveTowards(excitement, 0, dt * .55f);
        beat = Mathf.MoveTowards(beat, 0, dt * 4);
        // An idle heartbeat that races with excitement, and a strong throb on each beat.
        float pulse = Mathf.Pow(Mathf.Max(0, Mathf.Sin(t * Mathf.PI * 2 * (1.1f + 1.6f * excitement))), 12);
        Hull.localScale = baseScale * (1 + .035f * pulse + .07f * beat);
        // The eye holds its target, daydreams between fixations and sags in a crash.
        Vector3 look;
        if (focused)
        {
            Vector2 toward = Hull.InverseTransformDirection(focus - pupil.position);
            look = toward.sqrMagnitude > .0001f ? (Vector3)(toward.normalized * .14f) : Vector3.zero;
        }
        else
            look = new Vector3(Mathf.Sin(t * .7f) * .09f, Mathf.Sin(t * 1.13f) * .05f, 0);
        look = Vector3.Lerp(look, new Vector3(0, -.13f, 0), slump);
        pupil.localPosition = Vector3.MoveTowards(pupil.localPosition, EyeCentre + look, dt * (focused ? 3.5f : .5f));
        // The wands snap to point at the fixation and hang loose otherwise.
        for (int i = 0; i < 2; i++)
        {
            int side = i == 0 ? -1 : 1;
            float angle = side * (32 + 6 * Mathf.Sin(t * 1.3f + i));
            if (focused)
            {
                Vector2 toward = Hull.InverseTransformDirection(focus - wands[i].position);
                if (toward.sqrMagnitude > .0001f)
                    angle = Vector2.SignedAngle(Vector2.down, toward);
            }
            angle = Mathf.LerpAngle(angle, side * 8, slump);
            angle += excitement * 9 * Mathf.Sin(t * 31 + i * 2);
            float next = Mathf.MoveTowardsAngle(wands[i].localEulerAngles.z, angle, dt * (focused ? 900 : 160));
            wands[i].localRotation = Quaternion.Euler(0, 0, next);
        }
        // The crown shivers with excitement and sinks in a crash.
        crown.localRotation = Quaternion.Euler(0, 0, excitement * 13 * Mathf.Sin(t * 22));
        crown.localPosition = new Vector3(0, .55f + excitement * .04f * Mathf.Abs(Mathf.Sin(t * 22)) - slump * .08f, -.12f);
    }
}

public static partial class IndustrialMeshFactory
{
    static Mesh candyRegentHeart, candyRegentPupil, candyRegentWand, candyRegentCrown;

    // A heart outline (the classic sixteen-sine curve), centred on its visual middle.
    static Vector2[] HeartShape(float scale, int points = 20, float lift = .15f)
    {
        var outline = new Vector2[points];
        for (int i = 0; i < points; i++)
        {
            float t = i * Mathf.PI * 2 / points;
            float s = Mathf.Sin(t);
            outline[i] = new Vector2(16 * s * s * s,
                13 * Mathf.Cos(t) - 5 * Mathf.Cos(2 * t) - 2 * Mathf.Cos(3 * t) - Mathf.Cos(4 * t)) / 17f * scale + V(0, lift);
        }
        return outline;
    }

    // The regent's body: a faceted candy heart with a raised striped heart, icing piping,
    // blushing cheeks and a big eye, over a chocolate rim and a lace doily.
    public static Mesh CandyRegentHeart()
    {
        if (candyRegentHeart != null)
            return candyRegentHeart;
        var b = new Builder();
        // Lace doily: scalloped cream and white rounds behind the sides and top.
        for (int i = 0; i < 13; i++)
        {
            float a = Mathf.Lerp(-125, 125, i / 12f) * Mathf.Deg2Rad;
            var c = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * 1.3f + V(0, .02f);
            b.Drum(new Vector3(c.x, c.y, .24f), .27f, .05f, i % 2 == 0 ? 15 : 1, 7, 13);
        }
        // Chocolate rim, then the faceted shell with sloping shoulders.
        b.Prism(HeartShape(1.25f), .22f, 4, new Vector3(0, -.02f, .1f), 5, 4);
        var shell = HeartShape(1.15f);
        BevelArmor(b, shell, .6f, 12, 0, .2f);
        // A raised striped heart in the middle of the shell.
        BevelArmor(b, HeartShape(.66f, 20, .1f), .32f, 8, -.24f, .22f);
        // Icing piping along the shoulder.
        for (int i = 0; i < shell.Length; i++)
        {
            var p = shell[i] * .93f;
            CutStone(b, new Vector3(p.x, p.y, -.14f), new Vector3(.075f, .075f, .06f), 15, 5, i * .4f);
        }
        // Blushing cheeks and the eye with three lashes.
        for (int side = -1; side <= 1; side += 2)
        {
            CutStone(b, new Vector3(side * .5f, -.2f, -.36f), new Vector3(.14f, .08f, .04f), 0, 6);
            CutBlade(b, new[] { new Vector3(side * .18f, .38f, -.46f), new Vector3(side * .27f, .5f, -.47f), new Vector3(side * .38f, .55f, -.47f) }, .03f, .03f, 11);
        }
        CutBlade(b, new[] { new Vector3(0, .42f, -.46f), new Vector3(.02f, .53f, -.47f), new Vector3(.01f, .62f, -.47f) }, .03f, .03f, 11);
        CutStone(b, new Vector3(0, .02f, -.44f), new Vector3(.36f, .4f, .09f), 15, 10);
        return candyRegentHeart = b.Finish("Candy regent sweetheart");
    }

    // A chocolate pupil with a sugar glint; it moves inside the eye.
    public static Mesh CandyRegentPupil()
    {
        if (candyRegentPupil != null)
            return candyRegentPupil;
        var b = new Builder();
        CutStone(b, Vector3.zero, new Vector3(.15f, .17f, .05f), 11, 8);
        CutStone(b, new Vector3(.05f, .06f, -.06f), new Vector3(.045f, .045f, .02f), 15, 5);
        return candyRegentPupil = b.Finish("Candy regent pupil");
    }

    // A lollipop wand hanging from its shoulder knob along local -y: a striped stick and a
    // mint swirl disc in a gold band. The disc is the firing port.
    public static Mesh CandyRegentWand()
    {
        if (candyRegentWand != null)
            return candyRegentWand;
        var b = new Builder();
        CutStone(b, Vector3.zero, new Vector3(.13f, .13f, .09f), 10, 6);
        b.Box(new Vector3(0, -.46f, 0), new Vector3(.08f, .8f, .08f), 15, 0);
        var disc = new Vector3(0, -.98f, -.02f);
        CutStone(b, disc, new Vector3(.34f, .34f, .09f), 2, 10);
        CutHalo(b, disc + Vector3.back * .04f, .31f, .035f, Vector3.one, Quaternion.identity, 10, 10);
        for (int arm = 0; arm < 3; arm++)
        {
            var path = new Vector3[4];
            for (int k = 0; k < 4; k++)
            {
                float a = (arm * 120 + k * 55) * Mathf.Deg2Rad;
                float r = .05f + k * .07f;
                path[k] = disc + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, -.1f);
            }
            CutBlade(b, path, .03f, .02f, 12);
        }
        CutStone(b, disc + Vector3.back * .1f, new Vector3(.07f, .07f, .03f), 15, 6);
        return candyRegentWand = b.Finish("Candy regent lollipop wand");
    }

    // A sugar crown: a gold band, three aqua crystal spikes and a heart jewel.
    public static Mesh CandyRegentCrown()
    {
        if (candyRegentCrown != null)
            return candyRegentCrown;
        var b = new Builder();
        b.Prism(new[] { V(-.32f, 0), V(.32f, 0), V(.26f, .15f), V(-.26f, .15f) }, .16f, 10, Vector3.zero, 13, 13);
        CutBlade(b, new[] { new Vector3(0, .12f, -.02f), new Vector3(0, .38f, -.04f), new Vector3(0, .64f, -.04f) }, .1f, .07f, 9);
        for (int side = -1; side <= 1; side += 2)
        {
            CutBlade(b, new[] { new Vector3(side * .2f, .12f, -.02f), new Vector3(side * .28f, .3f, -.04f), new Vector3(side * .36f, .5f, -.04f) }, .08f, .06f, 9);
            CutStone(b, new Vector3(side * .36f, .52f, -.05f), new Vector3(.05f, .05f, .03f), 10, 5);
        }
        b.Prism(HeartShape(.15f, 12, 0), .08f, 12, new Vector3(0, .72f, -.04f), 0, 4);
        return candyRegentCrown = b.Finish("Candy regent sugar crown");
    }
}
