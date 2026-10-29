using UnityEngine;
using System.Collections.Generic;

// A nod to Zero Ranger's drill: a spinning lance held in front of the ship. It grinds whatever it
// touches, drags hostile rounds around it to a crawl and grows after enough damage.
[UnityEngine.Scripting.Preserve]
public sealed class Drill : AWeaponAutomatic
{
    // Cumulative damage that grows the drill to its second and third size.
    static readonly float[] Growth = { 0, 900, 2600 };
    static readonly float[] Scales = { 1, 1.3f, 1.6f };
    const float BaseLength = 1.5f, BaseWidth = .7f, SlowRate = .35f;
    static Drill spinning;
    static Vector2 slowBase, slowTip;
    static float slowRadius;
    readonly List<AEnemy> targets = new List<AEnemy>();
    Transform model;
    float dealt, extension, spin, scale = 1, shownScale = 1;
    int stage;

    void Awake()
    {
        // Damage ticks; the drill itself spins continuously while held.
        shootSpeedModifier = 1.3f;
    }

    /// <summary>Hostile round speed factor near a running drill (1 elsewhere).</summary>
    public static float SlowAt(Vector3 position)
    {
        if (spinning == null)
            return 1;
        Vector2 p = position, axis = slowTip - slowBase;
        float t = Mathf.Clamp01(Vector2.Dot(p - slowBase, axis) / Mathf.Max(.0001f, axis.sqrMagnitude));
        return (p - (slowBase + axis * t)).sqrMagnitude <= slowRadius * slowRadius ? SlowRate : 1;
    }

    bool Held => !GameRules.playerDisabled && (EffectiveInput == WeaponInput.both ? !isStopingPrimary || !isStopingSecondary :
        EffectiveInput == WeaponInput.primary ? !isStopingPrimary : !isStopingSecondary);

    float Length => BaseLength * scale;
    float Width => BaseWidth * scale;
    Vector2 Base => transform.position + Vector3.up * .45f;

    public override void Shoot()
    {
        if (!KeepFiring() || extension < .6f)
            return;
        Vector2 start = Base, tip = start + Vector2.up * Length * extension;
        targets.Clear();
        targets.AddRange(AEnemy.Live);
        bool hit = false;
        foreach (var enemy in targets)
        {
            if (enemy == null || !enemy.CanBeTargeted || !Touches(enemy, start, tip, out var point))
                continue;
            float amount = GameRules.playerShootDamage * 1.6f * scale * MagnifyingLens.Power;
            float damage = CombatUpgrades.Current != null ? CombatUpgrades.Current.DirectHit(enemy, amount) : amount;
            enemy.HitAt(point, Vector2.up);
            enemy.damage(damage);
            Score.IncreaseScore(Mathf.RoundToInt(damage));
            dealt += damage;
            hit = true;
            PSXImpactEffects.Burst(point, new Color(1f, .8f, .4f), false, Random.insideUnitCircle);
        }
        if (hit)
            ItemSound.Play("fan", .05f, 1.9f, .09f);
        while (stage + 1 < Growth.Length && dealt >= Growth[stage + 1])
            Grow();
    }

    void Grow()
    {
        stage++;
        scale = Scales[stage];
        PSXFireballs.Shockwave(Base + Vector2.up * Length * .5f, Length, .45f, new Color(1f, .75f, .3f), 2, .2f);
        ItemSound.Play("ready", .16f, .8f);
    }

    bool Touches(AEnemy enemy, Vector2 start, Vector2 tip, out Vector2 point)
    {
        point = enemy.transform.position;
        var collider = enemy.GetComponent<Collider2D>();
        if (collider == null || !collider.enabled)
            return false;
        Vector2 axis = tip - start;
        float lengthSquared = Mathf.Max(.0001f, axis.sqrMagnitude);
        Vector2 probe = start + axis * Mathf.Clamp01(Vector2.Dot((Vector2)collider.bounds.center - start, axis) / lengthSquared);
        for (int i = 0; i < 3; i++)
            probe = start + axis * Mathf.Clamp01(Vector2.Dot(collider.ClosestPoint(probe) - start, axis) / lengthSquared);
        // The drill tapers: full width at its collar, a point at the tip.
        float taper = 1 - Vector2.Dot(probe - start, axis) / lengthSquared * .7f;
        point = probe;
        return (collider.ClosestPoint(probe) - probe).sqrMagnitude <= Mathf.Pow(Width * .5f * taper, 2);
    }

    void LateUpdate()
    {
        if (Menu.GameplayPaused)
            return;
        float dt = WorldTimeStop.PlayerDelta;
        bool held = Held && enabled;
        extension = Mathf.MoveTowards(extension, held ? 1 : 0, dt * (held ? 7 : 5));
        shownScale = Mathf.Lerp(shownScale, scale, 1 - Mathf.Exp(-8 * dt));
        if (extension > .05f && model == null)
            Build();
        if (model == null)
            return;
        model.gameObject.SetActive(extension > .05f);
        spin += dt * (held ? 1500 : 500);
        model.position = new Vector3(Base.x, Base.y, transform.position.z - .05f);
        model.localRotation = Quaternion.AngleAxis(spin, Vector3.up);
        model.localScale = new Vector3(shownScale, shownScale * Mathf.SmoothStep(0, 1, extension), shownScale);
        if (extension > .6f)
        {
            spinning = this;
            slowBase = Base;
            slowTip = Base + Vector2.up * Length * extension;
            slowRadius = Width * .9f;
        }
        else if (spinning == this)
            spinning = null;
    }

    void Build()
    {
        var go = new GameObject("Drill lance");
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.DrillLance(BaseLength, BaseWidth * .5f);
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CombatMaterials.Fleet;
        var hull = GetComponentInParent<Shipcontroller>()?.GetComponent<SpriteRenderer>();
        if (hull != null)
        {
            renderer.sortingLayerID = hull.sortingLayerID;
            renderer.sortingOrder = Mathf.Max(1, hull.sortingOrder) - 1;
        }
        model = go.transform;
    }

    public override void StopFiring()
    {
        base.StopFiring();
        if (spinning == this)
            spinning = null;
    }

    void OnDisable()
    {
        if (spinning == this)
            spinning = null;
        if (model != null)
            model.gameObject.SetActive(false);
    }
}
