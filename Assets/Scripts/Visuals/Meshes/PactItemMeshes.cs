using UnityEngine;
public static partial class IndustrialMeshFactory
{
    // Pact models: each bargain reads as an object, tinted through its identity colour.
    static bool PactItem(Builder b, string kind)
    {
        switch (kind)
        {
            case "GlassHull":
                // A faceted crystal fighter with a pale fracture running down the hull.
                b.Prism(new[] { new Vector2(0, .5f), new Vector2(.14f, .12f), new Vector2(.4f, -.22f), new Vector2(.12f, -.3f), new Vector2(0, -.42f), new Vector2(-.12f, -.3f), new Vector2(-.4f, -.22f), new Vector2(-.14f, .12f) }, .18f, 9, Vector3.zero, 6);
                b.Prism(new[] { new Vector2(-.02f, .4f), new Vector2(.03f, .1f), new Vector2(-.03f, -.02f), new Vector2(.02f, -.3f), new Vector2(-.01f, -.3f), new Vector2(-.06f, -.02f), new Vector2(0, .1f) }, .01f, 15, new Vector3(0, 0, -.01f), 15);
                foreach (int side in new[] { -1, 1 })
                    b.Box(new Vector3(side * .19f, .06f, -.02f), new Vector3(.05f, .32f, .1f), 11, 6);
                return true;
            case "BloodPrice":
                // A ritual dagger above a single heavy drop.
                b.Prism(new[] { new Vector2(-.06f, .02f), new Vector2(.06f, .02f), new Vector2(.03f, .48f), new Vector2(0, .56f), new Vector2(-.03f, .48f) }, .04f, 15, Vector3.zero, 1);
                b.Box(new Vector3(0, 0, 0), new Vector3(.32f, .05f, .08f), 10);
                b.Box(new Vector3(0, -.13f, 0), new Vector3(.07f, .22f, .07f), 11, 11);
                b.Ball(new Vector3(0, -.27f, 0), new Vector3(.07f, .07f, .07f), 10);
                b.Ball(new Vector3(.24f, -.26f, -.02f), new Vector3(.1f, .12f, .1f), 6);
                b.Prism(new[] { new Vector2(-.05f, 0), new Vector2(0, .12f), new Vector2(.05f, 0) }, .1f, 6, new Vector3(.24f, -.21f, -.07f), 6);
                return true;
            case "DevilImp":
                // A round horned head with gold eyes and small bat wings.
                b.Ball(Vector3.zero, new Vector3(.24f, .22f, .22f), 6);
                foreach (int side in new[] { -1, 1 })
                {
                    b.Prism(new[] { new Vector2(side * .1f, .14f), new Vector2(side * .2f, .14f), new Vector2(side * .26f, .42f) }, .07f, 11, new Vector3(0, 0, -.03f), 11);
                    b.Box(new Vector3(side * .08f, .03f, -.2f), new Vector3(.07f, .05f, .03f), 10, 10);
                    b.Prism(new[] { new Vector2(side * .2f, .04f), new Vector2(side * .5f, .2f), new Vector2(side * .44f, -.02f), new Vector2(side * .5f, -.14f), new Vector2(side * .2f, -.08f) }, .03f, 11, new Vector3(0, 0, .04f), 11);
                }
                b.Box(new Vector3(0, -.1f, -.2f), new Vector3(.12f, .03f, .03f), 11, 11);
                return true;
            case "MirrorOath":
                // An oval hand mirror in a gold frame.
                b.Drum(new Vector3(0, .1f, 0), .36f, .06f, 10, 12, 10);
                b.Drum(new Vector3(0, .1f, -.04f), .29f, .02f, 9, 12, 9);
                b.Box(new Vector3(-.08f, .2f, -.06f), new Vector3(.05f, .2f, .01f), 15, 15);
                b.Box(new Vector3(0, -.38f, 0), new Vector3(.09f, .26f, .08f), 10);
                b.Ball(new Vector3(0, -.53f, 0), new Vector3(.07f, .06f, .07f), 10);
                return true;
            case "Hunger":
                // Open jaws: two dark arcs of pale fangs.
                foreach (int side in new[] { -1, 1 })
                {
                    b.Prism(new[] { new Vector2(-.42f, 0), new Vector2(-.3f, side * .26f), new Vector2(0, side * .34f), new Vector2(.3f, side * .26f), new Vector2(.42f, 0), new Vector2(.28f, side * .1f), new Vector2(-.28f, side * .1f) }, .16f, 6, new Vector3(0, side * .06f, 0), 11);
                    for (int i = 0; i < 5; i++)
                    {
                        float x = -.24f + i * .12f;
                        float length = i == 0 || i == 4 ? .16f : .1f;
                        b.Prism(new[] { new Vector2(x - .04f, side * .16f), new Vector2(x + .04f, side * .16f), new Vector2(x, side * (.16f - length)) }, .06f, 15, new Vector3(0, 0, -.03f), 15);
                    }
                }
                return true;
            case "CursedCoin":
                // A heavy gold coin stamped with a skull, ringed in a sickly glow.
                b.Drum(new Vector3(0, 0, .03f), .42f, .04f, 9, 14, 9);
                b.Drum(Vector3.zero, .36f, .1f, 10, 14, 10);
                b.Ball(new Vector3(0, .04f, -.07f), new Vector3(.15f, .14f, .03f), 15);
                b.Box(new Vector3(0, -.1f, -.07f), new Vector3(.12f, .08f, .03f), 15, 15);
                foreach (int side in new[] { -1, 1 })
                    b.Box(new Vector3(side * .055f, .05f, -.1f), new Vector3(.05f, .05f, .01f), 11, 11);
                return true;
            default:
                return false;
        }
    }
}
