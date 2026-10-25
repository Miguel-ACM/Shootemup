using UnityEngine;
public sealed class HelixRound : CombatRound
{
    public float phase;
    TrailRenderer wake;
    static readonly Gradient[] colors = new Gradient[System.Enum.GetValues(typeof(LevelId)).Length];
    protected override void Initialize()
    {
        phase = 0;
        if (wake == null)
        {
            wake = gameObject.AddComponent<TrailRenderer>();
            wake.sharedMaterial = CombatVisuals.ProjectileMaterial;
            wake.time = .30f;
            wake.minVertexDistance = .025f;
            wake.autodestruct = false;
            wake.sortingLayerName = "Bullet";
            wake.sortingOrder = 14;
            wake.numCornerVertices = 2;
            wake.numCapVertices = 2;
            wake.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
        }
        int palette = (int)LevelRun.Current;
        if (colors[palette] == null)
        {
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Accent, 0), new GradientColorKey(Accent, 1) }, new[] { new GradientAlphaKey(.70f, 0), new GradientAlphaKey(0, 1) });
            colors[palette] = gradient;
        }
        wake.colorGradient = colors[palette];
        wake.widthMultiplier = transform.localScale.x * .43f;
        wake.Clear();
        wake.emitting = true;
    }

    protected override void Tick(float dt)
    {
        var side = new Vector2(-heading.y, heading.x);
        var old = transform.position;
        transform.position = origin + (Vector3)(heading * age * speed + side * Mathf.Sin(age * 3.8f + phase) * .30f);
        var d = transform.position - old;
        if (d.sqrMagnitude > .001f)
            transform.rotation = Quaternion.FromToRotation(Vector3.up, d);
    }

    public static void Pair(Vector3 p, Vector2 d, float speed = 3)
    {
        Launch<HelixRound>(p, d, speed, .12f);
        var b = Launch<HelixRound>(p, d, speed, .12f);
        if (b != null)
            b.phase = Mathf.PI;
    }
}
