using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AActiveItem : AItem
{
    public float cost = 20f;
    public float cd = 2f;
    public float lastCD = 1000f;
    protected virtual bool StartsPaidActivation => true;
    public bool StartsActivation => StartsPaidActivation;
    public float EffectivePressCost => StartsPaidActivation ? cost * GameRules.activeEnergyCost : 0;


    public override void OnPickup()
    {
        s.GetComponent<ShipLoadout>().EquipActive(this, ReplacementSlot);
        base.OnPickup();
    }

    protected override void Update()
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

    // Apply efficiency to both the affordability check and the energy actually spent,
    // including original actives which implement their own OnPress subtraction.
    public float PressWithEnergyEfficiency(float mana)
    {
        float originalCost = cost;
        cost *= GameRules.activeEnergyCost;
        try
        {
            float result = OnPress(mana);
            if (result >= 0)
            {
                RunInventory.Ping(this);
                ActiveEcho.Heard(this);
            }
            return result;
        }
        finally { cost = originalCost; }
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
