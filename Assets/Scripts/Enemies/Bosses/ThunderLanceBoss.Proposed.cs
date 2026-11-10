using System.Collections;
using UnityEngine;

// Proposed Thunder lance book (ArtSource/BossPhaseProposals), flown only with ProposedCards.
// Attack() hands the fight to this book before its own pylon deployment, so every card
// deploys and dismisses its own Tesla pylons; beams always stay bright.
// Phase one
//   Pendulum Pylons: four pylons hang on the visible top edge and swing their beams
//     together, fifteen degrees either side of straight down, so the corridors between them
//     sway across the arena as one. The lance fires bright zigzag rice bolts down the
//     pilot's corridor, the shoulders crackle with dark arrows that stall and re-aim, and a
//     dark thunder roll of slow rice suddenly quickens.
//   Lightning Lance: the boss fires its own attached beams. The lance holds one wide beam
//     straight down while both shoulders throw dark zigzag rice to the sides; then the
//     scissors: both shoulder beams start out toward the walls and close to fifteen
//     degrees either side of straight down and back, while the lance pours bolts into the
//     open middle. The boss moves to a new lane between releases.
// Phase two
//   Squall Line: six pylons on the top edge, alternate ones leaning their beams twenty-four
//     degrees right or left. The two sets take turns: one fires slanted parallel beams while
//     gliding downwind, then shuts down as the other charges leaning the other way. The
//     lance throws bright forked lightning, zigzag bolts that split into three beads.
//   Faraday Cage: four wall pylons fire short horizontal beams inward, two bars with a door
//     each, right and left of centre. The bars glide up and down the arena out of phase, so
//     the pilot slips through one door, then the other. The shoulders shed dark zigzag
//     arrows; bright rice triplets are aimed through the doors.
// Hard final
//   Supercell: the squall gusts faster and further, with forked lightning and zigzag arrow
//     fans from the shoulders on alternate gusts.
public sealed partial class ThunderLanceBoss
{
    protected override IEnumerator Proposed()
    {
        // Phase three keeps the phase-two drones alive; every card brings its own.
        DismissDrones();
        StopBeams();
        Phrase(QuietShippingCues());
        while (true)
        {
            if (phaseThree)
                yield return Supercell();
            else if (phaseTwo)
            {
                yield return SquallLine(10);
                yield return Wait(.6f);
                yield return FaradayCage(9);
                yield return Wait(.6f);
            }
            else
            {
                yield return PendulumPylons(9);
                yield return Wait(.6f);
                yield return LightningLance();
                yield return Wait(.6f);
            }
        }
    }

    // PrepareScore cues the shipping score's muzzles from the volley count every frame.
    // Holding the count on a silent beat keeps those cues off this book, whose own
    // Charge() calls and beam warnings show the real anticipation.
    IEnumerator QuietShippingCues()
    {
        while (true)
        {
            volley = 1;
            yield return null;
        }
    }

    // Pendulum Pylons (phase one).
    IEnumerator PendulumPylons(float seconds)
    {
        foreach (float x in new[] { -3.3f, -1.1f, 1.1f, 3.3f })
            drones.Add(ThunderLaserBot.Deploy(Muzzle(2), new Vector2(x, TopBank), CombatVisuals.Rotate(Vector2.down, -15), 22, .2f)
                .OwnedBy(this)
                .Swing(30, 12)
                .ArmAt(1.8f));
        yield return Wait(1.2f);
        int n = 0;
        for (float t = 0; t < seconds; t += .6f, n++)
        {
            UseTone(HostileTone.Bright);
            Bolts(Muzzle(2), CombatVisuals.Aim(Muzzle(2)), Dense(9), 9, 3.3f);
            if (n % 2 == 1)
                StallingArrows();
            if (n % 3 == 2)
                ShoulderSparks();
            if (n % 4 == 3)
                ThunderRoll();
            yield return Wait(Rest(.6f));
        }
        DismissDrones();
    }

