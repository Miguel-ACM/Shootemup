using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SpaceCitadelBoss : DistrictBoss
{
    readonly SpaceBossPart[] parts = new SpaceBossPart[5];
    readonly Transform[] shutters = new Transform[2];
    PulseLaser centralLaser;
    bool completing;
    float iris;
    Vector2 barrageAxis = Vector2.down;
    public SpaceBossPart[] Parts => parts;
    public bool IsDefeated => completing;
    public bool CoreExposed => parts[0] != null && parts[1] != null && parts[0].health <= 0 && parts[1].health <= 0;
    public override bool CanBeTargeted => false;
    protected override void Configure()
    {
        hull = 45;
        hullScale = 1;
        health = 2500;
        attackInterval = Timing(.9f, .68f);
        nextAttack = 1.7f;
    }

    protected override void Start()
    {
        base.Start();
        GetComponent<Collider2D>().enabled = false;
        GetComponent<CombatHullVisual>().enabled = false;
        parts[0] = SpaceBossPart.Create(this, Hull, 0, new Vector3(-1.95f, .25f, -.08f), health * .13f);
        parts[1] = SpaceBossPart.Create(this, Hull, 0, new Vector3(1.95f, .25f, -.08f), health * .13f);
        parts[2] = SpaceBossPart.Create(this, Hull, 1, new Vector3(-1.13f, -1.0f, -.12f), health * .11f);
        parts[3] = SpaceBossPart.Create(this, Hull, 1, new Vector3(1.13f, -1.0f, -.12f), health * .11f);
        parts[4] = SpaceBossPart.Create(this, Hull, 2, new Vector3(0, 0, -.08f), health * .52f);
        for (int i = 0; i < 2; i++)
            firingCue.Register(parts[i].transform, new Vector3(0, -.3f, -.3f));
        for (int i = 2; i < 4; i++)
            foreach (int side in new[] { -1, 1 })
                firingCue.Register(parts[i].transform, new Vector3(side * .16f, -.695f, -.08f));
        for (int i = 0; i < 2; i++)
        {
            var go = new GameObject("Retracting reactor shutter " + i);
            go.transform.SetParent(Hull, false);
            go.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.SpaceModule(3);
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = SpaceMaterials.Fleet;
            renderer.sortingLayerName = "Enemy";
            go.transform.localScale = new Vector3(i == 0 ? 1 : -1, 1, 1);
            shutters[i] = go.transform;
        }
    }

    protected override bool PhaseTwoReady => CoreExposed;
    protected override void PressureAttack()
    {
        // Every generator, gun and reactor now belongs to the same audible score.
    }

    protected override void MoveBoss(float dt)
    {
        var target = new Vector3(centralLaser != null && centralLaser.IsAttachedTo(Hull) ? transform.position.x : Mathf.Sin(motionAge * .43f) * .82f, Camera.UpLimit - 3.0f + Mathf.Sin(motionAge * .31f) * .32f, 1);
        transform.position = Vector3.MoveTowards(transform.position, target, dt * 3.2f);
        iris = Mathf.MoveTowards(iris, CoreExposed ? 1 : 0, dt * 1.8f);
        for (int i = 0; i < shutters.Length; i++)
            if (shutters[i] != null)
                shutters[i].localPosition = new Vector3((i == 0 ? -1 : 1) * iris * .72f, iris * .12f, 0);
        if (parts[4] != null)
            parts[4].SetExposed(CoreExposed);
    }

    protected override void Attack()
    {
        if (parts[0] == null)
            return;
        int beat = (volley - 1) % 12;
        if (beat == 4)
        {
            centralLaser = Fire<PulseLaser>(Hull.TransformPoint(new Vector3(0, -1.48f, -.30f)), Vector2.down, 0);
            if (centralLaser != null)
            {
                centralLaser.Bind(Hull, new Vector3(0, -1.48f, -.30f));
                centralLaser.warning = Timing(1.6f, 1.3f);
                centralLaser.activeTime = Timing(1.1f, 1.5f);
                centralLaser.width = phaseTwo ? .42f : .34f;
            }
        }
        else if (beat == 0)
        {
            for (int i = 0; i < 2; i++)
            {
                var port = parts[i].health > 0 ? parts[i].transform.TransformPoint(new Vector3(0, -.3f, -.3f)) : Muzzle(i + 1);
                PetalFan<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, i == 0 ? -23 : 23), Shots(phaseTwo ? 5 : 3, phaseTwo ? 7 : 5), 24, 1.8f, .34f, .2f);
            }
        }
        else if (beat == 2 || beat == 3)
        {
            for (int i = 2; i < 4; i++)
            {
                bool turret = parts[i] != null && parts[i].health > 0;
                if (!turret)
                    continue;
                int side = i == 2 ? -1 : 1, count = Shots(phaseTwo ? 5 : 4, phaseTwo ? 7 : 5);
                // Half-lane replies form nested mirrored arcs, rather than repeating identical rays.
                var axis = CombatVisuals.Rotate(Vector2.down, side * (35 + (beat - 2) * 12));
                var port = parts[i].transform.TransformPoint(new Vector3(beat == 2 ? -.16f : .16f, -.695f, -.08f));
                PetalFan<RicochetBolt>(port, axis, count, 21, 3.1f, .23f, .16f);
            }
        }
        else if (beat == 6 || beat == 7)
        {
            if (beat == 6)
                barrageAxis = Aim;
            for (int side = -1; side <= 1; side += 2)
            {
                var port = Hull.TransformPoint(new Vector3(side * .4f, -.9f, -.4f));
                PetalFan<NeedleRound>(port, CombatVisuals.Rotate(barrageAxis, side * (beat == 6 ? 21 : 9)), Shots(phaseTwo ? 5 : 3, phaseTwo ? 7 : 5), 17, 4.3f, .2f);
            }
        }
        else if (beat == 9 || beat == 10)
        {
            // Core pressure survives gun destruction. Each exposed-reactor phrase
            // answers the laser with a slow, bowed fan and half-lane replies.
            if (beat == 9)
                barrageAxis = Aim;
            PetalFan<SimpleRound>(Hull.TransformPoint(new Vector3(0, -.7f, -.5f)),
                CombatVisuals.Rotate(barrageAxis, beat == 9 ? -9 : 9), Shots(phaseTwo ? 7 : 5, phaseTwo ? 11 : 9), 19, 2.6f, .29f, .18f);
        }
        // Beats 1, 5, 8 and 11 deliberately breathe; broken guns reduce ricochets.
    }

    protected override void PrepareFire(float seconds)
    {
        base.PrepareFire(seconds);
        firingCue.ClearWarning();
        if (parts[0] == null)
            return;
        int beat = volley % 12;
        if (beat == 0)
        {
            for (int i = 0; i < 2; i++)
                Cue(parts[i].health > 0 ? parts[i].transform.TransformPoint(new Vector3(0, -.3f, -.3f)) : Muzzle(i + 1), seconds);
        }
        else if (beat == 2 || beat == 3)
        {
            for (int i = 2; i < 4; i++)
                if (parts[i].health > 0)
                    Cue(parts[i].transform.TransformPoint(new Vector3(beat == 2 ? -.16f : .16f, -.695f, -.08f)), seconds);
        }
        else if (beat == 4)
            Cue(Muzzle(), seconds);
        else if (beat == 6 || beat == 7)
        {
            Cue(Muzzle(1), seconds);
            Cue(Muzzle(2), seconds);
        }
        else if (beat == 9 || beat == 10)
            Cue(Muzzle(3), seconds);
    }

    protected override void OnPhaseTwo()
    {
        volley = 0;
        nextAttack = age + 1.2f;
    }

    public void PartChanged()
    {
        if (completing)
            return;
        if (parts[4] != null && parts[4].health <= 0)
        {
            completing = true;
            base.damage(health / Mathf.Max(.001f, GameRules.enemyDamageReceived) + 1);
            return;
        }
        float total = 0;
        foreach (var part in parts)
            if (part != null)
                total += Mathf.Max(0, part.health);
        health = total;
        if (parts[4] != null)
            parts[4].SetExposed(CoreExposed);
    }

    public override void damage(float amount)
    {
    } // Hull hits cannot bypass generators or the core gate.
}

