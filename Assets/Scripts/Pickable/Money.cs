using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Money : APickable
{
    protected override void PickUp()
    {
        RunCoins.Add(1);
        if(CombatUpgrades.Current!=null)CombatUpgrades.Current.Coin();
        base.PickUp();
    }

}
