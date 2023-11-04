using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitPlayer : AMover
{
    private int index;
    private float offset;
    Shipcontroller s;

    new void Start()
    {
        base.Start();
        s = GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();
    }

    public void SetIndex(int index, int numOrbitals)
    {
        this.index = index;
        this.offset = index / numOrbitals;
    }

    public void SetMaxIndex(int numOrbitals)
    {
        this.offset = (float) this.index / (float) numOrbitals;
    }

    public override void Move()
    {
        //Debug.Log(index);
        float rotationPer = Mathf.PI * 2 * (((Time.time % GameRules.orbitTime / GameRules.orbitTime) + this.offset));//((Time.time % GameRules.orbitTime / GameRules.orbitTime + this.offset) % 1);

        Vector2 pos = s.transform.position;
        this.transform.position = new Vector3(pos.x + Mathf.Cos(rotationPer) * GameRules.orbitDistance, pos.y + Mathf.Sin(rotationPer) * GameRules.orbitDistance, this.transform.position.z);
    }


}
