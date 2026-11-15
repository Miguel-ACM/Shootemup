using UnityEngine;

// The elite's mark: four slowly turning pale-gold arcs around the hull, with a soft
// breathing glow. Drawn on the enemy layer beneath the hull, so every bullet stays above it.
public sealed class EliteMark : MonoBehaviour
{
    const int Arcs = 4, Points = 9;
    static readonly Color Gold = new Color(1, .70f, .16f, .9f);
    public float Radius { get; set; } = .6f;
    readonly LineRenderer[] arcs = new LineRenderer[Arcs];
    Transform ring;
    SpriteRenderer glow;
    float spin;

    void Start()
    {
        ring = new GameObject("Elite ring").transform;
        ring.SetParent(transform, false);
        for (int i = 0; i < Arcs; i++)
        {
            var arc = CombatVisuals.Line(ring, Gold, .05f);
            arc.name = "Elite arc " + i;
            arc.useWorldSpace = false;
            arc.sortingLayerName = "Enemy";
            arc.sortingOrder = -1;
            arc.positionCount = Points;
            arc.numCapVertices = 2;
            float start = i * Mathf.PI * 2 / Arcs + .25f, sweep = Mathf.PI * 2 / Arcs - .5f;
            for (int p = 0; p < Points; p++)
            {
                float a = start + sweep * p / (Points - 1);
                arc.SetPosition(p, new Vector3(Mathf.Cos(a), Mathf.Sin(a), -.1f) * Radius);
            }
            arcs[i] = arc;
        }
        glow = new GameObject("Elite glow").AddComponent<SpriteRenderer>();
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = new Vector3(0, 0, .2f);
        glow.sprite = CombatVisuals.Disc;
        glow.sortingLayerName = "Enemy";
        glow.sortingOrder = -2;
        float size = Radius * 2.1f / Mathf.Max(.01f, glow.sprite.bounds.size.x);
        glow.transform.localScale = Vector3.one * size;
    }

    void LateUpdate()
    {
        if (ring == null)
            return;
        spin += Time.deltaTime * 55;
        // The ring keeps its own steady turn whatever the hull banks or faces.
        ring.rotation = Quaternion.Euler(0, 0, spin);
        float pulse = .5f + .5f * Mathf.Sin(Time.time * 4.2f);
        var color = Gold;
        color.a = .65f + .3f * pulse;
        foreach (var arc in arcs)
        {
            arc.startColor = arc.endColor = color;
            arc.widthMultiplier = 1 + .25f * pulse;
        }
        glow.color = new Color(1, .70f, .20f, .12f + .08f * pulse);
    }
}
