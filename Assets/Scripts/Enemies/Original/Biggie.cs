using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Biggie : AEnemy
{
    protected override void Start()
    {
        size =  1.1f;
        base.Start();
    }
}