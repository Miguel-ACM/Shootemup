using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Money : APickable
{
    protected override void PickUp()
    {
        Score.IncreaseScore(10);
        base.PickUp();
    }

}
