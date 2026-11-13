using System.Collections;
using UnityEngine;

// Phase one (Miguel, 13 November): only slow flames crossing from both screen edges.
// Later phases keep the pincer, eye and mouth scores below.
public sealed partial class HellCrabBoss
{
    static readonly Vector3 Pincer = new Vector3(0, -.32f, -.4f);
    static readonly Vector3 Mouth = new Vector3(0, -.9f, -.4f);
    int BurningEye()
    {
        if (eyes[0] == null)
            return -1;
        return (volley / 16) % 2;
    }

    protected override void Attack()
    {
        if (claws[0] != null)
            OpenCards();
    }

    protected override IEnumerator Cards()
    {
        while (true)
        {
            if (phaseThree)
                yield return FinalSpell();
            else if (!phaseTwo)
            {
                yield return Crosswinds();
            }
            else
            {
                yield return Every(7, 1.1f, n =>
                {
                    if (n % 3 != 2)
                        Phrase(Scissors(n % 2 == 0 ? 1 : -1, HostileTone.Bright));
                    else
                        Canopy(n);
                });
                yield return Wait(.8f);
                Phrase(Every(8, 2.6f, n => SoulSnipe(BurningEye(), HostileTone.Bright)));
                yield return Toned(HostileTone.Dark, MirrorLace(claws[0], Pincer, claws[1], Pincer, Dense(90), 3, 75, 18, 3.6f, .07f));
                yield return Wait(.8f);
                yield return Every(6.6f, 2.2f, n => Phrase(EmberCage(n + 1, 2)));
                yield return Wait(.8f);
            }
        }
    }

    // One evolving horizontal score, never aimed. Opposed banks leave a broad shared
    // passage which walks up and down between releases; the old banks keep crossing.
    IEnumerator Crosswinds()
    {
        for (int beat = 0; ; beat++)
        {
            int lanes = Mathf.Clamp(Dense(17), 8, 24);
            int gap = 2 + Mathf.RoundToInt((lanes - 6) * (.5f + .5f * Mathf.Sin(beat * .73f)));
            yield return WarnCrosswinds(lanes, gap, .7f);
            ReleaseCrosswinds(lanes, gap);
            yield return Wait(.12f);
            HideSideWarnings();
            yield return Wait(Rest(2.15f) - .12f);
        }
    }

    static Vector3 FlameEdge(int side, int lane, int lanes)
    {
        var edge = CombatRound.Border;
        return new Vector3(side < 0 ? edge.xMin - .35f : edge.xMax + .35f,
            Mathf.Lerp(edge.yMin + .45f, edge.yMax - .45f, (lane + .5f) / lanes), -.5f);
    }

    // Reuse two small cue sprites per lane for the whole fight, with no lights or colliders.
    SpriteRenderer[] sideWarnings;
    void PrepareSideWarnings()
    {
        if (sideWarnings != null) return;
        sideWarnings = new SpriteRenderer[48];
        for (int i = 0; i < sideWarnings.Length; i++)
        {
            sideWarnings[i] = ItemSpark.Sprite(transform, "Crosswind edge charge", "flame", LevelProjectilePalette.Accent(LevelId.Hell), .13f);
            sideWarnings[i].enabled = false;
        }
    }

    void HideSideWarnings()
    {
        if (sideWarnings == null) return;
        foreach (var cue in sideWarnings) cue.enabled = false;
    }

    IEnumerator WarnCrosswinds(int lanes, int gap, float seconds)
    {
        PrepareSideWarnings();
        for (float t = 0; t < seconds;)
        {
            if (!Holding) t += Time.deltaTime * LevelRun.EnemyTempoScale;
            for (int lane = 0; lane < lanes; lane++)
                for (int bank = 0; bank < 2; bank++)
                {
                    int side = bank == 0 ? -1 : 1;
                    var cue = sideWarnings[lane * 2 + bank];
                    cue.enabled = !Holding && (lane < gap || lane > gap + 2);
                    cue.transform.position = FlameEdge(side, lane, lanes) + Vector3.left * side * .48f;
                    cue.transform.rotation = Quaternion.FromToRotation(Vector3.up, Vector3.left * side);
                    cue.color = new Color(1, 1, 1, Mathf.Lerp(.1f, .75f, t / seconds));
                }
            yield return null;
        }
    }

