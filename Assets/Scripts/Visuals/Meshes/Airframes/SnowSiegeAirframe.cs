using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildSnowSiegeAirframe(Builder b)
    {
        // A broad artillery sled carries one continuous recoil deck and three short guns.
        // Its squat bow distinguishes it from the long parallel booms of the Plains bomber.
        BevelArmor(b, new[] { V(-1.25f, .25f), V(-.92f, .72f), V(.92f, .72f), V(1.25f, .25f), V(1.14f, -.42f), V(.65f, -.65f), V(-.65f, -.65f), V(-1.14f, -.42f) }, .46f, 4);
        BevelArmor(b, new[] { V(-1.14f, .24f), V(-.85f, .51f), V(.85f, .51f), V(1.14f, .24f), V(1.05f, -.38f), V(-1.05f, -.38f) }, .24f, 1, -.25f);
        foreach (int s in new[] { -1, 1 })
        {
            Motor(b, s * .76f, .75f, .28f, .38f, 1);
            BevelArmor(b, new[] { V(s * .71f, .36f), V(s * 1.14f, .19f), V(s * 1.15f, -.27f), V(s * .75f, -.2f) }, .19f, 4, -.43f);
            b.Box(new Vector3(s * 1.08f, -.1f, -.55f), new Vector3(.055f, .22f, .035f), 15, 11);
        }
        // A low, wide bridge slit replaces the repeated forward pointed cockpit.
        BevelArmor(b, new[] { V(-.42f, .76f), V(.42f, .76f), V(.53f, .27f), V(.35f, .1f), V(-.35f, .1f), V(-.53f, .27f) }, .38f, 15, -.43f);
        b.Box(new Vector3(0, .27f, -.65f), new Vector3(.6f, .15f, .07f), 11, 11);
        b.Box(new Vector3(0, .27f, -.7f), new Vector3(.46f, .065f, .025f), 9, 11);
        Inlet(b, 0, .55f, .31f, .16f, -.65f, 4);
        for (int i = -1; i <= 1; i++)
        {
            float x = i * .78f, y = i == 0 ? -.46f : -.38f;
            BevelArmor(b, new[] { V(x - .25f, y + .19f), V(x + .25f, y + .19f), V(x + .23f, y - .24f), V(x + .16f, y - .38f), V(x - .16f, y - .38f), V(x - .23f, y - .24f) }, .31f, 6, -.38f);
            b.Box(new Vector3(x, y - .04f, -.56f), new Vector3(.23f, .29f, .045f), 11, 11);
            Barrel(b, x, y - .4f, .28f, .19f, -.34f);
        }
    }
}
