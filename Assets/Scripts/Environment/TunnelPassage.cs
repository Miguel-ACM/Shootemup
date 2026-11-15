using System.Collections.Generic;
using UnityEngine;

// Architectural routes anchored to the scrolling tunnel rather than the camera.
// Every layout starts and ends on the same centred opening, so any layout joins any other:
// a passage begun while another is still in view is appended to its end as one continuous run.
[DefaultExecutionOrder(-200)]
public sealed class TunnelPassage : MonoBehaviour
{
    sealed class Segment
    {
        public int layout;
        public float start;
        public CombatWall[] walls;
        public Transform model;
    }

    static TunnelPassage current;
    readonly List<Segment> segments = new List<Segment>();
    float travel;
    public const int LayoutCount = 12;
    const float Step = 9.5f;
    // (lane centre, half-width). Sloping joins give advance sight of each bend.
    static readonly Vector2[][] routes =
    {
        new[] { new Vector2(0, 2.5f), new Vector2(0, 2.1f), new Vector2(.4f, 1.85f), new Vector2(.4f, 2.7f), new Vector2(-.4f, 2.7f), new Vector2(-.4f, 1.85f), new Vector2(0, 2.5f) },
        new[] { new Vector2(0, 2.5f), new Vector2(-1.35f, 2.05f), new Vector2(-1.35f, 2.05f), new Vector2(1.35f, 2.05f), new Vector2(1.35f, 2.05f), new Vector2(-1.35f, 2.05f), new Vector2(-1.35f, 2.05f), new Vector2(0, 2.5f) },
        new[] { new Vector2(0, 2.5f), new Vector2(1.45f, 2.1f), new Vector2(1.45f, 1.9f), new Vector2(-1.45f, 2.1f), new Vector2(-1.45f, 2.7f), new Vector2(1.45f, 2.7f), new Vector2(1.45f, 1.9f), new Vector2(-1.45f, 2.1f), new Vector2(-1.45f, 2.1f), new Vector2(0, 2.5f) },
        new[] { new Vector2(0, 2.5f), new Vector2(-1.1f, 2.2f), new Vector2(-1.1f, 2.2f), new Vector2(1.1f, 2.2f), new Vector2(1.1f, 2.2f), new Vector2(-1.1f, 2.2f), new Vector2(-1.1f, 2.2f), new Vector2(0, 2.5f) },
        new[] { new Vector2(0, 2.5f), new Vector2(1.45f, 1.9f), new Vector2(1.45f, 1.9f), new Vector2(0, 3.3f), new Vector2(-1.45f, 1.9f), new Vector2(-1.45f, 1.9f), new Vector2(0, 3.3f), new Vector2(1.45f, 1.9f), new Vector2(0, 2.5f) },
        new[] { new Vector2(0, 2.5f), new Vector2(-1.5f, 2.1f), new Vector2(-1.5f, 1.85f), new Vector2(1.5f, 2.1f), new Vector2(1.5f, 2.8f), new Vector2(-1.5f, 2.8f), new Vector2(-1.5f, 1.85f), new Vector2(1.5f, 2.1f), new Vector2(1.5f, 2.1f), new Vector2(-1.5f, 2.1f), new Vector2(0, 2.5f) },
        // 7 Slalom: a quick left-right weave through a steady gap.
        new[] { new Vector2(0, 2.5f), new Vector2(1.55f, 1.95f), new Vector2(-1.55f, 1.95f), new Vector2(1.55f, 1.95f), new Vector2(-1.55f, 1.95f), new Vector2(1.55f, 1.95f), new Vector2(0, 2.5f) },
        // 8 Narrows: a long tight throat that sways gently.
        new[] { new Vector2(0, 2.5f), new Vector2(0, 1.8f), new Vector2(0, 1.75f), new Vector2(.8f, 1.75f), new Vector2(.8f, 1.75f), new Vector2(-.8f, 1.75f), new Vector2(-.8f, 1.8f), new Vector2(0, 2.5f) },
        // 9 Pinch and bulge: wide chambers between pinched doors.
        new[] { new Vector2(0, 2.5f), new Vector2(1.2f, 1.95f), new Vector2(1.2f, 3.2f), new Vector2(-1.2f, 3.2f), new Vector2(-1.2f, 1.9f), new Vector2(0, 1.85f), new Vector2(0, 3.1f), new Vector2(0, 2.5f) },
        // 10 Long drift: the lane crosses the whole tunnel in one slow sweep.
        new[] { new Vector2(0, 2.5f), new Vector2(-1.8f, 2.2f), new Vector2(-1.8f, 2.0f), new Vector2(-.6f, 2.0f), new Vector2(.6f, 2.0f), new Vector2(1.8f, 2.0f), new Vector2(1.8f, 2.2f), new Vector2(0, 2.5f) },
        // 11 Double chicane: two held lanes linked by sharp crossings.
        new[] { new Vector2(0, 2.5f), new Vector2(1.4f, 2.0f), new Vector2(1.4f, 2.0f), new Vector2(-1.4f, 2.0f), new Vector2(-1.4f, 2.0f), new Vector2(1.4f, 2.0f), new Vector2(1.4f, 2.0f), new Vector2(-1.4f, 2.0f), new Vector2(0, 2.5f) },
        // 12 Baffles: one wall at a time, alternating sides, with the lane swinging between them.
        new[] { new Vector2(0, 2.5f), new Vector2(-1.2f, 2.0f), new Vector2(1.2f, 2.0f), new Vector2(-1.2f, 2.0f), new Vector2(1.2f, 2.0f), new Vector2(-1.2f, 2.0f), new Vector2(1.2f, 2.0f), new Vector2(0, 2.5f) }
    };
    public static bool Active => current != null;
    // The layout under the top of the screen; zero when no passage is active.
    public static int Layout => current != null && current.segments.Count > 0 ? current.segments[current.segments.Count - 1].layout : 0;
    public static float Length(int layout) => (routes[layout - 1].Length - 1) * Step;
    public static Vector2 Opening(int layout, float y)
    {
        var route = routes[layout - 1];
        float at = Mathf.Clamp((y - Camera.UpLimit - 2) / Step, 0, route.Length - 1);
        int i = Mathf.Min(route.Length - 2, Mathf.FloorToInt(at));
        return Vector2.Lerp(route[i], route[i + 1], at - i);
    }

