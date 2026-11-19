using System.Collections;
using UnityEngine;

public sealed partial class VoidSovereignBoss
{
    const float InterludeLead = .65f, PassageSeconds = 7f;
    const float ChargeAt = 2.45f, FireAt = 3.4f, MoveAt = 3.65f, ArriveAt = 6.45f, CeaseAt = 6.65f;
    readonly PulseLaser[] interludeLasers = new PulseLaser[2];

    IEnumerator Interlude(int chapter)
    {
        Hidden = true;
        InterludeElapsed = 0;
        contact.enabled = false;
        foldBasis = Hull.localRotation;
        int passage = -1, beat = 0;
        float nextBeat = 0;
        while (InterludeElapsed < 15)
        {
            if (!GameRules.playerDisabled && !WorldTimeStop.Frozen)
            {
                float previous = InterludeElapsed;
                InterludeElapsed = Mathf.Min(15, previous + Time.deltaTime);
                if (Hull.gameObject.activeSelf)
                {
                    float fold = Mathf.Clamp01(InterludeElapsed / InterludeLead);
                    Fold(Mathf.SmoothStep(0, 1, fold), false);
                    if (fold >= 1)
                    {
                        Dissolve(0);
                        Hull.gameObject.SetActive(false);
                    }
                }

                int half = InterludeElapsed < InterludeLead + PassageSeconds ? 0 : 1;
                float start = InterludeLead + half * PassageSeconds;
                float t = InterludeElapsed - start;
                if (half != passage)
                {
                    passage = half;
                    beat = 0;
                    nextBeat = .28f;
                }
                for (int mouth = 0; mouth < 2; mouth++)
                    PositionMouth(chapter, half, mouth, t);

                if (t >= nextBeat && t < 2.12f)
                {
                    InterludeVolley(chapter, half, beat++);
                    nextBeat += Rest(chapter == 1 ? .45f : .38f);
                }

                if (chapter == 1)
                    SweepBeam(half, true, previous - start, t);
                else
                    for (int mouth = 0; mouth < 2; mouth++)
                        SweepBeam(mouth, false, previous - start, t);
            }
            yield return null;
        }
        foreach (var tell in edgeTell) tell.enabled = false;
        CombatRound.ClearAll();
        UnfoldAt(new Vector3(0, Camera.UpLimit - 3.25f, 1));
        Hidden = false;
        PlayPhaseBreak();
        yield return Unfold(1.1f);
    }

    void SweepBeam(int mouth, bool top, float previous, float t)
    {
        var anchor = edgeTell[2 * mouth].transform;
        if (previous < ChargeAt && t >= ChargeAt)
        {
            var beam = CombatRound.Launch<PulseLaser>(anchor.position, top ? Vector2.down : Vector2.left);
            if (beam != null)
            {
                beam.Bind(anchor, Vector3.zero).Sustain();
                beam.HoldForRelease(FireAt - ChargeAt);
                beam.width = .56f;
                var arena = CombatRound.Border;
                beam.length = (top ? arena.height : arena.width) + 2;
            }
            interludeLasers[mouth] = beam;
        }
        var laser = interludeLasers[mouth];
        if (laser == null || !laser.IsAttachedTo(anchor)) return;
        if (previous < FireAt && t >= FireAt) laser.ReleaseHold();
        if (t >= CeaseAt)
        {
            laser.Cease();
            interludeLasers[mouth] = null;
        }
    }

