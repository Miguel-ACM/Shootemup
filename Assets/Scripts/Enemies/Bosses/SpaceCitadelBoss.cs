using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed partial class SpaceCitadelBoss : DistrictBoss
{
    readonly SpaceBossPart[] parts = new SpaceBossPart[1];
    PulseLaser centralLaser;
    bool completing;
    public SpaceBossPart[] Parts => parts;
    public SpaceBossPart Reactor => parts[0];
    public bool IsDefeated => completing;
    public bool CoreExposed => Reactor != null && Reactor.health > 0;
    public override bool CanBeTargeted => false;
    protected override void Configure()
    {
        hullScale = 1;
        health = 2500;
        attackInterval = .68f;
        nextAttack = 1.7f;
    }

    protected override void Start()
    {
        base.Start();
        GetComponent<Collider2D>().enabled = false;
        GetComponent<CombatHullVisual>().enabled = false;
        parts[0] = SpaceBossPart.Create(this, Hull, health);
        firingCue.Register(parts[0].transform, Vector3.back * .25f);
    }

    protected override void PressureAttack() { }
    protected override void MoveBoss(float dt)
    {
        var target = new Vector3(centralLaser != null && centralLaser.IsAttachedTo(Hull) ? transform.position.x : Mathf.Sin(motionAge * .43f) * .82f,
            Camera.UpLimit - 3 + Mathf.Sin(motionAge * .31f) * .32f, 1);
        bool drawingIn = accretionOrbit != null && accretionOrbit.enabled;
        if (drawingIn)
        {
            // Carry the orbit through the dodge lanes and return to the upper firing station.
            float t = Mathf.Clamp01((motionAge - orbitStarted) / Rest(phaseThree ? 10 : 8));
            float dive = Mathf.Sin(t * Mathf.PI);
            target.y = Mathf.Lerp(target.y, CombatRound.Border.yMin + 4.1f, dive * dive);
        }
        transform.position = Vector3.MoveTowards(transform.position, target, dt * 3.2f);
        if (drawingIn)
            UpdateOrbitTell();
    }

    protected override void OnPhaseTwo()
    {
        CloseWell();
        volley = 0;
        nextAttack = age + 1.2f;
    }

    public void PartChanged()
    {
        if (completing || Reactor == null) return;
        health = Mathf.Max(0, Reactor.health);
        if (health > 0) return;
        completing = true;
        CloseWell();
        health = 1;
        base.damage(2 / Mathf.Max(.001f, GameRules.enemyDamageReceived));
    }

    public override void damage(float amount) { } // Only the central event horizon accepts hits.
}

public sealed class SpaceBossPart : AEnemy
{
    SpaceCitadelBoss owner;
    public bool IsExposed => CanBeTargeted;
    public override bool CanBeTargeted => health > 0 && owner != null && !owner.IsDefeated && !owner.Invulnerable;
    public override bool CanReceiveDamage => base.CanReceiveDamage && !owner.Invulnerable;
    public static SpaceBossPart Create(SpaceCitadelBoss owner, Transform parent, float health)
    {
        var go = new GameObject("Central event horizon weakpoint");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0, 0, -.38f);
        go.layer = 8; go.tag = "Enemy";
        go.AddComponent<SpriteRenderer>().enabled = false;
        var collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true; collider.radius = .76f;
        var visual = new GameObject("Dark event horizon");
        visual.transform.SetParent(go.transform, false);
        visual.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CitadelReactor();
        var renderer = visual.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = SpaceBlackHoleVisual.Core; renderer.sortingLayerName = "Enemy";
        PartHitFeedback.Bind(visual.transform);
        var part = go.AddComponent<SpaceBossPart>();
        part.owner = owner; part.health = health;
        part.lifeDropChance = part.manaDropChance = part.moneyDropChance = 0;
        part.scoreOnKill = 0;
        return part;
    }
    public override void damage(float amount)
    {
        if (!CanReceiveDamage) return;
        base.damage(amount);
        owner.PartChanged();
    }
    protected override void Die()
    {
        health = 0;
        GetComponent<Collider2D>().enabled = false;
        enabled = false; // The event horizon remains visible for the continuous finale.
    }
    protected override void OnBecameInvisible() { }
}
