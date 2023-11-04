using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mana : APickable
{
    protected override void PickUp()
    {
        Shipcontroller s = GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();
        s.HealManaFlat(2);
        base.PickUp();
    }
}