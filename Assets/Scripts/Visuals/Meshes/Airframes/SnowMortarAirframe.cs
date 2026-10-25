using UnityEngine;
public static partial class IndustrialMeshFactory
{
    static void BuildSnowMortarAirframe(Builder b)
    {
        // A single outsized mortar carried between two rounded stabilizer gondolas.
        // This is a compact artillery sled, not the siege fighter's pair of cannons.
        BevelArmor(b, new[] { V(-.49f, .83f), V(.49f, .83f), V(.73f, .27f), V(.57f, -.34f), V(-.57f, -.34f), V(-.73f, .27f) }, .43f, 4);
        foreach (int s in new[] { -1, 1 })
        {
            b.Ball(new Vector3(s * .72f, .02f, -.13f), new Vector3(.39f, .57f, .25f), 15);
            BevelArmor(b, new[] { V(s * .55f, -.05f), V(s * .88f, -.05f), V(s * .91f, -.38f), V(s * .62f, -.55f) }, .12f, 4, -.32f);
            Motor(b, s * .66f, .45f, .29f, .5f, 4);
            Inlet(b, s * .76f, .09f, .23f, .32f, -.37f, 1);
        }
        BevelArmor(b, new[] { V(-.28f, .58f), V(.28f, .58f), V(.38f, -.53f), V(.31f, -1.23f), V(-.31f, -1.23f), V(-.38f, -.53f) }, .52f, 1, -.19f);
        BevelArmor(b, new[] { V(-.17f, .32f), V(.17f, .32f), V(.2f, -.56f), V(-.2f, -.56f) }, .1f, 4, -.48f);
        b.Box(new Vector3(0, -.37f, -.55f), new Vector3(.2f, .4f, .035f), 11, 11);
        BevelArmor(b, new[] { V(-.34f, -.91f), V(.34f, -.91f), V(.4f, -1.2f), V(.28f, -1.43f), V(-.28f, -1.43f), V(-.4f, -1.2f) }, .39f, 6, -.16f);
        b.Box(new Vector3(0, -1.33f, -.3f), new Vector3(.43f, .15f, .09f), 11, 11);
        b.Box(new Vector3(0, -1.36f, -.36f), new Vector3(.22f, .055f, .025f), 3, 11);
        b.Box(new Vector3(0, .73f, -.28f), new Vector3(.4f, .09f, .045f), 11, 11);
        b.Box(new Vector3(0, .73f, -.31f), new Vector3(.22f, .035f, .025f), 9, 11);
    }
}
