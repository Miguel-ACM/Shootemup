using UnityEngine;

// Two ordinary phrases with a vulnerable, three-hit crate in tow. Its contact
// joins the existing nearby-target window and exact pixel narrow phase.
public sealed class CargoEscort : MonoBehaviour
{
    public int Hits { get; private set; } = 3;
    public int WavesRemaining { get; private set; } = 2;
    Shipcontroller ship;
    CircleCollider2D contact;
    Transform hull;
    readonly Transform[] seals = new Transform[3];
    Vector3 velocity;
    float grace;
    bool finished;

    public static CargoEscort Create(Shipcontroller ship)
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
        cargo.grace = 1;
        cargo.Build();
        return cargo;
    }

    void OnEnable() => ProjectileCollisionWindow.Register(GetComponent<Collider2D>());
    void OnDisable() => ProjectileCollisionWindow.Unregister(GetComponent<Collider2D>());
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
    }

    void Update()
    {
        if (finished)
            return;
        if (ship == null || ship.health <= 0 || GameOverState.Active)
        {
            Destroy(gameObject);
            if (FlightTrials.Active == FlightTrials.All[1]) GameOverState.Show();
            return;
        }
        float dt = GameClock.ActivePlayDelta;
        contact.enabled = GameClock.ActivePlay;
        if (dt <= 0)
            return;
        grace = Mathf.Max(0, grace - dt);
        var target = ship.transform.position + new Vector3(0, -.95f, .2f);
        target.x = Mathf.Clamp(target.x, Camera.LeftLimit + .4f, Camera.RightLimit - .4f);
        target.y = Mathf.Max(Camera.DownLimit + .45f, target.y);
        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, .3f, GameRules.playerMoveSpeed * 1.4f, dt);
        hull.localRotation = Quaternion.Euler(-14, Mathf.Sin(Time.time * 2) * 10, Mathf.Clamp(-velocity.x * 3, -18, 18));
        float visible = grace > 0 ? .7f + .3f * Mathf.Cos(Time.time * 28) : 1;
        hull.localScale = Vector3.one * visible;
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
            if (FlightTrials.Active == FlightTrials.All[1]) GameOverState.Show();
        }
    }

    public bool FinishWave() => !finished && --WavesRemaining == 0;
    public void Deliver()
    {
        finished = true;
        contact.enabled = false;
        PSXImpactEffects.Burst(transform.position, new Color(.95f, .78f, .35f));
        ItemSound.Play("ready", .2f);
        Destroy(gameObject);
    }
}
