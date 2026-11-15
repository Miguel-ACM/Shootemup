using System.Collections.Generic;
using UnityEngine;

// Echo Chamber: every active you use echoes. A moment later it goes off once more, free and
// without touching its cooldown. Long casts echo as soon as the first one ends. Healing
// actives and toggles never echo.
[UnityEngine.Scripting.Preserve]
public sealed class EchoChamber : AItem
{
    void Awake()
    {
        SetData("Echo Chamber", "Say it again", "Every active you use goes off again, free, a moment later.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.EchoChamber);
        if (s.GetComponent<ActiveEcho>() == null)
            s.gameObject.AddComponent<ActiveEcho>();
        base.OnPickup();
    }
}

public sealed class ActiveEcho : MonoBehaviour
{
    const float Delay = .6f, Patience = 6;
    static readonly Color Ring = new Color(.78f, .62f, 1f);
    static bool echoing;
    readonly List<(AActiveItem active, float wait)> pending = new List<(AActiveItem, float)>();
    Shipcontroller ship;

    void Start()
    {
        ship = GetComponent<Shipcontroller>();
    }

    static bool Echoes(AActiveItem active) =>
        !(active is HealthPacks || active is FillableFlask || active is PolarityEngine || active is Timewinder);

    /// <summary>Called whenever an active goes off; schedules its echo.</summary>
    public static void Heard(AActiveItem active)
    {
        if (echoing || active == null || active.s == null || !Echoes(active))
            return;
        if (CombatUpgrades.Current == null || CombatUpgrades.Current.Count(UpgradeKind.EchoChamber) == 0)
            return;
        var echo = active.s.GetComponent<ActiveEcho>();
        if (echo != null)
            echo.pending.Add((active, 0));
    }

    void Update()
    {
        if (pending.Count == 0 || !GameClock.ActivePlay || ship == null || ship.health <= 0)
            return;
        var loadout = ship.GetComponent<ShipLoadout>();
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            var (active, wait) = pending[i];
            wait += GameClock.PlayerDelta;
            pending[i] = (active, wait);
            bool equipped = active != null && loadout != null && System.Array.IndexOf(loadout.Actives, active) >= 0;
            if (!equipped || wait > Patience)
            {
                pending.RemoveAt(i);
                continue;
            }
            // A cast still running would take the echo as its second press: wait for it to end.
            if (wait < Delay || active is OffensiveActiveItem offensive && offensive.Cast != null)
                continue;
            pending.RemoveAt(i);
            Echo(active);
        }
    }

    void Echo(AActiveItem active)
    {
        float cooldown = active.lastCD;
        active.lastCD = active.cd;
        echoing = true;
        try
        {
            // Plenty of energy is offered and the result discarded: the echo is free.
            if (active.PressWithEnergyEfficiency(1e6f) < 0)
                return;
        }
        finally
        {
            echoing = false;
            active.lastCD = cooldown;
        }
        RunInventory.Ping(UpgradeKind.EchoChamber);
        var p = ship.transform.position;
        ItemProcVfx.Emit(ItemProcShape.Return, p, Ring, .7f);
        PSXFireballs.Shockwave(p, 1.2f, .4f, Ring, 2, .6f);
        ItemSound.Cue("ready", volume: .07f, pitch: .7f, cooldown: .2f);
    }
}
