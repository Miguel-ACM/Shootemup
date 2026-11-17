using UnityEngine;

public sealed class InkQuill : DistrictEnemy
{
    protected override void Configure()
    {
        health = 46;
        hullScale = .54f;
    }

    protected override void Attack()
    {
        // A stroke starts with a heavy dot. The lighter marks follow the same slant
        // at different speeds, curling into a short comma as the formation passes.
        float slant = (volley & 1) == 0 ? -.37f : .37f;
        int marks = Dense(5);
        Vector3 nib = Muzzle();
        for (int i = 0; i < marks; i++)
        {
            float ink = i / (float)Mathf.Max(1, marks - 1);
            Fire<CrescentRound>(nib, new Vector2(slant * (1 - ink * .8f), -1),
                4.3f - ink * 1.9f, .26f - ink * .09f, i == 0)?.Sway(slant * (.30f + ink), .62f);
        }
    }
}

public static partial class IndustrialMeshFactory
{
    static Mesh inkQuillMesh;

    public static Mesh BuildInkQuill()
    {
        if (inkQuillMesh != null) return inkQuillMesh;
        var b = new Builder();
        // Cut into both vanes so daylight shows through the stepped feather edge.
        Vector3 Spine(float t) => new Vector3(-.19f + .57f * t, -.40f + 1.40f * t, -.15f + .08f * t);
        for (int i = 0; i < 4; i++)
        {
            float t = i * .20f;
            float breadth = Mathf.Sin((t + .20f) * Mathf.PI) * .49f + .07f;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 root = Spine(t);
                Vector3 reach = new Vector3(side * breadth * (side > 0 ? 1f : .68f), .29f, .11f);
                FoldedFin(b, root, root + reach * .56f, root + reach + Vector3.up * .12f,
                    Spine(t + .23f), root + reach * .50f + Vector3.back * .09f, side > 0 ? 7 : 4, i % 2 == 0 ? 15 : 5);
            }
        }
        CutBlade(b, new[] { Spine(0), Spine(.54f), Spine(1) }, .052f, .048f, 15);
        BevelArmor(b, new[] { V(-.30f, -.36f), V(-.08f, -.28f), V(-.08f, -.60f),
            V(-.26f, -.88f), V(-.41f, -.58f) }, .13f, 13, -.12f);
        CutBlade(b, new[] { new Vector3(-.23f, -.52f, -.21f), new Vector3(-.26f, -.84f, -.19f) },
            .026f, .018f, 11);
        CutStone(b, new Vector3(-.23f, -.50f, -.20f), new Vector3(.058f, .068f, .025f), 11, 5);
        return inkQuillMesh = b.Finish(nameof(InkQuill));
    }
}

public static partial class WavePartLibrary
{
    public static readonly WavePart[] InkQuillParts =
    {
        DeepPart("Signature in the margin", nameof(InkQuill), WaveShape.Train, 4, 5.4f, 2, new[] { .26f, .47f, .66f },
            -1.40f, .59f, -.74f, .68f, -.32f, .86f, .02f, .75f, .45f, .55f, 1.4f, .66f),
        DeepPart("The crossed T", nameof(InkQuill), WaveShape.Rake, 3, 5.6f, 5, new[] { .26f, .43f, .62f, .75f },
            .62f, 1.36f, .48f, .92f, .12f, .68f, -.18f, .65f, -.54f, .67f, -1.4f, .66f),
        DeepPart("Downstroke", nameof(InkQuill), WaveShape.Chevron, 3, 5.4f, 8, new[] { .24f, .45f, .65f },
            -.51f, 1.36f, -.32f, .94f, .13f, .79f, .32f, .59f, .10f, .28f, -.20f, -.32f),
    };
}
