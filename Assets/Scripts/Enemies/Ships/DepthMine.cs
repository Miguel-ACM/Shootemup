using System.Collections;
using UnityEngine;

// An inert, shootable naval mine. Damage primes the fuse; proximity never does.
[UnityEngine.Scripting.Preserve]
public sealed class DepthMine : OceanEnemy
{
    bool primed;
    SpriteRenderer lamp;
    public bool Primed => primed;
    public override bool NoseAimed => false;
    public override bool CanBeTargeted => !primed && base.CanBeTargeted &&
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
        if (age > 28 || transform.position.y < Camera.DownLimit - 2) Depart();
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
        int count = Dense(24);
        for (int i = 0; i < count; i++)
        {
            var axis = CombatVisuals.Rotate(Vector2.up, (i + .5f) * 360f / count);
            Fire<SimpleRound>(transform.position + (Vector3)axis * .24f, axis, i % 2 == 0 ? 2.2f : 2.9f, .21f)
                .Ramp(1.2f, 1.3f, .5f);
        }
        SoundManager.PlayEnemyEvent("shellBurst");
        base.Die();
    }
}

public static partial class WavePartLibrary
{
    static readonly FlightCurve MineMoorings = Route("Ocean / Mine moorings", Vector2.right, -.52f, 1.23f, -.49f, .9f, -.54f, .55f, -.5f, .2f, -.53f, -.23f);
    static readonly FlightCurve MineChannel = Route("Ocean / Mine channel", Vector2.right, .42f, 1.23f, .38f, .92f, .43f, .55f, .39f, .15f, .42f, -.23f);
    static readonly FlightCurve MineDrift = Route("Ocean / Mine drift", Vector2.right, -.18f, 1.23f, -.22f, .87f, -.16f, .5f, -.2f, .12f, -.18f, -.23f);
    public static readonly WavePart[] DepthMineParts = {
        new WavePart("Moored mine pairs", nameof(DepthMine), MineMoorings, WaveShape.Train, 2, 7.2f, 2, new float[0]),
        new WavePart("Mine channel", nameof(DepthMine), MineChannel, WaveShape.Rake, 2, 7.4f, 4, new float[0]),
        new WavePart("Drifting mine row", nameof(DepthMine), MineDrift, WaveShape.Rank, 3, 7.1f, 7, new float[0])
    };
}