    void ReleaseCrosswinds(int lanes, int gap)
    {
        for (int lane = 0; lane < lanes; lane++)
        {
            if (lane >= gap && lane <= gap + 2) continue;
            for (int side = -1; side <= 1; side += 2)
            {
                UseTone(HostileTone.Bright);
                // Both banks have the same slow speed and horizontal heading.
                Fire<FlameRound>(FlameEdge(side, lane, lanes), Vector2.left * side, WorldSpeed(1.15f), .22f, flashMuzzle: false)
                    .LivesFor((CombatRound.Border.width + 7) / 1.15f);
            }
        }
    }


    // Brimstone canopy: arrows thrown up from the mouth that rebound off the ceiling.
    // Always dark.
    void Canopy(int n)
    {
        UseTone(HostileTone.Dark);
        int count = Dense(9);
        var mouth = Hull.TransformPoint(Mouth);
        for (int i = 0; i < count; i++)
            for (int mirror = -1; mirror <= 1; mirror += 2)
                ScoredShot(i%3==0,mouth, CombatVisuals.Rotate(Vector2.up, mirror * (6 + i * 9 + (n % 2) * 4)), 3.6f).Bounce(1);
    }

    // Each pincer sweeps its stream across the player's position, so the two lines cross and close.
    IEnumerator Scissors(int sign, HostileTone shade)
    {
        int shots = Dense(phaseTwo ? 15 : 12)|1;
        for (int i = 0; i < shots; i++)
        {
            float t = i / (shots - 1f);
            UseTone(shade);
            for (int side = 0; side < 2; side++)
            {
                if (claws[side] == null)
                    continue;
                var port = claws[side].TransformPoint(Pincer);
                if (player != null && Vector2.Distance(port, player.position) < 1.8f)
                    continue;
                float sweep = Mathf.Lerp(22, -22, t) * (side == 0 ? 1 : -1) * sign;
                ScoredShot(i%3==shots/2%3,port, CombatVisuals.Rotate(CombatVisuals.Aim(port), sweep), 4.3f);
            }
            yield return Wait(Rest(.075f));
        }
    }

    // Slow souls that stop, stare, then dart at the player one after another.
    void SoulSnipe(int eye, HostileTone shade)
    {
        if (eye < 0 || eyes[eye] == null || health <= 0)
            return;
        UseTone(shade);
        int souls = Dense(phaseTwo ? 9 : 7);
        var port = eyes[eye].position;
        for (int i = 0; i < souls; i++)
        {
            float lane = i - (souls - 1) * .5f;
            Fire<SimpleRound>(port, CombatVisuals.Rotate(Vector2.down, lane * 120f / Mathf.Max(1, souls - 1)), 1.7f, .36f)
                .Pause(.85f, .45f + i * .24f, CombatRound.Turn.Aim, 0, 2.5f);
        }
    }

    // Hard final phase, Inferno: a three-arm lace from the moving pincers with one
    // answer at a time through it, scissors closing, then the firing eye's souls.
    IEnumerator FinalSpell()
    {
        Phrase(Every(9, 1.4f, n =>
        {
            if (n % 4 == 3)
                Phrase(EmberCage(n / 4 + 2, 1));
            else if (n % 2 == 0)
                Phrase(Scissors(n % 4 == 0 ? 1 : -1, HostileTone.Dark));
            else
                SoulSnipe(BurningEye(), HostileTone.Dark);
        }));
        yield return Toned(HostileTone.Bright, MirrorLace(claws[0], Pincer, claws[1], Pincer, Dense(110), 3, 80, 18, 3.8f, .065f));
        yield return Wait(.8f);
    }

    // The claws draw: while a pincer traces its figure it leaves a flame that hangs on the
    // path, stares, then darts at the pilot, so the figure burns out in the order drawn.
    void ClawTrail(int side)
    {
        var port = claws[side].TransformPoint(Pincer);
        if (player != null && Vector2.Distance(port, player.position) < 1.6f)
            return;
        var was = tone;
        UseTone(phaseTwo ? HostileTone.Dark : HostileTone.Bright);
        Fire<FlameRound>(port, Vector2.down, .3f, .22f)
            .Pause(.25f, 1.0f, CombatRound.Turn.Aim, 0, 8.5f);
        tone = was;
    }

