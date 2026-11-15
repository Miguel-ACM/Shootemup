using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// A district condition's light: one multiplied and one added layer covering the view between
// the scenery and every ship and bullet, so only the ground takes the dusk, night or storm.
public sealed class SceneryGrade : MonoBehaviour
{
    Material multiply, add;
    Mesh quad;
    public float Strength { get; set; } = 1;

    public static SceneryGrade Create(Transform parent, string name, int sortingOrder)
    {
        var grade = new GameObject(name).AddComponent<SceneryGrade>();
        grade.transform.SetParent(parent, false);
        grade.Build(sortingOrder);
        return grade;
    }

    void Build(int sortingOrder)
    {
        quad = new Mesh { name = "Scenery grade quad" };
        quad.vertices = new[] { new Vector3(-.5f, -.5f), new Vector3(.5f, -.5f), new Vector3(.5f, .5f), new Vector3(-.5f, .5f) };
        quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        quad.RecalculateBounds();
        var shader = Resources.Load<Shader>("SceneryGrade");
        multiply = new Material(shader) { name = "Scenery grade multiply" };
        multiply.SetFloat("_SrcBlend", (float)BlendMode.DstColor);
        multiply.SetFloat("_DstBlend", (float)BlendMode.Zero);
        multiply.SetColor("_Neutral", Color.white);
        add = new Material(shader) { name = "Scenery grade glow" };
        add.SetFloat("_SrcBlend", (float)BlendMode.One);
        add.SetFloat("_DstBlend", (float)BlendMode.One);
        add.SetColor("_Neutral", Color.black);
        add.SetColor("_Near", Color.black);
        add.SetColor("_Far", Color.black);
        Layer("Multiplied light", multiply, sortingOrder);
        Layer("Added light", add, sortingOrder);
        LateUpdate();
    }

    void Layer(string name, Material material, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = quad;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = sortingOrder;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // side: -1 left, +1 right. reach: how far across the view the near colour carries.
    public SceneryGrade Multiply(Color near, Color far, int side, float reach = 1, float vignette = 0)
    {
        Set(multiply, near, far, side, reach, vignette);
        return this;
    }

    public SceneryGrade Glow(Color near, Color far, int side, float reach = 1)
    {
        Set(add, near, far, side, reach, 0);
        return this;
    }

    static void Set(Material material, Color near, Color far, int side, float reach, float vignette)
    {
        material.SetColor("_Near", near);
        material.SetColor("_Far", far);
        material.SetFloat("_Side", side);
        material.SetFloat("_Reach", Mathf.Clamp(reach, .05f, 1));
        material.SetFloat("_Vignette", vignette);
    }

    void LateUpdate()
    {
        var view = UnityEngine.Camera.main;
        if (view == null)
            return;
        // Cover the whole view, including the side margins, just in front of the scenery.
        float height = view.orthographicSize * 2 + 2, width = height * view.aspect + 2;
        var centre = view.transform.position;
        transform.position = new Vector3(centre.x, centre.y, 12);
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(width, height, 1);
        multiply.SetFloat("_Strength", Strength);
        add.SetFloat("_Strength", Strength);
    }

    void OnDestroy()
    {
        Destroy(multiply);
        Destroy(add);
        Destroy(quad);
    }
}

// Lamp pools, lanterns, fires, fireflies and water glints, gathered into one mesh per section.
public sealed class SceneryLights
{
    public enum Kind
    {
        Lamp = 0, Firefly = 1, Fire = 2, Glint = 3
    }

    static Texture2D falloff;
    readonly List<Vector3> vertices = new List<Vector3>();
    readonly List<Color> colors = new List<Color>();
    readonly List<Vector2> uvs = new List<Vector2>(), behaviour = new List<Vector2>();
    readonly List<int> triangles = new List<int>();
    public int Count => vertices.Count / 4;

    // A soft round falloff shared by every light; generated once.
    public static Texture2D Falloff
    {
        get
        {
            if (falloff != null)
                return falloff;
            const int Size = 64;
            falloff = new Texture2D(Size, Size, TextureFormat.R8, false) { name = "Scenery light falloff", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float dx = (x + .5f) / Size * 2 - 1, dy = (y + .5f) / Size * 2 - 1;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    // A bright centre with a long soft skirt, like light spilling on the ground.
                    float value = Mathf.Clamp01(1 - r);
                    value = value * value * (.55f + .45f * value);
                    pixels[y * Size + x] = new Color32((byte)(value * 255), 0, 0, 255);
                }
            falloff.SetPixels32(pixels);
            falloff.Apply(false, true);
            return falloff;
        }
    }

    public static Material CreateMaterial()
    {
        var material = new Material(Resources.Load<Shader>("SceneryGlow")) { name = "Scenery lights" };
        material.mainTexture = Falloff;
        return material;
    }

    public void Add(Vector3 centre, float radiusX, float radiusY, Color color, float phase, Kind kind, float angle = 0)
    {
        int first = vertices.Count;
        var right = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radiusX;
        var up = new Vector3(-Mathf.Sin(angle), Mathf.Cos(angle)) * radiusY;
        vertices.Add(centre - right - up);
        vertices.Add(centre + right - up);
        vertices.Add(centre + right + up);
        vertices.Add(centre - right + up);
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(0, 1));
        for (int i = 0; i < 4; i++)
        {
            colors.Add(color);
            behaviour.Add(new Vector2(phase, (float)kind));
        }
        triangles.Add(first);
        triangles.Add(first + 2);
        triangles.Add(first + 1);
        triangles.Add(first);
        triangles.Add(first + 3);
        triangles.Add(first + 2);
    }

    public Mesh Finish(string name)
    {
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, behaviour);
        mesh.SetTriangles(triangles, 0);
        // Fireflies wander a little beyond their anchors.
        var bounds = mesh.bounds;
        bounds.Expand(.6f);
        mesh.bounds = bounds;
        return mesh;
    }
}
