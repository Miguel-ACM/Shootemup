using UnityEngine;

// The bullet clear travels out from an opening starburst in three tapering arcs.
public sealed class PhaseBreak : MonoBehaviour
{
    const float Duration = 1.1f;
    readonly LineRenderer[] arcs = new LineRenderer[3];
    const int ArcPoints = 24;
    float age;

    public static void Play(Vector3 at)
    {
        var go = new GameObject("Phase break");
        go.transform.position = new Vector3(at.x, at.y, -.8f);
        var effect = go.AddComponent<PhaseBreak>();
        for (int i = 0; i < effect.arcs.Length; i++)
        {
            var arc = effect.arcs[i] = CombatVisuals.Line(go.transform, Color.white, .16f);
            arc.useWorldSpace = false;
            arc.positionCount = ArcPoints;
            arc.enabled = false;
        }
        PSXImpactEffects.SpellBreak(go.transform.position);
        if (EZCameraShake.CameraShaker.Instance != null)
            EZCameraShake.CameraShaker.Instance.ShakeOnce(4, 6, .05f, .8f);
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = age / Duration;
        if (t >= 1)
        {
            Destroy(gameObject);
            return;
        }
        for (int i = 0; i < arcs.Length; i++)
        {
            float local = Mathf.Clamp01(t * 1.3f - i * .09f);
            var arc = arcs[i];
            arc.enabled = local > 0 && local < 1;
            if (!arc.enabled)
                continue;
            float remaining = 1 - local;
            float radius = Mathf.Lerp(.45f, 11, 1 - remaining * remaining);
            arc.startWidth = .16f * remaining;
            arc.endWidth = .035f * remaining;
            arc.startColor = new Color(.9f, .96f, 1, .85f * remaining * remaining);
            arc.endColor = new Color(.76f, .86f, 1, .12f * remaining);
            float start = i * 120 + 24 * local;
            float sweep = Mathf.Lerp(108, 48, local);
            for (int point = 0; point < ArcPoints; point++)
            {
                float angle = (start + sweep * point / (ArcPoints - 1)) * Mathf.Deg2Rad;
                arc.SetPosition(point, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0));
            }
        }
    }
}
