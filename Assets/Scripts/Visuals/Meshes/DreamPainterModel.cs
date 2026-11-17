using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static Mesh dreamIcon, dreamPalm;
    static Material dreamPorcelain;
    public static Material DreamPorcelain => dreamPorcelain != null ? dreamPorcelain :
        (dreamPorcelain = new Material(CombatMaterials.Fleet) {
            name = "Dream / glazed porcelain and gold leaf",
            mainTexture = Resources.Load<Texture2D>("DreamPorcelain")
        });

    // An open thumb and three brush fingers. These points are also the live emitters.
    public static readonly Vector3[] DreamNibs = {
        new Vector3(-1.05f, -.46f, -.27f), new Vector3(-.63f, -1.25f, -.34f),
        new Vector3(-.02f, -1.55f, -.37f), new Vector3(.67f, -1.19f, -.29f)
    };

    // Convex enamel laid along an open arc. The narrow ends and raised gold rim
    // leave real crescents of negative space when seen from the flight camera.
    static void PorcelainArc(Builder b, Vector3 center, Vector2 radii, float start, float end,
        float breadth, int enamel, int edge)
    {
        const int steps = 22;
        Vector3 P(int i, float rim, float depth)
        {
            float t = i / (float)steps, a = Mathf.Lerp(start, end, t) * Mathf.Deg2Rad;
            float w = breadth * (.024f + Mathf.Sin(t * Mathf.PI));
            return center + new Vector3(Mathf.Cos(a) * (radii.x + rim * w),
                Mathf.Sin(a) * (radii.y + rim * w), depth * w);
        }
        for (int i = 0; i < steps; i++)
        {
            int j = i + 1;
            b.Face(enamel, P(i,-1,0), P(j,-1,0), P(j,.65f,-.45f), P(i,.65f,-.45f));
            b.Face(edge, P(i,.65f,-.45f), P(j,.65f,-.45f), P(j,1,0), P(i,1,0));
            b.Face(0, P(i,1,0), P(j,1,0), P(j,1,.55f), P(i,1,.55f));
        }
    }

    static void DreamEye(Builder b, Vector3 p, float size)
    {
        Vector3 P(float x, float y, float z) => p + new Vector3(x,y,z) * size;
        var almond = new[]{ V(-1,0), V(-.55f,.27f), V(.06f,.40f), V(.67f,.18f), V(1,.09f),
            V(.52f,-.14f), V(-.10f,-.28f), V(-.62f,-.17f) };
        for (int i=0;i<almond.Length;i++) almond[i] *= size;
        b.Prism(almond, .07f * size, 11, p, 11);
        b.Pearl(P(0,.035f,-.075f), new Vector3(.57f,.23f,.08f)*size, 15, 12, 4);
        CutStone(b,P(.015f,.035f,-.15f),new Vector3(.205f,.20f,.065f)*size,13,10);
        CutStone(b,P(.02f,.035f,-.20f),new Vector3(.105f,.16f,.04f)*size,11,7);
        CutStone(b,P(-.052f,.108f,-.24f),new Vector3(.041f,.04f,.015f)*size,15,5);
    }

    public static Mesh DreamPainterMesh()
    {
        if (dreamIcon != null) return dreamIcon;
        var b = new Builder();
        // A broken aureole frames a double profile. Three wet brush strokes trail the neck.
        PorcelainArc(b,new Vector3(-.13f,.32f,.11f),new Vector2(1.29f,1.27f),12,298,.12f,9,13);
        PorcelainArc(b,new Vector3(.12f,.30f,.17f),new Vector2(1.38f,1.39f),-69,75,.080f,13,10);
        // The broad, upturned collar carries the eye even when the hands cross behind it.
        for (int side=-1;side<=1;side+=2)
        {
            Vector3 P(float x,float y,float z) => new Vector3(side*x,y,z);
            FoldedFin(b,P(.15f,-.40f,-.09f),P(.74f,-.35f,-.11f),P(1.38f,-.02f,.03f),
                P(.92f,-.84f,0),P(.63f,-.56f,-.24f),side<0?8:5,side<0?9:7);
            FoldedFin(b,P(.27f,-.55f,-.10f),P(.99f,-.64f,-.07f),P(1.13f,-1.20f,.01f),
                P(.42f,-1.00f,-.01f),P(.65f,-.78f,-.26f),side<0?9:5,side<0?8:12);
            CutBlade(b,new[]{P(.22f,-.45f,-.21f),P(.69f,-.40f,-.20f),P(1.34f,-.02f,.015f)},
                .052f,.032f,13);
        }
        for (int i = 0; i < 3; i++)
        {
            float x=(i-1)*.31f;
            FoldedFin(b,new Vector3(x-.22f,-.41f,0),new Vector3(x+.31f,-.48f,.02f),
                new Vector3(x*2+.24f,-1.58f+.23f*Mathf.Abs(i-1),.10f),
                new Vector3(x-.34f,-.87f,.03f),new Vector3(x-.06f,-.81f,-.21f),i==1?5:8,i==1?7:9);
        }
        KeelSkin(b,0,0,Frame(-.92f,.12f,.08f),Frame(-.56f,.50f,.23f),
            Frame(.08f,.79f,.33f),Frame(.58f,.66f,.23f),Frame(.91f,.25f,.09f));
        // The warm face is in profile; its nose projects beyond the cool cheek.
        BevelArmor(b,new[]{V(-.66f,.60f),V(-.20f,.88f),V(.21f,.65f),V(.13f,.30f),
            V(.35f,-.04f),V(.10f,-.12f),V(.14f,-.26f),V(.01f,-.30f),V(.09f,-.41f),
            V(-.16f,-.62f),V(-.42f,-.61f),V(-.64f,-.18f)},.22f,15,-.28f,.085f);
        FoldedFin(b,new Vector3(.08f,.72f,-.23f),new Vector3(.69f,.49f,-.22f),new Vector3(.57f,-.39f,-.25f),
            new Vector3(-.12f,-.78f,-.32f),new Vector3(.22f,-.03f,-.54f),9,8);
        b.Sweep(new Vector3(-.67f,.52f,-.37f),new Vector3(-.42f,.85f,-.48f),
            new Vector3(-.03f,.66f,-.47f),new Vector3(.14f,.49f,-.40f),.068f,.051f,11,false,6,5);
        DreamEye(b,new Vector3(-.15f,.21f,-.52f),.69f);
        CutBlade(b,new[]{new Vector3(-.35f,-.29f,-.43f),new Vector3(-.17f,-.34f,-.48f),
            new Vector3(.07f,-.28f,-.42f)},.052f,.041f,5);
        PorcelainArc(b,new Vector3(.08f,.63f,-.30f),new Vector2(.75f,.55f),15,158,.16f,5,13);
        // Crown shaped like a fountain pen split down its nib.
        for (int side=-1;side<=1;side+=2)
            FoldedFin(b,new Vector3(side*.045f,.81f,-.16f),new Vector3(side*.44f,1.13f,-.17f),
                new Vector3(side*.28f,1.77f,.02f),new Vector3(side*.17f,1.14f,-.16f),
                new Vector3(side*.25f,1.18f,-.31f),13,side<0?5:9);
        CutStone(b,new Vector3(0,1.39f,-.22f),new Vector3(.12f,.24f,.08f),10,4);
        return dreamIcon=b.Finish("The Oneirist / moon reliquary");
    }

    static void BrushDigit(Builder b, Vector3 root, Vector3 joint, Vector3 tip, float width)
    {
        Vector3 knuckle=Vector3.Lerp(joint,tip,.46f) + Vector3.back*.065f;
        Vector3[] bones={root,joint,knuckle,Vector3.Lerp(knuckle,tip,.48f),tip};
        float[] widths={width,width*.84f,width*.68f,width*.56f,.004f};
        Vector3 Rim(int ring,int corner)
        {
            Vector3 tangent=bones[Mathf.Min(4,ring+1)]-bones[Mathf.Max(0,ring-1)];
            Vector3 side=new Vector3(-tangent.y,tangent.x,0).normalized;
            float a=corner*Mathf.PI/3;
            return bones[ring]+(side*Mathf.Cos(a)+Vector3.back*(.76f*Mathf.Sin(a)))*widths[ring];
        }
        for(int i=0;i<4;i++)
            for(int k=0;k<6;k++)
                b.Face(i==3?13:(k==0||k==5?14:15),Rim(i,k),Rim(i+1,k),Rim(i+1,k+1),Rim(i,k+1));
        CutStone(b,joint+Vector3.back*(width*.62f),new Vector3(width*.68f,width*.52f,.034f),13,6);
    }

    public static Mesh DreamHandMesh()
    {
        if (dreamPalm != null) return dreamPalm;
        var b=new Builder();
        KeelSkin(b,.01f,15,Frame(-.38f,.36f,.10f),Frame(-.23f,.55f,.19f),
            Frame(.07f,.60f,.25f),Frame(.39f,.46f,.21f,.09f),Frame(.69f,.22f,.13f,.14f));
        BrushDigit(b,new Vector3(-.41f,.18f,-.05f),new Vector3(-.86f,.02f,-.19f),DreamNibs[0],.18f);
        BrushDigit(b,new Vector3(-.38f,-.21f,-.05f),new Vector3(-.60f,-.70f,-.22f),DreamNibs[1],.16f);
        BrushDigit(b,new Vector3(-.04f,-.30f,-.05f),new Vector3(-.14f,-.93f,-.23f),DreamNibs[2],.175f);
        BrushDigit(b,new Vector3(.31f,-.23f,-.04f),new Vector3(.47f,-.69f,-.19f),DreamNibs[3],.14f);
        KeelSkin(b,0,8,Frame(.43f,.39f,.20f,.11f),Frame(.53f,.44f,.23f,.14f),
            Frame(.85f,.31f,.19f,.18f),Frame(1.05f,.45f,.11f,.23f));
        CutBlade(b,new[]{new Vector3(-.23f,.55f,-.15f),new Vector3(.13f,.48f,-.24f),
            new Vector3(.54f,.57f,-.15f)},.065f,.038f,13);
        DreamEye(b,new Vector3(.05f,.05f,-.27f),.43f);
        return dreamPalm=b.Finish("The Oneirist / four porcelain brush fingers");
    }
}
