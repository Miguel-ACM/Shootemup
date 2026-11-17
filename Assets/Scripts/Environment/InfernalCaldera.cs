using UnityEngine;
using System.Collections.Generic;

// Modeled basalt shelves over a molten caldera, with a continuous bed under the geometry.
public sealed class InfernalCaldera : MonoBehaviour
{
    readonly List<Transform> tiles = new List<Transform>();
    readonly List<Mesh> meshes = new List<Mesh>();
    Material lava, basalt, glow;
    float clock;
    void Start()
    {
        lava = new Material(Resources.Load<Shader>("CalderaLava"));
        basalt = new Material(Resources.Load<Shader>("CalderaBasalt"));
        var crust = Resources.Load<Texture2D>("CalderaCrust");
        basalt.SetTexture("_StoneMap", crust);
        lava.SetTexture("_CrustMap", crust);
        lava.SetTexture("_FlowMap", Resources.Load<Texture2D>("CalderaFlow"));
        var bed = new Mesh { name = "Continuous molten caldera" };
        bed.vertices = new[] { new Vector3(-16, -25), new Vector3(-16, 25), new Vector3(16, 25), new Vector3(16, -25) };
        bed.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        bed.RecalculateBounds();
        meshes.Add(bed);
        Add("Molten depth", bed, lava, transform, new Vector3(0, 0, 125), -80);
        for (int i = 0; i < 4; i++)
        {
            var root = new GameObject("Basalt shelf " + i).transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(0, (i - 1) * 20, 83);
            tiles.Add(root);
            int side = i % 2 == 0 ? -1 : 1;
            var mesh = Shelf(LevelRun.Seed, i, side, Erupting, out var craterSite, out var outerCraterSite);
            meshes.Add(mesh);
            Add("Layered volcanic rock", mesh, basalt, root, Vector3.zero, -45);
            var vent = Add("Terraced basalt crater", IndustrialMeshFactory.Volcano(), basalt, root, craterSite + Vector3.back * .015f, -39);
            vent.localScale = new Vector3(1, .85f, 1);
            var hazard = vent.gameObject.AddComponent<VolcanicVent>();
            hazard.phase = i * .55f;
            if (Erupting)
            {
                hazard.Eruption = true;
                Glow(vent);
                var added = Add("Erupting crater", IndustrialMeshFactory.Volcano(), basalt, root, outerCraterSite, -39);
                added.localScale = new Vector3(.9f, .78f, .9f);
                var addedVent = added.gameObject.AddComponent<VolcanicVent>();
                addedVent.phase = i * .55f + 1.7f;
                addedVent.Eruption = true;
                Glow(added);
            }
        }
        if (Erupting)
        {
            // Lift the caldera's darks so the shelves and volcanoes read clearly.
            SceneryGrade.Create(transform, "Eruption light", -36)
                .Multiply(Color.white, Color.white, 1)
                .Glow(new Color(.13f, .055f, .018f), new Color(.09f, .035f, .012f), 1, 1);
        }
    }

    bool Erupting => DistrictConditions.For(LevelId.Hell, LevelRun.Seed) == DistrictCondition.Eruption;

    // A molten pool of light around each erupting crater, flickering with the fire.
    void Glow(Transform vent)
    {
        if (glow == null)
            glow = SceneryLights.CreateMaterial();
        var lights = new SceneryLights();
        lights.Add(new Vector3(0, 0, -2), 1.9f, 1.7f, new Color(1, .42f, .12f, .55f), Random.value, SceneryLights.Kind.Fire);
        lights.Add(new Vector3(0, 0, -2.1f), .45f, .45f, new Color(1, .8f, .4f, .9f), Random.value, SceneryLights.Kind.Fire);
        var mesh = lights.Finish("Crater glow");
        meshes.Add(mesh);
        var go = new GameObject("Crater glow");
        go.transform.SetParent(vent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = glow;
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = -35;
    }

    static Transform Add(string label, Mesh mesh, Material material, Transform parent, Vector3 p, int order)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = p;
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
        if (lava == null)
            return;
        lava.SetFloat("_Flow", clock);
        var pilot = CombatVisuals.Player;
        float parallax = pilot != null ? -pilot.position.x * .08f : 0;
        foreach (var tile in tiles)
        {
            var p = tile.localPosition;
            p.x = parallax;
            p.y -= Time.deltaTime * 3.7f;
            if (p.y < -30)
                p.y += 80;
            tile.localPosition = p;
        }
    }

