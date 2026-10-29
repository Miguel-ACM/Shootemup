using System.Collections.Generic;
using UnityEngine;

// Distance-based following preserves segment spacing through turns and at changing speed.
public sealed class SandSpine
{
    readonly List<Vector3> trail = new List<Vector3>();
    readonly float spacing;
    public SandSpine(Vector3 head, float spacing, int count)
    {
        this.spacing = spacing;
        for (int i = 0; i < count * 20 + 40; i++)
            trail.Add(head + Vector3.up * i * .08f);
    }

    // Lay the whole body out in a straight line behind the head, e.g. offscreen
    // before a new pass, so nothing is ever stretched across the arena.
    public void Reset(Vector3 head, Vector3 back)
    {
        trail.Clear();
        for (int i = 0; i < 480; i++)
            trail.Add(head + back.normalized * i * .06f);
    }

    public void Follow(Vector3 head, Transform[] sections)
    {
        if (Vector3.Distance(head, trail[0]) > .06f)
            trail.Insert(0, head);
        if (trail.Count > 480)
            trail.RemoveRange(480, trail.Count - 480);
        Vector3 previous = head;
        float travelled = 0;
        int next = 0;
        for (int i = 0; i < trail.Count && next < sections.Length; i++)
        {
            Vector3 p = trail[i];
            float d = Vector3.Distance(previous, p);
            while (next < sections.Length && travelled + d >= (next + 1) * spacing)
            {
                var section = sections[next];
                if (section != null)
                {
                    section.position = Vector3.Lerp(previous, p, ((next + 1) * spacing - travelled) / Mathf.Max(.0001f, d));
                    Vector2 tangent = previous - p;
                    if (tangent.sqrMagnitude > .001f)
                        section.rotation = Quaternion.FromToRotation(Vector3.down, tangent);
                }
                next++;
            }
            travelled += d;
            previous = p;
        }
    }

    public static Transform Model(string name, Transform parent, Mesh mesh, float scale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localScale = Vector3.one * scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = CombatMaterials.Fleet;
        r.sortingLayerName = "Enemy";
        return go.transform;
    }
}
