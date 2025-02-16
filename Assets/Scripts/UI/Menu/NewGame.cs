public class NewGame : AMenuElement 
{
    public override void OnAccept()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("L1");
    }
}
