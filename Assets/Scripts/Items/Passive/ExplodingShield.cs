using UnityEngine;

// Every ship starts with it: each hit detonates the shield, and hostile rounds keep bursting across the arena for two seconds.
[UnityEngine.Scripting.Preserve]
public sealed class ExplodingShield : AItem
{
    void Awake()
    {
        SetData("Exploding Shield", "Standard issue", "Getting hit clears all bullets for 2 seconds.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        if (s.GetComponent<ShieldDetonation>() == null)
            s.gameObject.AddComponent<ShieldDetonation>();
        base.OnPickup();
    }

    // Granted once when the ship is built, through the normal pickup feedback and inventory.
    public static System.Collections.IEnumerator GrantStarter(Shipcontroller ship)
    {
        yield return null;
        var go = Instantiate(Resources.Load<GameObject>("Items/Item"), new Vector3(100, 100, 1), Quaternion.identity);
        var item = go.AddComponent<ExplodingShield>();
        yield return null;
        if (ship != null && item != null)
            item.GrantTo(ship);
    }
}

public sealed class ShieldDetonation : MonoBehaviour
{
    public const float ClearTime = 2f;
    static readonly Color Blast = new Color(1f, .55f, .2f);
    Shipcontroller ship;
    float clearing;

    void Start()
    {
        ship = GetComponent<Shipcontroller>();
        ship.HitTaken += Detonate;
    }

    void OnDestroy()
    {
        if (ship != null)
            ship.HitTaken -= Detonate;
    }

    void Detonate()
    {
        clearing = ClearTime;
        var p = transform.position;
        PSXImpactEffects.Explosion(p, 1.1f);
        ClearEffects.ScreenClear(Blast);
        SoundManager.PlaySound("blankbullet", .2f);
    }

    void Update()
    {
        if (clearing <= 0)
            return;
        clearing -= Time.deltaTime;
        foreach (var bullet in GameObject.FindGameObjectsWithTag("EnemyBullet"))
        {
            // Beams and tagged hulls (such as contact claws) are not rounds and survive the blast.
            var round = bullet.GetComponent<AEnemyBullet>();
            if (round == null || round is PulseLaser)
                continue;
            if (CombatUpgrades.Current != null)
                CombatUpgrades.Current.Block();
            round.Vanish();
        }
    }
}
