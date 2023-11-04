using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Biggie : AEnemy
{
    new public void Start()
    {
        size =  1.1f;
        base.Start();
    }
}