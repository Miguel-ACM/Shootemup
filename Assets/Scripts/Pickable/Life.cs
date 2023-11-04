using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Life : APickable
{
    protected override void PickUp()
    {
        Shipcontroller s = GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();
        s.HealFlat(2);
        base.PickUp();
    }
}
