using UnityEngine;

// A Plains ambusher hiding in the trees. It picks a big canopy on screen, the tree shakes and
// sheds leaves while it coils (the warning), then it leaps out of the crown, hovers with a visible
// charge, looses aimed arrows and dashes away, firing once more. Rare in the open valley, common
// in the FOREST. It is a district hazard: it never holds a wave open, and homing ignores it until
// it has left the tree.
[UnityEngine.Scripting.Preserve]
public sealed class ThicketLurker : DistrictEnemy
{
    enum Stage
    {
        Hidden, Leap, Hover, Dash
    }

    const float HideSeconds = 1.25f, LeapSeconds = .4f, HoverSeconds = .6f;
    static readonly Color Leaf = new Color(.42f, .62f, .26f), DryLeaf = new Color(.62f, .55f, .26f);
    Stage stage = Stage.Hidden;
    float timer, leafTimer, dashSign;
    Transform section, shaking;
    Vector3 pivot;
    Mesh crown;
    bool secondVolley;

    public override bool HoldsWave => false;
    public override bool CanBeTargeted => stage != Stage.Hidden && base.CanBeTargeted;
    protected override Vector2 CueAxis => Vector2.down;

    protected override void Configure()
    {
        hullScale = .52f;
        health = 24;
        attackInterval = float.PositiveInfinity;
        nextAttack = float.PositiveInfinity;
    }

    // Choose a canopy on screen and hide in it; false when no suitable tree is in view.
    public static bool Ambush(PSXGrassField field, System.Random random)
    {
        if (field == null || !field.TryPickTree(random, out var section, out var plant, out float origin))
            return false;
        var pivot = new Vector3(plant.x, plant.y - origin, 0);
        var world = section.TransformPoint(pivot);
        var lurker = (ThicketLurker)Spawn(typeof(ThicketLurker), new Vector3(world.x, world.y, 1));
        lurker.section = section;
        lurker.pivot = pivot;
        lurker.dashSign = world.x > 0 ? -1 : 1;
        lurker.BuildShakingCrown(field, plant, origin);
        return true;
    }

    // An exact copy of the tree's crown over the original, so the tree itself seems to shake.
    void BuildShakingCrown(PSXGrassField field, PSXValleyLayout.Plant plant, float origin)
    {
        crown = PSXValleyFoliage.Build(origin, 24, new[] { plant });
        var holder = new GameObject("Shaking canopy").transform;
        holder.SetParent(section, false);
        holder.localPosition = pivot;
        var copy = new GameObject("Canopy copy");
        copy.transform.SetParent(holder, false);
        copy.transform.localPosition = -pivot;
        copy.AddComponent<MeshFilter>().sharedMesh = crown;
        var renderer = copy.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = field.FoliageMaterial;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = -39;
        shaking = holder;
    }

    protected override void Start()
    {
        base.Start();
        scoreOnKill = 60;
        Hull.localScale = Vector3.zero;
        GetComponent<Collider2D>().enabled = false;
    }

    protected override void Update()
    {
        if (FrozenHull.Holds(gameObject) || WorldTimeStop.Frozen || !GameClock.ActivePlay)
            return;
        float dt = Time.deltaTime;
        timer += dt;
        switch (stage)
        {
            case Stage.Hidden:
                Hide(dt);
                break;
            case Stage.Leap:
                Leap();
                break;
            case Stage.Hover:
                Hover();
                break;
            default:
                Dash(dt);
                break;
        }
        SettleCrown(dt);
    }

    void Hide(float dt)
    {
        if (section == null || !DistrictHazards.Open)
        {
            // The moment passed: nothing leaves the tree.
            Depart();
            return;
        }
        var world = section.TransformPoint(pivot);
        transform.position = new Vector3(world.x, world.y, 1);
        // The crown trembles harder as the lurker coils to jump.
        float urgency = timer / HideSeconds;
        float shake = Mathf.Lerp(2, 9, urgency) * Mathf.Sin(timer * Mathf.Lerp(18, 46, urgency));
        shaking.localRotation = Quaternion.Euler(0, 0, shake);
        shaking.localScale = Vector3.one * (1 + .04f * Mathf.Abs(Mathf.Sin(timer * 23)));
        leafTimer -= dt;
        if (leafTimer <= 0)
        {
            leafTimer = Mathf.Lerp(.22f, .07f, urgency);
            var drop = (Vector3)(Random.insideUnitCircle * .5f) + Vector3.back;
            PSXImpactEffects.Burst(transform.position + drop, Random.value < .7f ? Leaf : DryLeaf, false, Vector2.down);
        }
        if (timer < HideSeconds)
            return;
        stage = Stage.Leap;
        timer = 0;
        for (int i = 0; i < 6; i++)
            PSXImpactEffects.Burst(transform.position + (Vector3)(Random.insideUnitCircle * .7f) + Vector3.back, i % 2 == 0 ? Leaf : DryLeaf, true, Random.insideUnitCircle);
        SoundManager.PlayEnemyEvent("enemyMissile");
        GetComponent<Collider2D>().enabled = true;
    }

