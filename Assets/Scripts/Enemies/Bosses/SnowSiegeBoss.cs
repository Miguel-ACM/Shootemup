using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class SnowSiegeBoss : DistrictBoss
{
    Vector2 shellAxis = Vector2.down;
    protected override void Configure()
    {
        hull = 21;
        hullScale = 1.6f;
        health = 2100;
        attackInterval = .65f;
        shellTravelDistance = 6.6f;
    }

    protected override void MoveBoss(float dt)
    {
        station.x = Mathf.Sin(motionAge * .45f) * 1.8f;
        base.MoveBoss(dt);
    }

    protected override void PressureAttack()
    {
    } // The score below owns the rests as well as the volleys.
    protected override void Attack()
    {
        int beat = (volley - 1) % 12;
        if (beat == 0)
            shellAxis = Aim;
        if (beat < 3)
        {
            // Three broad crowns leave room for complete rings instead of queueing
            // oversized shell clusters against the shared projectile cap.
            int count = phaseTwo ? 6 : 5;
            float distance = beat % 2 == 0 ? shellTravelDistance : shellTravelDistance - 2;
            for (int i = 0; i < count; i++)
            {
                var shot = Fire<BurstShell>(Hull.TransformPoint(new Vector3(0, -.65f, -.25f)),
                    CombatVisuals.Rotate(shellAxis, (i - (count - 1) * .5f) * 22 + (beat % 2 == 0 ? -5.5f : 5.5f)), 4.3f, .44f);
                if (shot != null)
                    shot.SetTravelDistance(distance);
            }
        }
        else if (beat == 4 || beat == 6)
        {
            // Two straight combs answer the shell crowns; the second fills the first's lanes.
            int count = phaseTwo ? 13 : 10;
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(Camera.LeftLimit + .35f, Camera.RightLimit - .35f, (i + .5f) / count);
                if (beat == 6)
                    x += .22f;
                Fire<SuspendedRound>(new Vector3(x, transform.position.y - .7f, 0), Vector2.down, phaseTwo ? 6 : 5, .18f);
            }
        }
        else if (beat == 8 || beat == 9)
        {
            int sign = beat == 8 ? -1 : 1;
            int count = phaseTwo ? 17 : 13;
            PetalFan<NeedleRound>(Hull.TransformPoint(new Vector3(0, -.65f, -.25f)),
                CombatVisuals.Rotate(Vector2.down, sign * 11.25f), count, 9, phaseTwo ? 5.8f : 5, .13f, .18f);
        }
        // Beats 3, 5, 7, 10 and 11 are intentional rests while existing patterns unfold.
    }

    protected override void OnPhaseTwo()
    {
        volley = 0;
        nextAttack = age + 1.1f;
    }
}
