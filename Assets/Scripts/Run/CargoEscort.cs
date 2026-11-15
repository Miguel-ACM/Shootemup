using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

// Keep the crate alive to improve its contents, or deliberately open it in flight.
// The simulator's two-wave delivery remains a separate, non-redeemable objective.
public sealed class CargoEscort : MonoBehaviour
{
    public const int MinimumOfferWaves = 5;
    public const float FollowDistance = .95f, FollowSmoothing = .3f, FollowSpeed = 1.4f, ClaimSeconds = .6f;
    public int Hits { get; private set; } = 3;
    public int CompletedWaves { get; private set; }
    public int WavesRemaining => Mathf.Max(0, (trial ? 2 : 3) - CompletedWaves);
    public ItemRarity Quality => (ItemRarity)Mathf.Min(3, CompletedWaves);
    public string Rarity => rarityNames[(int)Quality];
    public bool Finished => finished;
    // The simulator's Cold Delivery crate, which counts only its two ordinary waves.
    public bool Trial => trial;
    public float ClaimProgress => claimCharge / ClaimSeconds;
    public InputAction OpenControl => collect;
    Shipcontroller ship;
    CircleCollider2D contact;
    Transform hull, lid;
    LineRenderer outline;
    InputAction collect;
    Action<CargoEscort> onClaim;
    readonly Transform[] seals = new Transform[3];
    static readonly Vector3[] corners = { new Vector3(-.34f, -.33f, -.36f), new Vector3(-.34f, .33f, -.36f),
        new Vector3(.34f, .33f, -.36f), new Vector3(.34f, -.33f, -.36f) };
    static readonly string[] rarityNames = { "common", "rare", "epic", "legendary" };
    Vector3 velocity;
    // Campaign cargo cannot be shot: it shatters the moment the pilot takes a hit.
    // The Cold Delivery simulator trial keeps its three-hit crate that bullets wear down.
    float pilotHealth = -1;
    float grace, claimCharge, qualityFlash;
    bool finished, trial, claimArmed;

    public static CargoEscort Create(Shipcontroller ship, Action<CargoEscort> onClaim = null)
    {
        var go = new GameObject("Escort cargo");
        go.transform.position = ship.transform.position + Vector3.down * .85f;
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
        lid = new GameObject("Hinged crate lid").transform;
        lid.SetParent(hull, false);
        lid.localPosition = new Vector3(0, .28f, .21f);
        lid.gameObject.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CargoLid();
        var lidRenderer = lid.gameObject.AddComponent<MeshRenderer>();
        lidRenderer.sharedMaterial = CombatMaterials.Fleet;
        lidRenderer.sortingLayerName = "Player";
        for (int i = 0; i < seals.Length; i++)
        {
            var seal = new GameObject("Cargo seal");
            seal.transform.SetParent(hull, false);
            seal.transform.localPosition = new Vector3((i - 1) * .16f, .22f, -.28f);
            seal.transform.localScale = new Vector3(.1f, .06f, .06f);
            seal.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CargoSeal();
            seal.AddComponent<MeshRenderer>().sharedMaterial = CombatMaterials.Fleet;
            seals[i] = seal.transform;
            // Only the trial's crate counts hits; campaign cargo has no seals to lose.
            seal.SetActive(trial);
        }
        outline = Border("Cargo rarity outline", .045f);
        outline.loop = true;
        outline.positionCount = corners.Length;
        outline.SetPositions(corners);
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
        if (dt <= 0)
        {
            claimCharge = 0;
            claimArmed = false;
            UpdateOutline(0);
            return;
        }
        grace = Mathf.Max(0, grace - dt);
        if (!trial)
        {
            if (pilotHealth >= 0 && ship.health < pilotHealth)
            {
                Shatter();
                return;
            }
            pilotHealth = ship.health;
        }
        var target = FollowTarget(ship.transform.position);
        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, FollowSmoothing, GameRules.playerMoveSpeed * FollowSpeed, dt);
        hull.localRotation = Quaternion.Euler(-14, Mathf.Sin(Time.time * 2) * 10, Mathf.Clamp(-velocity.x * 3, -18, 18));
        float visible = grace > 0 ? .7f + .3f * Mathf.Cos(Time.time * 28) : 1;
        hull.localScale = Vector3.one * visible;
        if (collect != null)
        {
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
        if (finished || grace > 0 || !GameClock.ActivePlay || !trial)
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

    // The pilot was hit: the crate breaks open and its cargo is lost.
    void Shatter()
    {
        if (finished)
            return;
        finished = true;
        contact.enabled = false;
        collect?.Disable();
        PSXImpactEffects.Burst(transform.position, new Color(.95f, .62f, .3f));
        PSXImpactEffects.Explosion(transform.position, .65f);
        ItemSound.Play("shatter", .15f);
        Destroy(gameObject);
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

    public bool Deliver(string itemKey = null)
    {
        if (finished) return false;
        finished = true;
        contact.enabled = false;
        collect?.Disable();
        PSXImpactEffects.Burst(transform.position, AItem.GetRarityColor(Rarity));
        ItemSound.Play("ready", .2f);
        if (itemKey == null) Destroy(gameObject);
        else StartCoroutine(ReleaseItem(itemKey));
        return true;
    }

    IEnumerator ReleaseItem(string key)
    {
        // A display model only: the collectible is created by the normal decision afterwards.
        // It cannot attract, grant a duplicate or ask for a replacement during this animation.
        var model = new GameObject("Cargo item rising from crate").transform;
        model.SetParent(transform, false);
        var mesh = IndustrialMeshFactory.ExpansionItem(key);
        model.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = model.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = EverydayItemMaterials.For(key);
        renderer.sortingLayerName = "Player";
        renderer.sortingOrder = 4;
        float scale = .7f / Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z);
        var start = transform.position + new Vector3(0, .12f, -.6f);
        var raised = start + Vector3.up * 1.05f;
        var exit = new Vector3(start.x, Camera.UpLimit + 2, start.z);
        var hullScale = hull.localScale;
        for (float age = 0; age < 1.6f; age += Time.deltaTime)
        {
            if (GameOverState.Active || ship == null || ship.health <= 0) break;
            float opening = Mathf.SmoothStep(0, 1, age / .35f);
            lid.localRotation = Quaternion.Euler(115 * opening, 0, 0);
            float rise = Mathf.SmoothStep(0, 1, (age - .15f) / .65f);
            float launch = Mathf.Clamp01((age - .85f) / .75f);
            model.position = Vector3.Lerp(Vector3.Lerp(start, raised, rise), exit, launch * launch);
            model.localScale = Vector3.one * (scale * rise);
            model.rotation = Quaternion.Euler(-18, age * 170, -12);
            hull.localScale = hullScale * (1 - Mathf.SmoothStep(0, 1, (age - .9f) / .6f));
            yield return null;
        }
        Destroy(gameObject);
    }
}
