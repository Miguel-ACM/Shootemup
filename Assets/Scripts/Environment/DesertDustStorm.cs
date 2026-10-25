using UnityEngine;
using UnityEngine.Rendering;

// Dust sweeps over the scenery. Combat render layers stay in front, including every hostile round.
public sealed class DesertDustStorm : MonoBehaviour
{
    struct Grain
    {
        public Vector2 p; public float speed, length, alpha;
    }

    readonly Material[] banks = new Material[2];
    Mesh plane, grains;
    Material grainMaterial;
    Grain[] particles;
    Vector3[] vertices;
    Color[] colors;
    float clock, seedPhase;
    public float Strength
    {
        get; private set;
    }

    void Start()
    {
        seedPhase = (LevelRun.Seed % 71) * .13f;
        plane = new Mesh { name = "Dust bank plane" };
        plane.vertices = new[] { new Vector3(-15, -20), new Vector3(15, -20), new Vector3(15, 20), new Vector3(-15, 20) };
        plane.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        plane.RecalculateBounds();
        for (int i = 0; i < 2; i++)
        {
            banks[i] = new Material(Resources.Load<Shader>("DesertDust"));
            banks[i].SetFloat("_Layer", i);
            banks[i].SetFloat("_Seed", seedPhase + i * 13);
            Add(i == 0 ? "Rolling sand front" : "Fast wind streamers", plane, banks[i], 42 - i * 6, -8 + i);
        }
        var rng = new System.Random(LevelRun.Seed + 3571);
        float R(float a, float b) => Mathf.Lerp(a, b, (float)rng.NextDouble());
        particles = new Grain[100];
        vertices = new Vector3[400];
        colors = new Color[400];
        var indices = new int[600];
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i] = new Grain { p = new Vector2(R(-15, 15), R(-20, 20)), speed = R(5, 11), length = R(.05f, .23f), alpha = R(.20f, .48f) };
            int k = i * 4, t = i * 6;
            indices[t] = k;
            indices[t + 1] = k + 2;
            indices[t + 2] = k + 1;
            indices[t + 3] = k;
            indices[t + 4] = k + 3;
            indices[t + 5] = k + 2;
        }
        grains = new Mesh { name = "Flying sand grains" };
        grains.MarkDynamic();
        grains.vertices = vertices;
        grains.colors = colors;
        grains.triangles = indices;
        grains.bounds = new Bounds(Vector3.zero, new Vector3(32, 42, 1));
        grainMaterial = new Material(Resources.Load<Shader>("LevelScenery"));
        Add("Windblown grains", grains, grainMaterial, 30, -6);
    }

    void Add(string name, Mesh mesh, Material material, float z, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0, 0, z);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = order;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        clock += dt;
        // Front, sustained gust, then a long clear interval. The opening gets several seconds of calm.
        float cycle = Mathf.Repeat(clock + seedPhase * .35f, 27);
        Strength = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(6, 10, cycle)) * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(17, 23, cycle)));
        foreach (var bank in banks)
            if (bank != null)
            {
                bank.SetFloat("_Clock", clock);
                bank.SetFloat("_Strength", Strength);
            }
        if (particles == null)
            return;
        for (int i = 0; i < particles.Length; i++)
        {
            var grain = particles[i];
            grain.p += new Vector2(grain.speed * (.45f + Strength), -.6f) * dt;
            grain.p.x = Mathf.Repeat(grain.p.x + 15, 30) - 15;
            grain.p.y = Mathf.Repeat(grain.p.y + 20, 40) - 20;
            particles[i] = grain;
            float length = grain.length * (.5f + Strength), thickness = .009f;
            int k = i * 4;
            var p = (Vector3)grain.p;
            vertices[k] = p + new Vector3(-length, -thickness);
            vertices[k + 1] = p + new Vector3(length, -thickness - .02f);
            vertices[k + 2] = p + new Vector3(length, thickness - .02f);
            vertices[k + 3] = p + new Vector3(-length, thickness);
            for (int j = 0; j < 4; j++)
                colors[k + j] = new Color(.86f, .70f, .43f, grain.alpha * Strength);
        }
        grains.vertices = vertices;
        grains.colors = colors;
    }

    void OnDestroy()
    {
        foreach (var bank in banks)
            if (bank != null)
                Destroy(bank);
        if (plane != null)
            Destroy(plane);
        if (grains != null)
            Destroy(grains);
        if (grainMaterial != null)
            Destroy(grainMaterial);
    }
}