    static Mesh Shelf(int seed, int tile, int craterSide, bool eruption, out Vector3 craterSite, out Vector3 outerCraterSite)
    {
        var random = new System.Random(unchecked(seed + tile * 719));
        float R(float a, float b) => Mathf.Lerp(a, b, (float)random.NextDouble());
        var points = new List<Vector3>();
        var colors = new List<Color>();
        var triangles = new List<int>();
        void Face(Color c, params Vector3[] face)
        {
            c.a = 1;
            int start = points.Count;
            foreach (var p in face)
            {
                points.Add(p);
                colors.Add(c);
            }
            for (int j = 1; j < face.Length - 1; j++)
            {
                triangles.Add(start);
                triangles.Add(start + j);
                triangles.Add(start + j + 1);
            }
        }
        Vector3 Island(Vector3 p, float rx, float ry, int sides)
        {
            var rim = new Vector3[sides];
            var crown = new Vector3[sides];
            float height = R(.65f, 1.55f), phase = R(0, 6.28f);
            for (int i = 0; i < sides; i++)
            {
                float a = phase + i * Mathf.PI * 2 / sides, r = i % 4 == 0 ? R(.64f, .84f) : R(.91f, 1.12f);
                rim[i] = p + new Vector3(Mathf.Cos(a) * rx * r, Mathf.Sin(a) * ry * r, 0);
                crown[i] = p + new Vector3(Mathf.Cos(a) * rx * r * .94f - height * .13f, Mathf.Sin(a) * ry * r * .94f + height * .28f, -height + R(-.12f, .12f));
            }
            var middle = p + new Vector3(-height * .13f - rx * .05f, height * .28f + ry * .04f, -height - .10f);
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                float facing = .72f + .16f * Mathf.Sin(phase + i * Mathf.PI * 2 / sides);
                // Amber reflected undercuts, dark cliffs, and a broad cool-charcoal top.
                var footA = Vector3.Lerp(rim[i], crown[i], .20f);
                var footB = Vector3.Lerp(rim[j], crown[j], .20f);
                Face(new Color(.16f, .071f, .035f) * facing, rim[i], rim[j], footB, footA);
                Face(new Color(.065f, .063f, .069f) * facing, footA, footB, crown[j], crown[i]);
                Face(new Color(.112f, .109f, .115f) * R(.92f, 1.08f), crown[i], crown[j], middle);
            }
            return middle;
        }
        // Both banks are sampled in the same 80-unit periodic river. End rows
        // meet exactly as the four scroll tiles recycle, including the final join.
        float phaseOffset = (seed % 4093) * .017f;
        float BankX(float y, int side)
        {
            float t = Mathf.Repeat(tile * 20f + y + 10f, 80f) * (2 * Mathf.PI / 80f);
            float bend = .92f*Mathf.Sin(t+phaseOffset) + .47f*Mathf.Sin(2*t-phaseOffset);
            float width = 2.72f + .47f*Mathf.Sin(3*t+.7f) + .38f*Mathf.Cos(2*t+phaseOffset);
            float fracture = .21f*Mathf.Sin(12*t+side*1.3f) + .16f*Mathf.Cos(19*t+phaseOffset*side);
            return bend + side*(width+fracture);
        }
        Vector3 Bank(float y, int side, int ledge)
        {
            float x = BankX(y,side);
            float t = Mathf.Repeat(tile*20f+y+10f,80f) * (2*Mathf.PI/80f);
            float lip = .25f*Mathf.Sin(9*t+side*phaseOffset);
            float spur = Mathf.Pow(.5f+.5f*Mathf.Cos(5*t+side*1.8f+phaseOffset),4);
            switch (ledge)
            {
                case 0: return new Vector3(x-side*(.26f+.43f*spur),y,.31f);
                case 1: return new Vector3(x-side*.13f,y,-.06f);
                case 2: return new Vector3(x+side*(.18f+lip*.46f),y,-.42f);
                case 3: return new Vector3(x+side*(.59f+lip+.33f*spur),y,-.54f);
                case 4: return new Vector3(x+side*(.91f+lip+.76f*spur),y,-1.21f);
                case 5: return new Vector3(x+side*(1.94f+lip+.44f*spur),y,-1.32f);
                default: return new Vector3(side*14,y,-1.32f);
            }
        }
        Color[] strata = {
            new Color(.34f,.145f,.073f), new Color(.23f,.105f,.075f), new Color(.21f,.147f,.133f),
            new Color(.125f,.095f,.106f), new Color(.187f,.169f,.178f), new Color(.154f,.146f,.161f)
        };
        for (int side = -1; side <= 1; side += 2)
        {
            for (int row = 0; row < 16; row++)
            {
                float y = -10+row*1.25f;
                for (int ledge=0; ledge<strata.Length; ledge++)
                {
                    Vector3 a=Bank(y,side,ledge), b=Bank(y+1.25f,side,ledge);
                    Vector3 c=Bank(y+1.25f,side,ledge+1), d=Bank(y,side,ledge+1);
                    float face = Mathf.Clamp(.94f+side*(b.x-a.x)*.13f,.80f,1.08f);
                    Face(strata[ledge]*face,a,b,c,d);
                }
            }
            // Broken scoria at the foot of the cliffs; a few large plates share a fault.
            for (int seam=0; seam<2; seam++)
            {
                float y=R(-7.2f,6.4f);
                for (int plate=0; plate<3; plate++)
                {
                    float at=y+plate*.57f;
                    float x=BankX(at,side)-side*(.31f+.23f*plate);
                    Island(new Vector3(x,at,-.14f),R(.27f,.55f),R(.37f,.72f),7);
                }
            }
        }
        // Raised vent seats are anchored at their authored shores, including the
        // second ERUPTION vent. The main channel between them stays mostly exposed.
        var ventSite = new Vector3(craterSide*R(4.12f,4.47f), R(-.48f,.48f),-1.04f);
        craterSite=Island(ventSite,1.95f,1.72f,12);
        outerCraterSite=new Vector3(-craterSide*2.1f,-5.5f+craterSite.y*.2f,-1.02f);
        if (eruption)
            outerCraterSite=Island(outerCraterSite,1.48f,1.32f,11)+Vector3.back*.015f;
        float barY=R(3.2f,5.7f);
        float barX=BankX(barY,-craterSide)+craterSide*R(.85f,1.26f);
        Island(new Vector3(barX,barY,.08f),R(.62f,.96f),R(1.55f,2.12f),10);
        var mesh = new Mesh { name = "Basalt benches and welded scoria" };
        mesh.SetVertices(points);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    void OnDestroy()
    {
        void Release(Object item)
        {
            if (item == null)
                return;
            if (Application.isPlaying)
                Destroy(item);
            else
                DestroyImmediate(item);
        }
        Release(lava);
        Release(basalt);
        Release(glow);
        foreach (var mesh in meshes)
            Release(mesh);
    }
}
