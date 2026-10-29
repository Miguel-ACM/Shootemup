using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Money : APickable
{
    protected override void PickUp()
    {
        RunCoins.Add(1);
        SoundManager.PlayGated("coin", .1f, .045f);
        if (CombatUpgrades.Current != null)
            CombatUpgrades.Current.Coin();
        base.PickUp();
    }

}
