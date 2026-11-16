using UnityEngine;
using System.Collections.Generic;
public static partial class IndustrialMeshFactory
{
    static readonly Dictionary<string, Mesh> expansionItems = new Dictionary<string, Mesh>();
    public static Mesh ExpansionItem(string kind)
    {
        if (expansionItems.TryGetValue(kind, out var mesh) && mesh != null)
            return mesh;
        // The remodelled Coin Launcher read as a handbag (Miguel, 15 November 2026): keep its
        // original hopper-and-chute design, made clearer, instead of the authored model.
        if (kind == "SalvageConverter")
            return expansionItems[kind] = CoinLauncherModel();
        var authored = TerraModel("Item" + kind);
        if (authored != null)
            return expansionItems[kind] = authored;
        int original = System.Array.IndexOf(ItemKinds, kind);
        if (original >= 0)
        {
            var prefab = Resources.Load<GameObject>("Items/Item");
            var visual = prefab != null ? prefab.GetComponent<PSXObjectVisual>() : null;
            return expansionItems[kind] = visual != null && original < visual.itemMeshes.Length ? visual.itemMeshes[original] : CreateItem(original);
        }
        if (kind == "WindUpCannonItem")
            return expansionItems[kind] = WindUpCannonModel();
        if (kind == "Popcorn")
            return expansionItems[kind] = PopcornModel();
        if (kind == "Icicle")
            return expansionItems[kind] = IcicleModel();
        var b = new Builder();
        if (kind == nameof(Pufferfish))
        {
            b.Ball(new Vector3(0, -.017f, .04f), new Vector3(.57f, .57f, .33f), 9);
            b.Ball(new Vector3(0, -.072f, -.091f), new Vector3(.43f, .37f, .16f), 3);
            for (int n = 0; n < 9; n++)
            {
                float angle = (n + .5f) * Mathf.PI * 2 / 9;
                var axis = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                var across = new Vector3(axis.y, -axis.x, 0) * .047f;
                Vector2 baseA = axis * .24f + across, baseB = axis * .24f - across;
                b.Prism(new[] { baseA, baseB, (Vector2)(axis * .357f) }, .058f, n % 2 == 0 ? 9 : 11, Vector3.zero, 6);
            }
            foreach (int side in new[] { -1, 1 })
                b.Ball(new Vector3(side * .113f, .095f, -.158f), Vector3.one * .094f, 6);
            b.Drum(new Vector3(0, .18f, -.145f), .061f, .046f, 10, 8, 6);
            b.Drum(new Vector3(0, .182f, -.172f), .033f, .019f, 6, 8);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(Wasp))
        {
            b.Ball(new Vector3(0, -.12f, .04f), new Vector3(.26f, .51f, .24f), 10);
            b.Ball(new Vector3(0, .13f, -.012f), new Vector3(.19f, .24f, .18f), 6);
            b.Ball(new Vector3(0, .247f, -.055f), new Vector3(.22f, .18f, .20f), 10);
            foreach (float y in new[] { -.22f, -.097f })
                b.Ball(new Vector3(0, y, -.061f), new Vector3(.224f, .050f, .088f), 6);
            foreach (int side in new[] { -1, 1 })
            {
                var wing = new[] { new Vector2(side * .065f, .08f), new Vector2(side * .36f, .204f),
                    new Vector2(side * .355f, .093f), new Vector2(side * .133f, -.12f) };
                if (side > 0) System.Array.Reverse(wing);
                b.Prism(wing, .043f, 9, new Vector3(0, 0, .032f), 11);
                b.Ball(new Vector3(side * .078f, .251f, -.145f), Vector3.one * .049f, 6);
            }
            b.Prism(new[] { new Vector2(-.052f, -.307f), new Vector2(0, -.453f),
                new Vector2(.052f, -.307f) }, .07f, 11, new Vector3(0, 0, .055f), 6);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(Crab))
        {
            b.Ball(new Vector3(0, -.065f, 0), new Vector3(.59f, .395f, .28f), 1);
            b.Ball(new Vector3(0, -.074f, -.11f), new Vector3(.48f, .28f, .11f), 10);
            foreach (int side in new[] { -1, 1 })
            {
                for (int i = 0; i < 3; i++)
                {
                    float y = -.083f - i * .088f;
                    EquipmentRod(b, new Vector3(side * .215f, y, .016f), new Vector3(side * (.373f - i * .027f), y - .092f, .04f), .032f, 1);
                }
                EquipmentRod(b, new Vector3(side * .225f, .045f, -.008f), new Vector3(side * .339f, .177f, -.018f), .047f, 1);
                b.Ball(new Vector3(side * .346f, .205f, -.039f), new Vector3(.21f, .17f, .14f), 10);
                b.Box(new Vector3(side * .403f, .27f, -.042f), new Vector3(.082f, .119f, .095f), 1);
                b.Ball(new Vector3(side * .122f, .124f, -.132f), Vector3.one * .064f, 6);
            }
            b.Box(new Vector3(0, .166f, -.035f), new Vector3(.094f, .146f, .105f), 11, 6);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(Moth))
        {
            foreach (int side in new[] { -1, 1 })
            {
                var wing = new[] { new Vector2(side * .055f, .05f), new Vector2(side * .24f, .36f),
                    new Vector2(side * .429f, .23f), new Vector2(side * .405f, .032f),
                    new Vector2(side * .215f, -.122f), new Vector2(side * .08f, -.20f) };
                if (side < 0) System.Array.Reverse(wing);
                b.Prism(wing, .084f, 12, new Vector3(0, 0, .03f), 6);
                b.Ball(new Vector3(side * .207f, .141f, -.027f), new Vector3(.21f, .188f, .051f), 13);
                b.Ball(new Vector3(side * .231f, .136f, -.056f), new Vector3(.067f, .061f, .032f), 6);
                EquipmentRod(b, new Vector3(side * .039f, .188f, -.035f), new Vector3(side * .128f, .329f, -.056f), .023f, 11);
            }
            b.Ball(Vector3.zero, new Vector3(.16f, .50f, .16f), 6);
            b.Ball(new Vector3(0, .20f, -.045f), new Vector3(.17f, .15f, .13f), 11);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(Magpie))
        {
            b.Ball(new Vector3(0, -.035f, .04f), new Vector3(.32f, .50f, .27f), 6);
            b.Ball(new Vector3(0, -.048f, -.09f), new Vector3(.21f, .31f, .15f), 3);
            b.Ball(new Vector3(0, .22f, -.05f), new Vector3(.23f, .24f, .22f), 6);
            b.Prism(new[] { new Vector2(-.069f, .25f), new Vector2(.069f, .25f),
                new Vector2(.022f, .395f), new Vector2(-.022f, .395f) }, .105f, 11, Vector3.back * .084f, 6);
            foreach (int side in new[] { -1, 1 })
            {
                b.Ball(new Vector3(side * .084f, .234f, -.131f), Vector3.one * .046f, 3);
                var wing = new[] { new Vector2(side * .12f, .11f), new Vector2(side * .39f, -.04f),
                    new Vector2(side * .245f, -.27f), new Vector2(side * .10f, -.16f) };
                if (side < 0) System.Array.Reverse(wing);
                b.Prism(wing, .10f, 11, new Vector3(0, 0, .019f), 6);
                var tail = new[] { new Vector2(side * .02f, -.19f), new Vector2(side * .117f, -.27f),
                    new Vector2(side * .078f, -.45f), new Vector2(side * .018f, -.425f) };
                if (side > 0) System.Array.Reverse(tail);
                b.Prism(tail, .045f, 7, new Vector3(0, 0, .072f), 6);
            }
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(DivingGear))
        {
            foreach (int side in new[] { -1, 1 })
            {
                float x = side * .166f;
                b.Can(new Vector3(x, -.035f, .06f), .114f, .59f, 9, 11);
                b.Can(new Vector3(x, .255f, .06f), .068f, .093f, 11, 6);
                b.Can(new Vector3(x, -.30f, .06f), .125f, .073f, 6, 11);
                b.Box(new Vector3(x, -.049f, .06f), new Vector3(.244f, .085f, .245f), 6, 11);
                EquipmentRod(b, new Vector3(x, .27f, .039f), new Vector3(x * 1.25f, .195f, -.148f), .035f, 6);
                b.Drum(new Vector3(x * .62f, .106f, -.186f), .103f, .078f, 11, 10, 6);
                b.Drum(new Vector3(x * .62f, .106f, -.239f), .073f, .038f, 9, 10, 7);
            }
            b.Box(new Vector3(0, .106f, -.21f), new Vector3(.12f, .062f, .055f), 11, 6);
            b.Box(new Vector3(0, .008f, -.15f), new Vector3(.105f, .097f, .10f), 7, 6);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(TrafficCone))
        {
            b.Box(new Vector3(0, -.293f, 0), new Vector3(.60f, .095f, .49f), 6, 11);
            var profile = new[] { new Vector2(-.237f, -.25f), new Vector2(.237f, -.25f),
                new Vector2(.057f, .402f), new Vector2(-.057f, .402f) };
            b.Prism(profile, .26f, 10, Vector3.zero, 10);
            foreach (float y in new[] { -.04f, .159f })
            {
                float wide = .237f - (y + .25f) * .275f;
                b.Prism(new[] { new Vector2(-wide, y), new Vector2(wide, y),
                    new Vector2(wide - .022f, y + .078f), new Vector2(-wide + .022f, y + .078f) },
                    .265f, 3, Vector3.zero, 3);
            }
            b.Can(new Vector3(0, .395f, 0), .06f, .03f, 10, 11);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(Pinheads))
        {
            b.Drum(new Vector3(0, -.265f, 0), .32f, .16f, 8, 14, 6);
            b.Drum(new Vector3(0, -.25f, -.106f), .275f, .075f, 8, 14);
            for (int i = 0; i < 5; i++)
            {
                var root = new Vector3((i - 2) * .095f, -.16f, i % 2 == 0 ? -.083f : .028f);
                var head = root + new Vector3((i - 2) * .044f, .50f - .028f * Mathf.Abs(i - 2), -.03f);
                EquipmentRod(b, root, head, .017f, 11);
                b.Ball(head, Vector3.one * .105f, i % 2 == 0 ? 3 : 9);
            }
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(SteelToe))
        {
            var toe = new[] { new Vector2(-.32f, -.23f), new Vector2(.35f, -.23f), new Vector2(.35f, -.09f),
                new Vector2(.265f, .07f), new Vector2(.065f, .11f), new Vector2(.03f, .34f),
                new Vector2(-.23f, .34f), new Vector2(-.32f, .17f) };
            b.Prism(toe, .27f, 6, Vector3.zero, 8);
            b.Box(new Vector3(.008f, -.24f, 0), new Vector3(.70f, .063f, .33f), 6, 11);
            b.Box(new Vector3(.25f, -.095f, -.043f), new Vector3(.24f, .17f, .237f), 11, 6);
            for (int i = 0; i < 3; i++)
                b.Box(new Vector3(-.108f, .105f + i * .067f, -.152f), new Vector3(.194f, .026f, .045f), 8, 6);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(TicketPunch))
        {
            b.Drum(new Vector3(.20f, -.015f, 0), .13f, .18f, 11, 10, 6);
            b.Drum(new Vector3(.20f, -.015f, -.12f), .05f, .067f, 6, 8);
            b.Prism(new[] { new Vector2(-.42f, .29f), new Vector2(.19f, .105f), new Vector2(.22f, .012f),
                new Vector2(.03f, .032f), new Vector2(-.43f, .178f) }, .143f, 11, Vector3.zero, 6);
            b.Prism(new[] { new Vector2(-.42f, -.29f), new Vector2(-.43f, -.178f), new Vector2(.03f, -.032f),
                new Vector2(.22f, -.012f), new Vector2(.19f, -.105f) }, .143f, 11, Vector3.zero, 6);
            b.Box(new Vector3(.302f, -.027f, 0), new Vector3(.18f, .072f, .14f), 11);
            b.Box(new Vector3(.393f, -.064f, 0), new Vector3(.049f, .073f, .095f), 6);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(BenchRest))
        {
            b.Box(new Vector3(0, -.225f, 0), new Vector3(.63f, .11f, .35f), 6, 11);
            b.Box(new Vector3(0, -.013f, .035f), new Vector3(.2f, .35f, .20f), 8, 6);
            foreach (int side in new[] { -1, 1 })
            {
                b.Box(new Vector3(side * .155f, .10f, -.01f), new Vector3(.12f, .18f, .255f), 8, 6);
                b.Box(new Vector3(side * .149f, .203f, -.015f), new Vector3(.10f, .032f, .25f), 11, 6);
            }
            b.Drum(new Vector3(0, -.035f, -.09f), .081f, .056f, 11, 8, 6);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(Crowbar))
        {
            EquipmentRod(b, new Vector3(-.17f, -.40f, 0), new Vector3(.09f, .28f, 0), .054f, 1);
            EquipmentRod(b, new Vector3(.09f, .28f, 0), new Vector3(.195f, .355f, -.005f), .059f, 1);
            EquipmentRod(b, new Vector3(.195f, .355f, -.005f), new Vector3(.31f, .32f, -.018f), .062f, 1);
            b.Box(new Vector3(.311f, .29f, -.05f), new Vector3(.085f, .092f, .037f), 11, 6);
            b.Box(new Vector3(.311f, .29f, .021f), new Vector3(.085f, .092f, .037f), 11, 6);
            b.Box(new Vector3(-.184f, -.411f, 0), new Vector3(.117f, .082f, .072f), 11, 6);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(Flyswatter))
        {
            b.Can(new Vector3(0, -.19f, 0), .039f, .49f, 10, 6);
            b.Can(new Vector3(0, -.33f, 0), .059f, .19f, 6, 10);
            for (int i = -2; i <= 2; i++)
                b.Box(new Vector3(i * .11f, .20f, 0), new Vector3(.037f, .39f - .02f * Mathf.Abs(i), .05f), 7, 6);
            for (int i = 0; i < 5; i++)
                b.Box(new Vector3(0, .03f + i * .085f, 0), new Vector3(.48f - .029f * Mathf.Abs(i - 2), .032f, .055f), 7, 6);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(PaddedSeat))
        {
            b.Box(new Vector3(0, -.18f, 0), new Vector3(.59f, .30f, .30f), 6, 11);
            b.Box(new Vector3(0, -.12f, -.09f), new Vector3(.51f, .25f, .21f), 8, 6);
            b.Box(new Vector3(0, .135f, .065f), new Vector3(.50f, .44f, .22f), 6, 11);
            b.Box(new Vector3(0, .115f, -.075f), new Vector3(.43f, .34f, .16f), 8, 6);
            foreach (int side in new[] { -1, 1 })
                b.Box(new Vector3(side * .255f, -.12f, -.12f), new Vector3(.095f, .30f, .24f), 6, 8);
            b.Box(new Vector3(0, .38f, .04f), new Vector3(.28f, .105f, .2f), 6, 11);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(RubberMallet))
        {
            b.Can(new Vector3(0, -.15f, 0), .073f, .53f, 11, 6);
            b.Can(new Vector3(0, -.29f, 0), .095f, .26f, 6, 11);
            EquipmentRod(b, new Vector3(-.29f, .20f, 0), new Vector3(.29f, .20f, 0), .17f, 6);
            EquipmentRod(b, new Vector3(-.35f, .20f, 0), new Vector3(-.25f, .20f, 0), .183f, 11);
            EquipmentRod(b, new Vector3(.25f, .20f, 0), new Vector3(.35f, .20f, 0), .183f, 11);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(AirPump))
        {
            b.Can(new Vector3(-.09f, -.105f, 0), .132f, .48f, 9, 11);
            b.Can(new Vector3(-.09f, -.338f, 0), .147f, .075f, 6, 11);
            b.Can(new Vector3(-.09f, .13f, 0), .147f, .07f, 6, 11);
            b.Box(new Vector3(-.05f, -.405f, 0), new Vector3(.57f, .075f, .29f), 6, 11);
            b.Can(new Vector3(-.09f, .257f, 0), .047f, .25f, 11, 6);
            b.Box(new Vector3(-.09f, .401f, 0), new Vector3(.44f, .075f, .15f), 10, 6);
            b.Drum(new Vector3(-.09f, .053f, -.14f), .072f, .048f, 6, 10, 11);
            b.Drum(new Vector3(-.09f, .053f, -.171f), .05f, .022f, 3, 10);
            b.Box(new Vector3(-.079f, .059f, -.187f), new Vector3(.042f, .013f, .018f), 1);
            var hose = new[] { new Vector3(.024f, -.36f, -.09f), new Vector3(.2f, -.33f, -.09f),
                new Vector3(.288f, -.232f, -.09f), new Vector3(.305f, -.105f, -.09f),
                new Vector3(.284f, .004f, -.09f), new Vector3(.232f, .085f, -.09f) };
            for (int i = 1; i < hose.Length; i++)
            {
                EquipmentRod(b, hose[i - 1], hose[i], .03f, 6);
                b.Ball(hose[i], Vector3.one * .061f, 6);
            }
            EquipmentRod(b, hose[hose.Length - 1], new Vector3(.171f, .141f, -.09f), .045f, 11);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(CryoVent))
        {
            b.Drum(Vector3.zero, .34f, .22f, 6, 8, 11);
            b.Drum(Vector3.back * .13f, .27f, .09f, 9, 8, 6);
            b.Drum(Vector3.back * .19f, .18f, .055f, 6, 8, 11);
            for (int i = 0; i < 4; i++)
            {
                b.Box(new Vector3(0, -.11f + i * .073f, -.23f), new Vector3(.24f + .036f * (i % 2), .034f, .045f), 11, 7);
                float angle = i * Mathf.PI * .5f + Mathf.PI * .25f;
                b.Drum(new Vector3(Mathf.Cos(angle) * .28f, Mathf.Sin(angle) * .28f, -.15f), .052f, .06f, 11, 6);
            }
            b.Box(new Vector3(0, -.36f, .015f), new Vector3(.24f, .13f, .19f), 11, 6);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(ColdCoil))
        {
            b.Box(Vector3.zero, new Vector3(.37f, .57f, .26f), 6, 11);
            for (int i = 0; i < 5; i++)
            {
                float y = -.21f + i * .105f;
                b.Box(new Vector3(0, y, -.14f), new Vector3(.59f, .046f, .09f), 11, 6);
                b.Box(new Vector3(.22f * (i % 2 == 0 ? 1 : -1), y + .049f, -.19f), new Vector3(.073f, .11f, .08f), 9);
            }
            b.Box(new Vector3(0, .32f, 0), new Vector3(.25f, .075f, .23f), 9, 6);
            b.Box(new Vector3(0, -.32f, 0), new Vector3(.25f, .075f, .23f), 9, 6);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(SparkPlug))
        {
            b.Box(new Vector3(0, -.22f, 0), new Vector3(.19f, .32f, .18f), 11);
            for (int i = 0; i < 5; i++)
                b.Box(new Vector3(0, -.1f - i * .051f, 0), new Vector3(.245f, .023f, .225f), 6);
            b.Prism(new[] { new Vector2(-.18f, -.09f), new Vector2(.18f, -.09f), new Vector2(.22f, -.015f),
                new Vector2(.15f, .07f), new Vector2(-.15f, .07f), new Vector2(-.22f, -.015f) }, .27f, 11, Vector3.zero, 6);
            b.Box(new Vector3(0, .17f, 0), new Vector3(.2f, .23f, .2f), 3, 11);
            for (int i = 0; i < 3; i++)
                b.Box(new Vector3(0, .11f + i * .072f, 0), new Vector3(.25f, .03f, .23f), 3, 11);
            b.Box(new Vector3(0, .31f, 0), new Vector3(.12f, .1f, .12f), 11);
            b.Box(new Vector3(.07f, -.393f, 0), new Vector3(.045f, .11f, .07f), 11);
            b.Box(new Vector3(.041f, -.44f, 0), new Vector3(.1f, .032f, .07f), 11);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(DeflectorPlate))
        {
            var panel = new[] { new Vector2(-.39f, -.22f), new Vector2(.39f, -.22f), new Vector2(.32f, .04f),
                new Vector2(.14f, .3f), new Vector2(-.14f, .3f), new Vector2(-.32f, .04f) };
            b.Prism(panel, .15f, 11, Vector3.zero, 6);
            foreach (int side in new[] { -1, 1 })
            {
                var rail = new[] { new Vector2(side * .31f, -.13f), new Vector2(side * .25f, -.13f),
                    new Vector2(side * .07f, .19f), new Vector2(side * .13f, .19f) };
                if (side < 0) System.Array.Reverse(rail);
                b.Prism(rail, .08f, 9, Vector3.back * .09f, 6);
                b.Drum(new Vector3(side * .27f, -.13f, -.115f), .048f, .025f, 3, 6);
            }
            b.Box(new Vector3(0, -.12f, .115f), new Vector3(.26f, .23f, .13f), 6);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == nameof(RechargingShield))
        {
            var plate = new[] { new Vector2(-.34f, .27f), new Vector2(0, .38f), new Vector2(.34f, .27f),
                new Vector2(.27f, -.15f), new Vector2(0, -.37f), new Vector2(-.27f, -.15f) };
            b.Prism(plate, .17f, 6, Vector3.zero, 11);
            b.Drum(new Vector3(0, .055f, -.115f), .23f, .08f, 11, 6, 6);
            b.Drum(new Vector3(0, .055f, -.17f), .165f, .05f, 9, 6);
            for (int i = 0; i < 3; i++)
                b.Box(new Vector3(0, .14f - i * .09f, -.21f), new Vector3(.15f - i * .025f, .04f, .04f), 3);
            return expansionItems[kind] = b.Finish(kind + " equipment");
        }
        if (kind == "RocketBouquet") { RocketBouquetModel(b); return expansionItems[kind] = b.Finish(kind + " equipment"); }
        if (kind == "Redline") { RedlineModel(b); return expansionItems[kind] = b.Finish(kind + " equipment"); }
        if (kind == "NailBomb") { NailBombModel(b); return expansionItems[kind] = b.Finish(kind + " equipment"); }
        if (kind == "CanOpener") { CanOpenerModel(b); return expansionItems[kind] = b.Finish(kind + " equipment"); }
        if (kind == "FirecrackerBelt") { FirecrackerBeltModel(b); return expansionItems[kind] = b.Finish(kind + " equipment"); }
        if (kind == "CarbonPaper") { CarbonPaperModel(b); return expansionItems[kind] = b.Finish(kind + " equipment"); }
        if (kind == "Thunderhead") { ThunderheadModel(b); return expansionItems[kind] = b.Finish(kind + " equipment"); }
        if (kind == "DemolitionPlunger") { DemolitionPlungerModel(b); return expansionItems[kind] = b.Finish(kind + " equipment"); }
        if (TrySecondaryItem(b, kind) || TryFillerItem(b, kind) || TryRuleItem(b, kind))
            return expansionItems[kind] = b.Finish(kind + " equipment");
        if (DistinctEquipment(b, kind))
            return expansionItems[kind] = b.Finish(kind + " equipment");
        if (BackfireItem(b, kind))
            return expansionItems[kind] = b.Finish(kind + " equipment");
        if (EverydayItem(b, kind))
            return expansionItems[kind] = b.Finish(kind + " equipment");
        if (RequestedItem(b, kind))
            return expansionItems[kind] = b.Finish(kind + " equipment");
        if (ReferenceItem(b, kind))
            return expansionItems[kind] = b.Finish(kind + " equipment");
        if (PactItem(b, kind))
            return expansionItems[kind] = b.Finish(kind + " equipment");
        if (HealthItem(b, kind))
            return expansionItems[kind] = b.Finish(kind + " equipment");
        if (kind == "BaseWeaponItem")
        {
            // The starter is a paired ship-mounted autocannon, not a generic equipment icon.
            b.Prism(new[] { new Vector2(-.27f, -.34f), new Vector2(.27f, -.34f), new Vector2(.33f, .08f), new Vector2(.2f, .27f), new Vector2(-.2f, .27f), new Vector2(-.33f, .08f) }, .25f, 0, Vector3.zero, 6);
            foreach (int side in new[] { -1, 1 })
            {
                b.Box(new Vector3(side * .19f, .3f, -.03f), new Vector3(.15f, .65f, .18f), 11, 6);
                b.Box(new Vector3(side * .19f, .61f, -.03f), new Vector3(.2f, .13f, .23f), 6);
                b.Box(new Vector3(side * .19f, .68f, -.03f), new Vector3(.09f, .03f, .1f), 11);
                for (int fin = 0; fin < 4; fin++)
                    b.Box(new Vector3(side * .19f, .15f + fin * .1f, -.15f), new Vector3(.18f, .045f, .05f), 6);
                b.Box(new Vector3(side * .31f, -.18f, -.02f), new Vector3(.16f, .33f, .25f), 3);
            }
            b.Box(new Vector3(0, -.06f, -.18f), new Vector3(.13f, .29f, .08f), 9);
            b.Box(new Vector3(0, -.32f, -.03f), new Vector3(.24f, .14f, .3f), 6);
        }
        else if (kind == "LargeHealthKit")
        {
            b.Box(Vector3.zero, new Vector3(.65f, .48f, .26f), 1, 6);
            b.Box(new Vector3(0, 0, -.16f), new Vector3(.33f, .1f, .04f), 3);
            b.Box(new Vector3(0, 0, -.17f), new Vector3(.1f, .33f, .04f), 3);
            b.Box(new Vector3(0, .3f, 0), new Vector3(.27f, .08f, .16f), 11);
        }
        else if (kind == "Lighter")
        {
            b.Box(new Vector3(0, -.08f, 0), new Vector3(.3f, .5f, .2f), 3, 6);
            b.Box(new Vector3(0, .23f, 0), new Vector3(.32f, .16f, .22f), 6);
            b.Drum(new Vector3(-.08f, .32f, -.05f), .07f, .15f, 11, 8);
            b.Prism(new[] { new Vector2(-.05f, 0), new Vector2(.05f, 0), new Vector2(0, .2f) }, .04f, 10, new Vector3(.09f, .32f, 0));
        }
        else if (kind == "BallBearing")
        {
            b.Drum(Vector3.zero, .32f, .12f, 6, 16);
            b.Drum(new Vector3(0, 0, -.09f), .25f, .08f, 11, 16);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4;
                b.Ball(new Vector3(Mathf.Cos(a) * .22f, Mathf.Sin(a) * .22f, -.16f), Vector3.one * .085f, 1);
            }
        }
        else if (kind == "GravityWell" || kind == "MirrorBurst" || kind == "BlankBullet")
        {
            b.Drum(Vector3.zero, .33f, .18f, 11, 8, 6);
            b.Drum(new Vector3(0, 0, -.13f), .26f, .09f, kind == "BlankBullet" ? 3 : 7, 8);
            if (kind == "GravityWell")
            {
                b.Drum(new Vector3(0, 0, -.2f), .17f, .07f, 9, 12);
                b.Drum(new Vector3(0, 0, -.25f), .105f, .06f, 11, 12);
                foreach (int s in new[] { -1, 1 })
                    b.Box(new Vector3(s * .38f, 0, -.08f), new Vector3(.15f, .37f, .17f), 6);
            }
            else if (kind == "MirrorBurst")
                for (int i = 0; i < 6; i++)
                {
                    float a = i * Mathf.PI / 3;
                    var p = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    var q = new Vector2(-p.y, p.x);
                    b.Prism(new[] { p * .22f - q * .08f, p * .48f, p * .22f + q * .08f }, .12f, 9, Vector3.back * .23f, 6);
                }
            else
            {
                b.Box(new Vector3(0, .37f, 0), new Vector3(.2f, .16f, .2f), 6);
                b.Box(new Vector3(0, 0, -.23f), new Vector3(.12f, .28f, .05f), 10);
            }
        }
        else if (kind.EndsWith("Item"))
        {
            bool beam = kind == "BeamEmitterItem", shell = kind == "BurstLauncherItem", disc = kind == "DiscThrowerItem", flame = kind == "FlameProjectorItem";
            b.Box(new Vector3(0, -.12f, 0), new Vector3(.42f, .4f, .3f), 0, 6);
            if (disc)
            {
                b.Drum(Vector3.zero, .35f, .14f, 6, 12);
                b.Drum(new Vector3(0, 0, -.1f), .15f, .08f, 9, 12);
            }
            else
            {
                int count = kind == "ScatterCannonItem" ? 3 : kind == "SwarmRackItem" || kind == "RailDriverItem" ? 2 : 1;
                for (int i = 0; i < count; i++)
                {
                    float x = (i - (count - 1) * .5f) * .2f;
                    b.Box(new Vector3(x, .27f, 0), new Vector3(shell ? .3f : .13f, .65f, .18f), 11, 6);
                    b.Box(new Vector3(x, .56f, 0), new Vector3(shell ? .36f : .19f, .12f, .24f), beam ? 9 : flame ? 3 : 6);
                    if (kind == "RailDriverItem")
                        for (int coil = 0; coil < 4; coil++)
                            b.Box(new Vector3(x, .12f + coil * .13f, -.13f), new Vector3(.16f, .055f, .06f), 9);
                    if (kind == "SwarmRackItem")
                        for (int rocket = 0; rocket < 3; rocket++)
                            b.Drum(new Vector3(x, -.13f + rocket * .19f, -.22f), .075f, .13f, 3, 6);
                }
            }
            b.Box(new Vector3(0, -.17f, -.2f), new Vector3(.24f, .16f, .05f), beam ? 9 : 3);
            for (int i = 0; i < 3; i++)
                b.Box(new Vector3(-.14f + i * .14f, -.31f, -.17f), new Vector3(.045f, .08f, .04f), 6);
        }
        else if (kind == "DroneLink")
        {
            b.Drum(Vector3.zero, .22f, .2f, 0, 8);
            foreach (int s in new[] { -1, 1 })
            {
                b.Box(new Vector3(s * .28f, 0, 0), new Vector3(.25f, .13f, .14f), 6);
                b.Drum(new Vector3(s * .4f, 0, 0), .13f, .1f, 9, 8);
            }
            b.Drum(new Vector3(0, 0, -.14f), .08f, .05f, 9, 8);
        }
        else if (kind == "FocusingLens")
        {
            b.Drum(Vector3.zero, .32f, .24f, 6, 12);
            b.Drum(new Vector3(0, 0, -.15f), .25f, .1f, 9, 12);
            b.Drum(new Vector3(0, 0, -.22f), .14f, .08f, 10, 12);
        }
        else if (kind == "Capacitor" || kind == "SpareBattery" || kind == "ReserveCell" || kind == "Overdrive")
        {
            b.Box(Vector3.zero, new Vector3(.5f, .6f, .3f), kind == "SpareBattery" ? 7 : 0, 6);
            foreach (int s in new[] { -1, 1 })
                b.Box(new Vector3(s * .13f, .35f, 0), new Vector3(.1f, .13f, .2f), 3);
            b.Box(new Vector3(0, 0, -.18f), new Vector3(.31f, .38f, .05f), 11);
            for (int i = 0; i < 3; i++)
                b.Box(new Vector3(0, -.1f + i * .1f, -.22f), new Vector3(.23f, .055f, .03f), 9);
        }
        else
        {
            b.Drum(Vector3.zero, .3f, .22f, 6, 10);
            b.Drum(new Vector3(0, 0, -.15f), .2f, .1f, kind == "ChainReactor" ? 3 : 9, 10);
            int count = kind == "SplitterCore" ? 2 : kind == "ReboundCoil" ? 4 : 3;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2 / count;
                var p = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * .36f;
                b.Box(p, new Vector3(.16f, .16f, .2f), 0);
                b.Drum(p + Vector3.back * .15f, .06f, .05f, 3, 6);
            }
        }
        return expansionItems[kind] = b.Finish(kind + " equipment");
    }

    // A coin hopper feeding a short launcher: coins drop through a window in the feed tube into
    // the breech, and a gold-tipped homing dart leaves the angled barrel.
    static Mesh CoinLauncherModel()
    {
        var b = new Builder();
        b.Box(new Vector3(0, -.42f, 0), new Vector3(.66f, .06f, .38f), 11, 6);
        b.Box(new Vector3(.02f, -.26f, 0), new Vector3(.52f, .24f, .32f), 6, 6);
        b.Prism(new[] { new Vector2(-.48f, .40f), new Vector2(.02f, .40f), new Vector2(-.1f, .1f), new Vector2(-.36f, .1f) }, .3f, 6, Vector3.zero, 11);
        b.Box(new Vector3(-.23f, .41f, -.02f), new Vector3(.52f, .04f, .34f), 10, 10);
        for (int i = 0; i < 3; i++)
            b.Drum(new Vector3(-.36f + i * .13f, .5f + (i % 2) * .06f, -.04f), .085f, .045f, 10, 10, 6);
        b.Box(new Vector3(-.23f, -.04f, 0), new Vector3(.15f, .3f, .2f), 6, 11);
        b.Drum(new Vector3(-.23f, -.03f, -.11f), .06f, .03f, 10, 10, 6);
        EquipmentRod(b, new Vector3(.14f, -.2f, 0), new Vector3(.48f, .16f, 0), .1f, 6);
        EquipmentRod(b, new Vector3(.44f, .12f, 0), new Vector3(.54f, .22f, 0), .135f, 10);
        EquipmentRod(b, new Vector3(.6f, .28f, 0), new Vector3(.72f, .4f, 0), .045f, 3);
        b.Ball(new Vector3(.75f, .43f, 0), new Vector3(.06f, .06f, .06f), 10);
        foreach (int side in new[] { -1, 1 })
            b.Prism(new[] { new Vector2(.6f, .28f), new Vector2(.6f + side * .05f, .2f + (side > 0 ? .08f : 0)), new Vector2(.64f, .3f) }, .04f, 3, Vector3.zero);
        return b.Finish("Coin launcher equipment");
    }
}
