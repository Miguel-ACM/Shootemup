using UnityEngine;
using System.Collections.Generic;

// Seeded, batched scenery. Decoration stays behind the combat silhouettes.
public sealed class DepthBackdrop : MonoBehaviour
{
    public bool infernal;
    Mesh plane;
    Material sky, scenery;
    readonly List<Mesh> owned = new List<Mesh>();
    readonly List<Transform> sections = new List<Transform>();
    ParticleSystem motes;
    float clock, nextMote, nextWake;
    int wakeCursor;
    void Start()
    {
        sky = new Material(Resources.Load<Shader>("DepthWorld"));
        sky.SetFloat("_Hell", infernal ? 1 : 0);
        sky.SetFloat("_Seed", LevelRun.Seed % 997);
        plane = new Mesh { name = infernal ? "Volcanic abyss" : "Deep blue ocean" };
        plane.vertices = new[] { new Vector3(-16, -24), new Vector3(-16, 24), new Vector3(16, 24), new Vector3(16, -24) };
        plane.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        plane.RecalculateBounds();
        Add("Distant depth", plane, sky, new Vector3(0, 0, 125), -80);
        scenery = new Material(Resources.Load<Shader>("LevelScenery"));
        for (int i = 0; i < 4; i++)
        {
            var mesh = BuildSection(LevelRun.Seed + i * 193, infernal);
            owned.Add(mesh);
            sections.Add(Add(infernal ? "Obsidian spires and broken bones" : "Reef, kelp and sunken ribs", mesh, scenery, new Vector3(0, (i - 1) * 20, 75), -45));
            if (infernal)
            {
                var crater = Add("Environmental volcano", IndustrialMeshFactory.Volcano(), CombatMaterials.Fleet, Vector3.zero, -39);
                crater.SetParent(sections[i], false);
                crater.localPosition = new Vector3((i % 2 == 0 ? -1 : 1) * 3.65f, (i % 2 == 0 ? 1 : -1) * 3.8f, -2);
                crater.localScale = Vector3.one * .85f;
                crater.gameObject.AddComponent<VolcanicVent>().phase = i * .67f;
            }
        }
        if (!infernal)
            gameObject.AddComponent<OceanLife>();
        motes = PSXImpactEffects.Create(transform, infernal ? "Drifting cinders" : "Rising air bubbles", 180, true);
        var renderer = motes.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = -20;
    }

    Transform Add(string label, Mesh mesh, Material material, Vector3 position, int order)
    {
        var go = new GameObject(label);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = position;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = order;
        return go.transform;
    }

    void Update()
    {
        clock += Time.deltaTime;
        if (sky == null)
            return;
        sky.SetFloat("_Clock", clock);
        var pilot = CombatVisuals.Player;
        float offset = pilot != null ? -pilot.position.x * .09f : 0;
        foreach (var section in sections)
        {
            var p = section.localPosition;
            p.x = offset;
            p.y -= Time.deltaTime * (infernal ? 3.7f : 2.6f);
            if (p.y < -30)
                p.y += 80;
            section.localPosition = p;
        }
        if (infernal)
            return; // Fire is attached to craters; no random bright red points across the arena.
        if (clock >= nextWake)
        {
            nextWake = clock + .12f;
            int index = 0;
            int count = AEnemy.Live.Count;
            if (count > 0)
            {
                int selected = wakeCursor++ % count;
                foreach (var enemy in AEnemy.Live)
                {
                    if (index++ != selected || enemy == null || !enemy.isActiveAndEnabled)
                        continue;
                    var bubble = new ParticleSystem.EmitParams();
                    bubble.position = enemy.transform.position + new Vector3(Random.Range(-.18f, .18f), .35f, 0);
                    bubble.velocity = new Vector3(Random.Range(-.12f, .12f), Random.Range(.35f, .7f), 0);
                    bubble.startSize3D = Vector3.one * Random.Range(.04f, .09f);
                    bubble.startLifetime = Random.Range(.65f, 1.1f);
                    bubble.startColor = new Color(.45f, .75f, .8f, .3f);
                    motes.Emit(bubble, 1);
                    break;
                }
            }
        }
        if (clock < nextMote)
            return;
        nextMote = clock + .10f;
        var e = new ParticleSystem.EmitParams();
        e.position = new Vector3(Random.Range(-6, 6), Random.Range(-11, 11), 40);
        e.velocity = new Vector3(Random.Range(-.25f, .25f), infernal ? 1.4f : .85f, 0);
        e.startSize = Random.Range(.035f, .11f);
        e.startLifetime = 2.7f;
        e.startColor = infernal ? new Color(.76f, .26f, .08f, .43f) : new Color(.43f, .75f, .86f, .27f);
        motes.Emit(e, 1);
    }

