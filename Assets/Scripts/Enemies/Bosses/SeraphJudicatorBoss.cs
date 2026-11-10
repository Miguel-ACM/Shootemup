using System.Collections;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class SeraphJudicatorBoss : DistrictBoss
{
    public HeavenAngelVisual Angel { get; private set; }
    public int CardIndex { get; private set; }
    bool anchored;
    readonly SpriteRenderer[] apparitions=new SpriteRenderer[6];
    readonly Vector3[] sites=new Vector3[6];
    protected override void Configure(){health=3200;hullScale=1.05f;attackInterval=.7f;}
    protected override void Start()
    {
        base.Start();
        GetComponent<CombatHullVisual>().enabled=false;
        Angel=Hull.gameObject.AddComponent<HeavenAngelVisual>();
        Angel.Build();
        FollowEyes();
        for(int i=0;i<apparitions.Length;i++)
        {
            var go=new GameObject("Unbodied choir charge");go.transform.SetParent(transform,false);
            var glow=go.AddComponent<SpriteRenderer>();glow.sprite=CombatVisuals.Disc;
            glow.sharedMaterial=CombatVisuals.ProjectileMaterial;glow.sortingLayerName="Bullet";
            glow.enabled=false;apparitions[i]=glow;
        }
    }
    protected override void Update()
    {
        if(WorldTimeStop.Frozen)return;
        if(Angel!=null&&!GameRules.playerDisabled){Angel.Step(Time.deltaTime*LevelRun.EnemyTempoScale);FollowEyes();}
        base.Update();
    }
    void FollowEyes(){for(int i=0;i<5;i++)firingCue.FollowPort(i,Angel.Emitter(i));}
    Vector3 Eye(int index)=>Angel.Emitter(index).position;
    protected override void MoveBoss(float dt)
    {
        station=new Vector3(0,Camera.UpLimit-3.1f,1);
        if(!openingComplete){base.MoveBoss(dt*2);return;}
        if(!anchored)transform.position=Vector3.MoveTowards(transform.position,station+new Vector3(Mathf.Sin(motionAge*.4f)*1.35f,Mathf.Cos(motionAge*.6f)*.35f),dt*1.2f);
    }
    void Pray(bool value){anchored=value;Angel.Pray(value);}
    PulseLaser Beam(int port,float angle,float width,float hold,float charge=1.4f)
        =>HeavenArmory.Lance(Angel.Emitter(port),Vector3.zero,CombatVisuals.Rotate(Vector2.down,angle),width,hold,charge);
    protected override void OnPhaseEntered(int phase)
    {
        Pray(false);Angel.Phase=phase;
        foreach(var glow in apparitions)glow.enabled=false;
    }
    protected override void Attack() { if(openingComplete)OpenCards(); }

    // One concept per phase (Miguel, November 2026), developed in movements: the movement
    // number chooses the variation, and every completed cycle of variations escalates.
    //   Phase one: Wheel Within a Wheel. Phase two: Six Wings. Phase three (Hard): Last Trumpet.
    // Tones: a movement's body takes Body(CardIndex) and its accents Accent(CardIndex);
    // CardIndex alternates movement by movement, so the dominant tone flips with each
    // variation (phase one opens bright, phase two dark, phase three bright). Lances stay bright.
    HostileTone Body(int card)=>(CurrentPhase+card)%2==1?HostileTone.Bright:HostileTone.Dark;
    HostileTone Accent(int card)=>Body(card)==HostileTone.Bright?HostileTone.Dark:HostileTone.Bright;
    protected override IEnumerator Cards()
    {
        // The book restarts with each phase, so every concept opens on its first movement.
        for (int movement = 0; ; movement++)
        {
            CardIndex = movement % 2;
            Pray(true);
            yield return Charge(Angel.Emitter(0), Vector3.zero, .55f);
            if (CurrentPhase == 1)
                yield return WheelWithinAWheel(movement);
            else if (CurrentPhase == 2)
                yield return SixWings(movement);
            else
                yield return LastTrumpet(movement);
            Pray(false);
            // A longer breath closes each full cycle of variations.
            yield return Wait(Rest(movement % 4 == 3 ? 1.3f : .7f));
        }
    }

    // Wheel Within a Wheel (phase one): the eyes of the tumbling wheels are the weapon.
    // Each lash leaves an eye along the wheel's own projected outline, so the fans are
    // redrawn by the tumble, and one aisle sways through them. Movements: the middle
    // wheel lashes while the outer side eyes answer; the outer wheel takes it up, wider
    // and slower, over weeping pearls; the middle and inner wheels trade beats with
    // mirrored aisles; then every eye opens, the upper rims hanging rain along the top
    // edge and watchers closing the corners. Mid-movement the core eye lays a wide
    // glance along the pilot's line. Each later cycle sways the aisle faster and widens
    // the lashes.
    IEnumerator WheelWithinAWheel(int movement)
    {
        int verse = movement % 4;
        int cycle = Mathf.Min(movement / 4, 2);
        float sway = .17f * (1 + .25f * cycle);
        if (verse == 3)
            Phrase(CornerWatchers(CardIndex, 3));
        yield return Every(4.4f, .2f, beat =>
        {
            float aisle = 46 * Mathf.Sin(beat * sway + movement * 1.3f);
            if (beat == 9)
                Glance(1.0f, 1.8f, 1.3f);
            if (beat % 4 == 2)
                Gaze();
            UseTone(Body(CardIndex));
            if (verse == 0)
            {
                WheelLash(1, Dense(8 + cycle), 3, 1.35f, aisle);
                if (beat % 2 == 1)
                    RimFans(3, Dense(9), 30, 50, 2.0f, beat);
            }
            else if (verse == 1)
            {
                WheelLash(0, Dense(8 + cycle), 2.6f, 1.2f, aisle);
                UseTone(Accent(CardIndex));
                if (beat % 2 == 1)
                    WheelPearls(2, 1.2f);
            }
            else if (verse == 2)
            {
                bool middle = beat % 2 == 0;
                WheelLash(middle ? 1 : 2, Dense(10 + cycle), 3.5f, 1.4f, middle ? aisle : -aisle);
            }
            else
            {
                WheelLash(1, Dense(8 + cycle), 3, 1.35f, aisle);
                UseTone(Accent(CardIndex));
                if (beat % 3 == 0)
                    UpperRain(0, Dense(2));
            }
        });
    }

    // Six Wings (phase two): Isaiah's seraph, "with twain he covered his face, and with
    // twain he covered his feet, and with twain he did fly". Each movement opens one
    // pair of wings, and the fourth opens all six under the Holy chorus. Flight draws
    // layered wings sideways that shed curtains down the sides, with a beat of rest in
    // every four as a passage; the feet are a skirt of feathers and stars beneath the
    // body that opens and folds while the side eyes stare; the face is two lances held
    // out to the walls that beat, with raised wings hanging along the top edge. Stars
    // bead every wing. Kept clean (Miguel: about 40% fewer rounds than the first cut);
    // each later cycle beats the wings faster.
    IEnumerator SixWings(int movement)
    {
        int verse = movement % 4;
        float tempo = 1 + .2f * Mathf.Min(movement / 4, 3);
        if (verse == 0 || verse == 3)
            Phrase(Holy(CardIndex));
        if (verse >= 2)
            WingLances(tempo);
        yield return Every(4.4f, .2f, beat =>
        {
            float stroke = 7 * Mathf.Sin(beat * .45f * tempo);
            if (beat % 5 == 2)
                Gaze();
            UseTone(Body(CardIndex));
            if (verse == 0)
            {
                if (beat % 4 != 3)
                    MiddleWings(stroke, Dense(10), Dense(8));
            }
            else if (verse == 1)
            {
                LowerWings(stroke, Dense(8), 2);
                if (beat % 2 == 1)
                    Stare(3, 3);
            }
            else if (verse == 2)
            {
                RaisedWings(stroke, Dense(8));
                LowerWings(stroke, Dense(4), 1);
            }
            else
            {
                if (beat % 4 != 3)
                    MiddleWings(stroke, Dense(10), 0);
                LowerWings(stroke, Dense(4), 1);
                if (beat % 3 == 0)
                    RaisedWings(stroke, Dense(7));
            }
        });
    }

    // Last Trumpet (phase three, Hard): the lance frames of the former third phase
    // alternate movement by movement, the trumpet (a central column with two lancets) and
    // the covenant (two opposed slow scythes). Between them the wheels throw bunched
    // knots, a star at the heart of feathers that travel together, around one moving
    // aisle, with sparse single rounds between the knots and a light rain from the upper
    // rim. Nothing is aimed: every round leaves an eye along its wheel's outline.
    IEnumerator LastTrumpet(int movement)
    {
        bool covenant = movement % 2 == 1;
        if (covenant)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                var beam = Beam(s < 0 ? 1 : 2, s * 30, 1.0f, 3.5f, 1.7f);
                if (beam != null)
                    beam.sweep = -s * 6;
            }
        }
        else
        {
            Beam(0, 0, 2.1f, 3.1f, 1.7f);
            Beam(1, -47, .85f, 2.8f, 1.7f);
            Beam(2, 47, .85f, 2.8f, 1.7f);
        }
        float drift = .19f + .03f * Mathf.Min(movement / 4, 3);
        yield return Every(covenant ? 5.6f : 5.2f, .2f, beat =>
        {
            float aisle = 44 * Mathf.Sin(beat * drift + movement);
            UseTone(Body(CardIndex));
            Knots(0, aisle);
            Knots(1, -aisle);
            UseTone(Accent(CardIndex));
            if (beat % 2 == 1)
                Singles(2, beat);
            if (beat % 4 == 0)
                UpperRain(0, 1);
        });
    }

    static float Across(int i,int count)=>count<=1?0:i/(count-1f)*2-1;
    bool Near(Vector3 point) => player != null && Vector2.Distance(point, player.position) < 1.6f;

    // A living eye of one wheel: 0 outer, 1 middle, 2 inner.
    Transform WheelEye(int ring, int index)
    {
        var wheel = Angel.Wheel(ring);
        int count = wheel.childCount;
        return wheel.GetChild((index % count + count) % count);
    }

    // The eye's direction from the core on the wheel's projected, tumbling outline.
    Vector2 Outward(Transform eye)
    {
        Vector2 offset = eye.position - Angel.transform.position;
        return offset.sqrMagnitude > .0001f ? offset.normalized : Vector2.down;
    }

    // An eye of the wheel's lower rim, away from the moving aisle (degrees from straight down).
    static bool LowerRim(Vector2 axis, float aisle)
        => axis.y < -.15f && Mathf.Abs(Vector2.SignedAngle(Vector2.down, axis) - aisle) >= 15;

    // The core eye looks: a lance laid along the pilot's line at the charge.
    void Glance(float width, float hold, float charge)
    {
        Vector2 aim = CombatVisuals.Aim(Eye(0));
        Beam(0, Vector2.SignedAngle(Vector2.down, aim), width, hold, charge);
    }

    // The central eye looks through the moving aisle with a small star chord.
    void Gaze()
    {
        Vector3 p=Eye(0);Vector2 aim=CombatVisuals.Aim(p);int count=Dense(5)|1;
        UseTone(Accent(CardIndex));
        for(int i=0;i<count;i++)
            Fire<StarRound>(p,CombatVisuals.Rotate(aim,(i-(count-1)*.5f)*11),2.6f,.22f).Ramp(1.2f,1.3f);
    }

    // The angel's feather banks: arrow-like feathers, with circular beads where scored.
    CombatRound FeatherShot(bool circular, Vector3 port, Vector2 axis, float speed, float radius)
        => circular ? (CombatRound)Fire<SimpleRound>(port, axis, speed, radius) : Fire<FeatherRound>(port, axis, speed, radius);

    // Each wheel speaks in its own material: outer feathers, middle feathers with
    // star beads, inner pearls.
    CombatRound RingShot(int ring, int lane, Vector3 port, Vector2 axis, float speed)
    {
        if (ring == 0)
            return Fire<FeatherRound>(port, axis, speed, .17f);
        if (ring == 1)
            return lane % 3 == 1 ? (CombatRound)Fire<StarRound>(port, axis, speed, .2f) : Fire<FeatherRound>(port, axis, speed, .17f);
        return Fire<SimpleRound>(port, axis, speed, .21f);
    }

    // Every lower eye of a wheel casts a small lash along its own outward line, except
    // the eyes facing the moving aisle.
    void WheelLash(int ring, int lash, float spread, float speed, float aisle)
    {
        for (int k = 0; k < HeavenAngelVisual.EyeCounts[ring]; k++)
        {
            var eye = WheelEye(ring, k);
            Vector2 axis = Outward(eye);
            if (!LowerRim(axis, aisle))
                continue;
            // Eyes deeper below the core throw a little faster, bending each fan into a crescent.
            float pace = speed * (1 - .3f * axis.y);
            for (int i = 0; i < lash; i++)
                RingShot(ring, i, eye.position, CombatVisuals.Rotate(axis, Across(i, lash) * spread), pace)
                    .Ramp(1.9f, .8f, .45f)
                    .FadeAt(8);
        }
    }

    // A knot from every lower eye of a wheel, away from the aisle: a star at the heart of
    // up to six feathers, bunched by small angle and speed offsets so the cluster travels
    // together and loosens only slowly.
    void Knots(int ring, float aisle)
    {
        int members = Mathf.Min(6, Dense(5));
        for (int k = 0; k < HeavenAngelVisual.EyeCounts[ring]; k++)
        {
            var eye = WheelEye(ring, k);
            Vector2 axis = Outward(eye);
            if (!LowerRim(axis, aisle))
                continue;
            float pace = (1.45f - .1f * ring) * (1 - .3f * axis.y);
            Fire<StarRound>(eye.position, axis, pace, .2f).Ramp(1.6f, .8f, .4f).FadeAt(8);
            for (int j = 0; j < members; j++)
            {
                float angle = j < 2 ? 0 : j % 2 == 0 ? -3.5f : 3.5f;
                float pull = j == 0 ? .86f : j == 1 ? 1.14f : j < 4 ? .94f : 1.06f;
                Fire<FeatherRound>(eye.position, CombatVisuals.Rotate(axis, angle), pace * pull, .17f)
                    .Ramp(1.6f, .8f, .4f)
                    .FadeAt(8);
            }
        }
    }

    // Sparse singles between the knots: each lower eye of a wheel sends one slow round,
    // turned half a gap aside, alternating stars and pearls.
    void Singles(int ring, int beat)
    {
        for (int k = 0; k < HeavenAngelVisual.EyeCounts[ring]; k++)
        {
            var eye = WheelEye(ring, k);
            Vector2 axis = Outward(eye);
            if (axis.y > -.2f)
                continue;
            var heading = CombatVisuals.Rotate(axis, (k + beat) % 2 == 0 ? 12 : -12);
            if ((k + beat / 2) % 2 == 0)
                Fire<StarRound>(eye.position, heading, 1.15f, .2f).FadeAt(8);
            else
                Fire<SimpleRound>(eye.position, heading, 1.15f, .21f).FadeAt(8);
        }
    }

    // The upper rim: each upper eye sends a few feathers toward the top edge that halt
    // there and fall back as a light rain; nothing bounces along the ceiling.
    void UpperRain(int ring, int count)
    {
        for (int k = 0; k < HeavenAngelVisual.EyeCounts[ring]; k++)
        {
            var eye = WheelEye(ring, k);
            Vector2 axis = Outward(eye);
            if (axis.y < .35f)
                continue;
            for (int i = 0; i < count; i++)
            {
                float u = Across(i, count);
                Fire<FeatherRound>(eye.position, CombatVisuals.Rotate(axis, u * 6), 1.6f, .17f)
                    .Pause(.9f, .45f, CombatRound.Turn.Down, u * 10 - axis.x * 14, .75f)
                    .FadeAt(8);
            }
        }
    }

    // A wheel's lowest eyes weep pearls in two speeds.
    void WheelPearls(int ring, float speed)
    {
        for (int k = 0; k < HeavenAngelVisual.EyeCounts[ring]; k++)
        {
            var eye = WheelEye(ring, k);
            Vector2 axis = Outward(eye);
            if (axis.y > -.45f)
                continue;
            for (int layer = 0; layer < 2; layer++)
                Fire<SimpleRound>(eye.position, axis, speed + layer * .55f, .21f).FadeAt(8);
        }
    }

    // Mirrored rim fans from a pair of side eyes (1/2 middle wheel, 3/4 outer wheel),
    // opening outward and down from the inner angle across the arc, with one lane open
    // that drifts beat by beat.
    void RimFans(int leftPort, int count, float inner, float arc, float speed, int beat)
    {
        float lane = .3f * Mathf.Sin(beat * .31f);
        for (int s = -1; s <= 1; s += 2)
        {
            var port = Eye(s < 0 ? leftPort : leftPort + 1);
            for (int i = 0; i < count; i++)
            {
                float u = Across(i, count);
                if (Mathf.Abs(u - lane) < .2f)
                    continue;
                FeatherShot(i % 3 == 0, port, CombatVisuals.Rotate(Vector2.down, s * (inner + arc * (u + 1) * .5f)), speed + .5f * (1 - u * u), .17f)
                    .Ramp(1.3f, .8f, .3f)
                    .FadeAt(8);
            }
        }
    }

    // A stare: both side eyes throw a short aimed chord, a star between feathers, at a
    // pilot who has stopped moving.
    void Stare(int leftPort, int count)
    {
        UseTone(Accent(CardIndex));
        for (int s = -1; s <= 1; s += 2)
        {
            var port = Eye(s < 0 ? leftPort : leftPort + 1);
            Vector2 aim = CombatVisuals.Aim(port);
            for (int i = 0; i < count; i++)
            {
                float u = Across(i, count);
                var axis = CombatVisuals.Rotate(aim, u * 7);
                float speed = 2.9f - .25f * Mathf.Abs(u);
                if (i == count / 2)
                    Fire<StarRound>(port, axis, speed, .2f).FadeAt(7);
                else
                    Fire<FeatherRound>(port, axis, speed, .17f).FadeAt(7);
            }
        }
    }

    // Flight: feathers thrown sideways from the middle side eyes, tips fastest, halt as
    // a drawn wing and shed straight down as curtains beside the walls. Coverts halt
    // inside the primaries; a star beads every fourth feather.
    void MiddleWings(float stroke, int primaries, int coverts)
    {
        for (int s = -1; s <= 1; s += 2)
        {
            var port = Eye(s < 0 ? 1 : 2);
            for (int layer = 0; layer < 2; layer++)
            {
                int count = layer == 0 ? primaries : coverts;
                for (int i = 0; i < count; i++)
                {
                    float u = count <= 1 ? 1 : i / (count - 1f);
                    float speed = (2.0f + 2.6f * u) * (layer == 0 ? 1 : .78f);
                    var axis = CombatVisuals.Rotate(Vector2.down, s * (62 + 46 * u + stroke - layer * 8));
                    var feather = i % 4 == 2 ? (CombatRound)Fire<StarRound>(port, axis, speed, .2f) : Fire<FeatherRound>(port, axis, speed, .17f);
                    feather.Pause(.75f, .35f, CombatRound.Turn.Down, s * (6 + 20 * u), (1.9f + .4f * (1 - u)) / speed)
                        .FadeAt(8.5f);
                }
            }
        }
    }

    // The feet: a skirt of feathers (and a star layer offset by half a step) beneath the
    // body from the middle side eyes, opening outward and folding in with the stroke.
    void LowerWings(float stroke, int count, int layers)
    {
        float step = 36f / Mathf.Max(1, count - 1);
        for (int s = -1; s <= 1; s += 2)
        {
            var port = Eye(s < 0 ? 1 : 2);
            for (int layer = 0; layer < layers; layer++)
                for (int i = 0; i < count; i++)
                {
                    float u = Across(i, count);
                    var axis = CombatVisuals.Rotate(Vector2.down, s * (10 + 18 * u - stroke + layer * step * .5f));
                    float speed = 2.5f + .3f * u - layer * .35f;
                    var shot = layer == 1 ? (CombatRound)Fire<StarRound>(port, axis, speed, .2f) : Fire<FeatherRound>(port, axis, speed, .17f);
                    shot.FadeAt(8);
                }
        }
    }

    // The face: feathers thrown up and out from the outer side eyes, tips fastest; they
    // halt as a raised wing near the top edge, then shed down the upper sides.
    void RaisedWings(float stroke, int count)
    {
        for (int s = -1; s <= 1; s += 2)
        {
            var port = Eye(s < 0 ? 3 : 4);
            for (int i = 0; i < count; i++)
            {
                float u = count <= 1 ? 1 : i / (count - 1f);
                float speed = 1.6f + 2.0f * u;
                var axis = CombatVisuals.Rotate(Vector2.down, s * (112 + 40 * u + stroke));
                var feather = i % 4 == 2 ? (CombatRound)Fire<StarRound>(port, axis, speed, .2f) : Fire<FeatherRound>(port, axis, speed, .17f);
                feather.Pause(.8f, .4f, CombatRound.Turn.Down, s * (4 + 14 * u), (1.7f + .3f * (1 - u)) / speed)
                    .FadeAt(8.5f);
            }
        }
    }

    // The face's lances: held out to the walls from the riding outer side eyes, they
    // beat down and back. A rising eye lifts its beam toward the ceiling, shrinking but
    // never closing the corner pocket above it; the charge shows the line first.
    void WingLances(float tempo)
    {
        for (int s = -1; s <= 1; s += 2)
        {
            var beam = Beam(s < 0 ? 3 : 4, s * 88, .8f, 3.0f, 1.6f);
            if (beam != null)
            {
                beam.sweep = -s * 22 * tempo;
                beam.reversalArc = 36;
            }
        }
    }

    // Unbodied eyes open at the first sites in empty air and glow for the charge.
    IEnumerator Apparition(int count, float charge)
    {
        for (int i = 0; i < apparitions.Length; i++)
        {
            apparitions[i].enabled = false;
            if (i < count)
                apparitions[i].transform.position = sites[i];
        }
        for (float t = 0; t < charge;)
        {
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            float k = Mathf.Clamp01(t / charge);
            for (int i = 0; i < count; i++)
            {
                var glow = apparitions[i];
                glow.enabled = true;
                glow.transform.localScale = Vector3.one * Mathf.Lerp(.65f, .15f, k);
                var ink = LevelProjectilePalette.Accent(LevelId.Heaven);
                ink.a = .2f + k * .55f;
                glow.color = ink;
            }
            yield return null;
        }
        for (int i = 0; i < count; i++)
            apparitions[i].enabled = false;
    }

    // Watchers: unbodied eyes open in the upper and middle corners and throw feather
    // fans toward a point near the floor on the far side, closing every corner.
    IEnumerator CornerWatchers(int card, int verses)
    {
        for (int verse = 0; verse < verses; verse++)
        {
            Rect r = CombatRound.Border;
            float drop = verse * .7f;
            sites[0] = new Vector3(r.xMin + .8f, r.yMax - .9f - drop, -.8f);
            sites[1] = new Vector3(r.xMax - .8f, r.yMax - .9f - drop, -.8f);
            sites[2] = new Vector3(r.xMin + .7f, r.center.y - .4f - drop, -.8f);
            sites[3] = new Vector3(r.xMax - .7f, r.center.y - .4f - drop, -.8f);
            yield return Apparition(4, .7f);
            int count = Dense(9);
            UseTone(Accent(card));
            for (int i = 0; i < 4; i++)
            {
                var p = sites[i];
                if (Near(p))
                    continue;
                float side = p.x < r.center.x ? -1 : 1;
                var target = new Vector3(r.center.x - side * (1.2f + verse * .8f), r.yMin + 1.5f, p.z);
                Vector2 aim = ((Vector2)(target - p)).normalized;
                for (int k = 0; k < count; k++)
                {
                    float u = Across(k, count);
                    Fire<FeatherRound>(p, CombatVisuals.Rotate(aim, u * 24), 1.6f + .5f * (1 - u * u), .17f)
                        .Ramp(1.4f, 1, .3f)
                        .FadeAt(7.5f);
                }
            }
            yield return Wait(Rest(.35f));
        }
    }

    // Holy, holy, holy: three pairs of unbodied eyes along the top edge; each drops
    // a short star chord that halts, stares, and is thrown at the pilot.
    IEnumerator Holy(int card)
    {
        for (int verse = 0; verse < 3; verse++)
        {
            Rect r = CombatRound.Border;
            for (int i = 0; i < 6; i++)
            {
                int side = i < 3 ? -1 : 1;
                int lane = i % 3;
                float x = r.center.x + side * (1.0f + lane * 1.5f + (verse % 2) * .35f);
                sites[i] = new Vector3(x, r.yMax - .6f - lane * .45f, -.8f);
            }
            yield return Apparition(6, .6f);
            int count = Dense(4);
            UseTone(Accent(card));
            for (int i = 0; i < 6; i++)
            {
                if (Near(sites[i]))
                    continue;
                for (int k = 0; k < count; k++)
                {
                    float u = Across(k, count);
                    Fire<StarRound>(sites[i], CombatVisuals.Rotate(Vector2.down, u * 34), 1.7f, .2f)
                        .Pause(.55f, .3f, CombatRound.Turn.Aim, u * 9, 1.35f)
                        .FadeAt(7.5f);
                }
            }
            yield return Wait(Rest(.8f));
        }
    }
}
