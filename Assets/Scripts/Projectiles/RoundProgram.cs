using UnityEngine;

// Null-safe authored behaviours for any hostile round: a launch refused by the
// live budget returns null, and every step below simply passes that through.
public static class RoundProgram
{
    // Change speed by a multiplier, beginning after a delay and easing over a duration.
    public static T Ramp<T>(this T shot, float multiplier, float over, float after = 0) where T : CombatRound
    {
        if (shot == null)
            return null;
        shot.rampTo = multiplier;
        shot.rampOver = Mathf.Max(.01f, over);
        shot.rampAt = after;
        return shot;
    }

    // Brake to a full stop, hold with a glint, then resume with a new heading and speed.
    public static T Pause<T>(this T shot, float at, float hold, CombatRound.Turn turn = CombatRound.Turn.Keep, float angle = 0, float speed = 1) where T : CombatRound
    {
        if (shot == null)
            return null;
        shot.pauseAt = Mathf.Max(.05f, at);
        shot.pauseHold = hold;
        shot.resumeTurn = turn;
        shot.resumeAngle = angle;
        shot.resumeSpeed = speed;
        return shot;
    }

    // Shrink away at a fixed age, wherever the round happens to be.
    public static T FadeAt<T>(this T shot, float at) where T : CombatRound
    {
        if (shot != null)
            shot.fadeAt = at;
        return shot;
    }

    // Reflect from the side walls and ceiling a limited number of times.
    public static T Bounce<T>(this T shot, int walls) where T : CombatRound
    {
        if (shot != null)
            shot.bounces = walls;
        return shot;
    }

    // Constant acceleration in world space, independent of the heading.
    public static T Fall<T>(this T shot, Vector2 acceleration) where T : CombatRound
    {
        if (shot != null)
            shot.gravity = acceleration;
        return shot;
    }

    // Pulled toward a point in the arena: rounds curve round it, orbit and slingshot past.
    public static T Attract<T>(this T shot, Vector2 point, float strength) where T : CombatRound
    {
        if (shot == null)
            return null;
        shot.well = point;
        shot.pull = strength;
        return shot;
    }

    // Lateral sine motion around the travel line.
    public static T Sway<T>(this T shot, float amplitude, float cycles) where T : CombatRound
    {
        if (shot == null)
            return null;
        shot.swayAmplitude = amplitude;
        shot.swayRate = cycles;
        return shot;
    }

    // Lightning: alternate sharp turns either side of the original line.
    public static T Zigzag<T>(this T shot, float every, float angle) where T : CombatRound
    {
        if (shot == null)
            return null;
        shot.zigEvery = every;
        shot.zigAngle = angle;
        shot.zigSign = -1;
        shot.nextZig = every * .5f;
        return shot;
    }

    // Break into a fan of small rounds around the current heading, or toward the player.
    public static T SplitAt<T>(this T shot, float at, int count, float spread, float speed, float radius = .14f, bool aimed = false) where T : CombatRound
    {
        if (shot == null)
            return null;
        shot.splitAt = at;
        shot.splitCount = count;
        shot.splitSpread = spread;
        shot.splitSpeed = speed;
        shot.splitRadius = radius;
        shot.splitAimed = aimed;
        return shot;
    }

    // On reaching the arena edge, cling to it and circle the walls for a while.
    public static T RunWalls<T>(this T shot, float seconds, int direction = 1) where T : CombatRound
    {
        if (shot == null)
            return null;
        shot.wallDirection = direction >= 0 ? 1 : -1;
        shot.wallRunFor = seconds;
        return shot;
    }
}
