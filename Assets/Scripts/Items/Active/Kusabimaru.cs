using UnityEngine;

// A nod to Sekiro: a short, readable deflect window rather than a single frame.
// Every round that reaches the ship inside the window is sent back as the player's own.
[UnityEngine.Scripting.Preserve]
public sealed class Kusabimaru : AActiveItem
{
    void Awake()
    {
        SetData("Kusabimaru", "Deflect, don't dodge", "Deflect bullets back at enemies.", ItemType.active);
        cost = 0;
        cd = .6f;
    }

    public override float OnPress(float mana)
    {
        float valid = CheckValidPress(mana);
        if (valid < 0)
            return valid;
        var window = s.GetComponent<DeflectWindow>();
        if (window == null)
            window = s.gameObject.AddComponent<DeflectWindow>();
        window.Open();
        lastCD = 0;
        return mana;
    }
}

public sealed class DeflectWindow : MonoBehaviour
{
    // Nine frames at 60 fps.
    public const float Duration = .15f;
    const float Reach = .7f;
    static readonly Color Steel = new Color(.85f, .9f, 1f);
    float remaining;
    int deflected;
    readonly System.Collections.Generic.List<CombatRound> nearby = new System.Collections.Generic.List<CombatRound>();

    public bool Active => remaining > 0;

    public void Open()
    {
        remaining = Duration;
        deflected = 0;
        PSXFireballs.Shockwave(transform.position, Reach * 1.4f, Duration, Steel, 1, .05f);
        ItemSound.Play("roll", .12f, 1.8f);
    }

    /// <summary>Catches a round touching the ship; called from the ship's own hit check.</summary>
    public static bool Catches(GameObject ship, AEnemyBullet round)
    {
        return ship.TryGetComponent<DeflectWindow>(out var window) && window.Active && window.Deflect(round);
    }

    bool Deflect(AEnemyBullet round)
    {
        if (BulletReflection.Reflect(round, 1.5f) == null)
            return false;
        // One ringing clang per window keeps a dense deflect from turning into noise.
        if (deflected++ == 0)
        {
            ItemSound.Play("shatter", .16f, 1.7f);
            PSXImpactEffects.Burst(transform.position + Vector3.up * .3f, Steel, true);
        }
        return true;
    }

    void Update()
    {
        if (remaining <= 0)
            return;
        remaining -= WorldTimeStop.PlayerDelta;
        // Rounds a moment away from the hull count too, so a visibly timed press is honoured.
        nearby.Clear();
        foreach (var round in CombatRound.Live)
            if (round != null && ((Vector2)(round.transform.position - transform.position)).sqrMagnitude <= Reach * Reach)
                nearby.Add(round);
        foreach (var round in nearby)
            Deflect(round);
    }
}