    // A fan of zigzag bolts: rice, or arrows where a long phrase would outrun the rice reserve.
    void Bolts(Vector3 port, Vector2 axis, int count, float spread, float speed, bool rice = true)
    {
        for (int i = 0; i < count; i++)
        {
            var direction = CombatVisuals.Rotate(axis, (i - (count - 1) * .5f) * spread);
            if (rice)
                Fire<RiceRound>(port, direction, speed, .18f).Zigzag(.2f, 22);
            else
                Fire<NeedleRound>(port, direction, speed, .19f).Zigzag(.2f, 22);
        }
    }

    // Short mirrored zigzag sparks out and down from both shoulders.
    void ShoulderSparks()
    {
        UseTone(HostileTone.Bright);
        for (int sign = -1; sign <= 1; sign += 2)
            Bolts(Muzzle(sign < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, sign * 35), Dense(4), 12, 3.1f);
    }

    // Dark arrows crackle out of both shoulders, stall with a glint and re-aim at the pilot.
    void StallingArrows()
    {
        UseTone(HostileTone.Dark);
        int count = Dense(7);
        for (int sign = -1; sign <= 1; sign += 2)
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Lerp(30, 80, i / Mathf.Max(1f, count - 1));
                Fire<NeedleRound>(Muzzle(sign < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, sign * angle), 2.6f, .19f)
                    .Pause(.8f, .3f, CombatRound.Turn.Aim, 0, 1.3f);
            }
    }

    // Thunder roll: a wide, slow fan of dark rice that suddenly quickens.
    void ThunderRoll()
    {
        UseTone(HostileTone.Dark);
        int count = Dense(10);
        for (int i = 0; i < count; i++)
            Fire<RiceRound>(Muzzle(2), CombatVisuals.Rotate(Vector2.down, (i - (count - 1) * .5f) * (120f / Mathf.Max(1, count - 1))), 1.2f, .18f)
                .Ramp(3, .5f, .9f);
    }

    // Lightning Lance (phase one).
    IEnumerator LightningLance()
    {
        // The lance: one wide beam straight down, zigzag rice to both sides. The shoulders
        // crackle while the lance charges.
        yield return Charge(Hull, WeaponPort(2), .5f);
        LanceBeam(2, Vector2.down, 0, 0, 2.0f);
        StallingArrows();
        yield return Wait(beamCharge * .5f);
        ShoulderSparks();
        yield return Wait(beamCharge * .5f);
        int shot = 0;
        for (float t = 0; t < 2.0f; t += .2f, shot++)
        {
            UseTone(HostileTone.Dark);
            for (int sign = -1; sign <= 1; sign += 2)
                Bolts(Muzzle(sign < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, sign * 50), Dense(6), 10, 3.0f, shot % 2 == 0);
            yield return Wait(Rest(.2f));
        }
        yield return Wait(.35f);
        ThunderRoll();
        StallingArrows();
        yield return Wait(Rest(.6f));
        // The scissors: shoulder beams close from the walls toward straight down and back.
        Phrase(Charge(Hull, WeaponPort(1), .4f));
        yield return Charge(Hull, WeaponPort(0), .4f);
        const float scissors = 2.6f, arc = 60;
        float sweep = arc * .5f * Mathf.PI * 2 / scissors;
        LanceBeam(0, CombatVisuals.Rotate(Vector2.down, -75), sweep, arc, scissors);
        LanceBeam(1, CombatVisuals.Rotate(Vector2.down, 75), -sweep, arc, scissors);
        // The lance keeps firing into the middle through the scissors' charge.
        for (float t = 0; t < beamCharge; t += .3f)
        {
            UseTone(HostileTone.Bright);
            Bolts(Muzzle(2), CombatVisuals.Aim(Muzzle(2)), Dense(5), 10, 3.3f, false);
            yield return Wait(.3f);
        }
        int beat = 0;
        for (float t = 0; t < scissors; t += .18f, beat++)
        {
            UseTone(HostileTone.Bright);
            Bolts(Muzzle(2), CombatVisuals.Aim(Muzzle(2)), Dense(7), 8, 3.4f, beat % 2 == 0);
            if (beat == 7)
                ThunderRoll();
            yield return Wait(Rest(.18f));
        }
        yield return Wait(.35f);
        ThunderRoll();
        StallingArrows();
        yield return Wait(Rest(.6f));
    }

    // One of the boss's own attached beams, aimed and optionally swinging out and back.
    void LanceBeam(int index, Vector2 direction, float sweep, float arc, float active)
    {
        if (beams[index] != null && beams[index].IsAttachedTo(Hull))
            return;
        var beam = Fire<PulseLaser>(Hull.TransformPoint(BeamPorts[index]), direction, 0);
        if (beam == null)
            return;
        beams[index] = beam;
        beam.Bind(Hull, BeamPorts[index]);
        beam.warning = beamCharge;
        beam.activeTime = active;
        beam.width = beamWidth * (index == 2 ? 1.25f : 1);
        beam.length = 24;
        beam.sweep = sweep;
        beam.reversalArc = arc;
    }

    // Squall Line (phase two).
    IEnumerator SquallLine(float seconds)
    {
        DeploySquall();
        yield return Settle();
        Phrase(Gusts(drones.ToArray(), 2.4f, .8f));
        int n = 0;
        for (float t = 0; t < seconds; t += .85f, n++)
        {
            ForkedLightning(Dense(5));
            if (n % 3 == 2)
                CapacitorBeads();
            yield return Wait(Rest(.85f));
        }
        DismissDrones();
    }

    // Six pylons evenly along the visible top edge; even ones lean right, odd ones left.
    void DeploySquall()
    {
        DismissDrones();
        for (int i = 0; i < 6; i++)
        {
            var pylon = ThunderLaserBot.Deploy(Muzzle(2), SquallStation(i, 0), CombatVisuals.Rotate(Vector2.down, i % 2 == 0 ? 24 : -24), 22, .19f)
                .OwnedBy(this)
                .ArmAt(1.6f);
            pylon.Powered = false;
            drones.Add(pylon);
        }
    }

    static Vector2 SquallStation(int i, float drift) =>
        new Vector2(Mathf.Lerp(Camera.LeftLimit + .55f, Camera.RightLimit - .55f, i / 5f) + drift, TopBank);

    // A card's pylons are still deployed until the card dismisses them.
    bool Deployed(ThunderLaserBot[] pylons) => pylons.Length > 0 && drones.Contains(pylons[0]);

    // The two sets take turns: hold still through the visible charge, glide downwind while
    // firing, then shut down and glide home while the other set charges. Ends when the
    // card dismisses these pylons.
    IEnumerator Gusts(ThunderLaserBot[] pylons, float gust, float drift)
    {
        for (int turn = 0; Deployed(pylons); turn++)
        {
            int set = turn % 2;
            float wind = set == 0 ? 1 : -1;
            for (int i = 0; i < pylons.Length; i++)
                if (pylons[i] != null)
                {
                    pylons[i].Powered = i % 2 == set;
                    pylons[i].FiresWhileMoving = i % 2 == set;
                    pylons[i].MoveTo(SquallStation(i, 0));
                }
            yield return Wait(.85f);
            for (float g = 0; g < gust && Deployed(pylons);)
            {
                if (!Holding)
                    g += Time.deltaTime * LevelRun.EnemyTempoScale;
                float glide = wind * drift * Mathf.SmoothStep(0, 1, g / gust);
                for (int i = set; i < pylons.Length; i += 2)
                    if (pylons[i] != null)
                        pylons[i].MoveTo(SquallStation(i, glide));
                yield return null;
            }
            foreach (var pylon in pylons)
                if (pylon != null)
                {
                    pylon.Powered = false;
                    pylon.FiresWhileMoving = false;
                }
            yield return Wait(.3f);
        }
    }

    // Forked lightning: an aimed fan of bright zigzag bolts, each splitting into three beads.
    void ForkedLightning(int count)
    {
        UseTone(HostileTone.Bright);
        var port = Muzzle(2);
        var aim = CombatVisuals.Aim(port);
        for (int i = 0; i < count; i++)
            Fire<RiceRound>(port, CombatVisuals.Rotate(aim, (i - (count - 1) * .5f) * 16), 3.2f, .18f)
                .Zigzag(.22f, 22)
                .SplitAt(1.3f, 3, 24, 2.8f, .16f);
    }

    // Dark capacitor beads from both shoulders: slow aimed rounds that quicken.
    void CapacitorBeads()
    {
        UseTone(HostileTone.Dark);
        for (int port = 0; port <= 1; port++)
            Fire<SimpleRound>(Muzzle(port), CombatVisuals.Aim(Muzzle(port)), 2.2f, .22f).Ramp(2, 1);
    }

    // Faraday Cage (phase two).
    IEnumerator FaradayCage(float seconds)
    {
        DismissDrones();
        const float door = 1.7f;
        float span = Camera.RightLimit - Camera.LeftLimit - .7f;
        float longArm = (span - door) * .62f, shortArm = span - door - longArm;
        // Upper bar: long arm from the left, door right of centre. Lower bar: mirrored.
        AddCagePylon(0, -1, longArm);
        AddCagePylon(0, 1, shortArm);
        AddCagePylon(1, -1, shortArm);
        AddCagePylon(1, 1, longArm);
        yield return Settle();
        foreach (var drone in drones)
            if (drone != null)
                drone.FiresWhileMoving = true;
        Phrase(GlideCage(drones.ToArray()));
        int n = 0;
        for (float t = 0; t < seconds; t += .9f, n++)
        {
            StaticLace();
            if (n % 2 == 1)
            {
                UseTone(HostileTone.Bright);
                Bolts(Muzzle(2), CombatVisuals.Aim(Muzzle(2)), 3, 6, 3.6f);
            }
            yield return Wait(Rest(.9f));
        }
        DismissDrones();
    }

    // The two bars move together like rungs of a ladder 5.4 apart: the lower one reaches the
    // bottom of the arena and the upper one rises above the middle, and their ranges overlap,
    // so no level stays clear and the bars never cross.
    static float CageHeight(int bar, float clock) =>
        Camera.DownLimit + 3.7f + (bar == 0 ? 5.4f : 0) + 3.4f * Mathf.Sin(clock * .7f);

    static Vector2 CageStation(int bar, int wall, float clock) =>
        new Vector2(wall < 0 ? Camera.LeftLimit + .35f : Camera.RightLimit - .35f, CageHeight(bar, clock));

    void AddCagePylon(int bar, int wall, float reach)
    {
        drones.Add(ThunderLaserBot.Deploy(Muzzle(2), CageStation(bar, wall, 0), wall < 0 ? Vector2.right : Vector2.left, reach, .18f)
            .OwnedBy(this)
            .ArmAt(1.6f));
    }

    // Pylons are added in pairs per bar, left wall first. Ends when the card dismisses them.
    IEnumerator GlideCage(ThunderLaserBot[] pylons)
    {
        float clock = 0;
        while (Deployed(pylons))
        {
            if (!Holding)
                clock += Time.deltaTime * LevelRun.EnemyTempoScale;
            for (int i = 0; i < pylons.Length; i++)
                if (pylons[i] != null)
                    pylons[i].MoveTo(CageStation(i / 2, i % 2 == 0 ? -1 : 1, clock));
            yield return null;
        }
    }

    // Dark zigzag arrows shed gently out and down from both shoulders.
    void StaticLace()
    {
        UseTone(HostileTone.Dark);
        int count = Dense(7);
        for (int sign = -1; sign <= 1; sign += 2)
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Lerp(15, 60, i / Mathf.Max(1f, count - 1));
                Fire<NeedleRound>(Muzzle(sign < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, sign * angle), 2.6f, .19f).Zigzag(.3f, 14);
            }
    }

    // Supercell (Hard final): the squall never ends, gusting faster and further.
    IEnumerator Supercell()
    {
        DeploySquall();
        yield return Settle();
        Phrase(Gusts(drones.ToArray(), 1.7f, 1.2f));
        int arrows = Dense(6);
        for (int n = 0; ; n++)
        {
            ForkedLightning(Dense(5));
            if (n % 2 == 1)
            {
                UseTone(HostileTone.Dark);
                for (int sign = -1; sign <= 1; sign += 2)
                    for (int i = 0; i < arrows; i++)
                        Fire<NeedleRound>(Muzzle(sign < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, sign * (35 + i * 9)), 3.0f, .19f).Zigzag(.25f, 18);
            }
            yield return Wait(Rest(1.0f));
        }
    }
}