    // Distance from the top of the screen to the end of the walls, in world units.
    static float RemainingAbove => current == null || current.segments.Count == 0 ? -1 :
        Camera.UpLimit + 2 + current.End - current.travel - Camera.UpLimit;
    // The walls will end within about a second: a chained passage can join them seamlessly.
    public static bool EndingSoon => Active && RemainingAbove > 2.5f && RemainingAbove < 16;
    float End => segments.Count == 0 ? 0 : segments[segments.Count - 1].start + Length(segments[segments.Count - 1].layout);

    public static float CenterAt(float screenY)
    {
        if (current == null)
            return 0;
        float y = screenY + current.travel;
        foreach (var segment in current.segments)
            if (y - segment.start <= Camera.UpLimit + 2 + Length(segment.layout))
                return Opening(segment.layout, y - segment.start).x;
        return 0;
    }

    public static CombatWall[] Sections(int layout)
    {
        var result = new List<CombatWall>();
        var route = routes[layout - 1];
        float left = Camera.LeftLimit - 1, right = Camera.RightLimit + 1, bottom = Camera.UpLimit + 2;
        for (int i = 0; i < route.Length - 1; i++)
        {
            var a = route[i];
            var b = route[i + 1];
            bool leftWall = layout == 4 ? i % 4 < 2 : layout == 12 ? i == 0 || i == route.Length - 2 || i % 2 == 1 : true;
            bool rightWall = layout == 4 ? i % 4 >= 2 : layout == 12 ? i == 0 || i == route.Length - 2 || i % 2 == 0 : true;
            if (leftWall)
                result.Add(new CombatWall(bottom + i * Step, bottom + (i + 1) * Step, left, left, a.x - a.y, b.x - b.y));
            if (rightWall)
                result.Add(new CombatWall(bottom + i * Step, bottom + (i + 1) * Step, a.x + a.y, b.x + b.y, right, right));
        }
        return result.ToArray();
    }

    public static void Clear()
    {
        CombatWalls.Solid.Clear();
        if (current != null)
        {
            var old = current;
            current = null;
            Destroy(old.gameObject);
        }
    }

    public static void Begin(int layout)
    {
        // A passage arriving while the last one is still above the bottom of the screen
        // joins its end; otherwise it starts a fresh run at the top of the screen.
        if (current != null && current.End >= current.travel)
        {
            current.Append(layout, current.End);
            return;
        }
        Clear();
        var go = new GameObject("Solid tunnel passage");
        current = go.AddComponent<TunnelPassage>();
        current.Append(layout, 0);
    }

    void Append(int layout, float start)
    {
        var model = new GameObject("Passage layout " + layout).transform;
        model.SetParent(transform, false);
        model.localPosition = Vector3.up * start;
        model.gameObject.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.TunnelPassageMesh(layout);
        var renderer = model.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CombatMaterials.Fleet;
        renderer.sortingLayerName = "Enemy";
        var walls = Sections(layout);
        for (int i = 0; i < walls.Length; i++)
            walls[i] = walls[i].Translated(start);
        segments.Add(new Segment { layout = layout, start = start, walls = walls, model = model });
        Publish();
    }

    void Publish()
    {
        CombatWalls.Solid.Clear();
        foreach (var segment in segments)
            foreach (var wall in segment.walls)
            {
                var moved = wall.Translated(-travel);
                if (moved.Top >= Camera.DownLimit - 4 && moved.Bottom <= Camera.UpLimit + 6)
                    CombatWalls.Solid.Add(moved);
            }
    }

    void Update()
    {
        if (LevelRun.Current != LevelId.Tunnel || CloudTransition.Busy || GameOverState.Active || CombatDecision.Active)
        {
            Clear();
            return;
        }
        if (WorldTimeStop.Frozen || GameRules.playerDisabled)
            return;
        travel += LevelEnvironment.TunnelSpeed * Time.deltaTime;
        transform.position = Vector3.down * travel;
        // Segments fully below the screen are retired; the run ends with its last one.
        while (segments.Count > 1 && Camera.UpLimit + 2 + segments[0].start + Length(segments[0].layout) - travel < Camera.DownLimit - 1)
        {
            Destroy(segments[0].model.gameObject);
            segments.RemoveAt(0);
        }
        Publish();
        if (Camera.UpLimit + 2 + End - travel < Camera.DownLimit - 1)
            Clear();
    }

    void OnDestroy()
    {
        if (current == this)
        {
            current = null;
            CombatWalls.Solid.Clear();
        }
    }
}
