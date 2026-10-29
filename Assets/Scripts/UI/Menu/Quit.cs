using UnityEngine;

public class Quit : AMenuElement
{
    public override void OnAccept()
    {
        // Leaving mid-run still banks the wallet; leaving the hangar does nothing.
        MetaProgress.EndRun();
        CloudTransition.Load("Title", HangarHub.Leave);
    }
}

