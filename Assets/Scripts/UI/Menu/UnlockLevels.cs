public sealed class UnlockLevels : AMenuElement
{
    public override void OnAccept()
    {
        if (!MetaProgress.UnlockAllRoutes())
        {
            textElement.text = "SAVE FAILED";
            return;
        }

        textElement.text = "LEVELS UNLOCKED";
        FindAnyObjectByType<LevelChoice>()?.RefreshOpenRoutes();
        SoundManager.PlaySound("uiConfirm", .1f);
    }
}
