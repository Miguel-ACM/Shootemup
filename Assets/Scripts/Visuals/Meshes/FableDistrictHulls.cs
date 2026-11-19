using UnityEngine;

// Original sculptural silhouettes for the Mushroom and Dream districts. The front is -Z;
// the firing direction is -Y. Palette indices are supplied by the standard hull renderer.
public static partial class IndustrialMeshFactory
{
    static Mesh fableBell, fableCap, fableLancer;
    static Mesh fableKite, fableCrescent, fableMask, fableDreamChampion;

    static void FablePort(Builder b, float x, float y, float radius, int colour)
    {
        b.Drum(new Vector3(x,y,-.30f), radius, .16f, colour, 10, 11);
        b.Drum(new Vector3(x,y,-.40f), radius*.64f, .045f, 11, 10, colour);
    }
    static void FableVein(Builder b, Vector3[] points, float width, int colour)
    { CutBlade(b, points, width, .055f, colour); }

    // Shallow scales sit in the cap. Only their exposed crown needs geometry.
    static void CapSpot(Builder b, Vector3 center, Vector3 size, int tile)
    {
        const int sides = 5;
        Vector3 Ring(int i, float radius, float z)
        {
            float angle = -i * 2 * Mathf.PI / sides;
            return center + new Vector3(Mathf.Cos(angle) * size.x * radius,
                Mathf.Sin(angle) * size.y * radius, z * size.z);
        }

        var crown = new Vector3[sides];
        for (int i = 0; i < sides; i++)
            crown[i] = Ring(i, .73f, -1);
        b.Face(tile, crown);
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            b.Face(tile, crown[i], Ring(i, 1, -.2f), Ring(next, 1, -.2f), crown[next]);
        }
    }

    public static Mesh SporeBellMesh()
    {
        if (fableBell != null) return fableBell;
        var b = new Builder();
        // A pleated inkcap umbrella over a pendant spore sac. The rolled front lip
        // reveals pale gills from above and leaves the drop valve visible.
        CutStone(b,new Vector3(0,-.25f,-.05f),new Vector3(.34f,.46f,.25f),7,8);
        b.Surface(24,2,(u,t)=>{
            float a=-u*Mathf.PI*2, r=.024f+.976f*t;
            float scallop=1-.075f*r*Mathf.Cos(a*12);
            return new Vector3(.80f*r*scallop*Mathf.Cos(a),.28f+.60f*r*scallop*Mathf.Sin(a),
                -.13f-.53f*(1-r*r)+.052f*r*Mathf.Cos(12*a));
        },7);
        for(int i=0;i<12;i++)
        {
            float a=(i+.5f)*Mathf.PI/6, dx=Mathf.Cos(a), dy=Mathf.Sin(a);
            Vector3 root=new Vector3(dx*.10f,.28f+dy*.08f,-.65f);
            Vector3 ridge=new Vector3(dx*.51f,.28f+dy*.38f,-.48f);
            Vector3 tip=new Vector3(dx*.83f,.28f+dy*.62f,-.16f);
            Vector3 width=new Vector3(-dy,dx,0)*.035f;
            b.Face(i%3==0?15:4,root,ridge+width,tip);
            b.Face(i%3==0?15:4,root,tip,ridge-width);
            if(dy<0) b.Sweep(new Vector3(dx*.24f,-.17f,-.13f),new Vector3(dx*.49f,-.38f,-.21f),
                new Vector3(dx*.75f,.23f+dy*.60f,-.19f),new Vector3(dx*.82f,.28f+dy*.60f,-.15f),.046f,.05f,15,false,2,3);
        }
        CapSpot(b,new Vector3(0,.28f,-.62f),new Vector3(.17f,.16f,.09f),7);
        FieldPort(b,new Vector3(0,-.59f,-.09f),.18f,7);
        for(int i=0;i<7;i++)
        {
            float angle=(i+.34f)*Mathf.PI*2/7;
            CapSpot(b,new Vector3(.49f*Mathf.Cos(angle),.28f+.37f*Mathf.Sin(angle),-.45f),
                new Vector3(.061f,.060f,.048f),i%3==0?13:14);
        }
        return fableBell = b.Finish("SporeBell");
    }
    public static Mesh CapRicochetMesh()
    {
        if (fableCap != null) return fableCap;
        var b = new Builder();
        // A red fly-agaric bonnet with two lateral impact pads and a tucked-under launch shoe.
        KeelSkin(b,0,15,Frame(-.78f,.06f,.08f),Frame(-.53f,.29f,.20f),
            Frame(.11f,.32f,.24f),Frame(.49f,.13f,.12f));
        b.Pearl(new Vector3(0,.15f,-.08f),new Vector3(.83f,.57f,.32f),5,12,4);
        for(int s=-1;s<=1;s+=2)
        {
            CutStone(b,new Vector3(s*.78f,.09f,-.01f),new Vector3(.17f,.31f,.15f),13,6);
            CapSpot(b,new Vector3(s*.45f,.28f,-.31f),new Vector3(.14f,.13f,.071f),15);
            CapSpot(b,new Vector3(s*.26f,-.09f,-.32f),new Vector3(.10f,.075f,.055f),15);
            CutBlade(b,new[]{new Vector3(s*.13f,-.54f,-.16f),new Vector3(s*.40f,-.37f,-.20f),
                new Vector3(s*.77f,-.06f,-.14f)},.086f,.06f,1);
        }
        CapSpot(b,new Vector3(-.07f,.43f,-.33f),new Vector3(.125f,.085f,.055f),15);
        FieldBarrel(b,new Vector3(0,-.72f,-.24f),.11f,.30f);
        return fableCap = b.Finish("CapRicochet");
    }
    public static Mesh MyceliumLancerMesh()
    {
        if (fableLancer != null) return fableLancer;
        var b = new Builder();
        // A long ivory spine forks into branching antlers and amber lanterns.
        KeelSkin(b,0,14,Frame(-.97f,.04f,.06f),Frame(-.50f,.14f,.13f),Frame(-.12f,.25f,.20f),
            Frame(.32f,.20f,.17f),Frame(.71f,.07f,.09f));
        for(int s=-1;s<=1;s+=2)
        {
            Vector3 A(float x,float y,float z)=>new Vector3(s*x,y,z);
            b.Sweep(A(.10f,-.43f,-.06f),A(.22f,-.07f,-.24f),A(.65f,.08f,-.23f),A(.77f,.76f,-.08f),.14f,.11f,15,false,4,4);
            b.Sweep(A(.40f,.03f,-.14f),A(.43f,.21f,-.24f),A(.35f,.35f,-.17f),A(.40f,.60f,-.11f),.065f,.066f,14,false,3,3);
            b.Sweep(A(.63f,.34f,-.14f),A(.74f,.40f,-.18f),A(.86f,.38f,-.13f),A(.96f,.52f,-.10f),.053f,.062f,13,false,2,3);
            CutStone(b,A(.77f,.73f,-.09f),new Vector3(.125f,.19f,.10f),13,6);
            CutStone(b,A(.40f,.60f,-.12f),new Vector3(.075f,.12f,.08f),7,6);
        }
        CapSpot(b,new Vector3(0,.04f,-.19f),new Vector3(.14f,.31f,.12f),8);
        FieldBarrel(b,new Vector3(0,-.86f,-.24f),.105f,.51f);
        for(int s=-1;s<=1;s+=2)
        {
            Vector3 A(float x,float y,float z)=>new Vector3(s*x,y,z);
            FoldedFin(b,A(.07f,-.44f,-.11f),A(.42f,.07f,-.24f),A(.82f,.63f,-.09f),
                A(.20f,.29f,-.20f),A(.31f,.04f,-.31f),8,2);
            for(int i=0;i<3;i++)
                CutBlade(b,new[]{A(.15f,-.24f+i*.10f,-.26f),A(.28f+i*.10f,.04f+i*.14f,-.32f),
                    A(.36f+i*.15f,.19f+i*.16f,-.24f)},.028f,.024f,13);
        }
        CapSpot(b,new Vector3(0,-.52f,-.21f),new Vector3(.14f,.17f,.09f),13);
        return fableLancer = b.Finish("MyceliumLancer");
    }
    public static Mesh PaperKiteMesh()
    {
        if (fableKite != null) return fableKite;
        var b = new Builder();
        // An origami crane. Broad paper valleys lead into a crooked neck and long beak.
        for(int s=-1;s<=1;s+=2)
        {
            Vector3 A(float x,float y,float z)=>new Vector3(s*x,y,z);
            FoldedFin(b,A(.06f,-.32f,-.08f),A(.42f,.27f,-.34f),A(1.07f,.83f,-.02f),
                A(.77f,-.08f,.07f),A(.54f,.15f,-.29f),15,4);
            FoldedFin(b,A(.12f,-.25f,-.09f),A(.79f,-.09f,.06f),A(.57f,-.40f,-.04f),
                A(.24f,-.48f,-.09f),A(.37f,-.26f,-.21f),13,1);
        }
        FoldedFin(b,new Vector3(-.23f,-.22f,-.14f),new Vector3(-.26f,.31f,-.10f),new Vector3(.13f,.98f,-.17f),
            new Vector3(.22f,.01f,-.14f),new Vector3(0,.13f,-.39f),15,7);
        CutBlade(b,new[]{new Vector3(0,.11f,-.20f),new Vector3(-.22f,-.41f,-.37f),new Vector3(-.13f,-.65f,-.43f),new Vector3(0,-.85f,-.22f)},.14f,.09f,15);
        b.Prism(new[]{V(-.19f,-.59f),V(.02f,-.62f),V(0,-.85f)},.075f,13,new Vector3(0,0,-.24f));
        b.Drum(new Vector3(-.13f,-.55f,-.46f),.050f,.032f,11,6);
        return fableKite = b.Finish("PaperKite");
    }
    public static Mesh CrescentDancerMesh()
    {
        if (fableCrescent != null) return fableCrescent;
        var b = new Builder();
        // A tiny marionette balanced inside a pierced crescent. A pair of bells marks its emitters.
        const int steps=14;
        Vector3 Arc(float t,float r,float z)
        {
            float a=Mathf.Lerp(151,394,t)*Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r+.11f,z);
        }
        for(int i=0;i<steps;i++)
        {
            float a=i/(float)steps,d=(i+1f)/steps;
            float Inner(float t)=>.89f-.22f*Mathf.Sin(Mathf.PI*t);
            b.Face(13,Arc(a,.91f,-.12f),Arc(a,Inner(a),-.13f),Arc(d,Inner(d),-.13f),Arc(d,.91f,-.12f));
            b.Face(6,Arc(a,.91f,0),Arc(a,.91f,-.12f),Arc(d,.91f,-.12f),Arc(d,.91f,0));
        }
        CutStone(b,new Vector3(.10f,.39f,-.24f),new Vector3(.16f,.20f,.12f),15,8);
        FoldedFin(b,new Vector3(.03f,.23f,-.14f),new Vector3(.27f,-.18f,-.18f),new Vector3(-.24f,-.45f,-.12f),
            new Vector3(-.34f,-.17f,-.10f),new Vector3(-.07f,-.12f,-.39f),7,15);
        b.Sweep(new Vector3(.01f,.14f,-.20f),new Vector3(-.22f,.26f,-.22f),new Vector3(-.53f,.17f,-.14f),Arc(0,.90f,-.12f),.066f,.05f,15,false,4,4);
        b.Sweep(new Vector3(.14f,.14f,-.2f),new Vector3(.32f,-.06f,-.19f),new Vector3(.49f,.13f,-.18f),Arc(1,.90f,-.12f),.06f,.05f,15,false,4,4);
        CutBlade(b,new[]{new Vector3(-.08f,-.30f,-.18f),new Vector3(.03f,-.51f,-.22f),new Vector3(.13f,-.74f,-.12f)},.10f,.06f,15);
        for(int s=-1;s<=1;s+=2) FieldPort(b,new Vector3(s*.50f,-.53f,-.05f),.137f,13);
        CutBlade(b,new[]{new Vector3(-.09f,.45f,-.24f),new Vector3(.10f,.55f,-.31f),
            new Vector3(.27f,.43f,-.24f)},.095f,.059f,5);
        CutStone(b,new Vector3(.045f,.37f,-.34f),new Vector3(.035f,.044f,.024f),11,6);
        for(int i=-1;i<=1;i++)
            CutBlade(b,new[]{new Vector3(-.04f,.03f,-.27f),new Vector3(i*.11f-.06f,-.15f,-.35f),
                new Vector3(i*.17f-.12f,-.34f,-.18f)},.046f,.025f,i==0?13:1);
        return fableCrescent = b.Finish("CrescentDancer");
    }
    public static Mesh PaintedMaskMesh()
    {
        if (fableMask != null) return fableMask;
        var b = new Builder();
        // Feathered theatre half-mask. One eye laughs and one weeps over a twisted grin.
        BevelArmor(b,new[]{V(-.84f,.27f),V(-.75f,.70f),V(-.14f,.57f),V(.57f,.75f),V(.81f,.42f),
            V(.64f,-.28f),V(.17f,-.80f),V(-.25f,-.65f),V(-.67f,-.29f)},.36f,15);
        FoldedFin(b,new Vector3(-.73f,.49f,-.19f),new Vector3(-.08f,.50f,-.17f),new Vector3(.05f,-.34f,-.39f),
            new Vector3(-.67f,-.16f,-.18f),new Vector3(-.43f,.10f,-.39f),7,4);
        FoldedFin(b,new Vector3(.01f,.48f,-.18f),new Vector3(.72f,.49f,-.17f),new Vector3(.62f,-.13f,-.18f),
            new Vector3(.10f,-.29f,-.36f),new Vector3(.36f,.16f,-.38f),13,15);
        for(int s=-1;s<=1;s+=2)
        {
            float y=s<0?.20f:.11f;
            CutStone(b,new Vector3(s*.38f,y,-.40f),new Vector3(.22f,s<0?.10f:.14f,.05f),11,4,s*.12f);
            CutStone(b,new Vector3(s*.36f,y,-.45f),new Vector3(.071f,.062f,.035f),10,6);
        }
        CutBlade(b,new[]{new Vector3(-.09f,.38f,-.24f),new Vector3(.03f,-.03f,-.42f),new Vector3(-.10f,-.21f,-.54f)},.12f,.09f,15);
        for(int k=0;k<3;k++) Feather(b,new Vector3(-.55f,.43f,-.12f),new Vector3(-1.06f+k*.28f,.84f+k*.13f,-.02f),.14f,.08f,k%2==0?5:13);
        b.Sweep(new Vector3(-.38f,-.30f,-.22f),new Vector3(-.12f,-.61f,-.38f),
            new Vector3(.20f,-.45f,-.35f),new Vector3(.43f,-.29f,-.20f),.065f,.055f,5,false,4,4);
        FieldPort(b,new Vector3(.12f,-.77f,-.07f),.105f,13);
        return fableMask = b.Finish("PaintedMask");
    }
    public static Mesh DreamChampionMesh()
    {
        if(fableDreamChampion!=null)return fableDreamChampion;var b=new Builder();
        BevelArmor(b,new[]{V(-1.12f,.30f),V(-.78f,.83f),V(-.10f,.98f),V(.44f,.73f),V(1.16f,.34f),V(.91f,-.47f),V(.27f,-.95f),V(-.46f,-.70f),V(-1.03f,-.51f)},.28f,15);
        b.Prism(new[]{V(-.88f,.28f),V(-.48f,.71f),V(.05f,.73f),V(-.07f,-.55f),V(-.68f,-.43f)},.08f,5,new Vector3(0,0,-.22f),5);
        CutStone(b,new Vector3(-.43f,.16f,-.31f),new Vector3(.21f,.29f,.07f),11,6);
        CutStone(b,new Vector3(.43f,.21f,-.31f),new Vector3(.27f,.13f,.07f),11,6);
        FableVein(b,new[]{new Vector3(-.32f,-.40f,-.33f),new Vector3(.02f,-.26f,-.33f),new Vector3(.45f,-.46f,-.33f)},.055f,13);
        FablePort(b,-.76f,-.45f,.13f,5);FablePort(b,.76f,-.45f,.13f,8);
        return fableDreamChampion=b.Finish("Dream champion / fractured mask");
    }
}
