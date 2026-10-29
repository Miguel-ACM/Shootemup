using UnityEngine;

// A nod to Outer Wilds' 22-minute loop: the first death of the run sends the ship back to the
// start of the current district with the hits it had when the district began. The core is spent.
[UnityEngine.Scripting.Preserve]
public sealed class WarpCore : AItem
{
    void Awake()
    {
        SetData("Warp-Core", "Twenty-two minutes", "Die once: restart the level.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        if (s.GetComponent<WarpCoreLoop>() == null)
            s.gameObject.AddComponent<WarpCoreLoop>();
        base.OnPickup();
    }
}

public sealed class WarpCoreLoop : MonoBehaviour
{
    static readonly Color Supernova = new Color(.55f, .8f, 1f);
    // Recorded by the wave orchestrator whenever a district's first wave is prepared.
    public static float LevelStartHealth
    {
        get; private set;
    } = Shipcontroller.StartingHits;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRecord()
    {
        LevelStartHealth = Shipcontroller.StartingHits;
    }

    public static void MarkLevelStart(Shipcontroller ship)
    {
        if (ship != null)
            LevelStartHealth = Mathf.Max(1, ship.health);
    }

    /// <summary>Spends the core instead of dying; the ship is put back at its district start.</summary>
    public static bool TryLoop(Shipcontroller ship)
    {
        if (!ship.TryGetComponent<WarpCoreLoop>(out var core) || GameOverState.Active)
            return false;
        var orchestrator = FindAnyObjectByType<WaveOrchestrer>();
        if (orchestrator == null || CloudTransition.Busy)
            return false;
        Destroy(core);
        ship.Repair(Mathf.RoundToInt(Mathf.Min(LevelStartHealth, ship.maxHealth)));
        var p = ship.transform.position;
        PSXImpactEffects.Explosion(p, 2.4f);
        PSXFireballs.Shockwave(p, 16f, 1.1f, Supernova, 4, .9f);
        ItemSound.Play("timeStop", .26f, .7f);
        orchestrator.RestartLevel();
        return true;
    }
}
