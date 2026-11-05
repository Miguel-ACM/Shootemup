using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static readonly Mesh[] heavenBastions = new Mesh[6];
    static void SanctuaryPillar(Builder b, Vector3 foot, float height, float width = .16f, bool broken = false)
    {
        b.Box(foot, new Vector3(width * 3.1f, .14f, width * 3), 1, 15);
        b.Box(foot + Vector3.up * .13f, new Vector3(width * 2.5f, .12f, width * 2.5f), 15, 3);
        for (int face = 0; face < 8; face++)
        {
            Vector3 P(int i, bool top)
            {
                float a = i * Mathf.PI / 4;
                return foot + new Vector3(Mathf.Cos(a) * width * (top ? .82f : 1),
                    top ? height + (broken ? Mathf.Sin(i * 5.1f) * .12f : 0) : .18f, Mathf.Sin(a) * width);
            }
            b.Face(face % 3 == 0 ? 1 : 15, P(face, false), P(face, true), P(face + 1, true), P(face + 1, false));
            b.Face(1, P(face, true), foot + Vector3.up * height, P(face + 1, true));
        }
        if (broken) return;
        b.Box(foot + Vector3.up * (height - .10f), new Vector3(width * 2.2f, .10f, width * 2.2f), 3, 7);
        b.Box(foot + Vector3.up * height, new Vector3(width * 3.1f, .14f, width * 3), 15, 1);
    }
    static void SanctuaryArch(Builder b, float cx, float floor, float radius, float spring, float z, bool broken = false)
    {
        for (int side = -1; side <= 1; side += 2)
            SanctuaryPillar(b, new Vector3(cx + side * radius, floor, z), spring - floor, radius > .8f ? .19f : .13f);
        for (int stone = 0; stone < 13; stone++)
        {
            if (broken && stone > 5 && stone < 10) continue;
            float a = stone * Mathf.PI / 13, c = (stone + .96f) * Mathf.PI / 13;
            Vector2 P(float angle, float r) => new Vector2(cx + Mathf.Cos(angle) * r, spring + Mathf.Sin(angle) * r);
            BevelArmor(b, new[] { P(a, radius - .13f), P(c, radius - .13f), P(c, radius + .15f), P(a, radius + .15f) },
                .33f, stone == 6 ? 3 : stone % 4 == 0 ? 1 : 15, z);
            b.Face(3, (Vector3)P(a, radius - .12f) + Vector3.forward * (z - .18f),
                (Vector3)P(c, radius - .12f) + Vector3.forward * (z - .18f),
                (Vector3)P(c, radius - .16f) + Vector3.forward * (z - .18f),
                (Vector3)P(a, radius - .16f) + Vector3.forward * (z - .18f));
        }
    }
    static void SanctuaryRoof(Builder b, float x, float y, float z, float width, float height)
    {
        BevelArmor(b, new[] { V(x - width, y), V(x, y + height), V(x + width, y) }, .46f, 15, z);
        CutBlade(b, new[] { new Vector3(x - width - .08f, y, z - .27f), new Vector3(x, y + height + .09f, z - .27f),
            new Vector3(x + width + .08f, y, z - .27f) }, .045f, .025f, 3);
        b.Box(new Vector3(x, y, z), new Vector3(width * 2.2f, .15f, .58f), 15, 1);
    }
    static void SanctuaryWing(Builder b, Vector3 root, int side, float size)
    {
        for (int feather = 0; feather < 5; feather++)
            Feather(b, root + Vector3.up * feather * .035f,
                root + new Vector3(side * (.50f + feather * .13f), .12f + feather * .16f, 0) * size,
                .075f * size, .04f * side, feather % 2 == 0 ? 15 : 3);
    }
    static void SanctuaryPennant(Builder b, float x, int side)
    {
        var foot = new Vector3(x, -.06f, -.46f);
        b.Box(foot + Vector3.up * .87f, new Vector3(.035f, 1.84f, .035f), 7, 3);
        Feather(b, foot + Vector3.up * 1.78f, foot + Vector3.up * 2.0f, .065f, 0, 3);
        // Tile twelve identifies only cloth for the shared wind shader.
        b.Surface(6, 7, (u, v) => new Vector3(x + side * (.03f + u * .53f),
            1.62f - v * (.84f + .10f * Mathf.Cos(u * 13)), -.46f + Mathf.Sin(u * 4 + v * 2) * .07f), 12);
    }
    public static Mesh HeavenBastion(int variant)
    {
        if (heavenBastions[variant] != null) return heavenBastions[variant];
        var b = new Builder();
        // Continuous terraces, paving and inset stairs seat each landmark in its bank.
        for (int layer = 0; layer < 3; layer++)
        {
            float w = 2.04f - layer * .12f, y = -.54f + layer * .11f;
            BevelArmor(b, new[] { V(-w, y), V(w * .77f, y - .06f), V(w, y + .42f),
                V(w * .83f, y + .83f), V(-w * .84f, y + .91f), V(-w * 1.02f, y + .35f) },
                .16f, layer == 1 ? 1 : 15, .30f - layer * .19f);
        }
        for (int tile = 0; tile < 7; tile++)
            for (int row = 0; row < 2; row++)
                b.Box(new Vector3(-1.35f + tile * .45f, -.11f + row * .3f, -.18f),
                    new Vector3(.41f, .26f, .035f), (tile + row) % 4 == 0 ? 1 : 15, 1);
        for (int step = 0; step < 3; step++)
            b.Box(new Vector3(0, -.5f - step * .12f, -.28f + step * .09f),
                new Vector3(.85f + step * .16f, .18f, .18f), 15, 1);
        switch (variant)
        {
            case 0: // Crowned gate and open side windows.
                SanctuaryArch(b, 0, .05f, .81f, 1.12f, -.35f);
                SanctuaryRoof(b, 0, 2.01f, -.35f, 1.20f, .47f);
                SanctuaryArch(b, -1.45f, -.02f, .35f, .67f, -.10f);
                SanctuaryArch(b, 1.45f, -.02f, .35f, .67f, -.10f);
                CutStone(b, new Vector3(0, 2.27f, -.62f), new Vector3(.12f, .16f, .06f), 3, 4);
                break;
            case 1: // Breached arch with exposed courses and fallen stones.
                SanctuaryArch(b, -.20f, .04f, 1.03f, .90f, -.34f, true);
                SanctuaryPillar(b, new Vector3(1.54f, .03f, -.07f), .69f, .20f, true);
                for (int block = 0; block < 4; block++)
                    CutStone(b, new Vector3(.55f + block * .32f, .10f + (block % 2) * .13f, -.39f),
                        new Vector3(.19f, .13f, .17f), block % 2 == 0 ? 1 : 15, 5, block * .6f);
                SanctuaryPennant(b, -1.62f, 1);
                break;
            case 2: // Roofless cloister with two ranks of fluted columns.
                for (int rank = 0; rank < 2; rank++)
                    for (int col = 0; col < 4; col++)
                        SanctuaryPillar(b, new Vector3(-1.35f + col * .9f, rank * .38f, -.14f + rank * .40f),
                            col == 3 && rank == 0 ? .60f : 1.33f, .14f, col == 3 && rank == 0);
                b.Box(new Vector3(-.45f, 1.44f, -.14f), new Vector3(2.25f, .20f, .42f), 15, 1);
                b.Box(new Vector3(0, 1.82f, .26f), new Vector3(3.23f, .21f, .44f), 15, 1);
                b.Box(new Vector3(0, 1.86f, -.01f), new Vector3(3.32f, .055f, .055f), 3, 7);
                break;
            case 3: // A small shrine with a tall faceted gilded spire.
                SanctuaryArch(b, 0, .12f, .61f, 1.03f, -.35f);
                SanctuaryRoof(b, 0, 1.70f, -.31f, .88f, .38f);
                BevelArmor(b, new[] { V(-.35f, 2.0f), V(0, 2.86f), V(.35f, 2.0f) }, .50f, 3, -.16f);
                SanctuaryPillar(b, new Vector3(-1.26f, -.02f, -.13f), .83f, .15f);
                SanctuaryPillar(b, new Vector3(1.26f, -.02f, -.13f), .83f, .15f);
                SanctuaryWing(b, new Vector3(0, 1.79f, -.67f), -1, .56f);
                SanctuaryWing(b, new Vector3(0, 1.79f, -.67f), 1, .56f);
                break;
            case 4: // Winged war memorial on a marble plinth.
                b.Box(new Vector3(0, .33f, -.30f), new Vector3(.89f, .48f, .65f), 15, 1);
                BevelArmor(b, new[] { V(-.23f, .48f), V(-.15f, 1.93f), V(0, 2.20f), V(.15f, 1.93f), V(.23f, .48f) }, .36f, 15, -.33f);
                b.Box(new Vector3(0, 1.08f, -.54f), new Vector3(.055f, 1.03f, .04f), 3, 7);
                SanctuaryWing(b, new Vector3(0, 1.02f, -.40f), -1, 1.14f);
                SanctuaryWing(b, new Vector3(0, 1.02f, -.40f), 1, 1.14f);
                SanctuaryPennant(b, -1.45f, 1);
                SanctuaryPillar(b, new Vector3(1.45f, .04f, -.19f), .54f, .19f, true);
                break;
            default: // Long arcade, broken at one end.
                for (int arch = 0; arch < 3; arch++)
                    SanctuaryArch(b, -1.18f + arch * 1.18f, .04f, .49f, .90f, -.24f, arch == 2);
                b.Box(new Vector3(-.59f, 1.49f, -.24f), new Vector3(2.63f, .16f, .44f), 15, 1);
                b.Box(new Vector3(-.59f, 1.58f, -.24f), new Vector3(2.75f, .055f, .5f), 3, 7);
                break;
        }
        // Spear-point palisades flank the stairs. Some lances are broken.
        for (int side = -1; side <= 1; side += 2)
        {
            b.Box(new Vector3(side * 1.38f, -.21f, -.55f), new Vector3(1.10f, .045f, .045f), 3, 7);
            for (int lance = 0; lance < 4; lance++)
            {
                var foot = new Vector3(side * (.91f + lance * .28f), -.49f, -.55f);
                float h = .52f + lance * .13f;
                bool broken = lance == variant % 4;
                if (broken) h *= .56f;
                var tip = foot + new Vector3(side * (lance == 3 ? .13f : 0), h, -.025f);
                CutBlade(b, new[] { foot, Vector3.Lerp(foot, tip, .35f), tip }, .023f, .02f, 7);
                if (!broken) Feather(b, tip, tip + Vector3.up * .19f, .065f, 0, 3);
            }
        }
        return heavenBastions[variant] = b.Finish("Heaven / cloud sanctuary " + variant);
    }
}
