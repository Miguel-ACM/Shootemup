using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class Bookmark : OffensiveActiveItem
{
    void Awake()
    {
        SetData("Bookmark", "Save your place", "Mark this spot. Press again within 4 seconds to jump back. 25 energy.", ItemType.active);
        cost = 25;
        cd = 6;
    }

    protected override OffensiveCast Launch() => CreateCast<BookmarkMarker>("Bookmark marker");
    protected override bool Repress() => Cast is BookmarkMarker marker && marker.Return();
}

public sealed class BookmarkMarker : OffensiveCast
{
    const float Window = 4;
    static readonly Color Ink = new Color(.42f, .87f, .94f);
    LineRenderer ring;

    protected override void Begin()
    {
        ring = CombatVisuals.Line(transform, Ink, .035f);
        CombatVisuals.Ring(ring, transform.position, .33f);
        ItemSound.Cue("itemFocus", volume: .10f, pitch: 1.3f);
    }

    protected override void Step(float dt)
    {
        if (Age >= Window)
        {
            ring.enabled = false;
            Finish();
            return;
        }
        var color = Ink;
        color.a = Mathf.Lerp(.9f, .3f, Age / Window) * (.8f + .2f * Mathf.Cos(Age * 9));
        ring.startColor = ring.endColor = color;
        CombatVisuals.Ring(ring, transform.position, Mathf.Lerp(.33f, .19f, Age / Window));
    }

    protected override void Update()
    {
        if (CloudTransition.Busy)
        {
            Finish();
            return;
        }
        base.Update();
    }

    public bool Return()
    {
        if (!enabled || Age >= Window)
            return false;
        Vector3 from = Owner.transform.position;
        if (!Owner.TryReturnTo(transform.position))
        {
            ItemSound.Cue("itemFocus", volume: .07f, pitch: .55f);
            return false;
        }
        ItemProcVfx.Emit(ItemProcShape.Return, from, Ink, .5f);
        ItemProcVfx.Emit(ItemProcShape.Return, Owner.transform.position, Ink, .65f);
        ItemSound.Play("roll", .12f, 1.3f);
        ring.enabled = false;
        Finish();
        return true;
    }
}
