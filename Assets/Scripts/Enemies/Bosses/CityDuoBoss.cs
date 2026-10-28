using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed partial class CityDuoBoss : DistrictBoss
{
    public bool heavy;
    public CityDuoBoss partner;
    [EnemyTunable("Heavy hull / Base health", 1, 50000)] public float heavyBaseHealth = 1400;
    bool enraged;
    float combinedMaximum;
    public override bool NoseAimed => false;
    public override float DisplayFraction => (Mathf.Max(0, health) + (partner != null ? Mathf.Max(0, partner.health) : 0)) / Mathf.Max(1, combinedMaximum);
    protected override void Configure()
    {
        hull = 12;
        hullScale = .8f;
        health = 1000;
        attackInterval = (.64f);
        shellTravelDistance = 5.8f;
    }

    protected override void Start()
    {
        if (heavy)
        {
            hull = 14;
            hullScale = 1.15f;
            size = hullScale;
            Hull.GetComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CombatHull(14);
            Hull.localScale = Vector3.one * hullScale;
            firingCue.Build(Hull);
            GetComponent<CircleCollider2D>().radius = hullScale * .8f;
            GetComponent<EnemyThrusters>().sockets = IndustrialMeshFactory.CombatEngineSockets(14);
        }
        base.Start();
        if (partner == null && !heavy)
        {
            partner = (CityDuoBoss)Spawn(typeof(CityDuoBoss), new Vector3(2, Camera.UpLimit + 2, 1));
            partner.heavy = true;
            partner.health = partner.heavyBaseHealth * GameRules.enemyHealth * LevelRun.EnemyHealthScale;
            partner.partner = this;
            ShareEncounterWith(partner);
        }
        combinedMaximum = health + (partner != null ? partner.health : 0);
        if (bar != null)
            bar.SetMax(combinedMaximum);
    }

    protected override void MoveBoss(float dt)
    {
        float pace = heavy ? .8f : 2.05f;
        station.x = Mathf.Sin(motionAge * pace + (heavy ? Mathf.PI : 0)) * (heavy ? 2.1f : 3.2f);
        station.y = Camera.UpLimit - (heavy ? 2 : 3.5f);
        transform.position = Vector3.MoveTowards(transform.position, station, dt * (heavy ? 2.8f : 6.8f) * (enraged ? 1.2f : 1));
    }
    // Both ships keep one tempo until a partner dies; independent half-health
    // acceleration used to scramble the duo's alternating releases.
    protected override bool PhaseTwoReady => false;
    protected override float AttackPace => enraged ? (.83f) : 1;
    protected override void PressureAttack()
    {
    }

    protected override void UpdateBar()
    {
        if (bar != null)
            bar.Set(Mathf.Max(0, health) + (partner != null ? Mathf.Max(0, partner.health) : 0));
    }

    protected override void Die()
    {
        if (partner != null && partner.health > 0)
        {
            partner.health += Mathf.Max(0, partner.initialHealth - partner.health) * .5f;
            partner.enraged = true;
            partner.phaseTwo = true;
            partner.volley = 0;
            partner.nextAttack = partner.age + .8f;
            partner.partner = null;
            partner.NotifyBossPhaseChange(2);
            partner.PresentPhaseTwo();
            // The first kill gives neither pickups nor an encounter-completion event.
            isBoss = false;
            lifeDropChance = manaDropChance = moneyDropChance = 0;
            scoreOnKill = 0;
            bar = null;
        }
        base.Die();
    }

}
