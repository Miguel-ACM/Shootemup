using UnityEngine;

// Restores health over a short visible fill instead of all at once; the meter glows while it rises.
public sealed class HealthRecovery : MonoBehaviour
{
    static readonly Color Mint = new Color(.55f, 1, .72f);
    public static float Glow
    {
        get; private set;
    }

    Shipcontroller ship;
    float remaining, rate, afterglow;

    public static void Begin(Shipcontroller ship, float fraction, float seconds = .75f)
    {
        if (!ship.TryGetComponent<HealthRecovery>(out var recovery))
            recovery = ship.gameObject.AddComponent<HealthRecovery>();
        recovery.ship = ship;
        recovery.remaining += fraction;
        recovery.rate = recovery.remaining / seconds;
        PSXImpactEffects.Burst(ship.transform.position, Mint);
        ItemSound.Play("heal", .2f);
    }

    public static Color Tint(Color color) => Glow <= 0 ? color : Color.Lerp(color, Mint, Glow * (.65f + .35f * Mathf.Sin(Time.time * 18)));

    void Update()
    {
        if (ship == null || ship.health <= 0 || GameOverState.Active)
        {
            remaining = 0;
            Glow = 0;
            return;
        }
        if (remaining > 0)
        {
            float step = Mathf.Min(remaining, rate * Time.deltaTime);
            remaining -= step;
            ship.HealFraction(step);
            afterglow = .45f;
            Glow = 1;
            return;
        }
        afterglow = Mathf.Max(0, afterglow - Time.deltaTime);
        Glow = afterglow / .45f;
    }

    void OnDestroy()
    {
        Glow = 0;
    }
}
