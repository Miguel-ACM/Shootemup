public class Continue : APauseElement 
{
    public override void OnAccept()
    {
        pauseMenu.setMenuOpen(false);
    }
}
