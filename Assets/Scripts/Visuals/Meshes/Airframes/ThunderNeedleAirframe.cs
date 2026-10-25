using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildThunderNeedleAirframe(Builder b)
    {
        // A narrow dreadnought: long reactor spine, exposed rails, six shoulder emitters.
        BevelArmor(b, new[] { V(0, -1.98f), V(-.22f, -.66f), V(-.29f, .8f), V(0, 1.3f), V(.29f, .8f), V(.22f, -.66f) }, .5f, 6);
        BevelArmor(b, new[] { V(-.12f, .79f), V(.12f, .79f), V(.12f, -.79f), V(0, -1.57f), V(-.12f, -.79f) }, .16f, 11, -.29f);
        b.Box(new Vector3(0, -.21f, -.4f), new Vector3(.065f, 1.07f, .04f), 9, 11);
        foreach (int s in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(s * .2f, .74f), V(s * .55f, 1.03f), V(s * .69f, .35f), V(s * .54f, -.73f), V(s * .31f, -1.39f), V(s * .37f, -.29f) }, .28f, 7, -.04f);
            for (int i = 0; i < 3; i++)
            {
                float y = .46f - i * .47f;
                BevelArmor(b, new[] { V(s * .45f, y + .16f), V(s * .85f, y + .06f), V(s * .78f, y - .14f), V(s * .48f, y - .22f) }, .15f, 6, -.14f);
                b.Drum(new Vector3(s * .71f, y - .025f, -.26f), .085f, .05f, 11, 8);
                b.Drum(new Vector3(s * .71f, y - .025f, -.3f), .045f, .025f, 9, 8);
            }
            Motor(b, s * .29f, .85f, .24f, .62f, 7);
            Armor(b, new[] { V(s * .38f, -.67f), V(s * .44f, -.82f), V(s * .29f, -1.65f), V(s * .26f, -1.35f) }, .08f, 7, -.13f);
        }
        // The command station is a small transverse slit, not another fighter canopy.
        BevelArmor(b, new[] { V(-.18f, .71f), V(.18f, .71f), V(.21f, .4f), V(-.21f, .4f) }, .2f, 7, -.35f);
        b.Box(new Vector3(0, .51f, -.49f), new Vector3(.25f, .065f, .025f), 9, 11);
    }
}
