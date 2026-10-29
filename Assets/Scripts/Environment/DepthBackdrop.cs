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
        if (!hell)
            return BuildReefSection(seed);

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
        void Branch(Vector3 p, float length, float width, Color c, float angle, bool solid = false)
        {
            var d = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0);
            var side = new Vector3(d.y, -d.x, 0) * width;
            var tip = p + d * length;
            if (solid)
            {
                var ridge = Vector3.forward * -width * .8f;
                Face(c * .78f, p - side, p + ridge, tip + ridge * .18f, tip - side * .18f);
                Face(c, p + ridge, p + side, tip + side * .18f, tip + ridge * .18f);
            }
            else
                Face(c, p - side, p + side, tip + side * .18f, tip - side * .18f);
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
                        Branch(root, length, .07f, coral, a, true);
                        for (int twig = 1; twig < 4; twig++)
                            for (int s = -1; s <= 1; s += 2)
                                Branch(root + new Vector3(Mathf.Sin(a) * length * twig / 4, Mathf.Cos(a) * length * twig / 4, 0), .24f, .045f, coral * 1.17f, a + s * .68f, true);
                    }
                }
            }
        }
        // Sparse sunken hull ribs or fossil arches appear at the edges, not in the dodge lanes.
        for (int side = -1; side <= 1; side += 2)
            for (int rib = 0; rib < 6; rib++)
            {
                float y = -3 + rib * .52f, x = side * (4.5f + Mathf.Sin(rib * Mathf.PI / 5) * .65f);
                var ribColor = hell ? new Color(.34f, .27f, .22f) : new Color(.17f, .30f, .30f);
                var root = new Vector3(x, y, -1);
                float angle = -side * .65f;
                var joint = root + new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0) * .48f;
                Branch(root, .48f, .08f, ribColor, angle, true);
                Branch(joint, .38f, .065f, ribColor * .9f, angle - side * .38f, true);
            }
        var mesh = new Mesh { name = hell ? "Faceted volcanic shores" : "Reef garden and wreck ribs" };
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh BuildReefSection(int seed)
    {
        var random = new System.Random(seed);
        float R(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
        var vertices = new List<Vector3>();
        var colors = new List<Color>();
        var triangles = new List<int>();
        void Face(Color color, params Vector3[] points)
        {
            int first = vertices.Count;
            foreach (var point in points)
            {
                vertices.Add(point);
                colors.Add(color);
            }
            for (int i = 1; i < points.Length - 1; i++)
            {
                triangles.Add(first);
                triangles.Add(first + i);
                triangles.Add(first + i + 1);
            }
        }
        void Ridge(Vector3 a, Vector3 b, float wa, float wb, Color color)
        {
            var direction = (b - a).normalized;
            var side = new Vector3(direction.y, -direction.x, 0);
            var crest = Vector3.back * Mathf.Min(wa, wb) * .7f;
            Face(color * .74f, a - side * wa, a + crest, b + crest, b - side * wb);
            Face(color, a + crest, a + side * wa, b + side * wb, b + crest);
        }
        void Rock(Vector3 p, float radius, float height, Color color)
        {
            var crown = p + new Vector3(radius * .14f, radius * .08f, -height);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3;
                float b = (i + 1) * Mathf.PI / 3;
                var u = p + new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius * .72f, 0);
                var v = p + new Vector3(Mathf.Cos(b) * radius, Mathf.Sin(b) * radius * .72f, 0);
                var cu = Vector3.Lerp(crown, u, .43f);
                var cv = Vector3.Lerp(crown, v, .43f);
                Face(color * (.72f + .055f * i), u, v, cv, cu);
                Face(color * (.91f + .025f * i), cu, cv, crown);
            }
        }
        void Blade(Vector3 root, Vector3 tip, float width, Color color)
        {
            var direction = (tip - root).normalized;
            var side = new Vector3(direction.y, -direction.x, 0) * width;
            var mid = Vector3.Lerp(root, tip, .52f) + Vector3.back * width * .35f;
            Face(color * .76f, root, mid - side, tip, mid);
            Face(color, root, mid, tip, mid + side);
        }
        void Kelp(Vector3 root, float height, float bend, Color color)
        {
            var middle = root + new Vector3(bend * .34f, height * .48f, -.08f);
            var tip = root + new Vector3(bend, height, -.16f);
            Ridge(root, middle, .055f, .042f, color * .76f);
            Ridge(middle, tip, .042f, .012f, color * .9f);
            for (int i = 0; i < 4; i++)
            {
                float t = .18f + i * .19f;
                var joint = t < .48f ? Vector3.Lerp(root, middle, t / .48f) : Vector3.Lerp(middle, tip, (t - .48f) / .52f);
                float side = i % 2 == 0 ? -1 : 1;
                var end = joint + new Vector3(side * (.31f + height * .10f), .20f + i * .035f, -.05f);
                Blade(joint, end, .11f, color * (1.08f + i * .04f));
            }
        }
        void BranchCoral(Vector3 root, float height, Color color)
        {
            var fork = root + new Vector3(0, height * .48f, -.12f);
            var top = root + new Vector3(.04f, height, -.18f);
            Ridge(root, fork, .12f, .09f, color * .86f);
            Ridge(fork, top, .09f, .035f, color);
            for (int side = -1; side <= 1; side += 2)
            {
                var elbow = fork + new Vector3(side * height * .27f, height * .13f, -.04f);
                var end = elbow + new Vector3(side * height * .12f, height * .34f, -.04f);
                Ridge(fork, elbow, .075f, .05f, color * .92f);
                Ridge(elbow, end, .05f, .022f, color * 1.12f);
                Ridge(elbow, elbow + new Vector3(-side * height * .12f, height * .23f, -.06f), .04f, .015f, color * 1.07f);
            }
        }
        void FanCoral(Vector3 root, float size, Color color)
        {
            var hub = root + new Vector3(0, size * .32f, -.08f);
            Ridge(root, hub, .09f, .06f, color * .75f);
            for (int i = 0; i < 5; i++)
            {
                float angle = (i - 2) * .34f;
                var end = hub + new Vector3(Mathf.Sin(angle) * size * .68f, Mathf.Cos(angle) * size * .68f, -.08f);
                Ridge(hub, end, .045f, .012f, color * (1 + i * .035f));
                if (i < 4)
                {
                    float next = (i - 1) * .34f;
                    var nextEnd = hub + new Vector3(Mathf.Sin(next) * size * .68f, Mathf.Cos(next) * size * .68f, -.08f);
                    Face(color * .72f, Vector3.Lerp(hub, end, .34f), end, nextEnd, Vector3.Lerp(hub, nextEnd, .34f));
                }
            }
        }
        void TubeCoral(Vector3 root, float height, float radius, Color color)
        {
            var top = root + new Vector3(radius * .22f, height * .28f, -height);
            var inset = top + Vector3.forward * .04f;
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3;
                float b = (i + 1) * Mathf.PI / 3;
                var u = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                var v = new Vector3(Mathf.Cos(b), Mathf.Sin(b), 0);
                Face(color * (.72f + .06f * i), root + u * radius * .65f, root + v * radius * .65f, top + v * radius, top + u * radius);
                Face(color * 1.18f, top + u * radius, top + v * radius, inset + v * radius * .58f, inset + u * radius * .58f);
                Face(color * .38f, inset, inset + u * radius * .58f, inset + v * radius * .58f);
            }
        }
        void BrainCoral(Vector3 root, float size, Color color)
        {
            Rock(root, size, size * .68f, color * .85f);
            for (int i = -2; i <= 2; i++)
            {
                float x = i * size * .24f;
                float span = size * Mathf.Sqrt(Mathf.Max(0, .7f - x * x / (size * size)));
                var a = root + new Vector3(x, -span * .55f, -size * .55f);
                var b = root + new Vector3(x + size * .09f, 0, -size * .71f);
                var c = root + new Vector3(x - size * .04f, span * .55f, -size * .55f);
                Ridge(a, b, .025f, .025f, color * 1.18f);
                Ridge(b, c, .025f, .018f, color * 1.12f);
            }
        }
        for (int i = 0; i < 24; i++)
        {
            float side = i % 2 == 0 ? -1 : 1;
            var p = new Vector3(side * R(4.45f, 7.4f), R(-8.35f, 8.35f), R(-.4f, .4f));
            float radius = R(.42f, .85f);
            var rock = new Color(R(.08f, .13f), R(.19f, .25f), R(.21f, .28f));
            Rock(p, radius, R(.28f, .75f), rock);
            if (i % 3 == 0)
                Rock(p + new Vector3(side * .55f, -.22f, -.1f), radius * .38f, .22f, rock * 1.14f);
            if (i % 3 != 1)
            {
                var kelpColor = new Color(R(.07f, .12f), R(.28f, .36f), R(.22f, .29f));
                for (int j = 0; j < 2; j++)
                    Kelp(p + new Vector3((j - .5f) * .28f, -.08f, -.48f), R(.75f, 1.25f), R(-.30f, .30f), kelpColor);
            }
            if (i % 3 != 2)
            {
                var root = p + new Vector3(0, .06f, -.68f);
                int species = (i / 3 + random.Next(4)) % 4;
                if (species == 0)
                    BranchCoral(root, R(.65f, 1.05f), new Color(.52f, .25f, .27f));
                else if (species == 1)
                    FanCoral(root, R(.65f, .95f), new Color(.46f, .29f, .46f));
                else if (species == 2)
                    BrainCoral(root, R(.32f, .48f), new Color(.48f, .37f, .23f));
                else
                {
                    var coral = new Color(.30f, .43f, .42f);
                    TubeCoral(root, R(.35f, .55f), .18f, coral);
                    TubeCoral(root + new Vector3(.27f, -.08f, .06f), R(.25f, .4f), .13f, coral * .9f);
                    TubeCoral(root + new Vector3(-.24f, .06f, .04f), R(.25f, .42f), .14f, coral * 1.08f);
                }
            }
        }
        // Each wreck stays within one section and leaves the central lane clear.
        for (int side = -1; side <= 1; side += 2)
        {
            float y = R(-5.5f, 4.5f);
            float x = side * R(5.0f, 6.2f);
            var timber = new Color(.19f, .28f, .27f);
            var keelA = new Vector3(x, y - 1.15f, -.65f);
            var keelB = new Vector3(x + side * .15f, y + 1.15f, -.65f);
            Ridge(keelA, keelB, .11f, .08f, timber * .8f);
            for (int rib = 0; rib < 5; rib++)
            {
                var root = Vector3.Lerp(keelA, keelB, (rib + .5f) / 5);
                float length = .65f + .25f * Mathf.Sin((rib + 1) * Mathf.PI / 6);
                var joint = root + new Vector3(side * length * .55f, .08f, -.25f);
                var tip = joint + new Vector3(side * length * .45f, .08f, -.32f);
                Ridge(root, joint, .09f, .075f, timber);
                Ridge(joint, tip, .075f, .035f, timber * 1.13f);
            }
            var a = keelA + new Vector3(side * .16f, .2f, -.04f);
            var b = keelA + new Vector3(side * .70f, .34f, -.20f);
            var c = keelA + new Vector3(side * .75f, 1.20f, -.22f);
            var d = keelA + new Vector3(side * .25f, 1.48f, -.06f);
            Face(timber * .65f, a, b, c, d);
            Ridge(a, d, .035f, .025f, timber * 1.08f);
        }
        var mesh = new Mesh { name = "Reef garden and sunken wrecks" };
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
