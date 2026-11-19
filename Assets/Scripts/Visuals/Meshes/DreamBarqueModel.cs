using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static Mesh barqueHull, barqueCanvas, barqueWeight, barqueDisplay;
    static Material barquePaint;
    public static readonly Vector3 BarquePivot = new Vector3(.02f, -.54f, -.24f);
    public static Vector3 BarqueLantern(int side) => new Vector3(side * .94f, -.29f, -.405f);
    public static Material BarquePaint => barquePaint != null ? barquePaint :
        barquePaint = new Material(CombatMaterials.Fleet) {
            name = "Barque / lacquer, gold leaf and sailcloth", mainTexture = Resources.Load<Texture2D>("BarqueLivery")
        };

    public static Mesh DreamChampionMesh()
    {
        if (barqueDisplay != null) return barqueDisplay;
        barqueDisplay = new Mesh { name = "Dream champion / the moon barque" };
        barqueDisplay.CombineMeshes(new[] {
            new CombineInstance { mesh = BarqueHull(), transform = Matrix4x4.identity },
            new CombineInstance { mesh = BarqueSails(), transform = Matrix4x4.identity },
            new CombineInstance { mesh = BarquePendulum(), transform = Matrix4x4.Translate(BarquePivot) }
        });
        return barqueDisplay;
    }

    // Each frame gives the span, height of the sheer, keel and breadth of the deck.
    static readonly Vector4[] barqueFrames = {
        new Vector4(-1.42f,.42f,.38f,.03f), new Vector4(-1.29f,.19f,-.12f,.11f),
        new Vector4(-1.08f,-.06f,-.39f,.19f), new Vector4(-.70f,-.19f,-.61f,.28f),
        new Vector4(-.21f,-.25f,-.72f,.33f), new Vector4(.32f,-.23f,-.69f,.33f),
        new Vector4(.80f,-.14f,-.52f,.26f), new Vector4(1.12f,.05f,-.27f,.17f),
        new Vector4(1.34f,.31f,.07f,.09f), new Vector4(1.48f,.58f,.55f,.018f)
    };

    public static Mesh BarqueHull()
    {
        if (barqueHull != null) return barqueHull;
        var b = new Builder();
        Vector3 Chine(Vector4 f, int rail)
        {
            switch (rail)
            {
                case 0: return new Vector3(f.x, f.y, f.w);
                case 1: return new Vector3(f.x, f.y, -.60f*f.w);
                case 2: return new Vector3(f.x, Mathf.Lerp(f.y,f.z,.16f), -f.w);
                case 3: return new Vector3(f.x, Mathf.Lerp(f.y,f.z,.70f), -.84f*f.w);
                case 4: return new Vector3(f.x, f.z, -.18f*f.w);
                default: return new Vector3(f.x, f.z, .60f*f.w);
            }
        }
        int[] finish = { 14, 1, 4, 2, 13, 11 };
        for (int i=1; i<barqueFrames.Length; i++)
        {
            for (int rail=0; rail<finish.Length; rail++)
            {
                int next = (rail+1) % finish.Length;
                b.Face(finish[rail], Chine(barqueFrames[i-1],rail), Chine(barqueFrames[i],rail),
                    Chine(barqueFrames[i],next), Chine(barqueFrames[i-1],next));
            }
            // A continuous chased stripe follows the bend in the hull planking.
            Vector3 Stripe(Vector4 frame,float t) =>
                Vector3.Lerp(Chine(frame,2),Chine(frame,3),t) + Vector3.back*.008f;
            var a = barqueFrames[i-1]; var d = barqueFrames[i];
            b.Face(13,Stripe(a,.79f),Stripe(d,.79f),Stripe(d,.87f),Stripe(a,.87f));
        }
        foreach (int side in new[] {-1,1})
        {
            var lamp = BarqueLantern(side);
            // A peaked brass roof over a cut-glass cabin. The centre pane is the firing point.
            Vector3 L(float x,float y,float z) => lamp + new Vector3(x,y,z);
            b.Prism(new[]{V(-.13f,.095f),V(.13f,.095f),V(.091f,-.115f),V(0,-.18f),V(-.091f,-.115f)},
                .12f,11,L(0,0,.11f),13);
            FoldedFin(b,L(-.116f,.084f,.06f),L(.116f,.084f,.06f),L(.076f,-.095f,.06f),
                L(-.076f,-.095f,.06f),L(0,-.008f,-.006f),10,12);
            FoldedFin(b,L(-.158f,.078f,.04f),L(-.063f,.167f,.08f),L(.025f,.225f,.08f),
                L(.158f,.078f,.04f),L(0,.094f,-.022f),13,1);
            CutBlade(b,new[]{L(0,.092f,-.019f),L(0,-.024f,-.019f),L(0,-.13f,.03f)},.012f,.008f,13);
            for (int i=0;i<3;i++)
            {
                float x = side*(.32f+.17f*i), y = -.44f+.036f*i;
                FoldedFin(b,new Vector3(x-.05f,y,-.32f),new Vector3(x,y+.055f,-.32f),
                    new Vector3(x+.05f,y,-.32f),new Vector3(x,y-.042f,-.32f),new Vector3(x,y,-.34f),13,1);
            }
        }
        b.Drum(BarquePivot,.114f,.13f,13,8,11);
        // The raked mast and two stays tie the sail plan into the tips of the hull.
        CutBlade(b,new[]{new Vector3(-.08f,-.28f,0),new Vector3(.08f,.57f,.02f),new Vector3(.26f,1.46f,.02f)},.052f,.042f,13);
        CutBlade(b,new[]{new Vector3(.21f,1.27f,.04f),new Vector3(.89f,.70f,.03f),new Vector3(1.44f,.48f,.05f)},.019f,.014f,1);
        CutBlade(b,new[]{new Vector3(.22f,1.30f,.01f),new Vector3(.29f,1.05f,-.028f),new Vector3(.38f,.78f,-.085f)},.029f,.018f,13);
        CutBlade(b,new[]{new Vector3(1.15f,.13f,-.09f),new Vector3(1.27f,.20f,-.02f),new Vector3(1.38f,.36f,.02f)},.025f,.017f,13);
        CutBlade(b,new[]{new Vector3(-1.37f,.29f,.08f),new Vector3(-.75f,.72f,.08f),new Vector3(.18f,1.22f,.08f)},.023f,.016f,13);
        FoldedFin(b,new Vector3(.21f,1.26f,-.04f),new Vector3(.42f,1.40f,-.06f),
            new Vector3(.87f,1.39f,.01f),new Vector3(.43f,1.23f,.015f),new Vector3(.49f,1.31f,-.095f),12,5);
        b.MothTrimUV();
        return barqueHull = b.Finish("Barque / crescent keel and gunwales");
    }

    public static Mesh BarquePendulum()
    {
        if (barqueWeight != null) return barqueWeight;
        var b = new Builder();
        CutBlade(b,new[]{ new Vector3(0,0,-.09f), new Vector3(0,-.26f,-.10f),
            new Vector3(0,-.54f,-.09f) },.037f,.032f,1);
        var bob = new Vector3(0,-.54f,-.095f);
        CutStone(b,bob,new Vector3(.15f,.24f,.075f),13,4);
        CutStone(b,bob+Vector3.back*.062f,new Vector3(.088f,.15f,.05f),12,4);
        b.MothTrimUV();
        return barqueWeight = b.Finish("Barque / garnet pendulum");
    }

    public static Mesh BarqueSails()
    {
        if (barqueCanvas != null) return barqueCanvas;
        var b = new Builder();
        // The convex belly is broadside to the flight camera. Each horizontal batten
        // climbs from the scalloped leech to the mast; the smaller jib leans the other way.
        Vector3 Cloth(bool jib, int row, int column)
        {
            float t = row / 5f, u = column / 5f;
            float mast = Mathf.Lerp(-.08f,.19f,t);
            float leech = jib ? Mathf.Lerp(1.19f,.35f,t)+.04f*Mathf.Sin(Mathf.PI*t) :
                Mathf.Lerp(-1.14f,-.34f,t)-.18f*Mathf.Sin(Mathf.PI*t);
            float innerY = jib ? Mathf.Lerp(-.12f,.86f,t) : Mathf.Lerp(-.15f,1.23f,t);
            float outerY = jib ? Mathf.Lerp(.15f,.81f,t) : Mathf.Lerp(.085f,.96f,t);
            float belly = Mathf.Sin(Mathf.PI*u) * (.17f+.12f*Mathf.Sin(t*Mathf.PI));
            return new Vector3(Mathf.Lerp(mast+(jib?.19f:-.044f), leech, u),
                Mathf.Lerp(innerY,outerY,u)-belly*.27f, -.09f-belly);
        }
        for (int sail=0;sail<2;sail++)
            b.BarqueCloth(sail==1, Cloth);
        return barqueCanvas = b.Finish("Barque / sun and constellation sails");
    }

    sealed partial class Builder
    {
        public void BarqueCloth(bool jib, System.Func<bool,int,int,Vector3> cloth)
        {
            for (int row=0;row<5;row++)
                for (int col=0;col<5;col++)
                {
                    int left = jib ? col : col+1, right = jib ? col+1 : col;
                    int start = vertices.Count;
                    void Corner(int r,int c)
                    {
                        vertices.Add(cloth(jib,r,c));
                        uv.Add(new Vector2((jib?.506f:.006f)+.488f*c/5f,.105f+.885f*r/5f));
                    }
                    Corner(row,left); Corner(row+1,left); Corner(row+1,right); Corner(row,right);
                    indices.Add(start); indices.Add(start+1); indices.Add(start+2);
                    indices.Add(start); indices.Add(start+2); indices.Add(start+3);
                }
        }
    }
}
