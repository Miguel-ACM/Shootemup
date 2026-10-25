public static class CombatDecision
{
    public static bool Active
    {
        get; private set;
    }

    public static void Begin()
    {
        Active = true;
        GameRules.playerDisabled = true;
    }

    public static void End()
    {
        Active = false;
        GameRules.playerDisabled = GameOverState.Active;
    }
}
