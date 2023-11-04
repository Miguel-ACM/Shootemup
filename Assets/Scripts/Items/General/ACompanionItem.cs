using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ACompanionItem : AItem
{
    protected string companionName = null;
    private ACompanion companion;

    public override void OnPickup()
    {
        s.AddCompanion(companionName);
        base.OnPickup();
    }

    public string GetCompanionName()
    {
        return companionName;
    }

    /*protected new void Update()
    {
        base.Update();
    }*/

}
