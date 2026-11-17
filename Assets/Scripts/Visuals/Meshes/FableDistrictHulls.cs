using UnityEngine;

// Original sculptural silhouettes for the Mushroom and Dream districts. The front is -Z;
// the firing direction is -Y. Palette indices are supplied by the standard hull renderer.
public static partial class IndustrialMeshFactory
{
    static Mesh fablePainter, fableHand, fableBell, fableCap, fableLancer;
    static Mesh fableKite, fableCrescent, fableMask, fableMushChampion, fableDreamChampion;

    static void FablePort(Builder b, float x, float y, float radius, int colour)
    {
        b.Drum(new Vector3(x,y,-.30f), radius, .16f, colour, 10, 11);
        b.Drum(new Vector3(x,y,-.40f), radius*.64f, .045f, 11, 10, colour);
    }
    static void FableVein(Builder b, Vector3[] points, float width, int colour)
    { CutBlade(b, points, width, .055f, colour); }

    public static Mesh SporeBellMesh()
    {
        if (fableBell != null) return fableBell;
        var b = new Builder();
        // A pleated inkcap umbrella over a pendant spore sac. The rolled front lip
        // reveals pale gills from above and leaves the drop valve visible.
        b.Pearl(new Vector3(0,-.25f,-.05f),new Vector3(.34f,.46f,.25f),7,16,9);
        b.Surface(48,8,(u,t)=>{
            float a=-u*Mathf.PI*2, r=.024f+.976f*t;
            float scallop=1-.075f*r*Mathf.Cos(a*12);
            return new Vector3(.80f*r*scallop*Mathf.Cos(a),.28f+.60f*r*scallop*Mathf.Sin(a),
                -.13f-.53f*(1-r*r)+.052f*r*Mathf.Cos(12*a));
        },7);
        for(int i=0;i<12;i++)
        {
            float a=(i+.5f)*Mathf.PI/6, dx=Mathf.Cos(a), dy=Mathf.Sin(a);
            CutBlade(b,new[]{new Vector3(dx*.10f,.28f+dy*.08f,-.65f),new Vector3(dx*.51f,.28f+dy*.38f,-.45f),
                new Vector3(dx*.83f,.28f+dy*.62f,-.16f)},.042f,.035f,i%3==0?15:4);
            if(dy<0) b.Sweep(new Vector3(dx*.24f,-.17f,-.13f),new Vector3(dx*.49f,-.38f,-.21f),
                new Vector3(dx*.75f,.23f+dy*.60f,-.19f),new Vector3(dx*.82f,.28f+dy*.60f,-.15f),.046f,.05f,15,false,6);
        }
        b.Pearl(new Vector3(0,.28f,-.62f),new Vector3(.17f,.16f,.09f),7,12,6);
        DeepPort(b,new Vector3(0,-.59f,-.09f),.18f,7);
        for(int i=0;i<7;i++)
        {
            float angle=(i+.34f)*Mathf.PI*2/7;
            b.Pearl(new Vector3(.49f*Mathf.Cos(angle),.28f+.37f*Mathf.Sin(angle),-.45f),
                new Vector3(.061f,.060f,.048f),i%3==0?13:14,8,4);
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
        b.Pearl(new Vector3(0,.15f,-.08f),new Vector3(.83f,.57f,.32f),5,24,10);
        for(int s=-1;s<=1;s+=2)
        {
            b.Pearl(new Vector3(s*.78f,.09f,-.01f),new Vector3(.17f,.31f,.15f),13,12,6);
            b.Pearl(new Vector3(s*.45f,.28f,-.31f),new Vector3(.14f,.13f,.071f),15,12,6);
            b.Pearl(new Vector3(s*.26f,-.09f,-.32f),new Vector3(.10f,.075f,.055f),15,10,5);
            CutBlade(b,new[]{new Vector3(s*.13f,-.54f,-.16f),new Vector3(s*.40f,-.37f,-.20f),
                new Vector3(s*.77f,-.06f,-.14f)},.086f,.06f,1);
        }
        b.Pearl(new Vector3(-.07f,.43f,-.33f),new Vector3(.125f,.085f,.055f),15,10,5);
        MothBarrel(b,new Vector3(0,-.72f,-.24f),.11f,.30f);
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
            b.Sweep(A(.10f,-.43f,-.06f),A(.22f,-.07f,-.24f),A(.65f,.08f,-.23f),A(.77f,.76f,-.08f),.14f,.11f,15,false,10);
            b.Sweep(A(.40f,.03f,-.14f),A(.43f,.21f,-.24f),A(.35f,.35f,-.17f),A(.40f,.60f,-.11f),.065f,.066f,14,false,7);
            b.Sweep(A(.63f,.34f,-.14f),A(.74f,.40f,-.18f),A(.86f,.38f,-.13f),A(.96f,.52f,-.10f),.053f,.062f,13,false,6);
            b.Pearl(A(.77f,.73f,-.09f),new Vector3(.125f,.19f,.10f),13,12,7);
            b.Pearl(A(.40f,.60f,-.12f),new Vector3(.075f,.12f,.08f),7,10,6);
        }
        CutStone(b,new Vector3(0,.04f,-.19f),new Vector3(.14f,.31f,.12f),8,8);
        MothBarrel(b,new Vector3(0,-.86f,-.24f),.105f,.51f);
        for(int s=-1;s<=1;s+=2)
        {
            Vector3 A(float x,float y,float z)=>new Vector3(s*x,y,z);
            FoldedFin(b,A(.07f,-.44f,-.11f),A(.42f,.07f,-.24f),A(.82f,.63f,-.09f),
                A(.20f,.29f,-.20f),A(.31f,.04f,-.31f),8,2);
            for(int i=0;i<3;i++)
                CutBlade(b,new[]{A(.15f,-.24f+i*.10f,-.26f),A(.28f+i*.10f,.04f+i*.14f,-.32f),
                    A(.36f+i*.15f,.19f+i*.16f,-.24f)},.028f,.024f,13);
        }
        CutStone(b,new Vector3(0,-.52f,-.21f),new Vector3(.14f,.17f,.09f),13,6);
        return fableLancer = b.Finish("MyceliumLancer");
    }
    public static Mesh DreamPainterMesh()
    {
        if(fablePainter!=null)return fablePainter;var b=new Builder();
        // Projecting, slanted plates form an asymmetric sculptural portrait.
        BevelArmor(b,new[]{V(-1.45f,-.31f),V(-1.30f,.63f),V(-.94f,.52f),V(-1.03f,1.08f),V(-.53f,1.22f),V(.14f,1.07f),V(.58f,1.34f),V(1.03f,.98f),V(.98f,.65f),V(1.40f,.45f),V(1.30f,-.22f),V(1.14f,-.28f),V(1.12f,-.78f),V(.45f,-1.08f),V(.15f,-1.29f),V(-.62f,-.94f),V(-.92f,-1.02f)},.42f,15);
        b.Prism(new[]{V(-1.17f,.64f),V(-.35f,1.07f),V(-.08f,.12f),V(-.89f,-.15f)},.10f,8,new Vector3(0,0,-.27f),8);
        b.Prism(new[]{V(.02f,.93f),V(.91f,.83f),V(1.22f,.04f),V(.43f,-.26f)},.11f,5,new Vector3(0,0,-.30f),5);
        b.Prism(new[]{V(-.65f,-.24f),V(.30f,-.08f),V(.91f,-.65f),V(.07f,-1.05f),V(-.74f,-.71f)},.12f,13,new Vector3(0,0,-.32f),13);
        BevelArmor(b,new[]{V(-1.04f,.56f),V(-.38f,.70f),V(-.47f,.22f),V(-.93f,.12f)},.11f,15,-.43f);
        BevelArmor(b,new[]{V(.30f,.61f),V(.92f,.43f),V(.87f,.14f),V(.27f,.25f)},.11f,15,-.44f);
        BevelArmor(b,new[]{V(-.25f,.65f),V(.02f,.84f),V(.24f,-.18f),V(-.06f,-.46f),V(-.27f,-.31f)},.16f,15,-.47f);
        CutStone(b,new Vector3(-.59f,.40f,-.54f),new Vector3(.13f,.20f,.06f),11,6);
        CutStone(b,new Vector3(.55f,.34f,-.55f),new Vector3(.20f,.09f,.06f),11,6);


        FableVein(b,new[]{new Vector3(-.35f,-.56f,-.45f),new Vector3(.08f,-.44f,-.47f),new Vector3(.53f,-.65f,-.45f)},.055f,11);
        FablePort(b,-.89f,-.60f,.17f,8);FablePort(b,.91f,-.52f,.17f,5);
        return fablePainter=b.Finish("Dream painter / fractured portrait");
    }
    public static Mesh DreamHandMesh()
    {
        if(fableHand!=null)return fableHand;var b=new Builder();
        BevelArmor(b,new[]{V(-.70f,.68f),V(.68f,.68f),V(.79f,.03f),V(.62f,-.38f),V(-.62f,-.38f),V(-.79f,.03f)},.30f,15);
        // Cuff, articulated hinge and offset palm plates frame the four pointed digits.
        BevelArmor(b,new[]{V(-.48f,.80f),V(.35f,.88f),V(.43f,.62f),V(-.48f,.53f)},.17f,5,-.24f);
        BevelArmor(b,new[]{V(-.53f,.40f),V(-.12f,.48f),V(.08f,.02f),V(-.42f,-.19f),V(-.62f,-.04f)},.12f,8,-.25f);
        BevelArmor(b,new[]{V(-.08f,.46f),V(.48f,.50f),V(.56f,.03f),V(.28f,-.21f),V(.05f,.02f)},.13f,13,-.27f);
        CutStone(b,new Vector3(-.03f,.58f,-.37f),new Vector3(.22f,.13f,.08f),11,6);
        // Four distinct tapered digits, no hidden thumb. Tips: (-.72,-1.08), (-.25,-1.42), (.25,-1.49), (.72,-1.12).
        float[] xs={-.72f,-.25f,.25f,.72f};float[] ys={-1.08f,-1.42f,-1.49f,-1.12f};
        for(int i=0;i<4;i++)
        {
            float x=xs[i], inner=x*.72f;
            BevelArmor(b,new[]{V(inner-.16f,-.23f),V(inner+.16f,-.23f),V(x+.12f,ys[i]+.26f),V(x,ys[i]),V(x-.12f,ys[i]+.26f)},.22f,15);
            CutStone(b,new Vector3(inner,-.34f,-.23f),new Vector3(.14f,.11f,.07f),i%2==0?5:8,6);
            FableVein(b,new[]{new Vector3(inner,-.35f,-.22f),new Vector3(x,ys[i]+.28f,-.19f)},.035f,8);
        }
        FableVein(b,new[]{new Vector3(-.42f,.30f,-.23f),new Vector3(-.12f,.02f,-.24f),new Vector3(.22f,.02f,-.24f),new Vector3(.44f,.31f,-.23f)},.045f,5);
        CutStone(b,new Vector3(0,.27f,-.26f),new Vector3(.22f,.17f,.06f),13,5);
        return fableHand=b.Finish("Dream hand / four tapered fingers");
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
        const int steps=26;
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
        b.Sweep(new Vector3(.01f,.14f,-.20f),new Vector3(-.22f,.26f,-.22f),new Vector3(-.53f,.17f,-.14f),Arc(0,.90f,-.12f),.066f,.05f,15,false,8);
        b.Sweep(new Vector3(.14f,.14f,-.2f),new Vector3(.32f,-.06f,-.19f),new Vector3(.49f,.13f,-.18f),Arc(1,.90f,-.12f),.06f,.05f,15,false,8);
        CutBlade(b,new[]{new Vector3(-.08f,-.30f,-.18f),new Vector3(.03f,-.51f,-.22f),new Vector3(.13f,-.74f,-.12f)},.10f,.06f,15);
        for(int s=-1;s<=1;s+=2) DeepPort(b,new Vector3(s*.50f,-.53f,-.05f),.137f,13);
        CutBlade(b,new[]{new Vector3(-.09f,.45f,-.24f),new Vector3(.10f,.55f,-.31f),
            new Vector3(.27f,.43f,-.24f)},.095f,.059f,5);
        b.Pearl(new Vector3(.045f,.37f,-.34f),new Vector3(.035f,.044f,.024f),11,8,4);
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
            new Vector3(.20f,-.45f,-.35f),new Vector3(.43f,-.29f,-.20f),.065f,.055f,5,false,8);
        DeepPort(b,new Vector3(.12f,-.77f,-.07f),.105f,13);
        return fableMask = b.Finish("PaintedMask");
    }
    public static Mesh MushroomChampionMesh()
    {
        if(fableMushChampion!=null)return fableMushChampion;var b=new Builder();
        BevelArmor(b,new[]{V(-1.25f,.05f),V(-1.02f,.63f),V(-.53f,.91f),V(0,1.02f),V(.53f,.91f),V(1.02f,.63f),V(1.25f,.05f),V(.72f,.13f),V(.48f,-.14f),V(0,-.05f),V(-.48f,-.14f),V(-.72f,.13f)},.34f,8);
        BevelArmor(b,new[]{V(-.44f,.12f),V(-.52f,-.53f),V(0,-1.03f),V(.52f,-.53f),V(.44f,.12f)},.31f,15);
        foreach(int s in new[]{-1,1}) { CutStone(b,new Vector3(s*.60f,.49f,-.26f),new Vector3(.25f,.14f,.08f),15,6); FablePort(b,s*.48f,-.47f,.15f,8); }
        CutStone(b,new Vector3(0,-.30f,-.31f),new Vector3(.23f,.28f,.10f),5,6);
        return fableMushChampion=b.Finish("Mushroom champion / twin spore ports");
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