    // Ember Cage: a charge on the pincers and eyes, then one box (two frames in phase two)
    // around the pilot. Every flame arrives at once and stares; then the walls close.
    //   0, open floor: the bars slide in and the lid falls.
    //   1, open sides: the lid falls while the floor slides in from both sides.
    //   2, open left: the lid falls, the right bar and the floor close from the right.
    //   3, open right: the mirror.
    IEnumerator EmberCage(int n, int frames)
    {
        yield return CageGlow(.45f);
        var centre = CagePoint();
        int open = n % 4;
        int bar = Dense(10);
        int lid = Dense(11);
        int floor = Dense(8);
        var edge = CombatRound.Border;
        for (int frame = 0; frame < frames; frame++)
        {
            UseTone(frame == 0 ? HostileTone.Dark : HostileTone.Bright);
            float width = 1.45f + frame * .5f;
            float top = 1.35f + frame * .5f;
            float low = 1.25f + frame * .5f;
            float hold = .5f + frame * .35f;
            for (int k = 0; k < lid; k++)
            {
                float x = centre.x + Mathf.Lerp(-width + .3f, width - .3f, (k + .5f) / lid);
                CageEmber(centre, new Vector2(x, centre.y + top), hold, Vector2.down);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                bool far = open == 2 && side > 0 || open == 3 && side < 0;
                if (open == 0 || far)
                {
                    for (int k = 0; k < bar; k++)
                    {
                        float y = centre.y + Mathf.Lerp(-low, top - .25f, (k + .5f + frame * .5f) / bar);
                        CageEmber(centre, new Vector2(centre.x + side * width, y), hold, Vector2.right * -side);
                    }
                }
                // The floor waits beside the box, beyond the bar, and slides in under it.
                if (open == 1 || far)
                {
                    for (int k = 0; k < floor; k++)
                    {
                        float x = centre.x + side * (width + .25f + k * 2 * width / floor);
                        if (x > edge.xMin + .2f && x < edge.xMax - .2f)
                            CageEmber(centre, new Vector2(x, centre.y - low), hold + .2f, Vector2.right * -side);
                    }
                }
            }
        }
    }

    // The cage's centre: the pilot, kept inside the walls and clear below the crab.
    Vector2 CagePoint()
    {
        var at = player != null ? (Vector2)player.position : new Vector2(0, -3);
        float side = Camera.RightLimit - 2.1f;
        float under = transform.position.y - 3.4f;
        return new Vector2(Mathf.Clamp(at.x, -side, side), Mathf.Clamp(at.y, Camera.DownLimit + 1.6f, under));
    }

    // Each flame leaves whichever part (a pincer, an eye or the mouth) keeps its throw
    // farthest from the pilot, so no wall is thrown through the box it closes.
    void CageEmber(Vector2 centre, Vector2 point, float hold, Vector2 then)
    {
        Vector3 port = transform.position;
        float clearest = -1;
        for (int k = 0; k < 5; k++)
        {
            var part = k < 2 ? claws[k] : k < 4 ? eyes[k - 2] : Hull;
            if (part == null)
                continue;
            var candidate = k < 2 ? part.TransformPoint(Pincer) : k < 4 ? part.position : part.TransformPoint(Mouth);
            float clearance = Clearance(centre, candidate, point);
            if (clearance > clearest)
            {
                clearest = clearance;
                port = candidate;
            }
        }
        if (clearest < .9f)
            return;
        Hang<FlameRound>(port, point, 1.1f, hold, then, 1.5f, .24f)
            .FadeAt(6);
    }

    // Distance from a point to the segment from a to b.
    static float Clearance(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        float along = Mathf.Clamp01(Vector2.Dot(point - a, ab) / Mathf.Max(.0001f, ab.sqrMagnitude));
        return Vector2.Distance(point, a + ab * along);
    }

    // The cage's anticipation: every pincer and eye glows for the whole wait.
    IEnumerator CageGlow(float seconds)
    {
        for (float t = 0; t < seconds;)
        {
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            for (int i = 0; i < 2; i++)
            {
                if (claws[i] != null)
                    Cue(claws[i].TransformPoint(Pincer), seconds - t);
                if (eyes[i] != null)
                    Cue(eyes[i].position, seconds - t);
            }
            yield return null;
        }
    }

    // A round that brakes to a stop at a chosen point after `arrive` seconds of its own
    // clock, hangs for `hold`, then leaves along `then` at an authored speed.
    T Hang<T>(Vector3 port, Vector2 point, float arrive, float hold, Vector2 then, float speed, float radius) where T : CombatRound
    {
        Vector2 path = point - (Vector2)port;
        float distance = Mathf.Max(.05f, path.magnitude);
        Vector2 axis = path.sqrMagnitude > .0001f ? path / distance : Vector2.down;
        // The brake eases over the last .35 s, so the path takes arrive - .175 s of travel.
        float travel = distance / (arrive - .175f);
        return Fire<T>(port, axis, WorldSpeed(travel), radius)
            .Pause(arrive, hold, CombatRound.Turn.Rotate, Vector2.SignedAngle(axis, then), speed / WorldSpeed(1) / travel);
    }
}
