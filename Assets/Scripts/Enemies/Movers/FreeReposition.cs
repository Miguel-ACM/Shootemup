using System.Collections.Generic;
using UnityEngine;

public class FreeReposition : PatternMover
{
    [SerializeField] private Vector2 areaMin = new Vector2(-3.6f, 2f);
    [SerializeField] private Vector2 areaMax = new Vector2(3.6f, 5.8f);
    [SerializeField, Min(.1f)] private float reservationRadius = 1.2f;
    [SerializeField, Min(0)] private float minimumTravel = 1.5f;
    [SerializeField, Min(0)] private float playerClearance = 2f;
    [SerializeField, Min(0)] private float previewDuration = .35f;
    [SerializeField, Min(.1f)] private float holdDuration = 2.5f;
    [SerializeField, Min(1)] private int maximumMoves = 5;
    private static readonly HashSet<FreeReposition> reservations = new HashSet<FreeReposition>();
    private enum Phase { Select, Preview, Travel, Hold }
    private Phase phase;
    private float clock;
    private int moves;
    private bool reserved;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetReservations() { reservations.Clear(); }

    protected override void StepPattern(float dt)
    {
        clock += dt;
        switch (phase)
        {
            case Phase.Select:
                if (moves >= maximumMoves) { Leave(); return; }
                if (TryReserve()) { phase = Phase.Preview; clock = 0; }
                break; // If full, retry next frame; maximumLifetime still bounds the wait.
            case Phase.Preview:
                if (clock >= previewDuration) phase = Phase.Travel;
                break;
            case Phase.Travel:
                if (!PathIsSafe(Destination)) { Release(); phase = Phase.Select; break; }
                if (Travel(Destination, dt))
                { moves++; Arrive(); SetFiring(true); phase = Phase.Hold; clock = 0; }
                break;
            case Phase.Hold:
                if (clock >= holdDuration) { SetFiring(false); Release(); phase = Phase.Select; }
                break;
        }
    }

    private bool TryReserve()
    {
        reservations.RemoveWhere(other => other == null || !other.isActiveAndEnabled || other.IsLeaving);
        for (int attempt = 0; attempt < 24; attempt++)
        {
            Vector3 candidate = InBounds(new Vector3(Random.Range(Mathf.Min(areaMin.x, areaMax.x), Mathf.Max(areaMin.x, areaMax.x)),
                Random.Range(Mathf.Min(areaMin.y, areaMax.y), Mathf.Max(areaMin.y, areaMax.y)), origin.z));
            if (Vector2.Distance(candidate, transform.position) < minimumTravel || !PathIsSafe(candidate)) continue;
            bool available = true;
            foreach (var other in reservations)
            {
                if (other == this || !other.reserved) continue;
                float spacing = Mathf.Max(reservationRadius, other.reservationRadius);
                if (Vector2.Distance(candidate, other.Destination) < spacing ||
                    SegmentsOverlap(transform.position, candidate, other.transform.position, other.Destination, spacing))
                { available = false; break; }
            }
            if (!available) continue;
            Destination = candidate;
            reserved = true;
            reservations.Add(this);
            return true;
        }
        return false;
    }

    private bool PathIsSafe(Vector3 end)
    {
        return player == null || DistanceToSegment(player.position, transform.position, end) >= playerClearance;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 line = b - a;
        float t = line.sqrMagnitude > .00001f ? Mathf.Clamp01(Vector2.Dot(point - a, line) / line.sqrMagnitude) : 0;
        return Vector2.Distance(point, a + line * t);
    }

    private static float Cross(Vector2 a, Vector2 b) { return a.x * b.y - a.y * b.x; }
    private static bool SegmentsOverlap(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float clearance)
    {
        Vector2 ab = b - a, cd = d - c;
        float denominator = Cross(ab, cd);
        if (Mathf.Abs(denominator) > .00001f)
        {
            float t = Cross(c - a, cd) / denominator, u = Cross(c - a, ab) / denominator;
            if (t >= 0 && t <= 1 && u >= 0 && u <= 1) return true;
        }
        return Mathf.Min(Mathf.Min(DistanceToSegment(a, c, d), DistanceToSegment(b, c, d)),
            Mathf.Min(DistanceToSegment(c, a, b), DistanceToSegment(d, a, b))) < clearance;
    }

    private void Release() { reserved = false; reservations.Remove(this); }
    protected override void OnDisable() { Release(); base.OnDisable(); }
}