    static Mesh BuildSection(int seed, bool hell)
    {
        var random = new System.Random(seed);
        float R(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());
        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var triangles = new List<int>();
        void Face(Color c, params Vector3[] points)
        {
            int n = vertices.Count;
            foreach (var p in points)
            {
                vertices.Add(p);
                colors.Add(c);
            }
            for (int j = 1; j < points.Length - 1; j++)
            {
                triangles.Add(n);
                triangles.Add(n + j);
                triangles.Add(n + j + 1);
            }
        }
        void Spire(Vector3 p, float radius, float height, Color color)
        {
            var top = p + new Vector3(height * .16f, height * .22f, -height);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3, b = (i + 1) * Mathf.PI / 3;
                var u = p + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0);
                var v = p + new Vector3(Mathf.Cos(b) * radius, Mathf.Sin(b) * radius, 0);
                if (hell)
                    Face(color * (.64f + i * .07f), u, v, top);
                else
                {
                    var c = Vector3.Lerp(top, u, .38f);
                    var d = Vector3.Lerp(top, v, .38f);
                    Face(color * (.77f + i * .035f), u, v, d, c);
                    Face(color * .92f, c, d, top);
                }
            }
        }
        void Branch(Vector3 p, float length, float width, Color c, float angle)
        {
            var d = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0);
            var side = new Vector3(d.y, -d.x, 0) * width;
            Face(c, p - side, p + side, p + d * length + side * .18f, p + d * length - side * .18f);
        }
        for (int i = 0; i < 24; i++)
        {
            float side = i % 2 == 0 ? -1 : 1;
            var p = new Vector3(side * R(3.2f, 7.8f), R(-9.7f, 9.7f), R(-.8f, .8f));
            var rock = hell ? new Color(.16f, .095f, .09f) : new Color(.09f, .24f, .26f);
            Spire(p, R(.45f, 1.3f), hell ? R(1.1f, 3.4f) : R(.45f, 1.4f), rock);
            if (hell)
            {
                if (i % 3 == 0)
                    for (int j = 0; j < 4; j++)
                        Spire(p + new Vector3(j * .22f, .16f, -.2f), .10f, .65f, new Color(.45f, .34f, .25f));
            }
            else
            {
                var kelp = new Color(.08f, .32f, .28f);
                for (int j = 0; j < 4; j++)
                {
                    var stem = p + new Vector3((j - 1.5f) * .18f, 0, -.6f);
                    float length = R(.7f, 1.65f), bend = R(-.45f, .45f);
                    Branch(stem, length, .055f, kelp, bend);
                    for (int leaf = 1; leaf < 4; leaf++)
                        Branch(stem + new Vector3(bend * leaf * .21f, leaf * length / 4, 0), .36f, .10f, kelp * 1.25f, (leaf % 2 == 0 ? 1 : -1) * 1.1f);
                }
                if (i % 2 == 0)
                {
                    var coral = i % 4 == 0 ? new Color(.47f, .23f, .26f) : new Color(.35f, .27f, .48f);
                    for (int j = 0; j < 5; j++)
                    {
                        float a = (j - 2) * .30f;
                        var root = p + new Vector3((j - 2) * .09f, .12f, -.85f);
                        float length = R(.55f, 1.05f);
                        Branch(root, length, .07f, coral, a);
                        for (int twig = 1; twig < 4; twig++)
                            for (int s = -1; s <= 1; s += 2)
                                Branch(root + new Vector3(Mathf.Sin(a) * length * twig / 4, Mathf.Cos(a) * length * twig / 4, 0), .24f, .045f, coral * 1.17f, a + s * .68f);
                    }
                }
            }
        }
        // Sparse sunken hull ribs or fossil arches appear at the edges, not in the dodge lanes.
        for (int side = -1; side <= 1; side += 2)
            for (int rib = 0; rib < 6; rib++)
            {
                float y = -3 + rib * .52f, x = side * (4.5f + Mathf.Sin(rib * Mathf.PI / 5) * .65f);
                Branch(new Vector3(x, y, -1), .8f, .08f, hell ? new Color(.34f, .27f, .22f) : new Color(.17f, .30f, .30f), -side * .65f);
            }
        var mesh = new Mesh { name = hell ? "Faceted volcanic shores" : "Reef garden and wreck ribs" };
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    void OnDestroy()
    {
        if (plane != null)
            Destroy(plane);
        if (sky != null)
            Destroy(sky);
        if (scenery != null)
            Destroy(scenery);
        foreach (var mesh in owned)
            if (mesh != null)
                Destroy(mesh);
    }
}
