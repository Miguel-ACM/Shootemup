using UnityEngine;

public class NewGame : AMenuElement
{
    bool starting;
    public override void OnAccept()
    {
        if (starting) return;
        starting = true;
        GameRules.StartNewRun();
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("L1");
    }
}