    // The first cut leaves the far half open. After the rest the opposite cut
    // crosses the first one's endpoint. The side rifts divide that motion into
    // two parallel shelves, leaving the space between them open throughout.
    void PositionMouth(int chapter, int half, int mouth, float t)
    {
        bool top = chapter == 1;
        float home = mouth == 0 ? .26f : .74f;
        float from = top ? 0 : mouth * .5f;
        float to = from + (top ? .58f : .29f);
        if (half == 1)
        {
            from = 1 - from;
            to = 1 - to;
        }
        float track = home;
        bool cutting = !top || mouth == half;
        if (cutting)
        {
            float depart = Mathf.SmoothStep(home, from, Mathf.InverseLerp(1.5f, ChargeAt, t));
            track = Mathf.Lerp(depart, to, Mathf.InverseLerp(MoveAt, ArriveAt, t));
        }

        var r = CombatRound.Border;
        Vector3 p = top
            ? new Vector3(Mathf.Lerp(Camera.LeftLimit, Camera.RightLimit, track), Mathf.Max(Camera.UpLimit + .12f, r.yMax - .14f), -1.1f)
            : new Vector3(Mathf.Max(Camera.RightLimit + .12f, r.xMax - .14f), Mathf.Lerp(Camera.DownLimit, Camera.UpLimit, track), -1.1f);
        Vector3 inward = top ? Vector3.down : Vector3.left;
        Vector3 tangent = top ? Vector3.right : Vector3.up;
        float charge = Mathf.Clamp01(InterludeElapsed / InterludeLead);
        var ink = LevelProjectilePalette.Accent(LevelId.Void);
        ink.a = cutting || t < ChargeAt ? .2f + .56f * charge : .2f;
        for (int arm = 0; arm < 2; arm++)
        {
            var tell = edgeTell[mouth * 2 + arm];
            tell.transform.position = p;
            tell.enabled = true;
            tell.startColor = tell.endColor = ink;
            tell.SetPosition(0, p + tangent * (arm == 0 ? -.32f : .32f));
            tell.SetPosition(1, p + inward * (.18f + .24f * charge));
        }
    }

    void InterludeVolley(int chapter, int half, int beat)
    {
        var previousTone = CombatRound.LaunchTone;
        for (int mouth = 0; mouth < 2; mouth++)
        {
            var p = edgeTell[mouth * 2].transform.position;
            // Leave reaction time for a ship already beside a projectile mouth.
            // The following charged cut still visits that edge.
            if (CombatVisuals.Player != null && Vector2.Distance(p, CombatVisuals.Player.position) < 1.6f) continue;
            int hand = mouth == 0 ? -1 : 1;
            CombatRound.LaunchTone = mouth == 0 ? HostileTone.Bright : HostileTone.Dark;
            if (chapter == 1)
            {
                // The two plumes open outward on the call and fold inward on the reply.
                int count = Dense(half == 0 ? 11 : 9);
                for (int i = 0; i < count; i++)
                {
                    float u = Across(i, count);
                    float angle = u * (half == 0 ? 49 : 35) + hand * (half == 0 ? 1 : -1) * (beat * 8 - 12);
                    Vector2 axis = CombatVisuals.Rotate(Vector2.down, angle);
                    if (half == 0)
                        CombatRound.Launch<ChevronRound>(p, axis, 3.5f + .8f * (1 - u * u), .18f);
                    else
                        CombatRound.Launch<NeedleRound>(p, axis, 2.8f, .17f).Ramp(1.6f, .85f, .55f);
                }
                if (beat == 0)
                    CombatRound.Launch<GiantOrb>(p, CombatVisuals.Rotate(Vector2.down, hand * 75), 2.85f, .57f);
            }
            else
            {
                // A rack of lances brakes and turns together; the reply threads
                // faster, straight kunai between slower scallops.
                int count = Dense(half == 0 ? 9 : 11);
                for (int i = 0; i < count; i++)
                {
                    float u = Across(i, count);
                    var axis = CombatVisuals.Rotate(Vector2.left, u * 37 + hand * (beat % 3 - 1) * 8);
                    if (half == 0)
                        CombatRound.Launch<NeedleRound>(p, axis, 3.65f, .17f)
                            .Pause(.78f, .32f, CombatRound.Turn.Rotate, hand * (beat % 2 == 0 ? 32 : -32), 1.22f).ShowTurn();
                    else if (i % 2 == 0)
                        CombatRound.Launch<KunaiRound>(p, axis, 4.1f, .18f);
                    else if (i % 4 == 1)
                        CombatRound.Launch<ChevronRound>(p, axis, 2.85f, .19f).Sway(hand * .22f, .65f);
                    else
                        CombatRound.Launch<CrescentRound>(p, axis, 2.85f, .20f).Sway(hand * .22f, .65f);
                }
                if (beat == 0)
                    CombatRound.Launch<GiantOrb>(p, CombatVisuals.Rotate(Vector2.left, -hand * 70), 2.7f, .57f);
            }
        }
        CombatRound.LaunchTone = previousTone;
    }
}
