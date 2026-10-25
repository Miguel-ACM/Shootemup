using UnityEngine;

public static partial class IndustrialMeshFactory
{
    // Equipment atlas: ceramic, charcoal, red, blue, gold, coffee, moon, crater,
    // skin, ink, cloth, steel, white, glass, label, dark trim.
    static void Moon(Builder b,Vector3 center,float size,bool angry=false)
    {
        b.LunarSphere(center,size,6);
        if(angry)
        {
            b.Prism(new[]{new Vector2(-.24f,.18f),new Vector2(-.06f,.08f),new Vector2(-.22f,.03f)},.03f,9,center+new Vector3(0,0,-size));
            b.Prism(new[]{new Vector2(.24f,.18f),new Vector2(.22f,.03f),new Vector2(.06f,.08f)},.03f,9,center+new Vector3(0,0,-size));
            b.Box(center+new Vector3(0,-.16f,-size),new Vector3(.23f,.045f,.03f),9,9);
        }
    }

    public static Mesh CreateItem(int kind)
    {
        var b=new Builder();
        switch(kind)
        {
            case 0: // Red medical kit, white cross and carrying handle.
                b.Box(Vector3.zero,new Vector3(.66f,.48f,.30f),2,11);
                b.Box(new Vector3(0,0,-.18f),new Vector3(.37f,.10f,.06f),12,12);
                b.Box(new Vector3(0,0,-.19f),new Vector3(.10f,.33f,.06f),12,12);
                b.Box(new Vector3(0,.29f,0),new Vector3(.28f,.08f,.12f),1,1);break;
            case 1: // Blue energy cell with contacts and a lightning emblem.
                b.Box(Vector3.zero,new Vector3(.38f,.66f,.28f),3,11);
                foreach(float x in new[]{-.11f,.11f})b.Box(new Vector3(x,.37f,0),new Vector3(.11f,.10f,.18f),11,11);
                Bolt(b,new Vector3(0,0,-.18f),.48f);break;
            case 2: // Gold score coin.
                b.Drum(Vector3.zero,.34f,.12f,4,10,4);
                foreach(float y in new[]{-.12f,0,.12f})b.Box(new Vector3(0,y,-.09f),new Vector3(.24f,.045f,.035f),5,4);
                b.Box(new Vector3(-.10f,.06f,-.09f),new Vector3(.045f,.12f,.035f),5,4);
                b.Box(new Vector3(.10f,-.06f,-.09f),new Vector3(.045f,.12f,.035f),5,4);break;
            case 3: // Rabbit paw with three toes and a hanging loop.
                b.Ball(new Vector3(0,-.08f,0),new Vector3(.19f,.31f,.14f),0);
                foreach(float x in new[]{-.14f,0,.14f})b.Ball(new Vector3(x,-.29f,-.015f),new Vector3(.085f,.13f,.105f),0);
                b.Drum(new Vector3(0,.33f,0),.10f,.07f,4,6);
                b.Box(new Vector3(0,.22f,0),new Vector3(.20f,.10f,.20f),10,10);break;
            case 4: // Literal blank cartridge.
                b.Prism(new[]{new Vector2(-.15f,-.35f),new Vector2(.15f,-.35f),new Vector2(.15f,.18f),new Vector2(0,.42f),new Vector2(-.15f,.18f)},.23f,4,Vector3.zero);
                b.Box(new Vector3(0,-.31f,0),new Vector3(.38f,.10f,.28f),11,11);break;
            case 5: // Small helper robot.
                b.Box(new Vector3(0,-.07f,0),new Vector3(.35f,.31f,.23f),11,11);
                b.Box(new Vector3(0,.24f,0),new Vector3(.42f,.25f,.27f),0,0);
                foreach(float side in new[]{-1f,1f})
                {
                    b.Box(new Vector3(side*.26f,-.06f,0),new Vector3(.12f,.25f,.12f),11,11);
                    b.Box(new Vector3(side*.11f,-.31f,0),new Vector3(.13f,.15f,.18f),1,1);
                    b.Box(new Vector3(side*.10f,.24f,-.15f),new Vector3(.06f,.06f,.04f),3,3);
                }break;
            case 6: // Red horseshoe magnet, pale tips.
                foreach(float x in new[]{-.22f,.22f}){b.Box(new Vector3(x,0,0),new Vector3(.17f,.6f,.25f),2,2);b.Box(new Vector3(x,.24f,0),new Vector3(.18f,.15f,.26f),12,12);}
                b.Box(new Vector3(0,-.25f,0),new Vector3(.55f,.17f,.25f),2,2);break;
            case 7: // Rifle with barrel, magazine, stock and wood furniture.
                b.Box(Vector3.zero,new Vector3(.58f,.17f,.18f),1,1);b.Box(new Vector3(.43f,0,0),new Vector3(.36f,.065f,.065f),11,11);
                b.Box(new Vector3(-.39f,-.03f,0),new Vector3(.28f,.18f,.14f),5,5);
                b.Box(new Vector3(.03f,-.18f,0),new Vector3(.12f,.29f,.14f),1,1);
                b.Box(new Vector3(-.20f,-.13f,0),new Vector3(.08f,.20f,.12f),5,5);break;
            case 8: // Coffee mug with visible coffee, handle and steam.
                b.Can(Vector3.zero,.29f,.55f,0,5,true);
                b.Box(new Vector3(.38f,.19f,0),new Vector3(.27f,.09f,.10f),0,0);
                b.Box(new Vector3(.49f,0,0),new Vector3(.09f,.40f,.10f),0,0);
                b.Box(new Vector3(.38f,-.19f,0),new Vector3(.27f,.09f,.10f),0,0);
                b.Box(new Vector3(-.07f,.43f,0),new Vector3(.04f,.19f,.04f),12,12);
                b.Box(new Vector3(.04f,.56f,0),new Vector3(.14f,.04f,.04f),12,12);break;
            case 9: // Protein tub, label and dark screw lid.
                b.Can(Vector3.zero,.28f,.55f,0,0);
                b.Can(new Vector3(0,.31f,0),.31f,.09f,3,3);
                b.Box(new Vector3(0,0,-.28f),new Vector3(.33f,.34f,.035f),14,14);
                b.Box(new Vector3(-.07f,0,-.31f),new Vector3(.045f,.25f,.025f),3,3);
                b.Box(new Vector3(.025f,.10f,-.31f),new Vector3(.16f,.045f,.025f),3,3);
                b.Box(new Vector3(.025f,0,-.31f),new Vector3(.16f,.045f,.025f),3,3);
                b.Box(new Vector3(.10f,.05f,-.31f),new Vector3(.045f,.12f,.025f),3,3);break;
            case 10: Moon(b,Vector3.zero,.37f);break;
            case 11: // Miniature player ship.
                b.Prism(new[]{new Vector2(-.16f,-.4f),new Vector2(.16f,-.4f),new Vector2(.22f,.04f),new Vector2(0,.5f),new Vector2(-.22f,.04f)},.16f,11,Vector3.zero);
                foreach(float side in new[]{-1f,1f}){b.Prism(new[]{new Vector2(0,.12f),new Vector2(side*.42f,-.31f),new Vector2(0,-.23f)},.10f,11,new Vector3(side*.12f,0,0));b.Box(new Vector3(side*.25f,-.20f,0),new Vector3(.13f,.45f,.18f),1,1);}
                b.Box(new Vector3(0,.10f,-.13f),new Vector3(.12f,.25f,.08f),13,13);break;
            case 12: Bolt(b,Vector3.zero,1);break;
            case 13: // Bent arm, palm and fingers.
                b.Box(new Vector3(-.15f,.19f,0),new Vector3(.42f,.17f,.18f),8,8);
                b.Box(new Vector3(.10f,-.03f,0),new Vector3(.17f,.43f,.18f),8,8);
                b.Box(new Vector3(.10f,-.30f,0),new Vector3(.27f,.17f,.14f),8,8);
                for(int i=0;i<4;i++)b.Box(new Vector3(-.0f+i*.065f,-.44f,0),new Vector3(.05f,.18f,.11f),8,8);break;
            case 14: Moon(b,Vector3.zero,.37f,true);break;
            case 15: // Bent spoon.
                b.Ball(new Vector3(0,.27f,0),new Vector3(.19f,.27f,.065f),11);
                b.Box(new Vector3(.04f,-.10f,0),new Vector3(.075f,.36f,.06f),11,11);
                b.Box(new Vector3(.12f,-.29f,0),new Vector3(.22f,.075f,.06f),11,11);
                b.Box(new Vector3(.22f,-.40f,0),new Vector3(.075f,.26f,.06f),11,11);break;
            case 16:
                Moon(b,new Vector3(-.26f,.18f,0),.22f);
                Moon(b,new Vector3(.25f,.17f,0),.22f);
                Moon(b,new Vector3(0,-.26f,0),.22f);break;
        }
        return b.Finish("Equipment - "+ItemKinds[kind]);
    }

    static void Bolt(Builder b,Vector3 center,float scale)
    {
        Vector2[] A={new Vector2(-.06f,.45f),new Vector2(.22f,.45f),new Vector2(.02f,.03f),new Vector2(-.23f,.03f)};
        Vector2[] B={new Vector2(-.23f,.08f),new Vector2(.20f,.08f),new Vector2(.13f,-.08f),new Vector2(-.30f,-.08f)};
        Vector2[] C={new Vector2(-.06f,-.05f),new Vector2(.20f,-.05f),new Vector2(-.31f,-.48f)};
        foreach(var points in new[]{A,B,C}){for(int i=0;i<points.Length;i++)points[i]*=scale;b.Prism(points,.075f*scale,4,center);}
    }
}
