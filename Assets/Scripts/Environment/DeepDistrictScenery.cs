using System.Collections.Generic;
using UnityEngine;

public static partial class DeepDistrictScenery
{
    static readonly Color Concrete = new Color(.18f, .20f, .21f);
    static readonly Color Seam = new Color(.105f, .12f, .13f);
    static readonly Color Steel = new Color(.31f, .34f, .35f);
    static readonly Color Iron = new Color(.21f, .25f, .25f);
    static readonly Color Brick = new Color(.27f, .26f, .22f);
    static readonly Color Water = new Color(.095f, .18f, .165f);
    static readonly Color WaterDark = new Color(.053f, .107f, .12f);
    static readonly Color Copper = new Color(.32f, .184f, .105f);

    public static Mesh Section(LevelId level, int seed, int index, float length)
    {
        var b = new SolidMesh();
        float end = length * .5f;
        var random = new System.Random(unchecked(seed * 47 + index * 8609));
        b.Rect(-19, -end, 19, end, 0, level == LevelId.Core ? new Color(.37f, .40f, .40f) : Brick * new Color(.66f, .73f, .79f, 1));
        if (level == LevelId.Core)
            Core(b, end, (index + (seed & 3)) % 4);
        else
            Sewer(b, end, index);
        return b.Finish(level == LevelId.Core ? "Core service tunnel" : "Sewer culverts and flood channels");
    }

    static void Sewer(SolidMesh b, float edge, int index)
    {
        Color mortar = new Color(.15f, .16f, .145f);
        Color stone = new Color(.36f, .34f, .28f);
        Color wet = new Color(.20f, .23f, .20f);
        for (int side = -1; side <= 1; side += 2)
        {
            // Continuous bank foundations meet at the scroll seams.
            b.Rect(side * 4.55f - .38f, -edge, side * 4.55f + .38f, edge, .19f, mortar);
            b.Rect(side * 5.19f - .55f, -edge, side * 5.19f + .55f, edge, .17f, mortar);
            b.Rect(side * 3.83f - .27f, -edge, side * 3.83f + .27f, edge, .06f, WaterDark);
            b.Rect(side * 4.04f - .16f, -edge, side * 4.04f + .16f, edge, .25f, wet);
            int courses = Mathf.CeilToInt(edge * 2 / .48f);
            for (int row = 0; row < courses; row++)
            {
                float y = -edge + row * .48f;
                for (int col = 0; col < 3; col++)
                {
                    float x = side * (4.38f + col * .33f);
                    float shift = (row + col) % 2 == 0 ? .025f : -.025f;
                    b.Rect(x - .15f, Mathf.Max(-edge, y + .04f + shift), x + .15f,
                        Mathf.Min(edge, y + .45f + shift), .24f, (row + col + index) % 5 == 0 ? stone : Brick);
                }
            }
            for (int i = 0; i < 10; i++)
            {
                float y = -edge + 1 + 2 * i;
                if ((i + index + (side > 0 ? 1 : 0)) % 3 == 0)
                {
                    // Buttresses project into the channel and carry a stepped coping.
                    b.Block(side * 3.83f, y, .92f, 1.30f, .23f, wet);
                    b.Block(side * 3.72f, y, .70f, .98f, .39f, Brick, .23f);
                    b.Block(side * 3.69f, y, .75f, .18f, .48f, stone, .39f);
                    b.Block(side * 3.69f, y - .39f, .75f, .16f, .48f, stone, .39f);
                    b.Block(side * 3.69f, y + .39f, .75f, .16f, .48f, stone, .39f);
                }
                if (i % 2 == 0)
                {
                    // Recessed brick sluice, raised piers and separate arch stones.
                    b.Block(side * 5.12f, y, 1.05f, 3.25f, .50f, stone, .17f);
                    b.Block(side * 5.08f, y, .75f, 2.45f, .57f, mortar, .50f);
                    foreach (int end in new[] { -1, 1 })
                    {
                        b.Block(side * 4.72f, y + end * 1.42f, .47f, .36f, .82f, stone, .24f);
                        b.Block(side * 4.72f, y + end * 1.42f, .55f, .17f, .91f, stone * 1.12f, .82f);
                    }
                    for (int k = 0; k < 9; k++)
                    {
                        float angle = Mathf.PI * k / 8f;
                        b.Block(side * 5.12f + Mathf.Cos(angle) * .43f, y + .95f + Mathf.Sin(angle) * .46f,
                            .22f, .20f, .72f, k == 4 ? stone * 1.16f : stone, .50f);
                    }
                }
                if ((i + index) % 3 == 1)
                {
                    b.Block(side * 4.02f, y - .3f, .81f, .59f, .58f, Iron);
                    for (int slat = -1; slat <= 1; slat++)
                        b.Block(side * 3.81f, y - .3f + slat * .14f, .60f, .055f, .63f, Steel);
                }
            }
        }
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
        var bank = Iron;
        bank.a = 0;
        var seam = Brick;
        seam.a = 0;
        Vector2 Radii(float angle, Vector2 radius) =>
            Vector2.Scale(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), radius);
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2 / sides, c = (i + 1) * Mathf.PI * 2 / sides;
            Vector2 wide = new Vector2(4.91f, 4.2f), water = new Vector2(4.42f, 3.85f);
            Vector2 outerA = Radii(a, wide), outerC = Radii(c, wide);
            Vector2 innerA = Radii(a, water), innerC = Radii(c, water);
            b.Face(i % 4 == 0 ? seam : bank, innerA, outerA, outerC, innerC);
            b.Face(WaterDark, Vector2.zero, innerA, innerC);
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
