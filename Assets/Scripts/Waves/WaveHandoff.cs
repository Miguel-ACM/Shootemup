using UnityEngine;

// Match visible entrances to visible exits instead of the offscreen tails of authored paths.
public static class WaveHandoff
{
    public const float VisibleOverlap = .35f;
    public static float At(Wave outgoing, Wave incoming)
    {
        float last = Boundary(outgoing, true), first = Boundary(incoming, false), allLaunched = 0;
        foreach (var unit in outgoing.units)
            allLaunched = Mathf.Max(allLaunched, unit.time + .05f);
        return Mathf.Clamp(last - first - VisibleOverlap, allLaunched, Mathf.Max(allLaunched, outgoing.duration));
    }

    static float Boundary(Wave wave, bool exit)
    {
        float result = exit ? 0 : float.PositiveInfinity;
        foreach (var unit in wave.units)
        {
            if (unit.curve == null)
            {
                float fallback = exit ? wave.duration : unit.time + 1.15f;
                result = exit ? Mathf.Max(result, fallback) : Mathf.Min(result, fallback);
                continue;
            }
            for (int step = 0; step <= 64; step++)
            {
                float progress = exit ? 1 - step / 64f : step / 64f;
                float clock = progress * unit.flightDuration;
                var p = WaveGeometry.Position(unit, clock);
                if (p.x < Camera.LeftLimit || p.x > Camera.RightLimit || p.y < Camera.DownLimit || p.y > Camera.UpLimit)
                    continue;
                float at = unit.time + clock / Mathf.Max(.01f, WaveComposer.Tempo);
                result = exit ? Mathf.Max(result, at) : Mathf.Min(result, at);
                break;
            }
        }
        return float.IsFinite(result) ? result : 0;
    }
}
