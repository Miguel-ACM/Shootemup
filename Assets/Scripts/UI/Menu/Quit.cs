using UnityEngine;

public class Quit : AMenuElement 
{
    public override void OnAccept()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
    }
}
