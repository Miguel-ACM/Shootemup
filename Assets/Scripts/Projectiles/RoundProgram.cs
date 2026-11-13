using UnityEngine;

// Null-safe authored behaviours for any hostile round: a launch refused by the
// live budget returns null, and every step below simply passes that through.
public static class RoundProgram
{
    // A giant carrier that keeps flying while rapidly firing from either flank.
    public static GiantOrb SideSpray(this GiantOrb shot, float velocity = 1.25f)
    {
        if (shot != null)
            shot.EnableSideSpray(velocity);
        return shot;
    }

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

    // While a Pause holds it, a long round turns to face the heading it will resume on,
    // so a stopped arrow shows where it goes next.
    public static T ShowTurn<T>(this T shot) where T : CombatRound
    {
        if (shot != null)
            shot.showTurn = true;
        return shot;
    }

    // Visual only: the round never touches the pilot (snow-throwing sprays).
    public static T Harmless<T>(this T shot) where T : CombatRound
    {
        if (shot != null)
            shot.MakeHarmless();
        return shot;
    }

    // Legacy score endpoint: coast out of the scene at this age, without shrinking
    // or fading. Keep the name for existing authored scores and tuning data.
    public static T FadeAt<T>(this T shot, float at) where T : CombatRound
    {
        if (shot != null)
            shot.fadeAt = at;
        return shot;
    }

    // Reflect from the side walls, and the ceiling unless told not to, a limited
    // number of times, on the visible screen border.
    public static T Bounce<T>(this T shot, int walls, bool ceiling = true) where T : CombatRound
    {
        if (shot != null)
        {
            shot.bounces = walls;
            shot.bounceCeiling = ceiling;
        }
        return shot;
    }

    // Constant acceleration in world space, independent of the heading.
    public static T Fall<T>(this T shot, Vector2 acceleration) where T : CombatRound
    {
        if (shot != null)
            shot.gravity = acceleration;
        return shot;
    }

    // Override the launch lifetime, for rounds that loop back through the arena.
    public static T LivesFor<T>(this T shot, float seconds) where T : CombatRound
    {
        if (shot != null)
            shot.SetLifetime(seconds);
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

    // Drawn into a moving body that swallows the round once it comes within reach.
    public static T Absorb<T>(this T shot, Transform into, float strength, float reach) where T : CombatRound
    {
        if (shot == null)
            return null;
        shot.sink = into;
        shot.well = into.position;
        shot.pull = strength;
        shot.sinkRadius = reach;
        return shot;
    }

    // Pushed away from a point: the round accelerates out and bends onto the radial.
    public static T Repel<T>(this T shot, Vector2 point, float strength) where T : CombatRound
    {
        if (shot == null)
            return null;
        shot.well = point;
        shot.pull = -strength;
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
