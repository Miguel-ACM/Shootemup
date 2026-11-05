using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class ShrinkerJuice : AItem
{
    void Awake()
    {
        SetData("Shrinker juice", "Small sip", "Smaller ship. Same firepower.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        // Never scale the player root: it owns weapons, actives and companion offsets.
        if (s.GetComponent<ShrunkShip>() == null)
            s.gameObject.AddComponent<ShrunkShip>();
        base.OnPickup();
    }
}
public sealed class ShrunkShip : MonoBehaviour
{
    public const float Factor = .7f;
    bool applied;
    void Start()
    {
        applied = true;
        Scale(Factor);
    }

    void OnDestroy()
    {
        if (applied) Scale(1 / Factor);
    }

    void Scale(float factor)
    {
        GetComponent<PSXShipVisual>()?.ScaleHull(factor);
        var polygon = GetComponent<PolygonCollider2D>();
        if (polygon != null)
            for (int i = 0; i < polygon.pathCount; i++)
            {
                var points = polygon.GetPath(i);
                for (int j = 0; j < points.Length; j++)
                    points[j] *= factor;
                polygon.SetPath(i, points);
            }
        var circle = GetComponent<CircleCollider2D>();
        if (circle != null)
            circle.radius *= factor;
        var effects = GetComponent<PSXShipEffects>();
        if (effects != null)
            effects.HullScale *= factor;
    }
}
