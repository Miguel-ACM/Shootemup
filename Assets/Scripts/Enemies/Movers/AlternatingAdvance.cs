using UnityEngine;

public class AlternatingAdvance : PatternMover
{
    [SerializeField] private AlternatingAdvance partner;
    [SerializeField] private bool movesFirst = true;
    [SerializeField, Min(.1f)] private float stepDistance = 1.5f;
    [SerializeField, Min(.01f)] private float holdDuration = 2f;
    [SerializeField, Min(.01f)] private float arrivalWarning = .3f;
    private bool moving, settled, pendingTurn;
    private float holdClock;
    private Vector3 waypoint;
    public bool IsAdvancing => moving;

    public void SetPartner(AlternatingAdvance other, bool first)
    {
        partner = other;
        movesFirst = first;
    }

    protected override void InitializePattern()
    {
        // A unique instance ordering resolves accidentally identical Inspector flags.
        bool first = partner == null || (movesFirst != partner.movesFirst ? movesFirst : GetEntityId().CompareTo(partner.GetEntityId()) < 0);
        if (pendingTurn || first)
            BeginAdvance();
        else
        {
            Arrive();
            SetFiring(false);
        }
    }

    private void BeginAdvance()
    {
        pendingTurn = true;
        moving = true;
        settled = false;
        holdClock = 0;
        waypoint = new Vector3(transform.position.x, transform.position.y - stepDistance, transform.position.z);
        SetFiring(false);
    }

    protected override void StepPattern(float dt)
    {
        if (moving)
        {
            if (Travel(waypoint, dt))
            {
                moving = false;
                pendingTurn = false;
                settled = true;
                Arrive();
                holdClock = 0;
            }
        }
        else
        {
            holdClock += dt;
            SetFiring(holdClock >= arrivalWarning);
            bool partnerGone = partner == null || !partner.isActiveAndEnabled || partner.IsLeaving;
            if ((settled || partnerGone) && holdClock >= holdDuration)
            {
                settled = false;
                if (partnerGone)
                    BeginAdvance();
                else
                    partner.BeginAdvance();
                holdClock = 0;
            }
        }
        if (transform.position.y < Camera.DownLimit - 1)
            Leave();
    }
}
