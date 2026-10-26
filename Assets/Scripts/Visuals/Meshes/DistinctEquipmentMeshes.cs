using UnityEngine;

public static partial class IndustrialMeshFactory
{
    // A faceted tube between two points, used for actual dimensional mechanisms.
    static void EquipmentRod(Builder b, Vector3 start, Vector3 end, float radius, int tile)
    {
        Vector3 axis = (end - start).normalized;
        Vector3 across = Vector3.Cross(axis, Mathf.Abs(axis.z) < .9f ? Vector3.forward : Vector3.up).normalized;
        Vector3 other = Vector3.Cross(axis, across).normalized;
        const int sides = 6;
        var first = new Vector3[sides];
        var last = new Vector3[sides];
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2 / sides;
            Vector3 rim = (across * Mathf.Cos(angle) + other * Mathf.Sin(angle)) * radius;
            first[i] = start + rim;
            last[i] = end + rim;
        }
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            var face = new[] { first[i], last[i], last[next], first[next] };
            if (Vector3.Dot(Vector3.Cross(face[1] - face[0], face[2] - face[0]), (first[i] + first[next]) * .5f - start) < 0)
                System.Array.Reverse(face);
            b.Face(tile, face);
        }
        System.Array.Reverse(first);
        b.Face(tile, first);
        b.Face(tile, last);
    }

    // Recognize equipment by its outer contour, not a different dot on the same box.
    static bool DistinctEquipment(Builder b, string kind)
    {
        switch (kind)
        {
            case "BeamEmitterItem":
                // Open tuning fork around a recessed optical channel.
                foreach (int side in new[] { -1, 1 })
                {
                    b.Prism(new[] { new Vector2(side * .12f, -.35f), new Vector2(side * .34f, -.22f), new Vector2(side * .27f, .58f), new Vector2(side * .16f, .65f) }, .18f, 11, Vector3.zero, 6);
                    b.Box(new Vector3(side * .21f, .35f, -.13f), new Vector3(.08f, .38f, .06f), 9);
                }
                b.Box(new Vector3(0, -.27f, 0), new Vector3(.48f, .2f, .3f), 6);
                b.Drum(new Vector3(0, -.14f, -.15f), .13f, .1f, 9, 12);
                break;
            case "NeedleCannonItem":
                // Long syringe barrel, finger wings, and a visible needle tip.
                b.Can(Vector3.zero, .12f, .72f, 11, 6);
                b.Box(new Vector3(0, .54f, 0), new Vector3(.045f, .4f, .06f), 11);
                b.Box(new Vector3(0, -.28f, 0), new Vector3(.55f, .07f, .18f), 6);
                b.Box(new Vector3(0, -.5f, 0), new Vector3(.07f, .33f, .08f), 6);
                b.Box(new Vector3(0, -.65f, 0), new Vector3(.3f, .07f, .18f), 3);
                break;
            case "ScatterCannonItem":
                // Five genuinely splayed barrels form a fan silhouette.
                b.Drum(new Vector3(0, -.22f, 0), .25f, .22f, 6, 8);
                for (int i = -2; i <= 2; i++)
                {
                    float x = i * .17f;
                    b.Prism(new[] { new Vector2(i * .065f - .05f, -.15f), new Vector2(i * .065f + .05f, -.15f), new Vector2(x + .065f, .5f - Mathf.Abs(i) * .06f), new Vector2(x - .065f, .5f - Mathf.Abs(i) * .06f) }, .15f, 11, Vector3.zero, 6);
                }
                break;
            case "SwarmRackItem":
                // Broad six-cell honeycomb launcher with protruding rocket noses.
                foreach (int side in new[] { -1, 1 })
                    for (int row = 0; row < 3; row++)
                    {
                        Vector3 p = new Vector3(side * .22f, (row - 1) * .24f, 0);
                        b.Drum(p, .16f, .25f, 6, 6);
                        b.Drum(p + Vector3.back * .15f, .1f, .09f, 3, 6);
                        b.Prism(new[] { new Vector2(-.05f, 0), new Vector2(.05f, 0), new Vector2(0, .18f) }, .08f, 11, p + Vector3.up * .12f);
                    }
                break;
            case "BurstLauncherItem":
                // Short, wide mortar cup over a large round ammunition drum.
                b.Drum(new Vector3(0, -.18f, 0), .35f, .3f, 4, 12, 6);
                b.Prism(new[] { new Vector2(-.16f, -.05f), new Vector2(.16f, -.05f), new Vector2(.29f, .45f), new Vector2(-.29f, .45f) }, .32f, 11, Vector3.zero, 6);
                b.Box(new Vector3(0, .46f, 0), new Vector3(.64f, .11f, .4f), 6);
                b.Box(new Vector3(0, .48f, -.21f), new Vector3(.42f, .045f, .035f), 1);
                break;
            case "RailDriverItem":
                // Very long twin rails, open centre, three external cross coils.
                foreach (int side in new[] { -1, 1 })
                    b.Box(new Vector3(side * .16f, .09f, 0), new Vector3(.1f, 1.25f, .18f), 11);
                for (int i = 0; i < 3; i++)
                    b.Box(new Vector3(0, -.27f + i * .27f, -.05f), new Vector3(.57f, .1f, .27f), 9, 6);
                b.Box(new Vector3(0, -.55f, 0), new Vector3(.4f, .22f, .28f), 6);
                break;
            case "DiscThrowerItem":
                // Exposed toothed circular blade in a half-moon launch cradle.
                b.Drum(new Vector3(0, .08f, -.08f), .38f, .1f, 11, 16);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4;
                    Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), q = new Vector2(-p.y, p.x);
                    b.Prism(new[] { p * .3f, p * .49f + q * .07f, p * .37f + q * .17f }, .08f, 11, Vector3.up * .08f);
                }
                b.Drum(new Vector3(0, .08f, -.16f), .12f, .07f, 9, 8);
                b.Box(new Vector3(0, -.4f, 0), new Vector3(.68f, .2f, .24f), 6);
                break;
            case "FlameProjectorItem":
                // Single fuel cylinder, bent hose and flared nozzle.
                b.Can(new Vector3(-.22f, -.1f, 0), .19f, .63f, 3, 6);
                b.Box(new Vector3(.11f, -.26f, 0), new Vector3(.5f, .1f, .13f), 6);
                b.Box(new Vector3(.3f, -.02f, 0), new Vector3(.1f, .55f, .12f), 6);
                b.Prism(new[] { new Vector2(.21f, .14f), new Vector2(.39f, .14f), new Vector2(.49f, .49f), new Vector2(.11f, .49f) }, .23f, 11, Vector3.zero, 6);
                b.Box(new Vector3(.3f, .49f, 0), new Vector3(.42f, .09f, .28f), 3);
                break;
            case "Capacitor":
                // Tall paired capacitor cans with a metal bus bridge.
                foreach (int side in new[] { -1, 1 })
                {
                    b.Can(new Vector3(side * .2f, 0, 0), .15f, .76f, 9, 6);
                    b.Box(new Vector3(side * .2f, .45f, 0), new Vector3(.065f, .18f, .065f), 11);
                }
                b.Box(new Vector3(0, .52f, 0), new Vector3(.48f, .06f, .08f), 11);
                break;
            case "ReserveCell":
                // Compact storage cell in an open protective cage, with asymmetric terminals.
                b.Box(new Vector3(0, -.05f, .04f), new Vector3(.52f, .49f, .38f), 9, 6);
                foreach (int side in new[] { -1, 1 })
                    b.Box(new Vector3(side * .29f, -.05f, 0), new Vector3(.09f, .56f, .46f), 6, 11);
                b.Box(new Vector3(0, -.34f, .02f), new Vector3(.64f, .09f, .46f), 11, 6);
                b.Box(new Vector3(-.16f, .3f, .02f), new Vector3(.12f, .2f, .18f), 11);
                b.Drum(new Vector3(.16f, .25f, -.08f), .075f, .18f, 3, 8, 6);
                for (int bar = 0; bar < 3; bar++)
                    b.Box(new Vector3(-.13f + bar * .13f, -.08f, -.19f), new Vector3(.075f, .18f, .04f), bar == 2 ? 11 : 6);
                break;
            case "SpareBattery":
                // Horizontal battery strapped to a tiny guided rocket.
                b.Box(Vector3.zero, new Vector3(.77f, .31f, .27f), 4, 6);
                b.Box(new Vector3(.43f, 0, 0), new Vector3(.12f, .18f, .18f), 11);
                b.Can(new Vector3(0, 0, -.25f), .09f, .8f, 11, 6);
                b.Prism(new[] { new Vector2(-.1f, .39f), new Vector2(.1f, .39f), new Vector2(0, .61f) }, .14f, 3, Vector3.back * .25f);
                break;
            case "Overdrive":
                // A red throttle lever with swept acceleration fins.
                b.Prism(new[] { new Vector2(-.36f, -.35f), new Vector2(.36f, -.35f), new Vector2(.24f, .05f), new Vector2(-.24f, .05f) }, .27f, 6, Vector3.zero);
                b.Box(new Vector3(.13f, .21f, -.03f), new Vector3(.1f, .53f, .1f), 11);
                b.Box(new Vector3(.13f, .48f, -.03f), new Vector3(.4f, .15f, .18f), 3);
                foreach (int side in new[] { -1, 1 })
                    b.Prism(new[] { new Vector2(side * .2f, -.2f), new Vector2(side * .49f, -.02f), new Vector2(side * .29f, -.38f) }, .1f, 3, Vector3.zero);
                break;
            case "FocusingLens":
                // Telescope: large objective, narrow neck, small eyepiece.
                b.Drum(new Vector3(0, .21f, -.03f), .34f, .16f, 6, 12);
                b.Drum(new Vector3(0, .21f, -.14f), .27f, .07f, 9, 12);
                b.Box(new Vector3(0, -.14f, .02f), new Vector3(.22f, .55f, .22f), 11);
                b.Box(new Vector3(0, -.46f, .02f), new Vector3(.37f, .12f, .26f), 6);
                break;
            case "ReturnModule":
                // A return-feed machine: paired guide rollers and an exposed looped rail.
                b.Box(new Vector3(0, -.29f, .05f), new Vector3(.56f, .27f, .38f), 6, 11);
                b.Box(new Vector3(0, -.29f, -.17f), new Vector3(.25f, .11f, .06f), 9);
                foreach (int side in new[] { -1, 1 })
                {
                    b.Can(new Vector3(side * .2f, .04f, .08f), .1f, .38f, 11, 6);
                    b.Drum(new Vector3(side * .2f, .12f, -.07f), .075f, .08f, 9, 8);
                }
                Vector3[] returnRail = {
                    new Vector3(.29f, -.12f, -.13f), new Vector3(.4f, .12f, -.14f),
                    new Vector3(.31f, .38f, -.12f), new Vector3(0, .48f, -.08f),
                    new Vector3(-.31f, .38f, -.12f), new Vector3(-.4f, .1f, -.14f),
                    new Vector3(-.32f, -.1f, -.13f) };
                for (int i = 1; i < returnRail.Length; i++)
                    EquipmentRod(b, returnRail[i - 1], returnRail[i], .045f, 6);
                b.Prism(new[] { new Vector2(-.45f, -.08f), new Vector2(-.2f, -.08f), new Vector2(-.32f, -.27f) }, .12f, 9, Vector3.back * .13f);
                break;
            case "SplitterCore":
                // A feed cartridge enters a central manifold and divides into two barrels.
                b.Can(new Vector3(0, -.31f, .04f), .14f, .38f, 6, 11);
                b.Drum(new Vector3(0, -.03f, -.02f), .2f, .34f, 6, 8, 11);
                b.Drum(new Vector3(0, -.03f, -.22f), .11f, .06f, 9, 8);
                foreach (int side in new[] { -1, 1 })
                {
                    Vector3 inlet = new Vector3(side * .08f, .04f, .02f);
                    Vector3 muzzle = new Vector3(side * .34f, .4f, .02f);
                    EquipmentRod(b, inlet, muzzle, .085f, 6);
                    EquipmentRod(b, muzzle, muzzle + new Vector3(side * .05f, .07f, 0), .11f, 11);
                    b.Ball(muzzle + new Vector3(side * .07f, .09f, -.02f), new Vector3(.055f, .065f, .055f), 9);
                }
                break;
            case "ReboundCoil":
                // A real helical spring around a guide piston between two end plates.
                b.Can(Vector3.zero, .075f, .72f, 6, 11);
                const int segments = 40;
                Vector3 CoilPoint(int step)
                {
                    float t = step / (float)segments;
                    float angle = t * Mathf.PI * 7;
                    return new Vector3(Mathf.Cos(angle) * .23f, Mathf.Lerp(-.34f, .34f, t), Mathf.Sin(angle) * .2f);
                }
                for (int i = 1; i <= segments; i++)
                    EquipmentRod(b, CoilPoint(i - 1), CoilPoint(i), .042f, 11);
                foreach (int side in new[] { -1, 1 })
                {
                    b.Box(new Vector3(0, side * .43f, 0), new Vector3(.59f, .13f, .48f), 6);
                    b.Box(new Vector3(0, side * .43f, -.26f), new Vector3(.27f, .07f, .05f), 9);
                }
                break;
            case "RecoveryCoil":
                // A layered shield housing feeds an actual raised countershot barrel.
                b.Prism(new[] { new Vector2(-.4f, .29f), new Vector2(.4f, .29f), new Vector2(.31f, -.18f), new Vector2(0, -.48f), new Vector2(-.31f, -.18f) }, .3f, 6, new Vector3(0, 0, .07f), 11);
                foreach (int side in new[] { -1, 1 })
                {
                    b.Prism(new[] { new Vector2(side * .14f, .22f), new Vector2(side * .36f, .24f), new Vector2(side * .28f, -.15f), new Vector2(side * .11f, -.29f) }, .09f, 11, Vector3.back * .14f, 6);
                    b.Box(new Vector3(side * .32f, .19f, .04f), new Vector3(.1f, .16f, .36f), 6);
                }
                b.Can(new Vector3(0, .07f, -.19f), .095f, .55f, 9, 11);
                b.Prism(new[] { new Vector2(-.095f, .3f), new Vector2(.095f, .3f), new Vector2(0, .52f) }, .18f, 9, Vector3.back * .19f, 11);
                b.Box(new Vector3(0, -.29f, -.18f), new Vector3(.26f, .1f, .2f), 6);
                break;
            case "ProximityFuse":
                // A thick proximity sensor pod with two swept, dimensional feelers.
                b.Drum(new Vector3(0, 0, .02f), .23f, .38f, 6, 8, 11);
                b.Drum(new Vector3(0, 0, -.21f), .15f, .08f, 3, 8, 6);
                b.Drum(new Vector3(0, 0, -.26f), .065f, .035f, 9, 8);
                foreach (int side in new[] { -1, 1 })
                {
                    Vector3 root = new Vector3(side * .2f, .03f, .06f);
                    Vector3 bend = new Vector3(side * .42f, .11f, -.03f);
                    Vector3 tip = new Vector3(side * .48f, .35f, -.1f);
                    EquipmentRod(b, root, bend, .04f, 11);
                    EquipmentRod(b, bend, tip, .035f, 11);
                    b.Ball(tip, new Vector3(.055f, .055f, .055f), 9);
                }
                b.Box(new Vector3(0, -.31f, .02f), new Vector3(.18f, .25f, .25f), 6);
                break;
            case "SalvageConverter":
                // Open coin hopper feeds a small missile chute.
                b.Prism(new[] { new Vector2(-.43f, .38f), new Vector2(.43f, .38f), new Vector2(.2f, -.05f), new Vector2(-.2f, -.05f) }, .3f, 11, Vector3.zero, 6);
                for (int i = 0; i < 3; i++)
                    b.Drum(new Vector3(-.22f + i * .2f, .37f + i % 2 * .13f, -.05f), .12f, .08f, 4, 10);
                b.Box(new Vector3(.15f, -.25f, 0), new Vector3(.18f, .4f, .2f), 6);
                b.Prism(new[] { new Vector2(.04f, -.3f), new Vector2(.26f, -.3f), new Vector2(.15f, -.55f) }, .15f, 3, Vector3.zero);
                break;
            case "ChainReactor":
                // Three linked powder charges with an exposed fuse.
                for (int i = 0; i < 3; i++)
                {
                    var p = new Vector3((i - 1) * .27f, (i % 2) * .16f, 0);
                    b.Can(p, .17f, .57f, 3, 6);
                    b.Box(p + new Vector3(0, 0, -.18f), new Vector3(.08f, .5f, .04f), 11);
                }
                b.Box(new Vector3(0, .48f, 0), new Vector3(.065f, .3f, .07f), 4);
                break;
            case "MirrorBurst":
                // Faceted mirror with split reflected wings, not a round energy core.
                b.Prism(new[] { new Vector2(0, .48f), new Vector2(.29f, .15f), new Vector2(.21f, -.34f), new Vector2(-.21f, -.34f), new Vector2(-.29f, .15f) }, .14f, 9, Vector3.zero, 11);
                foreach (int side in new[] { -1, 1 })
                    b.Prism(new[] { new Vector2(side * .18f, -.1f), new Vector2(side * .5f, .34f), new Vector2(side * .46f, -.2f) }, .1f, 11, Vector3.forward * .1f, 6);
                break;
            case "GravityWell":
                // A dark centre enclosed by a four-claw containment cage.
                b.Drum(Vector3.zero, .22f, .2f, 1, 12);
                for (int i = 0; i < 4; i++)
                {
                    float a = i * Mathf.PI / 2;
                    Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), q = new Vector2(-p.y, p.x);
                    b.Prism(new[] { p * .22f - q * .07f, p * .48f - q * .14f, p * .48f + q * .14f, p * .3f + q * .14f }, .18f, 6, Vector3.zero, 9);
                }
                break;
            default:
                return false;
        }
        return true;
    }
}
