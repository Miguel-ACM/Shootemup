using UnityEngine;

public static class FableDistrictScenery
{
    public static Mesh Section(LevelId level, int seed, int section, float length)
    {
        if (level == LevelId.Mushroom)
            return MushroomGrove.Build(seed, section, length);

        // Treat all four sections as one long painted roll, including its last-to-first fold.
        const int columns = 12, rows = 16;
        var vertices = new Vector3[(columns + 1) * (rows + 1)];
        var uv = new Vector2[vertices.Length];
        var indices = new int[columns * rows * 6];
        var random = new System.Random(seed);
        int panel = random.Next(4);
        float direction = random.Next(2) == 0 ? 1 : -1;
        for (int j = 0; j <= rows; j++)
        {
            float along = j / (float)rows;
            float v = (section + along + panel) * .25f;
            for (int k = 0; k <= columns; k++)
            {
                int i = j * (columns + 1) + k;
                float x = (k / (float)columns - .5f) * 38;
                float fold = Mathf.Cos(x * .62f + Mathf.Sin(v * Mathf.PI * 4)) * .16f;
                vertices[i] = new Vector3(x, (along - .5f) * length, fold);
                uv[i] = new Vector2(.5f + direction * x / 12.8f, v);
                if (j == rows || k == columns) continue;
                int t = (j * columns + k) * 6;
                indices[t] = i; indices[t + 1] = i + columns + 1; indices[t + 2] = i + 1;
                indices[t + 3] = i + 1; indices[t + 4] = i + columns + 1; indices[t + 5] = i + columns + 2;
            }
        }
        return new Mesh { name = "Dream painted landscape", vertices = vertices, uv = uv, triangles = indices };
    }
}
