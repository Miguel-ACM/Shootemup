using UnityEngine;

// CANDY RAIN: a big modelled sweet tumbling down the arena. Player rounds knock it back up and
// keep it aloft; it can never be destroyed. If it lands on the pilot it hurts, and if it reaches
// the bottom of the screen it smashes into a spray of candy rounds. Homing ignores it, and it
// never holds a wave open.
public sealed class CandyDrop : AEnemy
{
    const float Gravity = 3.1f, Kick = 4.1f, MaxRise = 6.2f, Radius = .42f;
    static int live;
    static Material glowLine;
    public static int Count => live;
    Vector2 velocity;
    Transform model;
    float spin, spinRate, bumpUntil;
    bool gone;

    public override bool HoldsWave => false;
    public override bool CanBeTargeted => false;
    public override bool CanReceiveDamage => !gone;

    public static CandyDrop Drop(float x, int kind)
    {
        var go = new GameObject("Falling candy");
        go.SetActive(false);
        go.transform.position = new Vector3(x, Camera.UpLimit + 1, 1);
        go.tag = "Enemy";
        go.layer = 8;
        var sprite = go.AddComponent<SpriteRenderer>();
        sprite.enabled = false;
        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0;
        body.bodyType = RigidbodyType2D.Kinematic;
        go.AddComponent<CircleCollider2D>().isTrigger = true;
        go.GetComponent<CircleCollider2D>().radius = Radius;
        var candy = go.AddComponent<CandyDrop>();
        candy.health = float.PositiveInfinity;
        candy.scoreOnKill = 0;
        candy.lifeDropChance = candy.manaDropChance = candy.moneyDropChance = 0;
        var model = new GameObject("Sweet");
        model.transform.SetParent(go.transform, false);
        model.transform.localScale = Vector3.one * .85f;
        model.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CandyProp(kind);
        var renderer = model.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CandyMaterials.Glaze;
        renderer.sortingLayerName = "Enemy";
        candy.model = model.transform;
        // A warm halo sets the hazard apart from the identical sweets drifting in the scenery.
        var halo = new GameObject("Candy halo").AddComponent<SpriteRenderer>();
        halo.transform.SetParent(go.transform, false);
        halo.transform.localPosition = new Vector3(0, 0, .3f);
        halo.sprite = CombatVisuals.Disc;
        halo.sortingLayerName = "Enemy";
        halo.sortingOrder = -2;
        var hostile = LevelProjectilePalette.Accent(LevelId.Candyland);
        halo.color = new Color(hostile.r, hostile.g, hostile.b, .38f);
        halo.transform.localScale = Vector3.one * (Radius * 3f / Mathf.Max(.01f, halo.sprite.bounds.size.x));
        var rim = CombatVisuals.Line(go.transform, Color.Lerp(hostile, Color.white, .5f), .16f);
        if (glowLine == null)
            glowLine = new Material(Resources.Load<Shader>("HazardLine")) { name = "Candy hazard ring" };
        rim.sharedMaterial = glowLine;
        rim.useWorldSpace = false;
        rim.sortingLayerName = "Enemy";
        rim.sortingOrder = -1;
        CombatVisuals.Ring(rim, Vector3.zero, Radius * 1.25f);
        candy.velocity = new Vector2(Random.Range(-.4f, .4f), -1.2f);
        candy.spinRate = Random.Range(-140f, 140f);
        go.SetActive(true);
        return candy;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        live++;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        live--;
    }

    // A hit is a knock, never damage: the sweet is thrown back up, a little off the hit side.
    public override void damage(float amount)
    {
        if (gone || QuietDamage || amount <= 0)
            return;
        velocity.y = Mathf.Min(MaxRise, Mathf.Max(velocity.y, 0) * .35f + Kick);
        velocity.x = Mathf.Clamp(velocity.x + Random.Range(-.7f, .7f), -2.2f, 2.2f);
        spinRate = -spinRate * 1.1f;
        bumpUntil = Time.time + .1f;
        if (Time.time > nextBounceSound)
        {
            nextBounceSound = Time.time + .09f;
            DistrictHazards.Play("eventCandy");
        }
        PSXImpactEffects.Burst(transform.position, new Color(1, .7f, .85f), false, Vector2.up);
    }

    float nextBounceSound;

    protected override void Start()
    {
    }

    void Update()
    {
        if (gone || WorldTimeStop.Frozen || !GameClock.ActivePlay)
            return;
        if (!DistrictHazards.Open)
        {
            // The wave is over: the sweet sinks away without harming anyone.
            Leave();
            return;
        }
        float dt = Time.deltaTime;
        velocity.y -= Gravity * dt;
        var p = (Vector2)transform.position + velocity * dt;
        float left = Camera.LeftLimit + Radius, right = Camera.RightLimit - Radius;
        if (p.x < left || p.x > right)
        {
            p.x = Mathf.Clamp(p.x, left, right);
            velocity.x = -velocity.x * .7f;
        }
        // Knocked against the ceiling it rebounds, so it stays in play until it falls.
        if (p.y > Camera.UpLimit - Radius && velocity.y > 0 && transform.position.y < Camera.UpLimit)
            velocity.y = -velocity.y * .35f;
        transform.position = new Vector3(p.x, p.y, transform.position.z);
        spin += spinRate * dt;
        float squash = Time.time < bumpUntil ? 1.18f : 1;
        model.localRotation = Quaternion.Euler(0, 0, spin);
        model.localScale = new Vector3(.85f * squash, .85f / squash, .85f);
        var pilot = CombatVisuals.Player;
        if (pilot != null && Vector2.Distance(pilot.position, p) < Radius + .08f)
        {
            pilot.SendMessage("ReceiveHazardDamage", 1f, SendMessageOptions.DontRequireReceiver);
            Smash(false);
            return;
        }
        if (p.y < Camera.DownLimit + Radius * .6f && velocity.y < 0)
            Smash(true);
    }

    // Landing at the bottom breaks the sweet into a small upward spray of candy rounds.
    void Smash(bool spray)
    {
        gone = true;
        DistrictHazards.Play("eventCandy");
        PSXImpactEffects.Burst(transform.position, new Color(1, .55f, .8f), true);
        if (spray)
        {
            int count = Difficulty.Easy ? 3 : Difficulty.Hard ? 7 : 5;
            CombatRound.LaunchTone = HostileTone.Bright;
            for (int i = 0; i < count; i++)
            {
                float angle = (i - (count - 1) * .5f) * (110f / Mathf.Max(1, count - 1));
                CombatRound.Launch<CandyRound>(transform.position + Vector3.up * .2f, CombatVisuals.Rotate(Vector2.up, angle), 2.4f, .18f)
                    .Fall(new Vector2(0, -1.4f));
            }
        }
        Destroy(gameObject);
    }

    void Leave()
    {
        gone = true;
        PSXImpactEffects.Burst(transform.position, new Color(1, .7f, .85f), false);
        Destroy(gameObject);
    }

    protected override void OnBecameInvisible()
    {
    }
}
