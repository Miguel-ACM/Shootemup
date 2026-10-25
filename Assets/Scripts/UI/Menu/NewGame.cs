using UnityEngine;

public class NewGame : AMenuElement
{
    bool starting;
    public override void OnAccept()
    {
        if (starting)
            return;
        starting = true;
        GameRules.StartNewRun();
        LevelRun.Reset();
        BGMManager.PlayLevelMusic(LevelRun.Current);

        CloudTransition.Load("L1");
    }
}

