using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildBomberAirframe(Builder b)
    {

        BevelArmor(b, new[] { V(-1.65f, .25f), V(-1.1f, -.38f), V(-.38f, -.7f), V(0, -.9f), V(.38f, -.7f), V(1.1f, -.38f), V(1.65f, .25f), V(.5f, .43f), V(0, .65f), V(-.5f, .43f) }, .34f, 2);
        BevelArmor(b, new[] { V(-.39f, .51f), V(.39f, .51f), V(.36f, -.35f), V(0, -.77f), V(-.36f, -.35f) }, .33f, 0, -.17f);
        Glass(b, 0, -.33f, .57f, .32f, -.4f);
        foreach (int s in new[] { -1, 1 })
        {
            Motor(b, s * .95f, .46f, .34f, .72f, 2);
            BevelArmor(b, new[] { V(s * .44f, .18f), V(s * .94f, .18f), V(s * .82f, -.4f), V(s * .46f, -.57f) }, .16f, 6, -.2f);
            b.Box(new Vector3(s * .67f, -.19f, -.33f), new Vector3(.22f, .48f, .04f), 11, 11);
            for (int i = 0; i < 2; i++)
                b.Box(new Vector3(s * .67f, -.32f + i * .23f, -.36f), new Vector3(.15f, .12f, .04f), 3, 11);
            Armor(b, new[] { V(s * 1.15f, .1f), V(s * 1.43f, .16f), V(s * 1.06f, -.16f) }, .035f, 0, -.2f);
        }
        Inlet(b, 0, .24f, .38f, .3f, -.39f, 0);
    }
}
