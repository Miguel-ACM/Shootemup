using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : AMover
{
    public int index;
    Shipcontroller s;

    new void Start()
    {
        base.Start();
        s = GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();

    }

    public void SetIndex(int index)
    {
        this.index = index;
    }

    public override void Move()
    {
        Vector2 pos = s.GetPreviousPosition(this.index * GameRules.followCompanionDelay - 1);
        this.transform.position = new Vector3(pos.x, pos.y, this.transform.position.z);
    }


}