    void Leap()
    {
        float t = Mathf.Clamp01(timer / LeapSeconds), ease = 1 - (1 - t) * (1 - t);
        // Up out of the crown: it grows from treetop size to full flight size with a hop.
        Hull.localScale = Vector3.one * hullScale * Mathf.Lerp(.25f, 1, ease);
        var world = section != null ? section.TransformPoint(pivot) : transform.position;
        transform.position = new Vector3(world.x, world.y + .9f * ease + .35f * Mathf.Sin(Mathf.PI * t), 1);
        if (t < 1)
            return;
        stage = Stage.Hover;
        timer = 0;
    }

    void Hover()
    {
        transform.position += Vector3.up * Mathf.Sin(timer * 9) * .004f;
        Cue(Muzzle(0), Mathf.Max(0, HoverSeconds - timer));
        if (timer < HoverSeconds)
            return;
        Volley(Dense(3), 3.6f);
        stage = Stage.Dash;
        timer = 0;
    }

    void Dash(float dt)
    {
        // Away to the far side and up, quickening, with one parting shot.
        float speed = Mathf.Lerp(1.5f, 5.5f, Mathf.Clamp01(timer / .8f)) * LevelRun.EnemyTempoScale;
        transform.position += new Vector3(dashSign, .75f, 0).normalized * speed * dt;
        if (!secondVolley && timer > .55f)
        {
            secondVolley = true;
            Volley(Dense(2), 3.2f);
        }
        if (timer > .3f && !secondVolley)
            Cue(Muzzle(1), .55f - timer);
        var p = transform.position;
        if (p.y > Camera.UpLimit + 1.5f || p.x < Camera.LeftLimit - 1.5f || p.x > Camera.RightLimit + 1.5f)
            Depart();
    }

    void Volley(int count, float speed)
    {
        var aim = CombatVisuals.Aim(Muzzle(0));
        for (int i = 0; i < count; i++)
        {
            float spread = count == 1 ? 0 : (i - (count - 1) * .5f) * 14;
            Fire<NeedleRound>(Muzzle(i), CombatVisuals.Rotate(aim, spread), speed, .2f);
        }
        volley++;
    }

    void SettleCrown(float dt)
    {
        if (stage == Stage.Hidden || shaking == null)
            return;
        // After the jump the tree sways back to rest, then the copy is removed.
        float settle = Mathf.Exp(-(timer + (stage == Stage.Leap ? 0 : LeapSeconds)) * 4);
        shaking.localRotation = Quaternion.Euler(0, 0, 10 * settle * Mathf.Sin(Time.time * 20));
        if (settle < .02f)
            RemoveCrown();
    }

    void RemoveCrown()
    {
        if (shaking != null)
            Destroy(shaking.gameObject);
        shaking = null;
        if (crown != null)
            Destroy(crown);
        crown = null;
    }

    protected override void Attack()
    {
    }

    void OnDestroy()
    {
        RemoveCrown();
    }
}

public static partial class IndustrialMeshFactory
{
    static Mesh cachedThicketLurker;

    // A squat leaf-camouflaged hopper: an olive shell under layered leaf plates, four folded
    // spring legs for the jump, a red sensor eye and a twin arrow launcher under its chin.
    public static Mesh ThicketLurkerMesh()
    {
        if (cachedThicketLurker != null)
            return cachedThicketLurker;
        var b = new Builder();
        BevelArmor(b, new[] { V(0, -.62f), V(-.42f, -.38f), V(-.5f, .18f), V(-.24f, .55f), V(.24f, .55f), V(.5f, .18f), V(.42f, -.38f) }, .34f, 2);
        // Layered leaf plates over the back.
        var leaves = new[] { new Vector3(-.22f, .22f, 20), new Vector3(.22f, .24f, -24), new Vector3(0, .42f, 0), new Vector3(-.3f, -.02f, 40), new Vector3(.3f, -.04f, -40) };
        foreach (var leaf in leaves)
        {
            float a = leaf.z * Mathf.Deg2Rad;
            Vector2 P(float x, float y) => new Vector2(leaf.x + x * Mathf.Cos(a) - y * Mathf.Sin(a), leaf.y + x * Mathf.Sin(a) + y * Mathf.Cos(a));
            b.Prism(new[] { P(0, -.2f), P(.12f, -.02f), P(.07f, .16f), P(0, .22f), P(-.07f, .16f), P(-.12f, -.02f) }, .06f, leaf.z > 0 ? 14 : 2, new Vector3(0, 0, -.22f), 11);
        }
        // Four folded spring legs, knees out.
        foreach (int sx in new[] { -1, 1 })
            foreach (int sy in new[] { -1, 1 })
            {
                var hip = new Vector3(sx * .4f, sy * .26f, .02f);
                var knee = new Vector3(sx * .72f, sy * .42f, -.08f);
                var foot = new Vector3(sx * .62f, sy * .62f, .1f);
                EquipmentRod(b, hip, knee, .055f, 11);
                EquipmentRod(b, knee, foot, .045f, 6);
                b.Ball(foot, new Vector3(.07f, .07f, .05f), 11);
            }
        Glass(b, 0, -.3f, .3f, .22f, -.28f, 5);
        foreach (int s in new[] { -1, 1 })
        {
            b.Box(new Vector3(s * .16f, -.6f, -.16f), new Vector3(.09f, .3f, .09f), 11, 6);
            b.Box(new Vector3(s * .16f, -.76f, -.16f), new Vector3(.12f, .05f, .11f), 6, 6);
        }
        return cachedThicketLurker = b.Finish("Thicket lurker hull and arrow launcher");
    }
}
