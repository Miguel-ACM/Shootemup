using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AActiveItem : AItem
{
    public float cost = 20f;
    public float cd = 2f;
    public float lastCD = 1000f;


    public override void OnPickup()
    {
        s.AddActiveRight(this);
        base.OnPickup();
    }

    protected new void Update()
    {
        base.Update();
        lastCD += Time.deltaTime;
    }

    public float CheckValidPress(float mana)
    {
        if (mana < cost)
            return -1;
        if (lastCD < cd)
            return -2;
        return 0;
    }

    public override void OnBegin()
    {
    }

    public void SetCost(float _cost)
    {
        cost = _cost;
    }

    public void SetCD(float _cd)
    {
        cd = _cd;
    }


    public virtual float OnPress(float mana)
    {
        return mana;
    }
    public virtual float OnRelease(float mana)
    {
        return mana;
    }
    public virtual float OnHold(float mana)
    {
        return mana;
    }


}
