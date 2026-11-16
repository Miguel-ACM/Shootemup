using System.Collections.Generic;
using UnityEngine;

// Dense volleys retain their exact pixel fixtures, but only register them with
// Box2D near something they can hit. Otherwise overlapping newborn fixtures
// trigger a quadratic contact search even when collision layers reject the pairs.
public static class ProjectileCollisionWindow
{
    static readonly List<Collider2D> blockers = new List<Collider2D>();
    static readonly List<Bounds> targets = new List<Bounds>(12);
    static Collider2D player;
    static int frame = -1;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        blockers.Clear();
        targets.Clear();
        player = null;
        frame = -1;
    }

    public static void Register(Collider2D collider)
    {
        if (collider != null && !blockers.Contains(collider))
        {
            blockers.Add(collider);
            frame = -1;
        }
    }

    public static void Unregister(Collider2D collider)
    {
        blockers.Remove(collider);
        frame = -1;
    }

    public static bool Relevant(Vector3 position, float radius, float speed)
    {
        if (frame != Time.frameCount)
        {
            frame = Time.frameCount;
            targets.Clear();
            if (player == null && CombatVisuals.Player != null)
                player = CombatVisuals.Player.GetComponent<Collider2D>();
            Add(player);
            for (int i = blockers.Count - 1; i >= 0; i--)
                if (blockers[i] == null)
                    blockers.RemoveAt(i);
                else
                    Add(blockers[i]);
        }
        // Cover motion before the next physics step, target movement later in this
        // frame, and low frame rates. A stalled frame must not activate the whole field.
        float horizon = Mathf.Max(Time.fixedDeltaTime, Mathf.Min(Time.deltaTime, .05f));
        float margin = .15f + (Mathf.Abs(GameRules.playerMoveSpeed) + Mathf.Abs(speed)) * horizon * 1.5f;
        float reach = radius + margin;
        float squared = reach * reach;
        foreach (var bounds in targets)
        {
            position.z = bounds.center.z;
            if (bounds.SqrDistance(position) <= squared)
                return true;
        }
        return false;
    }

    static void Add(Collider2D collider)
    {
        if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy)
            targets.Add(collider.bounds);
    }

    public static bool TryDeflector(Vector2 from, Vector2 to, float radius, out Vector2 point)
    {
        point = from;
        float nearest = float.PositiveInfinity;
        var area = new Bounds((from + to) * .5f,
            new Vector3(Mathf.Abs(to.x - from.x) + radius * 2, Mathf.Abs(to.y - from.y) + radius * 2, 100));
        foreach (var collider in blockers)
        {
            if (collider == null || !area.Intersects(collider.bounds) || !collider.TryGetComponent<BlockerOrbital>(out _) ||
                !BeamContact.Touches(collider, from, to, radius, out var contact))
                continue;
            float distance = (contact - from).sqrMagnitude;
            if (distance >= nearest) continue;
            nearest = distance;
            point = contact;
        }
        return !float.IsPositiveInfinity(nearest);
    }
}
