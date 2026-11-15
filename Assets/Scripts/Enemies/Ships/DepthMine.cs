using System.Collections;
using UnityEngine;

// A shootable naval mine. Damage primes the fuse, and so does the pilot flying too close
// (a beeping arm and a short fuse first). Its burst is a double ring with aimed shrapnel.
// In a MINEFIELD some mines rise from the depths mid-screen instead of drifting in from above.
[UnityEngine.Scripting.Preserve]
public sealed class DepthMine : OceanEnemy
{
    bool primed;
    SpriteRenderer lamp;
    public bool Primed => primed;
    const float RiseSeconds = 1.7f, ArmingRange = 1.25f;
    // Mines that rose from the depths are district hazards: they never hold a wave open.
    public bool FromDepths { get; private set; }
    bool rising;
    float riseAge;
    public override bool HoldsWave => !FromDepths;

    public static DepthMine Rise(Vector3 at)
    {
        var mine = (DepthMine)Spawn(typeof(DepthMine), at);
        mine.FromDepths = true;
        mine.rising = true;
        if (mine.movement != null)
            mine.movement.enabled = false;
        mine.GetComponent<Collider2D>().enabled = false;
        return mine;
    }
    public override bool NoseAimed => false;
    public override bool CanBeTargeted => !primed && !rising && base.CanBeTargeted &&
        transform.position.y < Camera.UpLimit - .6f && transform.position.y > Camera.DownLimit + .25f;
    protected override void Configure()
    {
        hullScale = .62f;
        health = 1;
        attackInterval = float.PositiveInfinity;
        nextAttack = float.PositiveInfinity;
        Mover<DriftDownPattern>();
    }
    protected override void Start()
    {
        base.Start();
        scoreOnKill = 15;
        lifeDropChance = manaDropChance = moneyDropChance = 0;
        GetComponent<CombatHullVisual>().enabled = false;
        PartHitFeedback.Bind(Hull);
        lamp = ItemSpark.Sprite(Hull, "Pressure fuse", "standard", LevelProjectilePalette.Accent(LevelId.Underwater), .14f);
        lamp.transform.localPosition = new Vector3(0, 0, -.58f);
    }
    protected override void Attack() { }
    public override void damage(float amount)
    {
        // This is a fuse, not a health check: even a weak shot primes it on later loops.
        if (amount > 0 && float.IsFinite(amount) && GameRules.enemyDamageReceived > 0)
            base.damage(Mathf.Max(amount, health / GameRules.enemyDamageReceived + 1));
    }
    protected override void Update()
    {
        if (primed || WorldTimeStop.Frozen || GameRules.playerDisabled) return;
        age += Time.deltaTime;
        Hull.localRotation = Quaternion.Euler(8 * Mathf.Sin(age * 1.3f), 12 * Mathf.Sin(age * .9f), 5 * Mathf.Sin(age * 1.1f));
        if (rising)
        {
            // Out of the dark water: small, deep and dim, then swelling up to the surface.
            riseAge += Time.deltaTime;
            float t = Mathf.Clamp01(riseAge / RiseSeconds), ease = t * t * (3 - 2 * t);
            Hull.localScale = Vector3.one * hullScale * Mathf.Lerp(.15f, 1, ease);
            Hull.localPosition = new Vector3(0, 0, Mathf.Lerp(3, 0, ease));
            if (lamp != null)
                lamp.color = Color.Lerp(Color.clear, LevelProjectilePalette.Accent(LevelId.Underwater), ease);
            if (Random.value < Time.deltaTime * 10)
                PSXImpactEffects.Burst(transform.position + (Vector3)Random.insideUnitCircle * .4f, new Color(.55f, .8f, .95f, .5f), false, Vector2.up);
            if (t >= 1)
            {
                rising = false;
                GetComponent<Collider2D>().enabled = true;
                if (movement != null)
                    movement.enabled = true;
            }
            return;
        }
        if (FromDepths && !DistrictHazards.Open)
        {
            Depart();
            return;
        }
        var pilot = CombatVisuals.Player;
        if (pilot != null && Vector2.Distance(pilot.position, transform.position) < ArmingRange)
        {
            DistrictHazards.Play("eventMineArm");
            Die();
            return;
        }
        if (age > (FromDepths ? 16 : 28) || transform.position.y < Camera.DownLimit - 2) Depart();
    }
    protected override void Die()
    {
        if (primed) return;
        primed = true;
        if (movement != null) movement.enabled = false;
        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(Burst());
    }
    IEnumerator Burst()
    {
        float fuse = 0;
        while (fuse < .48f)
        {
            if (!WorldTimeStop.Frozen && !GameRules.playerDisabled) fuse += Time.deltaTime;
            if (lamp != null)
            {
                lamp.transform.localScale = Vector3.one * Mathf.Lerp(.14f, .42f, fuse / .48f);
                lamp.color = Color.Lerp(LevelProjectilePalette.Accent(LevelId.Underwater), Color.white, .5f + .5f * Mathf.Sin(fuse * 65));
            }
            yield return null;
        }
        // A dense outer ring, a slower inner ring offset into its gaps, and shrapnel at the pilot.
        int count = Dense(32);
        for (int i = 0; i < count; i++)
        {
            var axis = CombatVisuals.Rotate(Vector2.up, (i + .5f) * 360f / count);
            // The burst's slow rounds are dark and its fast ones bright.
            UseTone(i % 2 == 0 ? HostileTone.Dark : HostileTone.Bright);
            Fire<SimpleRound>(transform.position + (Vector3)axis * .24f, axis, i % 2 == 0 ? 2.4f : 3.2f, .21f)
                .Ramp(1.25f, 1.3f, .5f);
        }
        int inner = Dense(16);
        UseTone(HostileTone.Dark);
        for (int i = 0; i < inner; i++)
        {
            var axis = CombatVisuals.Rotate(Vector2.up, i * 360f / inner);
            Fire<SimpleRound>(transform.position + (Vector3)axis * .18f, axis, 1.5f, .24f).Ramp(1.6f, 1.2f, .7f);
        }
        UseTone(HostileTone.Bright);
        var aim = CombatVisuals.Aim(transform.position);
        foreach (float spread in new[] { -12f, 0, 12f })
            Fire<DropletRound>(transform.position, CombatVisuals.Rotate(aim, spread), 4.2f, .19f);
        SoundManager.PlayEnemyEvent("shellBurst");
        base.Die();
    }
}

