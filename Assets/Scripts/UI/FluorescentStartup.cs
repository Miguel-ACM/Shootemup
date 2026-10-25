using UnityEngine;

// All instruments use one clock. A tube catches weakly, drops out, then warms to a steady glow.
public sealed class FluorescentStartup : MonoBehaviour
{
    public const float Duration = 3.05f;
    const float LastCircuitDelay = .34f;
    public const float CompleteAt = Duration + LastCircuitDelay;
    static readonly float[] Times = { 0, .12f, .26f, .32f, .46f, .52f, .69f, .76f, 1.10f, 1.17f, 1.32f, 1.57f, 1.63f, 1.78f, 2.13f, 2.18f, 2.31f, 2.60f, 2.66f, 2.79f, Duration };
    static readonly float[] Power = { 0, 0, .12f, .38f, .42f, .025f, .08f, .46f, .55f, .14f, .52f, .62f, .18f, .68f, .78f, .42f, .82f, .9f, .72f, .93f, 1 };
    IndustrialHUD clock;
    CanvasGroup group;
    float delay, localAge;

    public static CanvasGroup Attach(GameObject target, float circuitDelay = 0)
    {
        var startup = target.AddComponent<FluorescentStartup>();
        startup.delay = Mathf.Clamp(circuitDelay, 0, LastCircuitDelay);
        return startup.group;
    }

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    void LateUpdate()
    {
        if (clock == null)
            clock = FindAnyObjectByType<IndustrialHUD>();
        localAge += Time.unscaledDeltaTime;
        float age = clock != null ? clock.EntranceAge : localAge;
        group.alpha = Sample(age, delay);
        if (age >= Duration + delay)
        {
            group.alpha = 1;
            enabled = false;
        }
    }

    public static float Sample(float age, float circuitDelay = 0)
    {
        float time = age - circuitDelay;
        if (time <= 0)
            return 0;
        if (time >= Duration)
            return 1;
        for (int i = 1; i < Times.Length; i++)
        {
            if (time > Times[i])
                continue;
            float blend = Mathf.InverseLerp(Times[i - 1], Times[i], time);
            // The short downward strokes are electrical dropouts; the longer rises warm gently.
            return Mathf.Lerp(Power[i - 1], Power[i], Mathf.SmoothStep(0, 1, blend));
        }
        return 1;
    }
}
