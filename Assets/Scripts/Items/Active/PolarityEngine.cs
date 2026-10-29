using UnityEngine;

// A nod to Ikaruga: while held, every hostile round is born light or in the district's own colour.
// Flipping spins the ship into the other polarity; rounds of the ship's colour are absorbed harmlessly.
[UnityEngine.Scripting.Preserve]
public sealed class PolarityEngine : AActiveItem
{
    public const int Light = 0, Shade = 1;
    const float SpinDuration = .3f;
    // Pale enough to read against the district hue, still rimmed by the sprite's dark outline.
    public static readonly Color LightColor = new Color(.95f, .97f, 1f);
    static PolarityEngine held;
    int pole = Light, previous = Light;
    float spinElapsed = SpinDuration;

    public static bool Active => held != null;
    // The second polarity is each district's own hostile hue, so it changes every level.
    public static Color ShadeColor => LevelProjectilePalette.Accent(LevelRun.Current);
    public static Color PoleColor(int pole) => pole == Light ? LightColor : ShadeColor;
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
        SetData("Polarity Engine", "Light and shadow", "Flip polarity. Your colour can't hurt you.", ItemType.active);
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
            spinElapsed = Mathf.Min(SpinDuration, spinElapsed + WorldTimeStop.PlayerDelta);
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

    // The ship keeps its own paint; the light pole brightens it, the shade pole washes it in the district hue.
    static Color HullTint(int pole) => pole == Light ? new Color(1.2f, 1.22f, 1.28f) : Color.Lerp(Color.white, ShadeColor * 1.15f, .7f);

    /// <summary>Gives a newly launched hostile round a random polarity while the engine is held.</summary>
    public static void Assign(ProjectileSpriteTint tint)
    {
        if (held != null && tint != null)
            tint.SetPolarity(Random.value < .5f ? Light : Shade);
    }

    /// <summary>True when the round shares the ship's polarity and should be absorbed instead of hurting.</summary>
    public static bool Absorbs(GameObject round)
    {
        if (held == null)
            return false;
        var tint = round.GetComponent<ProjectileSpriteTint>();
        return tint != null && tint.Polarity == held.pole;
    }

    public static void Absorb(AEnemyBullet round)
    {
        ItemSound.Play("ready", .05f, 1.9f);
        round.Vanish();
    }
}
