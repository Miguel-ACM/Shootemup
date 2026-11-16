using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class DivingGear : OffensiveActiveItem
{
    void Awake()
    {
        SetData("Diving Gear", "Below the fire", "Dive for 3 seconds of invulnerability. Keep steering. 45 energy.", ItemType.active);
        cost = 45;
        cd = 6;
    }

    protected override OffensiveCast Launch() =>
        s.DepthDive == null ? CreateCast<ShipDepthDive>("Diving Gear depth dive") : null;
}

public sealed class ShipDepthDive : OffensiveCast
{
    public const float Duration = 3;
    internal const float WorldDepth = 1.05f;
    static readonly Color Foam = new Color(.47f, .78f, 1);
    PSXShipVisual hull;
    PSXShipEffects effects;
    Transform model;
    Vector3 restPosition, restScale;
    float nextBubble;

    protected override void Begin()
    {
        Owner.DepthDive = this;
        hull = Owner.GetComponent<PSXShipVisual>();
        effects = Owner.GetComponent<PSXShipEffects>();
        model = hull != null ? hull.Model : null;
        if (model != null)
        {
            restPosition = model.localPosition;
            restScale = model.localScale;
        }
        Wake(.85f);
        ItemSound.Cue("roll", volume: .16f, pitch: .62f, cooldown: 0);
    }

    public static float DepthAt(float time) =>
        Mathf.SmoothStep(0, 1, time / .30f) * (1 - Mathf.SmoothStep(0, 1, (time - (Duration - .38f)) / .38f));

    protected override void Step(float dt)
    {
        if (Age >= Duration)
        {
            Wake(.67f);
            ItemSound.Cue("roll", volume: .1f, pitch: 1.17f, cooldown: 0);
            Finish();
            return;
        }
        float depth = DepthAt(Age);
        ApplyDepth(depth);
        if (depth > .5f && Age >= nextBubble)
        {
            nextBubble = Age + .17f;
            ItemSpark.Emit(Owner.transform.position + new Vector3(0, -.3f, -1), Foam, 2, .074f);
        }
    }

    protected override void Update()
    {
        if (CombatDecision.Active || CloudTransition.Busy || GameRules.playerDisabled || HangarHub.Active)
        {
            Finish();
            return;
        }
        base.Update();
    }

    void ApplyDepth(float depth)
    {
        if (model != null)
        {
            model.localPosition = restPosition + model.parent.InverseTransformVector(Vector3.forward * (WorldDepth * depth));
            model.localScale = restScale * (1 - .12f * depth);
        }
        if (hull != null) hull.DiveDepth = depth;
        if (effects != null) effects.DiveDepth = depth;
    }

    void Wake(float radius)
    {
        var at = Owner.transform.position;
        PSXFireballs.Shockwave(at, radius, .32f, Foam, 1);
        ItemSpark.Emit(at, Foam, 7, .11f);
    }

    void OnDisable()
    {
        ApplyDepth(0);
        if (Owner != null && Owner.DepthDive == this)
            Owner.DepthDive = null;
    }
}
