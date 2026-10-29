using UnityEngine;

// Restores hits one segment at a time over a short visible fill; the meter glows while it rises.
public sealed class HealthRecovery : MonoBehaviour
{
    static readonly Color Mint = new Color(.55f, 1, .72f);
    public static float Glow
    {
        get; private set;
    }

    Shipcontroller ship;
    int remaining;
    float interval, nextSegment, afterglow;

    public static void Begin(Shipcontroller ship, float fraction, float seconds = .75f) =>
        Begin(ship, Mathf.Max(1, Mathf.CeilToInt(ship.maxHealth * Mathf.Clamp01(fraction) - .01f)), seconds);

    public static void Begin(Shipcontroller ship, int hits, float seconds = .75f)
    {
        if (!ship.TryGetComponent<HealthRecovery>(out var recovery))
            recovery = ship.gameObject.AddComponent<HealthRecovery>();
        recovery.ship = ship;
        if (recovery.remaining == 0)
            recovery.nextSegment = Time.time + .12f;
        recovery.remaining += hits;
        recovery.interval = seconds / recovery.remaining;
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
            Glow = 1;
            afterglow = .45f;
            if (Time.time >= nextSegment)
            {
                remaining--;
                nextSegment = Time.time + interval;
                ship.Repair(1);
            }
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
