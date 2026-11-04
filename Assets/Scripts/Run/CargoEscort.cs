using UnityEngine;
using UnityEngine.InputSystem;
using System;

// Keep the crate alive to improve its contents, or deliberately open it in flight.
// The simulator's two-wave delivery remains a separate, non-redeemable objective.
public sealed class CargoEscort : MonoBehaviour
{
    public const int MinimumOfferWaves = 5;
    public const float FollowDistance = .65f, ClaimSeconds = .6f;
    public int Hits { get; private set; } = 3;
    public int CompletedWaves { get; private set; }
    public int WavesRemaining => Mathf.Max(0, (trial ? 2 : 3) - CompletedWaves);
    public ItemRarity Quality => (ItemRarity)Mathf.Min(3, CompletedWaves);
    public string Rarity => rarityNames[(int)Quality];
    public bool Finished => finished;
    public float ClaimProgress => claimCharge / ClaimSeconds;
    Shipcontroller ship;
    CircleCollider2D contact;
    Transform hull;
    LineRenderer outline, claimOutline;
    SpriteRenderer prompt;
    InputAction collect;
    Action<CargoEscort> onClaim;
    readonly Transform[] seals = new Transform[3];
    static readonly Vector3[] corners = { new Vector3(-.34f, -.33f, -.36f), new Vector3(-.34f, .33f, -.36f),
        new Vector3(.34f, .33f, -.36f), new Vector3(.34f, -.33f, -.36f) };
    static readonly Sprite[] glyphs = new Sprite[3];
    static readonly string[] rarityNames = { "common", "rare", "epic", "legendary" };
    Vector3 velocity;
    float grace, claimCharge, qualityFlash;
    bool finished, trial, claimArmed;

    public static CargoEscort Create(Shipcontroller ship, Action<CargoEscort> onClaim = null)
    {
        var go = new GameObject("Escort cargo");
        go.transform.position = FollowTarget(ship.transform.position);
        var contact = go.AddComponent<CircleCollider2D>();
        contact.radius = .28f;
        contact.isTrigger = true;
        var body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0;
        var cargo = go.AddComponent<CargoEscort>();
        cargo.ship = ship;
        cargo.contact = contact;
        cargo.trial = FlightTrials.EscortsCargo;
        cargo.onClaim = onClaim;
        cargo.grace = 1;
        cargo.Build();
        if (!cargo.trial && onClaim != null)
        {
            cargo.collect = new InputAction("Open cargo", InputActionType.Button, "<Keyboard>/c");
            cargo.collect.AddBinding("<Gamepad>/buttonNorth");
            cargo.collect.Enable();
        }
        return cargo;
    }

    void OnEnable()
    {
        ProjectileCollisionWindow.Register(GetComponent<Collider2D>());
        collect?.Enable();
    }
    void OnDisable()
    {
        ProjectileCollisionWindow.Unregister(GetComponent<Collider2D>());
        collect?.Disable();
        claimCharge = 0;
        claimArmed = false;
    }
    void OnDestroy() => collect?.Dispose();

    static Vector3 FollowTarget(Vector3 position)
    {
        var target = position + new Vector3(0, -FollowDistance, .2f);
        target.x = Mathf.Clamp(target.x, Camera.LeftLimit + .4f, Camera.RightLimit - .4f);
        target.y = Mathf.Max(Camera.DownLimit + .45f, target.y);
        return target;
    }
    void Build()
    {
        hull = new GameObject("Strapped salvage crate").transform;
        hull.SetParent(transform, false);
        hull.gameObject.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CargoCrate();
        var renderer = hull.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CombatMaterials.Fleet;
        renderer.sortingLayerName = "Player";
        for (int i = 0; i < seals.Length; i++)
        {
            var seal = new GameObject("Cargo seal");
            seal.transform.SetParent(hull, false);
            seal.transform.localPosition = new Vector3((i - 1) * .16f, .22f, -.28f);
            seal.transform.localScale = new Vector3(.1f, .06f, .06f);
            seal.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CargoSeal();
            seal.AddComponent<MeshRenderer>().sharedMaterial = CombatMaterials.Fleet;
            seals[i] = seal.transform;
        }
        outline = Border("Cargo rarity outline", .045f);
        outline.loop = true;
        outline.positionCount = corners.Length;
        outline.SetPositions(corners);
        claimOutline = Border("Cargo opening progress", .075f);
        claimOutline.startColor = claimOutline.endColor = Color.white;
        claimOutline.enabled = false;
        var hint = new GameObject("Cargo collect control");
        hint.transform.SetParent(transform, false);
        hint.transform.localPosition = new Vector3(.58f, -.1f, -.6f);
        hint.transform.localScale = Vector3.one * .5f;
        prompt = hint.AddComponent<SpriteRenderer>();
        prompt.sortingLayerName = "Player";
        prompt.sortingOrder = 3;
        prompt.enabled = false;
        UpdateOutline(0);
    }

    LineRenderer Border(string name, float width)
    {
        var line = CombatVisuals.Line(hull, Color.white, width);
        line.name = name;
        line.useWorldSpace = false;
        line.sortingLayerName = "Player";
        line.sortingOrder = 2;
        line.numCornerVertices = 1;
        return line;
    }

