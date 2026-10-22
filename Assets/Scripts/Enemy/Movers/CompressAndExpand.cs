using UnityEngine;

public class CompressAndExpand : PatternMover
{
    [SerializeField] private Transform formationAnchor;
    [SerializeField] private float centreX;
    [SerializeField, Range(0, 1)] private float compressedScale = .35f;
    [SerializeField, Min(.01f)] private float transitionDuration = 1.5f;
    [SerializeField, Min(.01f)] private float firingDuration = 2f;
    [SerializeField, Min(0)] private float recoveryDuration = 1f;
    private enum Phase { Compress, Fire, Expand, Recover }
    private Phase phase;
    private Vector3 restOffset, fixedAnchor;
    private float clock;

    protected override void InitializePattern()
    {
        fixedAnchor = new Vector3(centreX, origin.y, origin.z);
        restOffset = origin - (formationAnchor != null ? formationAnchor.position : fixedAnchor);
    }

    protected override void StepPattern(float dt)
    {
        clock += dt;
        float scale = 1;
        switch (phase)
        {
            case Phase.Compress:
                scale = Mathf.Lerp(1, compressedScale, Mathf.SmoothStep(0, 1, clock / Mathf.Max(.01f, transitionDuration)));
                break;
            case Phase.Fire: scale = compressedScale; break;
            case Phase.Expand:
                scale = Mathf.Lerp(compressedScale, 1, Mathf.SmoothStep(0, 1, clock / Mathf.Max(.01f, transitionDuration)));
                break;
        }
        Vector3 centre = formationAnchor != null ? formationAnchor.position : fixedAnchor;
        Vector3 target = centre + new Vector3(restOffset.x * scale, restOffset.y, 0);
        bool reached = Travel(InBounds(target), dt);
        if (phase == Phase.Compress && clock >= transitionDuration && reached)
        { Arrive(); Next(Phase.Fire); SetFiring(true); }
        else if (phase == Phase.Fire && clock >= firingDuration)
        { SetFiring(false); Next(Phase.Expand); }
        else if (phase == Phase.Expand && clock >= transitionDuration && reached) Next(Phase.Recover);
        else if (phase == Phase.Recover && clock >= recoveryDuration) Next(Phase.Compress);
    }

    private void Next(Phase next) { phase = next; clock = 0; }
}
