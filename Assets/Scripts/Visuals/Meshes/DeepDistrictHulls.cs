using System.Collections.Generic;
using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static readonly Dictionary<CombatHullId, Mesh> deepHulls = new Dictionary<CombatHullId, Mesh>();
    static Mesh plagueTail, gantryCarriage;

    public static Mesh DeepHull(CombatHullId id)
    {
        if (deepHulls.TryGetValue(id, out var mesh) && mesh != null)
            return mesh;
        var b = new Builder();
        switch (id)
        {
            case CombatHullId.PlagueMote:
                ModelPlagueMote(b);
                break;
            case CombatHullId.SiltLeech:
                ModelSiltLeech(b);
                break;
            case CombatHullId.SluiceBarge:
                ModelSluiceBarge(b);
                break;
            case CombatHullId.OverflowBell:
                ModelOverflowBell(b);
                break;
            case CombatHullId.PipeSkater:
                ModelPipeSkater(b);
                break;
            case CombatHullId.GrateKeeper:
                BevelArmor(b, new[] { V(-1.12f, -.45f), V(-.76f, -.95f), V(.76f, -.95f), V(1.12f, -.45f), V(.91f, .82f), V(0, 1.02f), V(-.91f, .82f) }, .54f, 2);
                for (int i = -3; i <= 3; i++)
                    b.Box(new Vector3(i * .21f, .17f, -.39f), new Vector3(.086f, .95f, .16f), 6, 11);
                DeepPort(b, new Vector3(-.64f, -.60f, -.39f), .34f, 13);
                DeepPort(b, new Vector3(.64f, -.60f, -.39f), .34f, 13);
                break;
            case CombatHullId.TransitCube:
                ModelTransitCube(b);
                break;
            case CombatHullId.FuseCube:
                ModelFuseCube(b);
                break;
            case CombatHullId.SentryCube:
                ModelSentryCube(b);
                break;
            case CombatHullId.RamCube:
                ModelRamCube(b);
                break;
            case CombatHullId.RelayCarrier:
                ModelRelayCarrier(b);
                break;
            case CombatHullId.RivetCutter:
                BevelArmor(b, new[] { V(0, -1.12f), V(.41f, -.62f), V(1.02f, .52f), V(.73f, .95f), V(.28f, .40f), V(-.27f, .69f), V(-.93f, .68f), V(-.76f, .13f), V(-.34f, -.50f) }, .34f, 6);
                CutStone(b, new Vector3(-.08f, .12f, -.27f), new Vector3(.39f, .38f, .19f), 5, 4);
                Barrel(b, 0, -.55f, .48f, .17f);
                break;
            case CombatHullId.PressWarden:
                BevelArmor(b, new[] { V(-1.1f, -.72f), V(-.47f, -.82f), V(-.35f, -.24f), V(.35f, -.24f), V(.47f, -.82f), V(1.1f, -.72f), V(.88f, .65f), V(.52f, 1.02f), V(-.52f, 1.02f), V(-.88f, .65f) }, .47f, 6);
                foreach (int side in new[] { -1, 1 })
                {
                    b.Box(new Vector3(side * .74f, .02f, -.29f), new Vector3(.37f, 1.23f, .35f), 0, 11);
                    DeepPort(b, new Vector3(side * .77f, -.65f, -.33f), .25f, 5);
                }
                CutStone(b, new Vector3(0, .51f, -.35f), new Vector3(.38f, .38f, .16f), 3, 6);
                break;
            case CombatHullId.CisternHeart:
                BuildCisternHeart(b);
                break;
            case CombatHullId.CoreGantry:
                BuildGantry(b);
                break;
        }
        return deepHulls[id] = b.Finish(id.ToString());
    }

    static void ModelPlagueMote(Builder b)
    {
        // A swimming rat, with a narrow snout, two cupped ears and tucked hind legs.
        KeelSkin(b, 0, 2, Frame(-.84f,.025f,.02f), Frame(-.58f,.22f,.16f),
            Frame(-.26f,.37f,.24f), Frame(.22f,.46f,.30f), Frame(.48f,.37f,.24f), Frame(.73f,.09f,.08f));
        for (int s = -1; s <= 1; s += 2)
        {
            b.Pearl(new Vector3(s*.30f,-.23f,-.21f),new Vector3(.20f,.19f,.10f),13,10,5);
            b.Pearl(new Vector3(s*.32f,-.23f,-.29f),new Vector3(.115f,.12f,.045f),5,8,4);
            b.Pearl(new Vector3(s*.16f,-.53f,-.15f),new Vector3(.08f,.09f,.07f),10,8,4);
            CutBlade(b,new[]{new Vector3(s*.33f,.14f,.03f),new Vector3(s*.53f,.31f,0),
                new Vector3(s*.44f,.57f,0),new Vector3(s*.65f,.68f,0)},.105f,.08f,14);
        }
        CutBlade(b,new[]{new Vector3(0,-.15f,-.22f),new Vector3(0,.26f,-.32f),
            new Vector3(0,.70f,-.09f)},.11f,.04f,14);
        b.Pearl(new Vector3(0,-.76f,-.06f),new Vector3(.106f,.091f,.075f),5,10,5);
        for (int side=-1;side<=1;side+=2)
        {
            b.Sweep(new Vector3(side*.14f,-.60f,-.12f),new Vector3(side*.28f,-.63f,-.17f),
                new Vector3(side*.46f,-.56f,-.12f),new Vector3(side*.58f,-.56f,-.10f),.027f,.026f,15,false,5);
            CutBlade(b,new[]{new Vector3(side*.28f,-.19f,.06f),new Vector3(side*.44f,-.31f,.03f),
                new Vector3(side*.42f,-.52f,0)},.087f,.055f,2);
        }
    }

    static void ModelSiltLeech(Builder b)
    {
        // A ribbon leech with an offset spine curling back from its broad sucker.
        KeelSkin(b, 0, 8, Frame(-.78f,.25f,.12f), Frame(-.45f,.39f,.24f,-.10f),
            Frame(-.10f,.33f,.24f,-.24f),Frame(.26f,.36f,.21f,-.11f),
            Frame(.54f,.27f,.18f,.16f),Frame(.76f,.17f,.13f,.44f),Frame(.91f,.045f,.04f,.58f));
        for(int k=0;k<6;k++)
        {
            float y=-.40f+k*.19f, x=-.23f*Mathf.Cos((y+.08f)*3.4f)+Mathf.Max(0,y)*.48f;
            float r=.34f-.11f*Mathf.Abs(y);
            b.Sweep(new Vector3(x-r,y,0),new Vector3(x-r*.54f,y+.16f,-.29f),
                new Vector3(x+r*.54f,y+.16f,-.29f),new Vector3(x+r,y,0),.048f,.045f,14,false,6);
        }
        var mouth=new Vector3(0,-.63f,-.24f);
        b.Pearl(mouth,new Vector3(.43f,.38f,.13f),11,16,6);
        b.Halo(mouth+Vector3.back*.09f,.32f,.075f,new Vector3(1.16f,.91f,1),Quaternion.identity,13,16);
        for(int k=0;k<9;k++)
        {
            float a=k*Mathf.PI*2/9;
            Vector3 lip=new Vector3(Mathf.Cos(a)*.30f,Mathf.Sin(a)*.24f,-.12f);
            CutBlade(b,new[]{mouth+lip,mouth+lip*.56f+Vector3.back*.10f},.048f,.035f,15);
        }
        for(int side=-1;side<=1;side+=2)
            for(int i=0;i<4;i++)
            {
                float y=-.20f+i*.20f, x=-.19f+i*.053f;
                CutBlade(b,new[]{new Vector3(x+side*.20f,y,-.05f),new Vector3(x+side*.42f,y+.07f,0),
                    new Vector3(x+side*.49f,y+.18f,.04f)},.086f,.044f,2);
            }
    }

    static void ModelSluiceBarge(Builder b)
    {
        // Twin flotation caissons carry a recessed three-tube mortar deck.
        for(int s=-1;s<=1;s+=2)
        {
            float x=s*.73f;
            KeelSkin(b,.02f,8,Frame(-.91f,.035f,.07f,x),Frame(-.57f,.28f,.24f,x),
                Frame(.57f,.28f,.24f,x),Frame(.97f,.10f,.14f,x));
            for(int k=-1;k<=1;k++)
                b.Box(new Vector3(x,k*.40f,-.21f),new Vector3(.50f,.10f,.095f),13,2);
        }
        BevelArmor(b,new[]{V(-.79f,-.49f),V(-.62f,-.75f),V(.62f,-.75f),V(.79f,-.49f),V(.68f,.62f),V(-.68f,.62f)},.36f,2);
        b.Box(new Vector3(0,.40f,-.26f),new Vector3(.88f,.59f,.35f),13,8);
        b.Box(new Vector3(0,.29f,-.45f),new Vector3(.72f,.23f,.055f),11);
        for(int k=-1;k<=1;k++)
        {
            b.Box(new Vector3(k*.22f,.27f,-.49f),new Vector3(.13f,.12f,.035f),9);
            MothBarrel(b,new Vector3(k*.46f,-.84f,-.43f),.18f,.65f);
        }
        for(int side=-1;side<=1;side+=2)
        {
            float x=side*.89f;
            b.Sweep(new Vector3(x,-.55f,-.22f),new Vector3(x,-.48f,-.36f),
                new Vector3(x,.48f,-.36f),new Vector3(x,.63f,-.22f),.033f,.034f,6,false,6);
            Inlet(b,side*.29f,.69f,.32f,.27f,-.26f);
        }
        b.Box(new Vector3(0,.42f,-.47f),new Vector3(.97f,.13f,.065f),6);
    }

    static void ModelOverflowBell(Builder b)
    {
        // A riveted diving bell suspended between two return elbows.
        b.Pearl(new Vector3(0,.02f,0),new Vector3(.65f,.77f,.44f),13,20,10);
        for(int s=-1;s<=1;s+=2)
        {
            b.Sweep(new Vector3(s*.34f,-.35f,-.17f),new Vector3(s*.72f,-.65f,-.24f),
                new Vector3(s*1.04f,-.19f,-.17f),new Vector3(s*.77f,.21f,-.18f),.12f,.13f,8,false,10);
            DeepPort(b,new Vector3(s*.77f,.21f,-.18f),.21f,13);
            CutBlade(b,new[]{new Vector3(s*.28f,-.70f,-.12f),new Vector3(s*.40f,-.14f,-.40f),
                new Vector3(s*.32f,.41f,-.35f),new Vector3(s*.13f,.70f,-.13f)},.066f,.04f,6);
        }
        var hatch=new Vector3(0,.05f,-.39f);
        b.Halo(hatch,.34f,.065f,Vector3.one,Quaternion.identity,13,20);
        b.Pearl(hatch,new Vector3(.285f,.285f,.11f),8,14,7);
        for(int i=-1;i<=1;i++) b.Box(hatch+new Vector3(i*.13f,0,-.12f),new Vector3(.040f,.43f,.04f),6);
    }

    static void ModelPipeSkater(Builder b)
    {
        // Water-strider chassis. Four long articulated skates leave open water between the feet.
        KeelSkin(b,0,8,Frame(-.88f,.08f,.07f),Frame(-.37f,.22f,.19f),Frame(.02f,.29f,.23f),
            Frame(.46f,.19f,.14f),Frame(.79f,.03f,.04f));
        for(int s=-1;s<=1;s+=2)
        {
            for(int leg=0;leg<2;leg++)
            {
                float y=leg==0?-.18f:.28f, end=leg==0?-.60f:.86f;
                var knee=new Vector3(s*(leg==0?.63f:.56f),y+.25f,-.16f);
                CutBlade(b,new[]{new Vector3(s*.13f,y,-.11f),knee,new Vector3(s*.94f,end+.05f,0)},.087f,.073f,13);
                b.Pearl(knee,new Vector3(.10f,.11f,.08f),6,8,5);
                b.Pearl(new Vector3(s*.94f,end,0),new Vector3(.082f,.22f,.06f),8,8,5);
            }
        }
        b.Pearl(new Vector3(0,-.16f,-.17f),new Vector3(.16f,.30f,.13f),9,12,6);
        MothBarrel(b,new Vector3(0,-1.05f,-.23f),.095f,.51f);
        for(int side=-1;side<=1;side+=2)
        {
            b.Pearl(new Vector3(side*.13f,.25f,-.14f),new Vector3(.11f,.29f,.105f),14,12,6);
            CutBlade(b,new[]{new Vector3(side*.18f,.34f,-.24f),new Vector3(side*.24f,.18f,-.20f),
                new Vector3(side*.50f,.21f,-.12f)},.044f,.03f,1);
        }
    }

    static void ModelTransitCube(Builder b)
    {
        // A sealed freight block. Two diagonal load straps lock into cast corner shoes.
        BevelArmor(b,new[]{V(-.81f,-.64f),V(-.64f,-.81f),V(.64f,-.81f),V(.81f,-.64f),V(.81f,.64f),V(.64f,.81f),V(-.64f,.81f),V(-.81f,.64f)},.64f,0);
        for(int s=-1;s<=1;s+=2)
        {
            b.Sweep(new Vector3(s*-.63f,-.62f,-.32f),new Vector3(s*-.25f,-.24f,-.42f),
                new Vector3(s*.25f,.24f,-.42f),new Vector3(s*.63f,.62f,-.32f),.115f,.06f,6,false,4);
            for(int j=-1;j<=1;j+=2)
            {
                b.Box(new Vector3(s*.61f,j*.62f,-.29f),new Vector3(.39f,.36f,.20f),1,0);
                b.Drum(new Vector3(s*.60f,j*.62f,-.41f),.077f,.04f,11,6);
            }
        }
        CutStone(b,new Vector3(0,0,-.41f),new Vector3(.30f,.29f,.085f),0,6);
        b.Box(new Vector3(0,.01f,-.50f),new Vector3(.30f,.075f,.035f),13);
    }

    static void ModelFuseCube(Builder b)
    {
        // The vulnerable cartridge is suspended between two porcelain terminal blocks.
        b.Box(Vector3.zero,new Vector3(1.60f,1.60f,.39f),11,0);
        for(int s=-1;s<=1;s+=2)
        {
            b.Box(new Vector3(0,s*.62f,-.26f),new Vector3(1.58f,.36f,.32f),1,6);
            b.Box(new Vector3(s*.72f,0,-.20f),new Vector3(.15f,1.26f,.22f),0);
            b.Drum(new Vector3(s*.52f,-.63f,-.45f),.09f,.06f,13,8);
            b.Drum(new Vector3(s*.52f,.63f,-.45f),.09f,.06f,13,8);
        }
        for(int i=-1;i<=1;i++)
        {
            float x=i*.37f;
            b.Pearl(new Vector3(x,0,-.27f),new Vector3(.14f,.54f,.14f),i==0?10:3,12,8);
            for(int end=-1;end<=1;end+=2)
                b.Box(new Vector3(x,end*.40f,-.28f),new Vector3(.31f,.145f,.28f),13,6);
        }
    }

    static void ModelSentryCube(Builder b)
    {
        // A recessed turret socket flanked by an ammunition belt and a sloping access hatch.
        BevelArmor(b,new[]{V(-.81f,-.81f),V(.81f,-.81f),V(.81f,.65f),V(.65f,.81f),V(-.81f,.81f)},.48f,0);
        b.Drum(new Vector3(0,.04f,-.28f),.59f,.15f,11,16,6);
        b.Drum(new Vector3(0,.05f,-.38f),.49f,.15f,6,12,0);
        CutStone(b,new Vector3(0,.12f,-.43f),new Vector3(.38f,.45f,.22f),5,8);
        MothBarrel(b,new Vector3(0,-.43f,-.64f),.19f,.50f);
        for(int j=-2;j<=2;j++)
            b.Box(new Vector3(-.62f,j*.22f,-.31f),new Vector3(.22f,.13f,.16f),13,0);
        b.Box(new Vector3(.64f,0,-.31f),new Vector3(.23f,1.19f,.18f),1,6);
        CutStone(b,new Vector3(.31f,.39f,-.49f),new Vector3(.11f,.16f,.09f),9,8);
    }

    static void ModelRamCube(Builder b)
    {
        // A hydraulic press with two protective jaws and an exposed orange piston.
        b.Box(Vector3.zero,new Vector3(1.60f,1.52f,.40f),11,6);
        for(int s=-1;s<=1;s+=2)
        {
            BevelArmor(b,new[]{V(s*.40f,-.80f),V(s*.80f,-.80f),V(s*.80f,.80f),V(s*.57f,.80f),V(s*.40f,.56f)},.51f,1,-.13f);
            b.Box(new Vector3(s*.55f,.53f,-.41f),new Vector3(.42f,.23f,.10f),0);
            DeepPort(b,new Vector3(s*.57f,-.13f,-.45f),.21f,6);
        }
        b.Box(new Vector3(0,.46f,-.28f),new Vector3(.83f,.51f,.39f),5,6);
        KeelSkin(b,-.30f,13,Frame(-.48f,.15f,.14f),Frame(-.43f,.21f,.18f),Frame(.38f,.21f,.18f));
        for(int k=0;k<4;k++)
            b.Box(new Vector3(0,-.33f+k*.15f,-.42f),new Vector3(.45f,.074f,.12f),6);
        b.Box(new Vector3(0,-.63f,-.31f),new Vector3(.87f,.32f,.36f),1,0);
    }

    static void ModelRelayCarrier(Builder b)
    {
        // Twin racks are joined only at the back. Ceramic insulators frame the space between.
        BevelArmor(b,new[]{V(-1.04f,-.53f),V(-.56f,-.68f),V(-.43f,.37f),V(.43f,.37f),
            V(.56f,-.68f),V(1.04f,-.53f),V(.94f,.63f),V(.60f,.93f),V(-.60f,.93f),V(-.94f,.63f)},.40f,0);
        for(int s=-1;s<=1;s+=2)
        {
            float x=s*.69f;
            b.Box(new Vector3(x,.04f,-.21f),new Vector3(.44f,.99f,.15f),11);
            for(int i=-2;i<=2;i++)
                b.Box(new Vector3(x,i*.17f+.08f,-.35f),new Vector3(.50f,.094f,.17f),i==0?3:1,6);
            MothBarrel(b,new Vector3(x,-.81f,-.24f),.17f,.48f);
        }
        CutStone(b,new Vector3(0,.62f,-.22f),new Vector3(.41f,.24f,.18f),1,8);
        CutStone(b,new Vector3(0,.55f,-.36f),new Vector3(.24f,.15f,.09f),9,8);
    }

    public static Mesh PlagueTail()
    {
        if (plagueTail != null) return plagueTail;
        var b = new Builder();
        KeelSkin(b, .02f, 14, Frame(-.11f,.125f,.085f), Frame(.04f,.14f,.097f),
            Frame(.22f,.103f,.073f,.045f), Frame(.46f,.074f,.051f,.065f), Frame(.53f,.045f,.04f,.055f));
        return plagueTail = b.Finish("Sinuous plague tail");
    }

    static void DeepPort(Builder b, Vector3 p, float radius, int paint)
    {
        b.Drum(p, radius, .25f, paint, 10, 6);
        b.Drum(p + Vector3.back * .147f, radius * .70f, .04f, 11, 10, paint);
        CutHalo(b, p + Vector3.back * .17f, radius * .79f, radius * .09f, Vector3.one, Quaternion.identity, paint, 10);
    }

    static void BuildCisternHeart(Builder b)
    {
        var ring = new Vector2[20];
        for (int i = 0; i < ring.Length; i++)
        {
            float a = i * Mathf.PI * 2 / ring.Length;
            ring[i] = new Vector2(Mathf.Cos(a) * 1.87f, Mathf.Sin(a) * 1.29f);
        }
        BevelArmor(b, ring, .54f, 8, .04f, .26f);
        // Stepped intake and structural rim make this a submerged pumping station.
        CutHalo(b, new Vector3(0, .02f, -.30f), 1.22f, .12f, new Vector3(1.30f, .86f, 1), Quaternion.identity, 6, 10);
        CutStone(b, new Vector3(0, .01f, -.30f), new Vector3(1.22f, .83f, .24f), 11, 10);
        CutStone(b, new Vector3(0, .01f, -.54f), new Vector3(.76f, .48f, .12f), 2, 10);
        CutHalo(b, new Vector3(0, 0, -.64f), .65f, .11f, new Vector3(1.25f, .83f, .9f), Quaternion.identity, 13, 16);
        for (int i = -2; i <= 2; i++)
            b.Box(new Vector3(i * .22f, -.04f, -.70f), new Vector3(.095f, .53f - Mathf.Abs(i) * .08f, .19f), 14, 11);
        foreach (int side in new[] { -1, 1 })
        {
            CutBlade(b, new[] { new Vector3(side * .62f, -.90f, -.40f), new Vector3(side * 1.12f, -.86f, -.38f), new Vector3(side * 1.55f, -.54f, -.34f) }, .13f, .14f, 6);
            CutBlade(b, new[] { new Vector3(side * .54f, .89f, -.40f), new Vector3(side * .89f, 1.04f, -.35f), new Vector3(side * 1.13f, .80f, -.30f) }, .12f, .13f, 6);
            b.Box(new Vector3(side * 1.15f, -.02f, -.38f), new Vector3(.20f, .58f, .15f), 6, 11);
            for (int n = -1; n <= 1; n++)
                CutStone(b, new Vector3(side * 1.16f, n * .19f, -.48f), new Vector3(.055f, .055f, .04f), 13, 6);
            DeepPort(b, new Vector3(side * 1.40f, -.47f, -.33f), .39f, 13);
            DeepPort(b, new Vector3(side * 1.61f, .19f, -.29f), .33f, 13);
            DeepPort(b, new Vector3(side * 1.13f, .80f, -.24f), .34f, 6);
        }
        DeepPort(b, new Vector3(0, 1.11f, -.32f), .35f, 13);
    }

    static void BuildGantry(Builder b)
    {
        b.Box(new Vector3(0, 0, .14f), new Vector3(7.82f, .51f, .27f), 11, 0);
        for (int rib = -5; rib <= 5; rib++)
        {
            float x = rib * .64f;
            float incline = rib % 2 == 0 ? 1 : -1;
            CutBlade(b, new[] { new Vector3(x - .29f, -.31f * incline, -.17f), new Vector3(x + .29f, .31f * incline, -.17f) },
                .135f, .17f, rib % 4 == 0 ? 13 : 6);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            b.Box(new Vector3(0, side * .34f, -.29f), new Vector3(8.5f, .19f, .33f), 6, 0);
            Vector3 end = new Vector3(side * 4.18f, .08f, -.11f);
            b.Prism(new[] { V(-.45f, -.72f), V(.45f, -.72f), V(.45f, .64f), V(.23f, .87f), V(-.23f, .87f), V(-.45f, .64f) },
                .65f, 6, end, 0, 11);
            b.Box(end + new Vector3(0, .24f, -.39f), new Vector3(.65f, .69f, .18f), 0, 11);
            for (int bearing = -1; bearing <= 1; bearing += 2)
                CutStone(b, end + new Vector3(0, bearing * .58f, -.43f), new Vector3(.21f, .24f, .13f), 5, 6);
            DeepPort(b, new Vector3(side * 3.78f, -.53f, -.38f), .26f, 3);
        }
    }

    public static Mesh GantryCarriage()
    {
        if (gantryCarriage != null) return gantryCarriage;
        var b = new Builder();
        BevelArmor(b, new[] { V(-1.16f, -.23f), V(-.72f, -1.02f), V(.72f, -1.02f), V(1.16f, -.23f), V(.88f, .72f), V(.43f, 1.10f), V(-.43f, 1.10f), V(-.88f, .72f) }, .62f, 6);
        foreach (int side in new[] { -1, 1 })
        {
            b.Box(new Vector3(side * .96f, .20f, -.36f), new Vector3(.30f, 1.15f, .28f), 0, 11);
            b.Box(new Vector3(side * .98f, .48f, -.52f), new Vector3(.37f, .20f, .20f), 13, 6);
            b.Box(new Vector3(side * .98f, -.23f, -.52f), new Vector3(.37f, .20f, .20f), 13, 6);
            CutBlade(b, new[] { new Vector3(side * .44f, .89f, -.38f), new Vector3(side * .75f, .57f, -.43f), new Vector3(side * .83f, -.42f, -.40f) }, .11f, .12f, 0);
            CutStone(b, new Vector3(side * .72f, -.66f, -.40f), new Vector3(.19f, .24f, .16f), 11, 6);
        }
        CutStone(b, new Vector3(0, .15f, -.34f), new Vector3(.80f, .76f, .20f), 11, 10);
        CutHalo(b, new Vector3(0, .15f, -.57f), .67f, .12f, Vector3.one, Quaternion.identity, 0, 10);
        CutHalo(b, new Vector3(0, .15f, -.71f), .48f, .075f, Vector3.one, Quaternion.identity, 13, 10);
        CutStone(b, new Vector3(0, .15f, -.72f), new Vector3(.38f, .38f, .21f), 5, 8, .39f);
        CutStone(b, new Vector3(0, .15f, -.94f), new Vector3(.19f, .19f, .08f), 3, 8, .39f);
        for (int i = 0; i < 4; i++)
        {
            float angle = (i * 90 + 45) * Mathf.Deg2Rad;
            Vector3 radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            CutBlade(b, new[] { new Vector3(0, .15f, -.65f) + radial * .80f, new Vector3(0, .15f, -.77f) + radial * .51f }, .10f, .10f, 6);
        }
        // The lip coincides with CoreMuzzle on the independently moving carriage.
        DeepPort(b, new Vector3(0, -.77f, -.11f), .29f, 3);
        return gantryCarriage = b.Finish("Gantry sliding reactor carriage");
    }
}
