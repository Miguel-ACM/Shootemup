using System.Collections.Generic;
using UnityEngine;

public static partial class DeepDistrictScenery
{
    static readonly Color Brick = new Color(.27f, .26f, .22f);
    static readonly Color Water = new Color(.095f, .18f, .165f);
    static readonly Color WaterDark = new Color(.053f, .107f, .12f);

    public static Mesh Section(LevelId level, int seed, int index, float length)
    {
        var b = new SolidMesh();
        float end = length * .5f;
        var random = new System.Random(unchecked(seed * 47 + index * 8609));
        b.Rect(-19, -end, 19, end, 0, level == LevelId.Core ? new Color(.37f, .40f, .40f) : Brick * new Color(.66f, .73f, .79f, 1));
        if (level == LevelId.Core)
            Core(b, end, (index + (seed & 3)) % 4);
        else
            Sewer(b, end, index, random);
        return b.Finish(level == LevelId.Core ? "Core service tunnel" : "Sewer culverts and flood channels");
    }

    public static Mesh Channel(float length)
    {
        var b = new SolidMesh();
        const float bank = 3.91f;
        float edge = length * .5f;
        b.Rect(-bank, -edge, -2.78f, edge, .014f, WaterDark);
        b.Rect(2.78f, -edge, bank, edge, .014f, WaterDark);
        b.Gradient(-2.78f, -edge, 2.78f, edge, .014f, WaterDark, Water);
        return b.Finish("Flowing sewer channel");
    }

    public static Mesh Lake()
    {
        var b = new SolidMesh();
        const int sides = 64;
        Color Dry(Color c) => new Color(c.r,c.g,c.b,0);
        Vector2 lip = new Vector2(4.42f, 3.85f), rim = new Vector2(4.94f,4.28f);
        Band(b,Vector2.zero,lip,0,0,Mathf.PI*2,0,WaterDark,sides);
        Band(b,Vector2.zero,rim,.885f,0,Mathf.PI*2,.05f,Dry(Joint),sides);
        for(int i=0;i<40;i++)
        {
            float a=i*Mathf.PI/20, d=(i+1)*Mathf.PI/20;
            Color stone=Lit(Limestone,.68f+.20f*Mathf.Cos(a-1.95f));
            Band(b,Vector2.zero,rim,.91f,a+.007f,d-.007f,.10f,Dry(stone),3);
            Band(b,Vector2.zero,rim*.926f,.98f,a+.007f,d-.007f,.12f,Dry(Lit(stone,1.25f)),3);
            if(i%5!=0) continue;
            Vector2 boss=Vector2.Scale(Polar((a+d)*.5f),rim)*.95f;
            Band(b,boss,P(.09f,.09f),0,0,Mathf.PI*2,.13f,Dry(Bronze),8);
        }
        return b.Finish("Cistern circular lake");
    }

    sealed class SolidMesh
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> indices = new List<int>();
        readonly List<Vector4> rotatingParts = new List<Vector4>();
        public Vector4 Rotor { get; set; }
        public void Face(Color ink, params Vector2[] points) => Face(ink, 0, points);
        public void Face(Color ink, float height, params Vector2[] points)
        {
            int start = vertices.Count;
            foreach (var point in points)
            {
                vertices.Add(new Vector3(point.x, point.y, -height));
                colors.Add(ink);
                rotatingParts.Add(Rotor);
            }
            for (int i = 1; i < points.Length - 1; i++)
            {
                indices.Add(start);
                indices.Add(start + i);
                indices.Add(start + i + 1);
            }
        }
        public void Rect(float left, float bottom, float right, float top, float height, Color ink)
            => Face(ink, height, new Vector2(left, bottom), new Vector2(right, bottom), new Vector2(right, top), new Vector2(left, top));

        public void Gradient(float left, float bottom, float right, float top, float height, Color bank, Color centre)
        {
            float middle = (left + right) * .5f;
            Rect(left, bottom, middle, top, height, bank);
            colors[colors.Count - 3] = colors[colors.Count - 2] = centre;
            Rect(middle, bottom, right, top, height, bank);
            colors[colors.Count - 4] = colors[colors.Count - 1] = centre;
        }

        public void Block(float x, float y, float width, float length, float height, Color top, float baseHeight = 0)
        {
            float left = x - width * .5f, right = x + width * .5f, front = y - length * .5f, back = y + length * .5f;
            float dx = height * .17f, dy = height * .36f;
            Face(top * new Color(.64f, .69f, .73f, 1), baseHeight,
                new Vector2(left, front), new Vector2(right, front), new Vector2(right + dx, front + dy), new Vector2(left + dx, front + dy));
            Face(top * new Color(.72f, .76f, .76f, 1), baseHeight,
                new Vector2(left, front), new Vector2(left + dx, front + dy), new Vector2(left + dx, back + dy), new Vector2(left, back));
            Rect(left + dx, front + dy, right + dx, back + dy, height, top);
        }
        public Mesh Finish(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(1, rotatingParts);
            mesh.SetTriangles(indices, 0);
            return mesh;
        }
    }
}
