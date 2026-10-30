using UnityEngine;

// A boss's spell break: a white flash, three bright rings racing out across the
// arena, a starburst of light and a short camera shake. Purely visual.
public sealed class PhaseBreak : MonoBehaviour
{
    const float Duration = 1.1f;
    readonly LineRenderer[] rings = new LineRenderer[3];
    SpriteRenderer flash;
    float age;

    public static void Play(Vector3 at)
    {
        var go = new GameObject("Phase break");
        go.transform.position = new Vector3(at.x, at.y, -.8f);
        var effect = go.AddComponent<PhaseBreak>();
        for (int i = 0; i < effect.rings.Length; i++)
            effect.rings[i] = CombatVisuals.Line(go.transform, Color.white, .3f);
        var disc = new GameObject("Flash");
        disc.transform.SetParent(go.transform, false);
        effect.flash = disc.AddComponent<SpriteRenderer>();
        effect.flash.sprite = CombatVisuals.Disc;
        effect.flash.sharedMaterial = CombatVisuals.ProjectileMaterial;
        effect.flash.sortingLayerName = "Bullet";
        effect.flash.sortingOrder = 40;
        for (int i = 0; i < 24; i++)
        {
            var direction = CombatVisuals.Rotate(Vector2.up, i * 15);
            PSXImpactEffects.Vanish(at + (Vector3)direction * (.6f + (i % 3) * .5f), Color.white, .7f);
        }
        PSXImpactEffects.Burst(at, Color.white, true);
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
        float glow = 1 - t;
        flash.transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 9, Mathf.Sqrt(Mathf.Min(1, t * 2.5f)));
        flash.color = new Color(1, 1, 1, Mathf.Max(0, 1 - t * 2.5f) * .9f);
        for (int i = 0; i < rings.Length; i++)
        {
            float local = Mathf.Clamp01(t * 1.4f - i * .15f);
            var ring = rings[i];
            ring.enabled = local > 0 && local < 1;
            if (!ring.enabled)
                continue;
            float width = Mathf.Lerp(.42f, .04f, local);
            ring.startWidth = ring.endWidth = width;
            var color = new Color(1, 1, 1, glow * (1 - local));
            ring.startColor = ring.endColor = color;
            CombatVisuals.Ring(ring, transform.position, Mathf.Lerp(.4f, 11, 1 - (1 - local) * (1 - local)));
        }
    }
}
