using UnityEngine;

// Loan Shark: coins now, repayments for the rest of the run. At every new district the shark
// swims in for its cut; if the wallet is short it takes what there is and bites off a hit.
[UnityEngine.Scripting.Preserve]
public sealed class LoanShark : APactItem
{
    public const int Loan = 80, Repayment = 15;

    void Awake()
    {
        SetPact("Loan Shark", "+" + Loan + " coins now.", "Every new district it takes " + Repayment + ". Short? It bites off a max hit.");
    }

    protected override void Seal()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.LoanShark);
        RunCoins.Add(Loan);
        ItemSpark.Emit(s.transform.position, new Color(1, .79f, .22f), 12, .12f);
        ItemSound.Cue("itemSalvage", volume: .12f, pitch: .85f);
    }

    /// <summary>The shark collects one repayment per copy as a new district opens.</summary>
    public static void Collect(Shipcontroller ship, int copies)
    {
        if (copies <= 0 || ship == null)
            return;
        int owed = Repayment * copies;
        bool shortfall = RunCoins.Count < owed;
        RunCoins.TrySpend(Mathf.Min(owed, RunCoins.Count));
        SharkVisit.Begin(ship, shortfall && ship.maxHealth > 1);
    }
}

// The shark itself swims across under the ship while coins stream out of it into its jaws.
// When the wallet came up short it lunges and takes a bite (a max hit).
public sealed class SharkVisit : MonoBehaviour
{
    const float Duration = 1.6f, Width = 1.1f;
    static readonly Color Gold = new Color(1, .79f, .22f);
    Shipcontroller ship;
    Transform model;
    bool bite, bitten;
    float age, side, nextCoin;

    public static void Begin(Shipcontroller ship, bool bite)
    {
        var go = new GameObject("Loan shark visit");
        var visit = go.AddComponent<SharkVisit>();
        visit.ship = ship;
        visit.bite = bite;
        visit.side = ship.transform.position.x > 0 ? -1 : 1;
        var body = new GameObject("Loan shark");
        body.transform.SetParent(go.transform, false);
        var mesh = IndustrialMeshFactory.ExpansionItem(nameof(LoanShark));
        body.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = body.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = EverydayItemMaterials.For(nameof(LoanShark));
        renderer.sortingLayerName = "Player";
        body.transform.localScale = Vector3.one * (Width / Mathf.Max(.01f, mesh.bounds.size.x));
        visit.model = body.transform;
        visit.Place();
        ItemSound.Cue("roll", volume: .1f, pitch: .6f);
    }

    // Crosses from one side to the other, dipping under the ship as it passes; a bite
    // swaps the dip for a lunge straight at the hull.
    void Place()
    {
        float t = Mathf.Clamp01(age / Duration);
        var centre = ship != null ? ship.transform.position : transform.position;
        float lunge = bite ? Mathf.Exp(-Mathf.Pow((t - .5f) * 7, 2)) : 0;
        float dip = .85f * Mathf.Sin(Mathf.PI * t) * (1 - lunge);
        var previous = transform.position;
        transform.position = new Vector3(centre.x + Mathf.Lerp(-5.5f, 5.5f, t) * side, centre.y - dip, -.4f);
        var heading = (Vector2)(transform.position - previous);
        if (heading.sqrMagnitude < .00001f)
            return;
        // The model's nose points along +X: turn it to the travel direction, then pitch and roll.
        float pitch = Mathf.Atan2(heading.y, Mathf.Abs(heading.x)) * Mathf.Rad2Deg;
        model.localRotation = Quaternion.Euler(0, heading.x < 0 ? 180 : 0, 0) * Quaternion.Euler(0, 0, pitch) * Quaternion.Euler(-15, 0, 0);
    }

    void Update()
    {
        if (ship == null)
        {
            Destroy(gameObject);
            return;
        }
        age += Time.deltaTime;
        Place();
        // Coins stream from the ship into the shark while it passes.
        if (age > Duration * .3f && age < Duration * .7f && Time.time >= nextCoin)
        {
            nextCoin = Time.time + .07f;
            ItemSpark.Emit(Vector3.Lerp(ship.transform.position, transform.position, Random.value), Gold, 2, .09f);
        }
        if (bite && !bitten && age >= Duration * .5f)
        {
            bitten = true;
            ship.ReduceMaxHits(1);
            PSXImpactEffects.Burst(ship.transform.position, new Color(.9f, .12f, .2f));
            ItemSound.Cue("shatter", volume: .16f, pitch: .55f);
            RunInventory.Ping(UpgradeKind.LoanShark);
        }
        if (age >= Duration)
            Destroy(gameObject);
    }
}
