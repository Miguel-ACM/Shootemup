using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SandWorm : DistrictEnemy
{
    Transform[] segments;
    SandSpine spine;
    Vector3 previous;
    protected override void Configure()
    {
        hull = 32;
        hullScale = .49f;
        health = 80;
        attackInterval = Timing(2.5f,1.8f);
        Mover<Slalom>();
    }

    protected override void Start()
    {
        base.Start();
        segments = new Transform[4];
        previous = transform.position;
        spine = new SandSpine(transform.position, .50f, segments.Length);
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i] = SandSpine.Model("Sandworm articulated emitter " + i, transform, IndustrialMeshFactory.SandWormSegment(), hullScale * (1 - i * .09f));
            segments[i].position = transform.position + Vector3.up * (i + 1) * .5f;
            segments[i].gameObject.layer = 8;
            segments[i].gameObject.tag = "Enemy";
            var hit = segments[i].gameObject.AddComponent<CircleCollider2D>();
            hit.isTrigger = true;
            hit.radius = .62f;
            var section = segments[i].gameObject.AddComponent<SandWormSection>();
            section.owner = this;
            firingCue.Register(segments[i],new Vector3(0,0,-.32f));
        }
        // The articulated model owns its orientation, including the head.
        GetComponent<CombatHullVisual>().enabled = false;
    }

    void LateUpdate()
    {
        if (spine == null)
            return;
        Vector2 tangent = transform.position - previous;
        previous = transform.position;
        if (tangent.sqrMagnitude > .0001f)
            Hull.rotation = Quaternion.FromToRotation(Vector3.down, tangent);
        spine.Follow(transform.position, segments);
    }

    protected override void Attack()
    {
        if (segments == null)
            return;
        // Alternating body ports create a travelling accent rather than a blob from the head.
        for (int i = volley % 2; i < segments.Length; i += 2)
        {
            Vector3 port = segments[i].TransformPoint(new Vector3(0, 0, -.32f));
            if (port.y > Camera.UpLimit - .6f || port.y < Camera.DownLimit + 1)
                continue;
            if (player != null && Vector2.Distance(player.position, port) < 2)
                continue;
            Vector2 aim = CombatVisuals.Aim(port);
            for (int j = 0; j < Shots(2,3); j++)
                Fire<SimpleRound>(port, CombatVisuals.Rotate(aim, (j-(Shots(2,3)-1)*.5f)*22), 3.7f, .19f);
        }
    }
}

public sealed class SandWormSection : AEnemy
{
    public SandWorm owner;
    protected override void Start()
    {
    }

    void Update()
    {
        if (owner != null)
            health = owner.health;
    }

    public override void damage(float amount)
    {
        if (owner != null)
            owner.damage(amount);
    }

    protected override void OnBecameInvisible()
    {
    }
}

public static partial class IndustrialMeshFactory
{
    static Mesh sandSegment;
    public static Mesh SandWormSegment()
    {
        if (sandSegment != null)
            return sandSegment;
        var b = new Builder();
        b.Ball(Vector3.zero, new Vector3(.46f, .53f, .24f), 11);
        for (int i = 0; i < 3; i++)
        {
            float y = .27f - i * .25f;
            BevelArmor(b, new[] { V(-.52f, y + .13f), V(0, y + .26f), V(.52f, y + .13f), V(.48f, y - .09f), V(0, y + .01f), V(-.48f, y - .09f) }, .18f, 13, -.14f);
        }
        foreach (int s in new[] { -1, 1 })
            b.Prism(new[] { V(s * .35f, .18f), V(s * .74f, .38f), V(s * .63f, -.16f), V(s * .35f, -.3f) }, .10f, 6, Vector3.zero, 11);
        b.Drum(new Vector3(0, -.05f, -.30f), .15f, .11f, 11, 8);
        b.Drum(new Vector3(0, -.05f, -.37f), .08f, .035f, 3, 8);
        return sandSegment = b.Finish("Sandworm overlapping emitter scales");
    }

    static void BuildSandWormHead(Builder b)
    {
        BevelArmor(b, new[] { V(-.46f, .51f), V(.46f, .51f), V(.60f, -.18f), V(.29f, -.78f), V(-.29f, -.78f), V(-.60f, -.18f) }, .38f, 13);
        foreach (int s in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(s * .19f, -.12f), V(s * .54f, -.28f), V(s * .45f, -.96f), V(s * .12f, -.64f) }, .24f, 6, -.08f);
            b.Box(new Vector3(s * .30f, .03f, -.30f), new Vector3(.09f, .25f, .04f), 3, 11);
        }
        b.Box(new Vector3(0, -.1f, -.24f), new Vector3(.22f, .62f, .16f), 11, 6);
        BevelArmor(b, new[] { V(-.32f, .4f), V(.32f, .4f), V(.22f, .08f), V(-.22f, .08f) }, .14f, 1, -.27f);
    }
}