public static partial class WavePartLibrary
{
    static readonly FlightCurve MineMoorings = Route("Ocean / Mine moorings", Vector2.right, -.52f, 1.23f, -.49f, .9f, -.54f, .55f, -.5f, .2f, -.53f, -.23f);
    static readonly FlightCurve MineChannel = Route("Ocean / Mine channel", Vector2.right, .42f, 1.23f, .38f, .92f, .43f, .55f, .39f, .15f, .42f, -.23f);
    static readonly FlightCurve MineCurtain = Route("Ocean minefield / Mine curtain", Vector2.right, .05f, 1.23f, .02f, .86f, .06f, .5f, .02f, .14f, .04f, -.23f);
    static readonly FlightCurve MineSlalom = Route("Ocean minefield / Mine slalom", Vector2.right, .55f, 1.23f, .2f, .9f, -.35f, .62f, .1f, .32f, .45f, -.23f);
    static readonly FlightCurve MineDrift = Route("Ocean / Mine drift", Vector2.right, -.18f, 1.23f, -.22f, .87f, -.16f, .5f, -.2f, .12f, -.18f, -.23f);
    public static readonly WavePart[] DepthMineParts = {
        new WavePart("Moored mine pairs", nameof(DepthMine), MineMoorings, WaveShape.Train, 2, 7.2f, 2, new float[0]),
        new WavePart("Mine channel", nameof(DepthMine), MineChannel, WaveShape.Rake, 2, 7.4f, 4, new float[0]),
        new WavePart("Drifting mine row", nameof(DepthMine), MineDrift, WaveShape.Rank, 3, 7.1f, 7, new float[0]),
        // MINEFIELD: whole curtains and weaving lines of mines between the ordinary waves.
        new WavePart("Mine curtain", nameof(DepthMine), MineCurtain, WaveShape.Rank, 4, 7.4f, 2, new float[0]).Under(DistrictCondition.Minefield),
        new WavePart("Mine slalom", nameof(DepthMine), MineSlalom, WaveShape.Train, 4, 7.6f, 3, new float[0]).Under(DistrictCondition.Minefield),
        new WavePart("Crossed mine lines", nameof(DepthMine), MineChannel, WaveShape.Rake, 3, 7.2f, 4, new float[0]).Under(DistrictCondition.Minefield),
        new WavePart("Sunken mine pairs", nameof(DepthMine), MineMoorings, WaveShape.Rank, 2, 7.0f, 5, new float[0]).Under(DistrictCondition.Minefield)
    };
}
