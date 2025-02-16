using UnityEngine;

public class Quit : APauseElement 
{
    public override void OnAccept()
    {
        // Quit the game, no questions asked
        Debug.Log("QUIT");
        Application.Quit();
    }
}
