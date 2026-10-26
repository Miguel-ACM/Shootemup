using UnityEngine;
using System.Collections.Generic;

public static class BasaltCraterGeometry
{
    public static Mesh Build()
    {
        const int N = 14;
        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var triangles = new List<int>();
        var rings = new Vector3[6, N];
        float[] radius = { 1.75f, 1.42f, 1.08f, .63f, .43f, .26f };
        float[] height = { 0, .22f, .63f, 1.13f, .91f, .67f };
        for (int layer = 0; layer < 6; layer++)
            for (int i = 0; i < N; i++)
            {
                float a = i * Mathf.PI * 2 / N;
                float irregular = 1 + .09f * Mathf.Sin(i * 2.17f) + .045f * Mathf.Cos(i * 4.33f + layer * .7f);
                rings[layer, i] = new Vector3(Mathf.Cos(a) * radius[layer] * irregular + layer * .025f, Mathf.Sin(a) * radius[layer] * irregular * .79f, -height[layer] - (layer == 3 ? .13f * Mathf.Sin(i * 2.7f) : 0));
            }
        void Face(Color color, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int k = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            for (int j = 0; j < 4; j++)
                colors.Add(color);
            triangles.Add(k);
            triangles.Add(k + 1);
            triangles.Add(k + 2);
            triangles.Add(k);
            triangles.Add(k + 2);
            triangles.Add(k + 3);
        }
        for (int layer = 0; layer < 5; layer++)
            for (int i = 0; i < N; i++)
            {
                int j = (i + 1) % N;
                Color c = layer >= 3 ? new Color(.12f, .065f, .04f) : new Color(.095f, .098f, .11f);
                c *= .85f + .12f * Mathf.Cos(i * .45f) + layer * .025f;
                c.a = 1;
                Face(c, rings[layer, i], rings[layer, j], rings[layer + 1, j], rings[layer + 1, i]);
            }
        for (int i = 0; i < N; i++)
            Face(new Color(.24f, .065f, .015f), rings[5, i], rings[5, (i + 1) % N], new Vector3(.12f, .04f, -.62f), new Vector3(.12f, .04f, -.62f));
        var mesh = new Mesh { name = "Irregular terraced basalt crater with recessed throat" };
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
