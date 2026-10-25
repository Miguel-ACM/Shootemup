using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Life : APickable
{
    public float maxHealthFraction;
    protected override void PickUp()
    {
        Shipcontroller s = GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();
        if (maxHealthFraction > 0)
            s.HealFraction(maxHealthFraction);
        else
            s.HealFlat(2);
        base.PickUp();
    }
}
