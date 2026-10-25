using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Runs as the PolygonBreakup test in Unity's Test Runner (Assets/Editor/Tests/LogicTests.cs).
public static class VerifyPolygonBreakup
{
    // Throws on the first failed check and returns the report otherwise.
    public static string RunChecks()
    {
        var report = new System.Text.StringBuilder();
        foreach (string path in new[] { "Enemy/enemy1", "Enemy/waver", "Enemy/Biggie", "Enemy/Bosses/stage1boss" })
        {
            var source = Object.Instantiate(Resources.Load<GameObject>(path));
            source.transform.SetPositionAndRotation(new Vector3(2, 3, 7), Quaternion.Euler(0, 0, 23));
            var model = source.GetComponent<PSXObjectVisual>().model;
            model.localRotation *= Quaternion.Euler(12, 9, -17);
            var originals = model.GetComponentsInChildren<MeshFilter>();
            Check(originals.All(f => f.sharedMesh.isReadable), path + " has unreadable model parts");
            int faces = originals.Sum(f => f.sharedMesh.triangles.Length / 3);
            var block = new MaterialPropertyBlock();
            block.SetColor("_Tint", new Color(.8f, .9f, 1, .15f));
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
                renderer.SetPropertyBlock(block);
            var debris = PSXPolygonDebris.Spawn(source);
            Check(debris != null, path + " generated no debris");
            var meshes = debris.GetComponentsInChildren<MeshFilter>();
            Check(meshes.Length == originals.Length, path + " lost an animated model part");
            Check(meshes.Sum(f => f.sharedMesh.triangles.Length / 6) == faces, path + " lost hull polygons");
            foreach (var renderer in debris.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.GetPropertyBlock(block);
                Check(block.GetColor("_Tint").a == 1, "Damage blink made wreckage transparent");
            }
            var before = meshes.Select(f => f.sharedMesh.vertices).ToArray();
            var advance = typeof(PSXPolygonDebris).GetMethod("Advance", BindingFlags.NonPublic | BindingFlags.Instance);
            advance.Invoke(debris, new object[] { 0f });
            for (int m = 0; m < meshes.Length; m++)
            {
                var initial = meshes[m].sharedMesh.vertices;
                Check(initial.Zip(before[m], (a, b) => Vector3.Distance(a, b)).All(d => d < .0001f), "Initial pose changed during fracture");
                Check(meshes[m].sharedMesh.uv.Length == initial.Length, "Hull UVs missing");
            }
            advance.Invoke(debris, new object[] { .9f });
            Check(meshes.Select((f, m) => f.sharedMesh.vertices.Zip(before[m], (a, b) => Vector3.Distance(a, b)).Max()).Max() > .5f,
                path + " plates did not fly apart");
            Check(debris.GetComponentsInChildren<Collider>().Length == 0 && debris.GetComponentsInChildren<Collider2D>().Length == 0,
                "Visual debris affects collisions");
            float lifetime = (float)typeof(PSXPolygonDebris).GetField("lifetime", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(debris);
            Check(lifetime <= (source.GetComponent<Boss>() != null ? 1.65f : .95f), "Wreckage lingers too long");
            advance.Invoke(debris, new object[] { lifetime * .8f });
            int remaining = VisibleFaces(meshes);
            Check(remaining > 0 && remaining < faces, path + " did not clear polygons in groups");
            advance.Invoke(debris, new object[] { lifetime });
            Check(VisibleFaces(meshes) == 0, path + " left visible polygons after cleanup");
            var allocated = meshes.Select(f => f.sharedMesh).ToArray();
            Object.DestroyImmediate(source);
            Check(meshes.All(f => f != null), "Debris was still parented to destroyed enemy");
            Object.DestroyImmediate(debris.gameObject);
            Check(allocated.All(mesh => mesh == null), "Debris leaked generated meshes");
            report.AppendLine("PASS " + path + ": " + faces + " textured faces, " + originals.Length + " model parts; pose, motion, blink, collision, grouped disappearance and cleanup checks (" + remaining + " faces at 80% lifetime).");
        }
        foreach (var type in new[] { typeof(ScoutInterceptor), typeof(AvalancheBomber), typeof(TunnelSawBoss), typeof(ThunderLanceBoss) })
            VerifyDistrictHull(type, report);
        return report.ToString();
    }

    static void VerifyDistrictHull(Type type, System.Text.StringBuilder report)
    {
        var source = DistrictEnemy.Spawn(type, new Vector3(2, 3, 7));
        PSXPolygonDebris debris = null;
        Mesh exhaustMesh = null;
        try
        {
            source.transform.rotation = Quaternion.Euler(0, 0, 23);
            source.Hull.localRotation = Quaternion.Euler(12, 9, -17);
            var hull = source.Hull.GetComponent<MeshFilter>();
            int faces = hull.sharedMesh.triangles.Length / 3;
            // A rendered child deliberately simulates the animated exhaust hierarchy.
            // It must never be included in the rigid hull breakup.
            var exhaust = new GameObject("Probe animated exhaust");
            exhaust.transform.SetParent(source.Hull, false);
            exhaust.AddComponent<MeshFilter>().sharedMesh = exhaustMesh = IndustrialMeshFactory.CreateFlame();
            exhaust.AddComponent<MeshRenderer>().sharedMaterial = CombatMaterials.Fleet;
            var block = new MaterialPropertyBlock();
            block.SetColor("_Tint", new Color(.8f, .9f, 1, .15f));
            source.Hull.GetComponent<MeshRenderer>().SetPropertyBlock(block);
            debris = PSXPolygonDebris.Spawn(source.gameObject);
            Check(debris != null, type.Name + " generated no district hull debris");
            var meshes = debris.GetComponentsInChildren<MeshFilter>();
            Check(meshes.Length == 1, type.Name + " fractured animated exhaust as solid wreckage");
            Check(meshes[0].sharedMesh.triangles.Length / 6 == faces, type.Name + " lost hull faces or included exhaust faces");
            meshes[0].GetComponent<MeshRenderer>().GetPropertyBlock(block);
            Check(block.GetColor("_Tint").a == 1, type.Name + " inherited an invisible damage blink");
            float lifetime = (float)typeof(PSXPolygonDebris).GetField("lifetime", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(debris);
            Check(Mathf.Approximately(lifetime, source is DistrictBoss ? 1.65f : .95f), type.Name + " uses the wrong boss debris budget");
            var before = meshes[0].sharedMesh.vertices;
            var advance = typeof(PSXPolygonDebris).GetMethod("Advance", BindingFlags.NonPublic | BindingFlags.Instance);
            advance.Invoke(debris, new object[] { 0f });
            Check(meshes[0].sharedMesh.vertices.Zip(before, (a, b) => Vector3.Distance(a, b)).All(d => d < .0001f), type.Name + " lost its rotated hull pose");
            advance.Invoke(debris, new object[] { .4f });
            Check(meshes[0].sharedMesh.vertices.Zip(before, (a, b) => Vector3.Distance(a, b)).Max() > .25f, type.Name + " did not break apart");
            advance.Invoke(debris, new object[] { lifetime });
            Check(VisibleFaces(meshes) == 0, type.Name + " did not clear its debris");
            var allocated = meshes[0].sharedMesh;
            Object.DestroyImmediate(source.gameObject);
            source = null;
            Check(meshes[0] != null, type.Name + " debris was parented to the destroyed hull");
            Object.DestroyImmediate(debris.gameObject);
            debris = null;
            Check(allocated == null, type.Name + " leaked its generated debris mesh");
            report.AppendLine("PASS " + type.Name + ": " + faces + " hull faces, exhaust excluded, rotated pose retained, correct boss budget, motion and cleanup.");
        }
        finally
        {
            if (source != null)
                Object.DestroyImmediate(source.gameObject);
            if (debris != null)
                Object.DestroyImmediate(debris.gameObject);
            if (exhaustMesh != null)
                Object.DestroyImmediate(exhaustMesh);
        }
    }

    static int VisibleFaces(MeshFilter[] filters)
    {
        int count = 0;
        foreach (var filter in filters)
        {
            var vertices = filter.sharedMesh.vertices;
            for (int v = 0; v < vertices.Length; v += 3)
                if (Vector3.Cross(vertices[v + 1] - vertices[v], vertices[v + 2] - vertices[v]).sqrMagnitude > 1e-12f)
                    count++;
        }
        return count;
    }

    static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
