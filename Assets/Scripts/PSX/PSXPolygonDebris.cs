using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Breaks the current model pose into textured polygon plates, without physics bodies.</summary>
[ExecuteAlways]
public sealed class PSXPolygonDebris : MonoBehaviour
{
    const int MaxBursts = 8;
    static readonly List<PSXPolygonDebris> active = new List<PSXPolygonDebris>();
    readonly List<Batch> batches = new List<Batch>();
    float started, lifetime;
    bool boss;

    sealed class Plate
    {
        public Vector3 centre, velocity, axis;
        public float spin, delay, clearAt, shrink;
        public Quaternion rotation;
        public Vector3 position;
    }

    sealed class Batch
    {
        public Mesh mesh;
        public Vector3[] rest, normals, vertices, rotatedNormals;
        public int[] plates;
        public Plate[] pieces;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { active.Clear(); }

    public static PSXPolygonDebris Spawn(GameObject source)
    {
        var visual = source.GetComponent<PSXObjectVisual>();
        var ship = source.GetComponent<PSXShipVisual>();
        Transform model = visual != null ? visual.model : ship != null ? ship.Model : null;
        if (model == null) return null;
        bool isBoss = source.GetComponent<Boss>() != null;
        // Keep a busy wave bounded, reserving the longest-lived breakup for the boss.
        active.RemoveAll(effect => effect == null);
        if (active.Count >= MaxBursts)
        {
            var oldest = active.Find(effect => !effect.boss);
            if (oldest == null) return null;
            active.Remove(oldest);
            oldest.gameObject.SetActive(false);
            Destroy(oldest.gameObject);
        }
        var go = new GameObject(isBoss ? "Boss polygon wreckage" : "Hull polygon wreckage");
        go.transform.position = model.position;
        var effect = go.AddComponent<PSXPolygonDebris>();
        effect.boss = isBoss;
        effect.started = Time.time;
        effect.lifetime = isBoss ? 1.65f : .95f;
        var renderers = model.GetComponentsInChildren<MeshRenderer>();
        Bounds bounds = new Bounds(model.position, Vector3.zero);
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        // Larger cells keep neighbouring polygons attached as a few readable hull chunks.
        float cell = Mathf.Max(.08f, Mathf.Max(bounds.size.x, bounds.size.y) / (isBoss ? 8f : 4f));
        foreach (var renderer in renderers)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable) continue;
            effect.Build(filter, renderer, bounds.center, cell);
        }
        if (effect.batches.Count == 0) { Destroy(go); return null; }
        active.Add(effect);
        return effect;
    }

    void Build(MeshFilter filter, MeshRenderer source, Vector3 blastCentre, float cell)
    {
        var original = filter.sharedMesh;
        var positions = original.vertices;
        var normals = original.normals;
        var uv = original.uv;
        var colors = original.colors;
        var rest = new List<Vector3>();
        var faceNormals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var vertexColors = new List<Color>();
        var ids = new List<int>();
        var pieces = new List<Plate>();
        var cells = new Dictionary<Vector3Int, int>();
        var submeshes = new List<int[]>();
        var matrix = transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
        var normalMatrix = matrix.inverse.transpose;
        Vector3 centre = transform.InverseTransformPoint(blastCentre);
        for (int sub = 0; sub < original.subMeshCount; sub++)
        {
            if (original.GetTopology(sub) != MeshTopology.Triangles) { submeshes.Add(new int[0]); continue; }
            var triangles = original.GetTriangles(sub);
            var indices = new List<int>(triangles.Length * 2);
            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 a = matrix.MultiplyPoint3x4(positions[triangles[t]]);
                Vector3 b = matrix.MultiplyPoint3x4(positions[triangles[t + 1]]);
                Vector3 c = matrix.MultiplyPoint3x4(positions[triangles[t + 2]]);
                Vector3 midpoint = (a + b + c) / 3;
                var key = Vector3Int.FloorToInt((midpoint - centre) / cell);
                if (!cells.TryGetValue(key, out int id))
                {
                    id = pieces.Count;
                    cells.Add(key, id);
                    Vector3 outward = midpoint - centre;
                    outward.z *= .25f;
                    Vector3 direction = (outward.normalized + Random.onUnitSphere * .45f).normalized;
                    uint cleanupGroup = (uint)(key.x * 73856093 ^ key.y * 19349663 ^ key.z * 83492791) % 3;
                    pieces.Add(new Plate {
                        centre = midpoint,
                        velocity = direction * Random.Range(boss ? 2.6f : 1.4f, boss ? 6.2f : 3.6f),
                        axis = Random.onUnitSphere, spin = Random.Range(-260f, 260f),
                        delay = boss ? Random.Range(0f, .18f) : 0f,
                        // Three shared cleanup beats clear whole groups together, across model parts.
                        clearAt = lifetime * (.55f + .225f * cleanupGroup)
                    });
                }
                int first = rest.Count;
                Vector3 face = Vector3.Cross(b - a, c - a).normalized;
                for (int v = 0; v < 3; v++)
                {
                    int index = triangles[t + v];
                    rest.Add(matrix.MultiplyPoint3x4(positions[index]));
                    faceNormals.Add(normals.Length == positions.Length ? normalMatrix.MultiplyVector(normals[index]).normalized : face);
                    uvs.Add(uv.Length == positions.Length ? uv[index] : Vector2.zero);
                    vertexColors.Add(colors.Length == positions.Length ? colors[index] : Color.white);
                    ids.Add(id);
                }
                // Hull plates remain visible when their unmodelled inside turns toward the camera.
                indices.Add(first); indices.Add(first + 1); indices.Add(first + 2);
                indices.Add(first + 2); indices.Add(first + 1); indices.Add(first);
            }
            submeshes.Add(indices.ToArray());
        }
        if (rest.Count == 0) return;
        var mesh = new Mesh { name = "Fractured " + original.name, indexFormat = IndexFormat.UInt32 };
        mesh.MarkDynamic();
        mesh.SetVertices(rest); mesh.SetNormals(faceNormals); mesh.SetUVs(0, uvs); mesh.SetColors(vertexColors);
        mesh.subMeshCount = submeshes.Count;
        for (int sub = 0; sub < submeshes.Count; sub++) mesh.SetTriangles(submeshes[sub], sub);
        mesh.RecalculateBounds();
        var go = new GameObject(original.name + " plates");
        go.transform.SetParent(transform, false);
        go.layer = source.gameObject.layer;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = source.sharedMaterials;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        var properties = new MaterialPropertyBlock();
        source.GetPropertyBlock(properties);
        var tint = properties.HasColor("_Tint") ? properties.GetColor("_Tint") : Color.white;
        tint.a = 1; // Lethal damage may occur during the sprite's damage blink.
        properties.SetColor("_Tint", tint);
        renderer.SetPropertyBlock(properties);
        batches.Add(new Batch { mesh = mesh, rest = rest.ToArray(), normals = faceNormals.ToArray(),
            vertices = rest.ToArray(), rotatedNormals = faceNormals.ToArray(), plates = ids.ToArray(), pieces = pieces.ToArray() });
    }

    void Update()
    {
        float age = Time.time - started;
        if (age >= lifetime) { Destroy(gameObject); return; }
        Advance(age);
    }

    void Advance(float age)
    {
        foreach (var batch in batches)
        {
            foreach (var plate in batch.pieces)
            {
                float t = Mathf.Max(0, age - plate.delay);
                plate.rotation = Quaternion.AngleAxis(plate.spin * t, plate.axis);
                plate.shrink = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(plate.clearAt - .16f, plate.clearAt, age));
                // Drag slows the initial blast; gravity pulls the wreckage down the playfield.
                plate.position = plate.centre + plate.velocity * ((1 - Mathf.Exp(-t * .7f)) / .7f)
                    + Vector3.down * (.65f * t * t);
            }
            for (int v = 0; v < batch.vertices.Length; v++)
            {
                var plate = batch.pieces[batch.plates[v]];
                batch.vertices[v] = plate.position + plate.rotation * (batch.rest[v] - plate.centre) * plate.shrink;
                batch.rotatedNormals[v] = plate.rotation * batch.normals[v];
            }
            batch.mesh.vertices = batch.vertices;
            batch.mesh.normals = batch.rotatedNormals;
            batch.mesh.RecalculateBounds();
        }
    }

    void OnDestroy()
    {
        active.Remove(this);
        foreach (var batch in batches)
            if (Application.isPlaying) Destroy(batch.mesh); else DestroyImmediate(batch.mesh);
    }
}
