using UnityEngine;

// Umbrella: pops open over the ship for a moment and stops every round that comes down on it
// from above. Fire arriving from below the ship's line still gets through, and beams pass.
[UnityEngine.Scripting.Preserve]
public sealed class Umbrella : OffensiveActiveItem
{
    void Awake()
    {
        SetData("Umbrella", "Rain or shine", "Open an umbrella overhead that stops fire from above. 30 energy.", ItemType.active);
        cost = 30;
        cd = 2.5f;
    }

    protected override OffensiveCast Launch() => CreateCast<UmbrellaCanopy>("Open umbrella");
}

public sealed class UmbrellaCanopy : OffensiveCast
{
    // The sheltered box: the canopy's width, from just above the hull to its tip.
    const float Open = 1.8f, Unfold = .14f, Width = 1.7f, HalfWidth = .9f, Floor = .1f, Top = 1.45f, Lift = .45f;
    static readonly Color Rain = new Color(.75f, .9f, 1f);
    readonly System.Collections.Generic.List<CombatRound> caught = new System.Collections.Generic.List<CombatRound>();
    Transform model;
    Vector3 size;
    float lean;

    protected override void Begin()
    {
        model = Model(nameof(Umbrella), Width);
        model.GetComponent<MeshRenderer>().sortingLayerName = "Player";
        size = model.localScale;
        Follow(0);
        ItemSound.Cue("roll", volume: .12f, pitch: 1.4f);
    }

    // The canopy sits over the ship with its handle at the hull and leans into the motion.
    void Follow(float dt)
    {
        transform.position = Owner.transform.position + new Vector3(0, Lift, -.3f);
        if (dt > 0)
            lean = Mathf.Lerp(lean, Mathf.Clamp(-Owner.MoveInput.x * 14, -14, 14), 1 - Mathf.Exp(-8 * dt));
        float folding = Mathf.Min(Mathf.Clamp01(Age / Unfold), Mathf.Clamp01((Open - Age) / Unfold));
        float open = Mathf.SmoothStep(.15f, 1, folding);
        model.localScale = new Vector3(size.x * open, size.y * Mathf.Lerp(1.15f, 1, open), size.z * open);
        model.localRotation = Quaternion.Euler(0, Age * 40, lean);
    }

    protected override void Step(float dt)
    {
        Follow(dt);
        if (Age >= Open)
        {
            Finish();
            return;
        }
        if (Age < Unfold * .6f)
            return;
        Vector2 ship = Owner.transform.position;
        caught.Clear();
        foreach (var round in CombatRound.Live)
        {
            if (!BulletReflection.CanReflect(round))
                continue;
            Vector2 p = round.transform.position;
            if (Mathf.Abs(p.x - ship.x) < HalfWidth && p.y > ship.y + Floor && p.y < ship.y + Top)
                caught.Add(round);
        }
        foreach (var round in caught)
        {
            ItemSpark.Emit(round.transform.position, Rain, 2, .06f);
            CombatUpgrades.Current?.Block();
            round.Vanish();
        }
        if (caught.Count > 0)
            ItemSound.Cue("itemRicochet", volume: .04f, pitch: 1.5f, cooldown: .09f);
    }
}
