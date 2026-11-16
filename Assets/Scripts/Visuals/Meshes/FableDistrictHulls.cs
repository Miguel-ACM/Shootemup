using UnityEngine;

// Original sculptural silhouettes for the Mushroom and Dream districts. The front is -Z;
// the firing direction is -Y. Palette indices are supplied by the standard hull renderer.
public static partial class IndustrialMeshFactory
{
    static Mesh fableWarden, fablePainter, fableHand, fableBell, fableCap, fableLancer;
    static Mesh fableKite, fableCrescent, fableMask, fableMushChampion, fableDreamChampion;

    static void FablePort(Builder b, float x, float y, float radius, int colour)
    {
        b.Drum(new Vector3(x,y,-.30f), radius, .16f, colour, 10, 11);
        b.Drum(new Vector3(x,y,-.40f), radius*.64f, .045f, 11, 10, colour);
    }
    static void FableVein(Builder b, Vector3[] points, float width, int colour)
    { CutBlade(b, points, width, .055f, colour); }

    public static Mesh MushroomWardenMesh()
    {
        if (fableWarden != null) return fableWarden;
        var b = new Builder();
        // A broad, shouldered cap with an irregular scalloped underside and a hanging stem.
        BevelArmor(b, new[]{V(-1.9f,.20f),V(-1.70f,.70f),V(-1.15f,1.12f),V(-.50f,1.35f),V(.20f,1.39f),V(.90f,1.20f),V(1.50f,.85f),V(1.87f,.28f),V(1.58f,.02f),V(1.20f,.15f),V(.85f,-.06f),V(.43f,.08f),V(0,-.13f),V(-.43f,.08f),V(-.87f,-.06f),V(-1.25f,.14f),V(-1.60f,-.02f)}, .38f, 5);
        BevelArmor(b, new[]{V(-.53f,.15f),V(-.67f,-.41f),V(-.43f,-1.08f),V(0,-1.34f),V(.43f,-1.08f),V(.67f,-.41f),V(.53f,.15f)}, .40f, 15);
        BevelArmor(b,new[]{V(-1.48f,.30f),V(-1.05f,.84f),V(-.51f,1.14f),V(0,1.27f),V(.51f,1.14f),V(1.05f,.84f),V(1.48f,.30f),V(.92f,.16f),V(.43f,.06f),V(0,-.06f),V(-.43f,.06f),V(-.92f,.16f)},.12f,5,-.29f);
        for (int i=-3;i<=3;i++)
        {
            float x=i*.42f;
            FableVein(b,new[]{new Vector3(x*.42f,.30f,-.29f),new Vector3(x,.02f,-.28f),new Vector3(x*1.18f,-.02f,-.25f)},.052f,3);
        }
        BevelArmor(b,new[]{V(-.34f,-.32f),V(0,-.20f),V(.34f,-.32f),V(.30f,-.80f),V(0,-1.01f),V(-.30f,-.80f)},.15f,13,-.30f);

        foreach (int s in new[]{-1,1})
        {
            CutStone(b,new Vector3(s*.91f,.67f,-.29f),new Vector3(.37f,.21f,.10f),15,6);
            CutStone(b,new Vector3(s*.43f,1.04f,-.30f),new Vector3(.23f,.14f,.08f),15,6);
            BevelArmor(b,new[]{V(s*1.26f,.30f),V(s*1.70f,.18f),V(s*1.91f,-.25f),V(s*1.70f,-.56f),V(s*1.50f,-.38f),V(s*1.54f,-.13f),V(s*1.20f,-.06f)},.25f,13,-.06f);
            CutStone(b,new Vector3(s*1.57f,-.12f,-.25f),new Vector3(.18f,.19f,.12f),11,6);
            FableVein(b,new[]{new Vector3(s*.12f,.58f,-.42f),new Vector3(s*.69f,.66f,-.42f),new Vector3(s*1.28f,.38f,-.38f)},.065f,13);
            FablePort(b,s*.90f,-.02f,.18f,5);
        }
        FablePort(b,0,-1.13f,.19f,8);
        return fableWarden=b.Finish("Fungal warden / scalloped cap and gills");
    }
    public static Mesh SporeBellMesh()
    {
        if(fableBell!=null)return fableBell; var b=new Builder();
        BevelArmor(b,new[]{V(-.76f,.12f),V(-.62f,.55f),V(-.31f,.79f),V(0,.86f),V(.31f,.79f),V(.62f,.55f),V(.76f,.12f),V(.48f,-.02f),V(.25f,.07f),V(0,-.10f),V(-.25f,.07f),V(-.48f,-.02f)},.28f,8);
        CutStone(b,new Vector3(0,-.26f,-.13f),new Vector3(.32f,.40f,.22f),15,8);
        FablePort(b,0,-.59f,.18f,8);
        foreach(int s in new[]{-1,1}) CutStone(b,new Vector3(s*.37f,.45f,-.22f),new Vector3(.16f,.11f,.05f),15,6);
        return fableBell=b.Finish("Spore bell / domed dropper");
    }
    public static Mesh CapRicochetMesh()
    {
        if(fableCap!=null)return fableCap;var b=new Builder();
        BevelArmor(b,new[]{V(0,-.91f),V(.50f,-.56f),V(.88f,.02f),V(.71f,.47f),V(.30f,.66f),V(0,.72f),V(-.30f,.66f),V(-.71f,.47f),V(-.88f,.02f),V(-.50f,-.56f)},.30f,8);
        CutHalo(b,new Vector3(0,.05f,-.23f),.55f,.08f,new Vector3(1,.72f,1),Quaternion.identity,15,12);
        CutStone(b,new Vector3(0,.04f,-.28f),new Vector3(.25f,.25f,.11f),5,7);
        FablePort(b,0,-.72f,.14f,8);
        return fableCap=b.Finish("Ricochet cap / armoured saucer");
    }
    public static Mesh MyceliumLancerMesh()
    {
        if(fableLancer!=null)return fableLancer;var b=new Builder();
        BevelArmor(b,new[]{V(0,-1.05f),V(.19f,-.46f),V(.78f,.18f),V(.83f,.61f),V(.28f,.38f),V(0,.73f),V(-.28f,.38f),V(-.83f,.61f),V(-.78f,.18f),V(-.19f,-.46f)},.24f,15);
        FableVein(b,new[]{new Vector3(0,-.78f,-.19f),new Vector3(0,.35f,-.19f)},.07f,8);
        foreach(int s in new[]{-1,1}) FableVein(b,new[]{new Vector3(s*.10f,-.20f,-.19f),new Vector3(s*.40f,.18f,-.19f),new Vector3(s*.71f,.43f,-.17f)},.045f,8);
        CutStone(b,new Vector3(0,.20f,-.24f),new Vector3(.19f,.28f,.12f),5,6);
        FablePort(b,0,-.86f,.10f,8);
        return fableLancer=b.Finish("Mycelium lancer / branching spear");
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
        if(fableKite!=null)return fableKite;var b=new Builder();
        BevelArmor(b,new[]{V(0,-1.05f),V(.82f,.16f),V(0,.83f),V(-.82f,.16f)},.12f,15);
        b.Prism(new[]{V(0,-.91f),V(.72f,.16f),V(0,.68f)},.055f,5,new Vector3(0,0,-.10f),5);
        FableVein(b,new[]{new Vector3(0,-.92f,-.17f),new Vector3(0,.73f,-.17f)},.032f,11);
        FableVein(b,new[]{new Vector3(-.68f,.16f,-.16f),new Vector3(0,.05f,-.17f),new Vector3(.68f,.16f,-.16f)},.03f,11);
        FablePort(b,0,-.85f,.10f,13);
        return fableKite=b.Finish("Paper kite / folded diamond");
    }
    public static Mesh CrescentDancerMesh()
    {
        if(fableCrescent!=null)return fableCrescent;var b=new Builder();
        BevelArmor(b,new[]{V(-.88f,.70f),V(-.67f,.04f),V(-.35f,-.38f),V(0,-.52f),V(.35f,-.38f),V(.67f,.04f),V(.88f,.70f),V(.78f,-.13f),V(.48f,-.75f),V(0,-.98f),V(-.48f,-.75f),V(-.78f,-.13f)},.20f,5);
        CutStone(b,new Vector3(0,-.61f,-.22f),new Vector3(.22f,.19f,.09f),13,6);
        foreach(int s in new[]{-1,1}) FablePort(b,s*.50f,-.53f,.11f,8);
        return fableCrescent=b.Finish("Crescent dancer / swept horns");
    }
    public static Mesh PaintedMaskMesh()
    {
        if(fableMask!=null)return fableMask;var b=new Builder();
        BevelArmor(b,new[]{V(-.78f,.56f),V(-.30f,.80f),V(.38f,.73f),V(.81f,.44f),V(.66f,-.47f),V(.12f,-.91f),V(-.53f,-.64f)},.25f,15);
        b.Prism(new[]{V(-.65f,.49f),V(-.05f,.65f),V(-.02f,-.47f),V(-.46f,-.53f)},.06f,8,new Vector3(0,0,-.20f),8);
        CutStone(b,new Vector3(-.34f,.18f,-.27f),new Vector3(.19f,.25f,.06f),11,6);
        CutStone(b,new Vector3(.35f,.18f,-.26f),new Vector3(.24f,.12f,.06f),11,6);
        FableVein(b,new[]{new Vector3(-.22f,-.43f,-.28f),new Vector3(.12f,-.30f,-.28f),new Vector3(.40f,-.45f,-.28f)},.045f,11);
        FablePort(b,.12f,-.77f,.11f,13);
        return fableMask=b.Finish("Painted mask / uneven eyes");
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
