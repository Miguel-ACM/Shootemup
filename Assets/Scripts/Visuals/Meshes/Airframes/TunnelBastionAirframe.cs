using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildTunnelBastionAirframe(Builder b)
    {
        // A horseshoe armored carriage, with a suspended central rotary breech.
        ArcPlate(b, .79f, 1.31f, -28, 208, .02f, .31f, 11);
        foreach (int s in new[] { -1, 1 })
        {
            BevelArmor(b, new[] { V(s * .65f, .91f), V(s * 1.28f, .52f), V(s * 1.37f, -.28f), V(s * .99f, -.84f), V(s * .77f, -.62f), V(s * .96f, .13f) }, .38f, 0, -.14f);
            Motor(b, s * .93f, .56f, .26f, .6f, 0);
            Inlet(b, s * 1.12f, -.14f, .26f, .4f, -.38f, 6);
            b.Box(new Vector3(s * .63f, -.08f, -.06f), new Vector3(.36f, .22f, .26f), 6, 11);
        }
        b.Ball(new Vector3(0, -.16f, -.1f), new Vector3(.62f, .57f, .34f), 6);
        Ring(b, new Vector3(0, -.16f, -.39f), .27f, .44f, .11f, 0, 12);
        b.Drum(new Vector3(0, -.16f, -.47f), .26f, .075f, 11, 12);
        for (int i = -1; i <= 1; i++)
            Barrel(b, i * .2f, -.71f, .68f, .13f, -.48f);
        b.Box(new Vector3(0, .56f, -.24f), new Vector3(.7f, .1f, .05f), 3, 11);
    }
}
