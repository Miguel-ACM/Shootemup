using UnityEngine;

public sealed class GiantOrb : CombatRound
{
    public override float HitboxScale => .125f;
    const float WarningTime = .36f;
    const int Phrases = 3;
    float sideSpeed, nextRelease;
    int phrase, stroke, strokes, firstFlank;
    bool taffy;

    protected override void Initialize()
    {
        strokes = 0;
        visual.sortingOrder = 16;
    }

    internal void EnableSideSpray(float velocity)
    {
        taffy = LevelRun.Current == LevelId.Candyland;
        sideSpeed = velocity;
        firstFlank = heading.x < 0 ? -1 : 1;
        strokes = Mathf.Max(2, Difficulty.Dense(taffy ? 7 : 5));
        phrase = stroke = 0;
        nextRelease = age + Difficulty.Rest(.78f);
    }

    protected override void Tick(float dt)
    {
        Advance(dt);
        transform.rotation = Quaternion.Euler(0, 0, age * 40);
        if (strokes == 0) return;
        if (!Border.Contains(transform.position) || Paused || fadeAt >= 0 && age >= fadeAt)
        {
            nextRelease = age + WarningTime;
            stroke = 0;
            visual.color = Color.white;
            return;
        }

        float charge = stroke == 0 ? Mathf.Clamp01(1 - (nextRelease-age)/WarningTime) : 0;
        float glow = 1 + .60f * charge;
        visual.color = new Color(glow, glow, glow, 1);
        if (age < nextRelease) return;

        float u = stroke / (strokes - 1f);
        bool reverse = (phrase & 1) != 0;
        if (taffy)
        {
            // A string peels off one shoulder, hooks down as a unit, then travels on.
            // Its next answer peels off the opposite shoulder.
            int flank = reverse ? -firstFlank : firstFlank;
            Vector2 direction = WingDirection(flank, Mathf.Lerp(-28,15,u));
            var bead = Launch<SimpleRound>(Lip(direction),direction,sideSpeed*(1.25f-.30f*u),.17f);
            if (bead != null) bead.Curl(-flank*66, .95f);
            Inherit(bead);
        }
        else if (Live.Count <= Limit - 2)
        {
            // Each pair of ice needles grows the two sides of a feather. On the
            // next beat the stroke is drawn from tip to root.
            float tooth = reverse ? 1-u : u;
            for (int flank = -1; flank <= 1; flank += 2)
            {
                Vector2 direction = WingDirection(flank, Mathf.Lerp(-24,38,tooth));
                var needle = Launch<NeedleRound>(Lip(direction),direction,sideSpeed*(1.26f-.38f*tooth),.15f);
                Inherit(needle);
            }
        }
        if (++stroke < strokes)
            nextRelease = age + (taffy ? .52f : .38f) / (strokes-1);
        else
        {
            stroke = 0;
            nextRelease = age + Difficulty.Rest(taffy ? .82f : 1.02f);
            if (++phrase == Phrases) strokes = 0;
        }
        visual.color = strokes == 0 ? Color.white : new Color(1.55f,1.55f,1.55f,1);
    }

    Vector2 WingDirection(int flank, float forwardAngle)
        => CombatVisuals.Rotate(new Vector2(-heading.y,heading.x)*flank, -flank*forwardAngle);

    Vector3 Lip(Vector2 direction)
        => transform.position + (Vector3)direction * (visual.sprite.bounds.extents.x * Mathf.Abs(transform.localScale.x) + .07f);

    void Inherit(CombatRound child)
    {
        if (child == null) return;
        child.damage = damage;
        child.generation = generation + 1;
    }
}
