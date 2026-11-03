using UnityEngine;

public static class GameClock
{
    public static float SimulationDelta => Time.deltaTime;
    public static float PlayerDelta => WorldTimeStop.PlayerCanAct ? Time.unscaledDeltaTime : Time.deltaTime;
    public static float PresentationDelta => Time.unscaledDeltaTime;
    public static bool ActivePlay => !Menu.GameplayPaused && !CombatDecision.Active && !CloudTransition.Busy &&
        !GameRules.playerDisabled && !GameOverState.Active && !HangarHub.Active;
    public static float ActivePlayDelta => ActivePlay ? SimulationDelta : 0;
}
