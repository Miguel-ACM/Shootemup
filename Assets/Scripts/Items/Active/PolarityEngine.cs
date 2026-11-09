using UnityEngine;

// A nod to Ikaruga. Every hostile attack is authored bright or dark in its district's hue;
// the engine's two poles are those tones. Flipping spins the ship into the other pole:
// rounds of the ship's tone are absorbed harmlessly and each one refills a little energy.
[UnityEngine.Scripting.Preserve]
public sealed class PolarityEngine : AActiveItem
{
    public const int Light = (int)HostileTone.Bright, Shade = (int)HostileTone.Dark;
    const float SpinDuration = .3f;
    // Energy returned per absorbed round, so flipping into an attack pays off.
    const float AbsorbEnergy = 1.2f;
    static PolarityEngine held;
    int pole = Light, previous = Light;
    float spinElapsed = SpinDuration;

    public static bool Active => held != null;
    // Both poles are the district's own hostile hue, so they change every level.
    static Color Hue => LevelProjectilePalette.Accent(LevelRun.Current);
    public static Color PoleColor(int pole) => pole == Light ? Color.Lerp(Hue, Color.white, .25f) : Hue * .35f;
    public static float SpinAngle => held != null && held.spinElapsed < SpinDuration ? 360 * Mathf.SmoothStep(0, 1, held.spinElapsed / SpinDuration) : 0;

    public static Color ShipTint
    {
        get
        {
            if (held == null)
                return Color.white;
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(held.spinElapsed / SpinDuration));
            return Color.Lerp(HullTint(held.previous), HullTint(held.pole), t);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetHolder()
    {
        held = null;
    }

    void Awake()
    {
        SetData("Polarity Engine", "Light and shadow", "Flip between bright and dark. Rounds of your tone can't hurt you and refill energy.", ItemType.active);
        cost = 0;
        cd = .5f;
    }

    public override void OnPickup()
    {
        base.OnPickup();
        held = this;
        pole = previous = Light;
        spinElapsed = SpinDuration;
    }

    void OnDestroy()
    {
        if (held == this)
            held = null;
    }

    protected override void Update()
    {
        base.Update();
        if (held == this && spinElapsed < SpinDuration)
            spinElapsed = Mathf.Min(SpinDuration, spinElapsed + GameClock.PlayerDelta);
    }

    public override float OnPress(float mana)
    {
        float valid = CheckValidPress(mana);
        if (valid < 0)
            return valid;
        if (held != this)
            return -2;
        previous = pole;
        pole = 1 - pole;
        spinElapsed = 0;
        PSXImpactEffects.Burst(s.transform.position, PoleColor(pole), true);
        ItemSound.Play("roll", .18f, 1.35f);
        lastCD = 0;
        return mana;
    }

    // The ship keeps its own paint: the bright pole lifts it in the district hue, the dark
    // pole sinks it, matching the two kinds of round it can absorb.
    static Color HullTint(int pole) => pole == Light ? Color.Lerp(new Color(1.2f, 1.22f, 1.28f), Hue * 1.4f, .35f) : Color.Lerp(Color.white, Hue * .45f, .75f);

    /// <summary>True when the round shares the ship's polarity and should be absorbed instead of hurting.</summary>
    public static bool Absorbs(GameObject round)
    {
        if (held == null)
            return false;
        // Beams are never absorbed, whatever their colour.
        if (round.GetComponent<PulseLaser>() != null)
            return false;
        var tint = round.GetComponent<ProjectileSpriteTint>();
        return tint != null && tint.Polarity == held.pole;
    }

    public static void Absorb(AEnemyBullet round)
    {
        ItemSound.Play("ready", .05f, 1.9f);
        if (held != null && held.s != null)
            held.s.HealManaFlat(AbsorbEnergy);
        round.Vanish();
    }
}
