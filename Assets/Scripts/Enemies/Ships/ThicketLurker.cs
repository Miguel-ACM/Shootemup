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
        hullScale = .46f;
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

    // A mechanical band-winged locust. Leaf-green armour hides it in the canopy; in flight it
    // flashes fanned orange hind wings with a black band. Big folded jumping legs with orange
    // spring pistons, gold compound eyes, swept antennae and a twin arrow launcher under the jaw.
    public static Mesh ThicketLurkerMesh()
    {
        if (cachedThicketLurker != null)
            return cachedThicketLurker;
        var b = new Builder();
        foreach (int s in new[] { -1, 1 })
        {
            // Hind wing: a scalloped fan spread sideways from the shoulder, orange with a dark band.
            var root = new Vector2(s * .13f, -.2f);
            float[] reach = { .72f, .86f, .9f, .84f, .7f, .56f };
            Vector2 Fan(int i, float scale)
            {
                float a = Mathf.Lerp(-18, 58, i / 5f) * Mathf.Deg2Rad;
                return root + new Vector2(s * Mathf.Cos(a), Mathf.Sin(a)) * reach[i] * scale;
            }
            var membrane = new Vector2[7];
            membrane[0] = root;
            for (int i = 0; i < 6; i++)
                membrane[i + 1] = Fan(i, 1);
            b.Prism(membrane, .03f, 3, new Vector3(0, 0, .02f), 12, 11);
            var band = new Vector2[12];
            for (int i = 0; i < 6; i++)
            {
                band[i] = Fan(i, .86f);
                band[11 - i] = Fan(i, .7f);
            }
            b.Prism(band, .02f, 11, new Vector3(0, 0, -.005f), 11, 11);
            // Dark veins radiating across the membrane.
            for (int i = 1; i < 5; i++)
            {
                var tip = Fan(i, .97f);
                EquipmentRod(b, new Vector3(root.x, root.y, -.01f), new Vector3(tip.x, tip.y, -.01f), .012f, 11);
            }
            // Fore wing: a narrow olive leaf along the fan's leading edge, with a tan midrib.
            float lean = -14 * Mathf.Deg2Rad;
            var axis = new Vector2(s * Mathf.Cos(lean), Mathf.Sin(lean));
            var normal = new Vector2(-axis.y, axis.x) * s;
            var start = new Vector2(s * .1f, -.27f);
            Vector2 L(float along, float across) => start + axis * along + normal * across;
            BevelArmor(b, new[] { L(0, -.06f), L(.22f, -.15f), L(.58f, -.13f), L(.84f, 0), L(.58f, .1f), L(.22f, .13f), L(0, .06f) }, .08f, 14, -.07f, .2f);
            var mid0 = L(.04f, 0);
            var mid1 = L(.72f, 0);
            EquipmentRod(b, new Vector3(mid0.x, mid0.y, -.1f), new Vector3(mid1.x, mid1.y, -.1f), .014f, 13);

            // Jumping leg: a thick femur sweeping back to a high knee, the tibia folded under it,
            // an orange spring piston along the thigh.
            BevelArmor(b, new[] { V(s * .14f, -.02f), V(s * .27f, -.06f), V(s * .5f, .62f), V(s * .5f, .8f), V(s * .42f, .78f), V(s * .19f, .2f) }, .16f, 14, .02f, .2f);
            b.Prism(new[] { V(s * .27f, .1f), V(s * .32f, .1f), V(s * .47f, .6f), V(s * .43f, .62f) }, .02f, 13, new Vector3(0, 0, -.07f), 13, 13);
            EquipmentRod(b, new Vector3(s * .24f, .16f, -.12f), new Vector3(s * .42f, .66f, -.12f), .03f, 3);
            b.Ball(new Vector3(s * .47f, .8f, -.02f), new Vector3(.075f, .075f, .07f), 11);
            EquipmentRod(b, new Vector3(s * .47f, .8f, .06f), new Vector3(s * .36f, .14f, .06f), .028f, 6);
            for (int spine = 0; spine < 3; spine++)
            {
                var at = Vector3.Lerp(new Vector3(s * .47f, .8f, .06f), new Vector3(s * .36f, .14f, .06f), .25f + spine * .22f);
                b.Box(at + new Vector3(s * .035f, 0, 0), new Vector3(.05f, .018f, .018f), 11, 11);
            }
            b.Box(new Vector3(s * .35f, .1f, .06f), new Vector3(.08f, .07f, .05f), 11, 11);
            // Two pairs of small walking legs folded under the thorax.
            foreach (var (hip, knee, foot) in new[]
            {
                (new Vector3(s * .16f, -.42f, .04f), new Vector3(s * .33f, -.52f, .04f), new Vector3(s * .3f, -.7f, .04f)),
                (new Vector3(s * .17f, -.3f, .04f), new Vector3(s * .36f, -.36f, .04f), new Vector3(s * .42f, -.5f, .04f))
            })
            {
                EquipmentRod(b, hip, knee, .025f, 11);
                EquipmentRod(b, knee, foot, .02f, 6);
            }
            // Gold compound eye and a swept antenna.
            b.Ball(new Vector3(s * .17f, -.66f, -.13f), new Vector3(.085f, .1f, .08f), 10);
            EquipmentRod(b, new Vector3(s * .08f, -.78f, -.12f), new Vector3(s * .2f, -.98f, -.12f), .016f, 11);
            EquipmentRod(b, new Vector3(s * .2f, -.98f, -.12f), new Vector3(s * .38f, -1.08f, -.12f), .013f, 11);
            // Twin arrow launcher under the jaw.
            b.Box(new Vector3(s * .08f, -.88f, -.06f), new Vector3(.07f, .2f, .07f), 11, 6);
            b.Box(new Vector3(s * .08f, -.98f, -.06f), new Vector3(.095f, .04f, .095f), 6, 6);
        }
        // Segmented tan abdomen behind the wings.
        BevelArmor(b, new[] { V(-.15f, -.12f), V(.15f, -.12f), V(.17f, .36f), V(.11f, .78f), V(0, .9f), V(-.11f, .78f), V(-.17f, .36f) }, .2f, 13, .03f, .18f);
        for (int ring = 0; ring < 4; ring++)
        {
            float y = .12f + ring * .17f, w = Mathf.Lerp(.3f, .17f, ring / 3f);
            b.Box(new Vector3(0, y, -.075f), new Vector3(w, .03f, .02f), 11, 11);
        }
        // Saddle-shaped thorax shield with tan side stripes, and the blunt head.
        BevelArmor(b, new[] { V(-.24f, -.5f), V(.24f, -.5f), V(.29f, -.32f), V(.25f, -.1f), V(0, -.04f), V(-.25f, -.1f), V(-.29f, -.32f) }, .26f, 2, -.04f, .2f);
        foreach (int s in new[] { -1, 1 })
            b.Prism(new[] { V(s * .2f, -.46f), V(s * .25f, -.32f), V(s * .22f, -.13f), V(s * .18f, -.14f), V(s * .2f, -.32f), V(s * .16f, -.45f) }, .02f, 13, new Vector3(0, 0, -.18f), 13, 13);
        BevelArmor(b, new[] { V(-.15f, -.83f), V(.15f, -.83f), V(.21f, -.68f), V(.18f, -.5f), V(-.18f, -.5f), V(-.21f, -.68f) }, .24f, 2, -.06f, .22f);
        b.Box(new Vector3(0, -.84f, -.1f), new Vector3(.14f, .05f, .06f), 11, 11);
        return cachedThicketLurker = b.Finish("Thicket lurker locust hull and arrow launcher");
    }
}
