using UnityEngine;

public sealed class BossPhaseState
{
    public int Phase { get; set; } = 1;
    public float TransitionUntil { get; set; }
    public float MeterStart { get; set; } = 1;
    public bool Transitioning(float clock) => clock < TransitionUntil;
    public void BeginBreak(float clock, float duration, float meterValue)
    {
        TransitionUntil = clock + duration;
        MeterStart = meterValue;
    }
    public float Fraction(float value, float end) => Mathf.Clamp01((value - end) / Mathf.Max(.00001f, MeterStart - end));
}
