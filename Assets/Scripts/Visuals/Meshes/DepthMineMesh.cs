using UnityEngine;

public static partial class IndustrialMeshFactory
{
    static Mesh depthMine;
    public static Mesh DepthMineMesh()
    {
        if (depthMine != null) return depthMine;
        var b = new Builder();
        // Flat facets, a raised equatorial belt and eight separate contact horns.
        const int sides = 12;
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2 / sides, c = (i + 1) * Mathf.PI * 2 / sides;
            Vector3 Rim(float angle, float r, float z) => new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, z);
            b.Face(8, Rim(a,.30f,-.43f), Rim(c,.30f,-.43f), Rim(c,.48f,0), Rim(a,.48f,0));
            b.Face(6, Rim(a,.48f,0), Rim(c,.48f,0), Rim(c,.30f,.34f), Rim(a,.30f,.34f));
            b.Face(13, Rim(a,.51f,-.055f), Rim(c,.51f,-.055f), Rim(c,.51f,.055f), Rim(a,.51f,.055f));
            b.Face(6, new Vector3(0,0,-.46f), Rim(c,.30f,-.43f), Rim(a,.30f,-.43f));
        }
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4;
            var axis = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var side = new Vector2(-axis.y, axis.x);
            b.Prism(new[] { axis*.40f-side*.11f, axis*.40f+side*.11f, axis*.76f+side*.05f, axis*.76f-side*.05f }, .15f, 6, Vector3.zero, 7);
            b.Box((Vector3)axis*.73f, new Vector3(.10f,.10f,.18f), 13, 7);
        }
        b.Drum(new Vector3(0,0,-.48f), .15f, .07f, 13, 8);
        b.Box(new Vector3(0,-.91f,.08f), new Vector3(.06f,.25f,.08f), 6);
        b.Drum(new Vector3(0,-1.07f,.08f), .11f, .08f, 6, 6);
        return depthMine = b.Finish("Faceted naval mine with contact horns and tether");
    }
}
