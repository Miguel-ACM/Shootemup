using UnityEngine;
using UnityEngine.Rendering;

// Terrain-bound grit and a low, displaced sand lip. All effects stay behind airborne combat.
public sealed class SandEmergenceEffects : MonoBehaviour
{
    struct Grain
    {
        public Vector2 point, velocity; public float height, lift, life, duration, size; public Color color;
    }

    const int Capacity = 72;
    static Material sand;
    static Mesh lip, contact;
    readonly Grain[] grains = new Grain[Capacity];
    readonly Vector3[] vertices = new Vector3[Capacity * 3];
    readonly Color[] colors = new Color[Capacity * 3];
    Mesh debris;
    Transform root;
    System.Random random;
    int cursor;
    public int ActiveGrains
    {
        get; private set;
    }

    public void Initialize(Vector3 ground)
    {
        if (root != null)
            return;
        if (sand == null)
            sand = new Material(Resources.Load<Shader>("LevelScenery")) { name = "Displaced desert sand" };
        if (lip == null)
        {
            lip = Ring(false);
            contact = Ring(true);
        }
        root = new GameObject("Terrain-bound sand lip and grains").transform;
        root.SetParent(transform, false);
        root.position = ground;
        Add("Attached contact shadow", contact, -36);
        Add("Displaced sand collar", lip, -34);
        debris = new Mesh { name = "Ejected sand chips" };
        debris.MarkDynamic();
        debris.vertices = vertices;
        debris.colors = colors;
        var indices = new int[vertices.Length];
        for (int i = 0; i < indices.Length; i++)
            indices[i] = i;
        debris.triangles = indices;
        debris.bounds = new Bounds(Vector3.zero, new Vector3(6, 6, 6));
        Add("Ballistic sand particles", debris, -33);
        random = new System.Random(LevelRun.Seed + Mathf.RoundToInt(ground.x * 917 + ground.y * 271));
    }

    void Add(string label, Mesh mesh, int order)
    {
        var go = new GameObject(label);
        go.transform.SetParent(root, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = sand;
        r.sortingLayerName = "Background";
        r.sortingOrder = order;
        r.shadowCastingMode = ShadowCastingMode.Off;
    }

    public void Place(Vector3 ground)
    {
        if (root != null)
            root.position = ground;
    }

    public void Burst(int count, float force)
    {
        if (random == null)
            return;
        float R(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());
        for (int i = 0; i < count; i++)
        {
            float angle = R(0, Mathf.PI * 2);
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float duration = R(.55f, 1.1f);
            grains[cursor] = new Grain
            {
                point = direction * R(.26f, .49f),
                velocity = direction * R(.65f, 1.65f) * force,
                height = .04f,
                lift = R(1.1f, 2.2f) * force,
                life = duration,
                duration = duration,
                size = R(.018f, .045f),
                color = Color.Lerp(new Color(.48f, .32f, .17f), new Color(.91f, .73f, .46f), R(0, 1))
            };
            cursor = (cursor + 1) % Capacity;
        }
    }

    void Update()
    {
        if (debris == null)
            return;
        float dt = Time.deltaTime;
        ActiveGrains = 0;
        for (int i = 0; i < Capacity; i++)
        {
            var g = grains[i];
            g.life = Mathf.Max(0, g.life - dt);
            int k = i * 3;
            if (g.life <= 0)
            {
                for (int j = 0; j < 3; j++)
                    colors[k + j] = Color.clear;
                grains[i] = g;
                continue;
            }
            ActiveGrains++;
            g.point += g.velocity * dt;
            g.lift -= 5.4f * dt;
            g.height += g.lift * dt;
            if (g.height < 0)
            {
                g.height = 0;
                g.lift = Mathf.Abs(g.lift) * .18f;
                g.velocity *= .68f;
            }
            var p = new Vector3(g.point.x + g.height * .2f, g.point.y + g.height * .3f, -.09f - g.height);
            vertices[k] = p + new Vector3(-g.size, -g.size * .6f);
            vertices[k + 1] = p + new Vector3(g.size, -g.size * .4f);
            vertices[k + 2] = p + new Vector3(0, g.size);
            g.color.a = Mathf.Clamp01(g.life / .24f);
            for (int j = 0; j < 3; j++)
                colors[k + j] = g.color;
            grains[i] = g;
        }
        debris.vertices = vertices;
        debris.colors = colors;
    }

    static Mesh Ring(bool shadow)
    {
        const int sides = 32;
        var points = new Vector3[sides * 12];
        var colors = new Color[points.Length];
        var indices = new int[points.Length];
        int n = 0;
        for (int i = 0; i < sides; i++)
            for (int band = 0; band < 2; band++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 P(float angle, int ring)
                {
                    float radius = shadow ? (ring == 0 ? 0 : ring == 1 ? .48f : .79f) : (ring == 0 ? .43f : ring == 1 ? .64f : .88f);
                    float rough = 1 + .035f * Mathf.Sin(angle * 7) + .025f * Mathf.Cos(angle * 11);
                    return new Vector3(Mathf.Cos(angle) * radius * rough, Mathf.Sin(angle) * radius * rough * .84f, shadow ? -.035f : ring == 1 ? -.13f : -.055f);
                }
                Color C(int ring)
                {
                    if (shadow)
                        return new Color(.24f, .17f, .09f, ring == 2 ? 0 : .38f);
                    return ring == 0 ? new Color(.49f, .34f, .19f, .92f) : ring == 1 ? new Color(.76f, .59f, .37f, .95f) : new Color(.72f, .55f, .33f, 0);
                }
                void V(float angle, int ring)
                {
                    points[n] = P(angle, ring);
                    colors[n] = C(ring);
                    indices[n] = n;
                    n++;
                }
                V(a, band);
                V(a, band + 1);
                V(b, band + 1);
                V(a, band);
                V(b, band + 1);
                V(b, band);
            }
        var mesh = new Mesh { name = shadow ? "Close ground contact" : "Faceted displaced sand lip" };
        mesh.vertices = points;
        mesh.colors = colors;
        mesh.triangles = indices;
        mesh.RecalculateBounds();
        return mesh;
    }

    void OnDestroy()
    {
        if (debris != null)
            Destroy(debris);
    }
}