    void UpdateOutline(float dt)
    {
        qualityFlash = Mathf.Max(0, qualityFlash - dt);
        var color = Color.Lerp(AItem.GetRarityColor(Rarity), Color.white, qualityFlash * .65f);
        outline.startColor = outline.endColor = color;
        outline.startWidth = outline.endWidth = .045f + .025f * qualityFlash;
        claimOutline.enabled = claimCharge > 0;
        if (claimCharge <= 0) return;
        float progress = Mathf.Clamp01(ClaimProgress) * 4;
        int edge = Mathf.Min(3, Mathf.FloorToInt(progress));
        claimOutline.positionCount = edge + 2;
        for (int i = 0; i <= edge; i++) claimOutline.SetPosition(i, corners[i] + Vector3.back * .01f);
        claimOutline.SetPosition(edge + 1, Vector3.Lerp(corners[edge], corners[(edge + 1) % 4], progress - edge) + Vector3.back * .01f);
    }

    // Tiny directly drawn key/button sprites. Keep the hint beside the box, with no caption.
    static Sprite Glyph(int kind)
    {
        if (glyphs[kind] != null) return glyphs[kind];
        string[] rows = kind == 0 ? new[] { "01110", "11001", "11000", "11000", "11000", "11001", "01110" } :
            kind == 1 ? new[] { "00100", "00100", "01010", "01010", "10001", "10001", "11111" } :
            new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" };
        var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "Cargo control glyph" };
        var pixels = new Color[256];
        for (int y = 1; y < 15; y++)
            for (int x = 1; x < 15; x++)
                pixels[y * 16 + x] = x == 1 || x == 14 || y == 1 || y == 14 ? new Color(.68f, .76f, .79f) : new Color(.035f, .055f, .08f, .95f);
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
                if (rows[y][x] == '1') pixels[(11 - y) * 16 + 5 + x] = new Color(.94f, .96f, .9f);
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return glyphs[kind] = Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.one * .5f, 16);
    }

    void Update()
    {
        if (finished)
            return;
        if (ship == null || ship.health <= 0 || GameOverState.Active)
        {
            Destroy(gameObject);
            if (FlightTrials.EscortsCargo) GameOverState.Show();
            return;
        }
        float dt = GameClock.ActivePlayDelta;
        contact.enabled = GameClock.ActivePlay;
        prompt.enabled = collect != null && GameClock.ActivePlay;
        if (dt <= 0)
        {
            claimCharge = 0;
            claimArmed = false;
            UpdateOutline(0);
            return;
        }
        grace = Mathf.Max(0, grace - dt);
        var target = FollowTarget(ship.transform.position);
        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, .07f, GameRules.playerMoveSpeed * 2, dt);
        // Hard turns cannot stretch the tow across the combat area.
        transform.position = target + Vector3.ClampMagnitude(transform.position - target, .35f);
        hull.localRotation = Quaternion.Euler(-14, Mathf.Sin(Time.time * 2) * 10, Mathf.Clamp(-velocity.x * 3, -18, 18));
        float visible = grace > 0 ? .7f + .3f * Mathf.Cos(Time.time * 28) : 1;
        hull.localScale = Vector3.one * visible;
        if (collect != null)
        {
            int glyph = !DockInstrument.UsingGamepad ? 0 : Gamepad.current is UnityEngine.InputSystem.XInput.XInputController ? 2 : 1;
            prompt.sprite = Glyph(glyph);
            if (!collect.IsPressed()) claimArmed = true;
            claimCharge = claimArmed && collect.IsPressed() ? Mathf.Min(ClaimSeconds, claimCharge + dt) : 0;
            if (claimCharge >= ClaimSeconds) TryClaim();
        }
        UpdateOutline(dt);
    }

    void OnTriggerEnter2D(Collider2D other) => Touch(other);
    void OnTriggerStay2D(Collider2D other) => Touch(other);
    void Touch(Collider2D other)
    {
        if (finished || grace > 0 || !GameClock.ActivePlay)
            return;
        var bullet = other.GetComponent<AEnemyBullet>();
        if (bullet == null || bullet is PulseLaser)
            return;
        var round = bullet as CombatRound;
        if (round != null && !round.PixelOverlaps(contact))
            return;
        bullet.OnTrigger();
        Hit();
    }

    public void Hit()
    {
        if (finished || grace > 0)
            return;
        grace = .8f;
        Hits--;
        seals[Hits].gameObject.SetActive(false);
        PSXImpactEffects.Burst(transform.position, new Color(.95f, .62f, .3f));
        ItemSound.Play("shatter", .15f);
        if (Hits <= 0)
        {
            finished = true;
            contact.enabled = false;
            PSXImpactEffects.Explosion(transform.position, .65f);
            Destroy(gameObject);
            if (FlightTrials.EscortsCargo) GameOverState.Show();
        }
    }

    public bool FinishWave()
    {
        if (finished) return false;
        var previous = Quality;
        CompletedWaves++;
        if (Quality != previous)
        {
            qualityFlash = 1;
            UpdateOutline(0);
            ItemSound.Play("ready", .12f);
        }
        return trial && CompletedWaves >= 2;
    }

    public bool TryClaim()
    {
        if (finished || trial || onClaim == null || !GameClock.ActivePlay) return false;
        onClaim(this);
        return finished;
    }

    public bool Deliver()
    {
        if (finished) return false;
        finished = true;
        contact.enabled = false;
        collect?.Disable();
        PSXImpactEffects.Burst(transform.position, AItem.GetRarityColor(Rarity));
        ItemSound.Play("ready", .2f);
        Destroy(gameObject);
        return true;
    }
}
