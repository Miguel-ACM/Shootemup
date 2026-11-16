using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Money : APickable
{
    public static readonly HashSet<Money> Live = new HashSet<Money>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetLive() => Live.Clear();

    void OnEnable() => Live.Add(this);
    void OnDisable() => Live.Remove(this);

    protected override void PickUp()
    {
        RunCoins.Add(1);
        SoundManager.PlayGated("coin", .1f, .045f);
        if (CombatUpgrades.Current != null)
            CombatUpgrades.Current.Coin();
        base.PickUp();
    }

}
