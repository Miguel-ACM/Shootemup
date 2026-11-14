using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class Grudge : AItem
{
    void Awake()
    {
        SetData("Grudge", "Never forget", "Each hit taken: +0.15 damage this district.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.Grudge);
        if (s.GetComponent<GrudgeTally>() == null)
            s.gameObject.AddComponent<GrudgeTally>();
        base.OnPickup();
    }
}

// Counts the hits the ship takes in the current district; every copy adds .15 shot damage per hit.
public sealed class GrudgeTally : MonoBehaviour
{
    public const float PerHit = .15f;
    static readonly Color Ember = new Color(.85f, .2f, .12f);
    static GrudgeTally current;
    Shipcontroller ship;
    int hits, stage;
    float nextEmber;

    public static float Bonus => current != null ? current.Hits * PerHit * Copies : 0;

    static int Copies => CombatUpgrades.Current != null ? CombatUpgrades.Current.Count(UpgradeKind.Grudge) : 0;

    int Hits
    {
        get
        {
            if (stage != LevelRun.Stage)
            {
                stage = LevelRun.Stage;
                hits = 0;
            }
            return hits;
        }
    }

    void Start()
    {
        current = this;
        stage = LevelRun.Stage;
        ship = GetComponent<Shipcontroller>();
        ship.HitTaken += Remember;
    }

    void OnDestroy()
    {
        if (ship != null)
            ship.HitTaken -= Remember;
        if (current == this)
            current = null;
    }

    void Remember()
    {
        hits = Hits + 1;
        RunInventory.Ping(this);
        ItemProcVfx.Emit(ItemProcShape.Charge, transform.position, Ember, Mathf.Min(.9f, .35f + hits * .05f));
        ItemSpark.Emit(transform.position, Ember, 4, .1f);
        ItemSound.Cue("itemFocus", volume: .08f, pitch: Mathf.Max(.55f, .8f - hits * .03f), cooldown: .3f);
    }

    // A held grudge smoulders: faint embers rise from the hull, a little more often per hit.
    void Update()
    {
        if (!GameClock.ActivePlay || Copies == 0 || Hits == 0 || Time.time < nextEmber)
            return;
        nextEmber = Time.time + Mathf.Max(.45f, 2.2f - hits * .2f);
        ItemSpark.Emit(transform.position + new Vector3(Random.Range(-.25f, .25f), -.2f, 0), Ember, 2, .06f);
    }
}