public sealed class SpaceBossPart : AEnemy
{
    SpaceCitadelBoss owner;
    bool exposed;
    int kind;
    MeshRenderer model;
    public bool IsExposed => exposed;
    public override bool CanBeTargeted => exposed && health > 0 && owner != null && !owner.IsDefeated;
    public static SpaceBossPart Create(SpaceCitadelBoss owner, Transform parent, int kind, Vector3 position, float health)
    {
        var go = new GameObject(kind == 0 ? "Breakable shield generator" : kind == 1 ? "Breakable ricochet turret" : "Protected reactor core");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.layer = 8;
        go.tag = "Enemy";
        go.AddComponent<SpriteRenderer>().enabled = false;
        var collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = kind == 0 ? .48f : kind == 1 ? .38f : .51f;
        var visual = new GameObject("Orbital module model");
        visual.transform.SetParent(go.transform, false);
        visual.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.SpaceModule(kind);
        var renderer = visual.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = SpaceMaterials.Fleet;
        renderer.sortingLayerName = "Enemy";
        PartHitFeedback.Bind(visual.transform);
        var part = go.AddComponent<SpaceBossPart>();
        part.owner = owner;
        part.health = health;
        part.kind = kind;
        part.model = renderer;
        part.SetExposed(kind != 2);
        part.lifeDropChance = part.manaDropChance = part.moneyDropChance = 0;
        part.scoreOnKill = 0;
        return part;
    }

    public void SetExposed(bool value)
    {
        if (exposed == value && GetComponent<Collider2D>().enabled == value)
            return;
        exposed = value;
        GetComponent<Collider2D>().enabled = value && health > 0;
        var tint = new MaterialPropertyBlock();
        tint.SetColor("_Tint", value ? Color.white : new Color(.24f, .48f, .70f));
        model.SetPropertyBlock(tint);
    }

    public override void damage(float amount)
    {
        if (!CanBeTargeted || owner == null)
            return;
        base.damage(amount);
        if (owner != null)
            owner.PartChanged();
    }

    protected override void Die()
    {
        health = 0;
        GetComponent<Collider2D>().enabled = false;
        if (kind != 2)
        {
            PSXPolygonDebris.Spawn(gameObject);
            PSXImpactEffects.Explosion(transform.position, .65f);
            model.enabled = false;
            Score.IncreaseScore(80);
        }
        enabled = false;
    }

    protected override void OnBecameInvisible()
    {
    }
}
