using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static Mesh stagBody, stagDisplay;
    static readonly Mesh[] stagCases = new Mesh[2];
    static Material stagPaint;

    public static Material StagPaint => stagPaint != null ? stagPaint :
        stagPaint = new Material(CombatMaterials.Fleet) {
            name = "Stag / fly agaric enamel", mainTexture = Resources.Load<Texture2D>("StagEnamel")
        };

    public static Vector3 StagMuzzle(int side) => new Vector3(side * .54f, -.98f, -.21f);
    public static Vector3 StagHinge(int side) => new Vector3(side * .22f, -.34f, -.12f);

    public static Mesh MushroomChampionMesh()
    {
        if (stagDisplay != null) return stagDisplay;
        var pieces = new CombineInstance[3];
        pieces[0] = new CombineInstance { mesh = StagBody(), transform = Matrix4x4.identity };
        for (int i = 0; i < 2; i++)
        {
            int side = i * 2 - 1;
            pieces[i + 1] = new CombineInstance { mesh = StagCase(side), transform = Matrix4x4.Translate(StagHinge(side)) };
        }
        stagDisplay = new Mesh { name = "Mushroom champion / stag gunship" };
        stagDisplay.CombineMeshes(pieces);
        return stagDisplay;
    }

    public static Mesh StagBody()
    {
        if (stagBody != null) return stagBody;
        var b = new Builder();
        KeelSkin(b, .07f, 2, Frame(-.82f,.15f,.12f), Frame(-.53f,.43f,.22f),
            Frame(-.16f,.59f,.27f), Frame(.32f,.63f,.24f), Frame(.76f,.49f,.17f), Frame(1.03f,.12f,.08f));

        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 P(float x, float y, float z) => new Vector3(side * x, y, z);
            // The pale accordion under the shell shows through the dorsal slit as the covers lift.
            for (int i = 0; i < 6; i++)
            {
                float y = -.12f + .17f * i;
                float span = .64f - .043f * i;
                FoldedFin(b, P(.016f,y-.028f,-.27f), P(span,y-.12f,-.10f), P(span+.05f,y-.04f,-.09f),
                    P(.025f,y+.068f,-.28f), P(.24f,y+.016f,-.32f), 15, 13);
            }
            // Three jointed outriggers sweep in different directions, leaving air between the feet.
            for (int i = 0; i < 3; i++)
            {
                float y = -.43f + i * .41f;
                var knee = P(1.09f + .12f * (i % 2), y+.12f, .05f);
                var ankle = P(1.18f - .09f*i,y-.15f+.23f*i,.08f);
                CutBlade(b, new[] { P(.38f,y,-.07f), knee, ankle, P(1.28f-.14f*i,y-.36f+.44f*i,.02f) }, .085f,.07f, 4);
                FoldedFin(b,P(.65f,y-.04f,-.01f),knee+Vector3.down*.07f,knee+Vector3.right*side*.07f,
                    knee+Vector3.up*.06f,P(.85f,y+.05f,-.10f),13,14);
            }

            // The mandible carries a straight barrel inside its fork. The long outer tine hooks back inward.
            var joint = P(.35f,-.48f,-.13f);
            var elbow = P(.69f,-.74f,-.19f);
            CutBlade(b,new[] { joint,elbow,P(.76f,-1.09f,-.19f),P(.64f,-1.30f,-.14f),P(.41f,-1.47f,-.12f) },.16f,.13f,1);
            CutBlade(b,new[] { P(.73f,-1.02f,-.22f),P(.59f,-1.04f,-.23f),P(.48f,-1.18f,-.20f) },.074f,.051f,13);
            MothBarrel(b,StagMuzzle(side),.135f,.38f);
            // Flat-faceted cheek lenses stay readable on the darker forward helmet.
            FoldedFin(b,P(.10f,-.65f,-.46f),P(.16f,-.49f,-.48f),P(.47f,-.45f,-.39f),
                P(.36f,-.64f,-.39f),P(.29f,-.56f,-.515f),9,4);
            CutBlade(b,new[] { P(.09f,-.51f,-.48f),P(.25f,-.44f,-.50f),P(.48f,-.46f,-.38f) },.034f,.034f,1);
        }
        CutStone(b,new Vector3(0,-.45f,-.23f),new Vector3(.48f,.33f,.18f),2,8);
        FoldedFin(b,new Vector3(-.28f,-.39f,-.38f),new Vector3(0,-.29f,-.42f),
            new Vector3(.28f,-.39f,-.38f),new Vector3(0,-.79f,-.28f),new Vector3(0,-.53f,-.53f),4,2);
        FoldedFin(b,new Vector3(0,-.31f,-.46f),new Vector3(.072f,-.47f,-.51f),
            new Vector3(0,-.77f,-.32f),new Vector3(-.072f,-.47f,-.51f),new Vector3(0,-.52f,-.55f),13,1);
        b.MothTrimUV();
        return stagBody = b.Finish("Stag / gills, mandibles and keel");
    }

    // From the neck aft: lengthwise position, inner seam, outer rim and crown depth.
    static readonly Vector4[] stagSections = {
        new Vector4(-.38f,.16f,.53f,-.31f), new Vector4(-.23f,.045f,.84f,-.49f),
        new Vector4(.04f,.028f,1.02f,-.60f), new Vector4(.40f,.052f,1.04f,-.62f),
        new Vector4(.72f,.095f,.93f,-.49f), new Vector4(.98f,.16f,.67f,-.34f),
        new Vector4(1.10f,.24f,.46f,-.22f), new Vector4(1.15f,.31f,.36f,-.17f)
    };

    public static Mesh StagCase(int side)
    {
        int index = side < 0 ? 0 : 1;
        if (stagCases[index] != null) return stagCases[index];
        var b = new Builder();
        Vector3 pivot = StagHinge(side);
        Vector3 Panel(int row, int rail)
        {
            Vector4 s = stagSections[row];
            float x = rail == 0 ? s.y : rail == 1 ? Mathf.Lerp(s.y,s.z,.42f) : s.z;
            float z = rail == 0 ? s.w+.115f : rail == 1 ? s.w : -.14f;
            return new Vector3(side*x,s.x,z)-pivot;
        }
        Vector3 Lower(int row,int rail) => Panel(row,rail) + Vector3.forward * .09f;
        void Edge(Vector3 a, Vector3 d)
        {
            var drop = Vector3.forward * .09f;
            b.Face(13, d, a, a+drop, d+drop);
        }
        for (int row = 0; row < stagSections.Length-1; row++)
        {
            int left = side>0 ? 0 : 2, right = 2-left;
            Edge(Panel(row,left),Panel(row+1,left));
            Edge(Panel(row+1,right),Panel(row,right));
            b.Face(7,Lower(row,left),Lower(row,right),Lower(row+1,right),Lower(row+1,left));
        }
        for (int rail=0; rail<2; rail++)
        {
            int a=side>0 ? rail : 2-rail, d=side>0 ? rail+1 : 1-rail;
            Edge(Panel(0,d),Panel(0,a));
            Edge(Panel(stagSections.Length-1,a),Panel(stagSections.Length-1,d));
        }
        b.MothTrimUV();
        for (int row = 0; row < stagSections.Length-1; row++)
            for (int rail = 0; rail < 2; rail++)
            {
                Vector3 a = Panel(row,rail), d = Panel(row+1,rail);
                Vector3 c = Panel(row+1,rail+1), e = Panel(row,rail+1);
                b.Face(0, side>0?a:e, side>0?d:c, side>0?c:d, side>0?e:a);
            }
        Mesh shell = b.Finish("Stag / lacquered wing case");
        var vertices = shell.vertices;
        var uv = shell.uv;
        int enamelVertices = (stagSections.Length-1) * 2 * 4;
        for (int i=vertices.Length-enamelVertices; i<uv.Length; i++)
        {
            Vector3 point = vertices[i]+pivot;
            uv[i] = new Vector2(.02f+.94f*Mathf.Abs(point.x)/1.08f, .12f+.85f*(point.y+.38f)/1.53f);
        }
        shell.uv = uv;
        return stagCases[index] = shell;
    }
}
