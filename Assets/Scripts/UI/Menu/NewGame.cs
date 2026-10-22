using UnityEngine;

public class NewGame : AMenuElement
{
    bool starting;
    public override void OnAccept()
    {
        if (starting) return;
        starting = true;
        GameRules.StartNewRun();
        
        CloudTransition.Load("L1");
    }
}

