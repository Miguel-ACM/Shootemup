using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildCarrierAirframe(Builder b)
    {

        BevelArmor(b, new[] { V(-.33f, 1.02f), V(.33f, 1.02f), V(.41f, -.85f), V(0, -1.2f), V(-.41f, -.85f) }, .54f, 0);
        foreach (int s in new[] { -1, 1 })
        {
            b.Box(new Vector3(s * .59f, .1f, .02f), new Vector3(.55f, .7f, .22f), 11);
            BevelArmor(b, new[] { V(s * .52f, 1.02f), V(s * .98f, .93f), V(s * 1.06f, -.65f), V(s * .81f, -1.08f), V(s * .52f, -.87f) }, .42f, 0);
            b.Box(new Vector3(s * .76f, -.08f, -.26f), new Vector3(.31f, 1.32f, .035f), 11, 11);
            for (int i = 0; i < 3; i++)
                BevelArmor(b, new[] { V(s * .6f, -.55f + i * .4f), V(s * .92f, -.55f + i * .4f), V(s * .92f, -.27f + i * .4f), V(s * .6f, -.27f + i * .4f) }, .12f, i == 1 ? 2 : 0, -.29f);
            Motor(b, s * .87f, .8f, .3f, .76f);
            Barrel(b, s * .65f, -1.02f, .4f, .13f);
        }
        BevelArmor(b, new[] { V(-.24f, .65f), V(.24f, .65f), V(.27f, .04f), V(-.27f, .04f) }, .23f, 0, -.34f);
        Inlet(b, 0, .33f, .31f, .34f, -.47f, 6);
        b.Box(new Vector3(0, -.61f, -.33f), new Vector3(.39f, .13f, .08f), 11);
        b.Box(new Vector3(0, -.61f, -.38f), new Vector3(.3f, .055f, .03f), 9, 11);
    }
}
