using UnityEngine;

public class QuitGame : AMenuElement 
{
    public override void OnAccept()
    {
        Debug.Log("QUIT");
        Application.Quit();
    }
}
