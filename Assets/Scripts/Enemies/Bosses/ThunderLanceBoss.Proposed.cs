using System.Collections;
using UnityEngine;

// Proposed Thunder lance book (ArtSource/BossPhaseProposals), flown only with ProposedCards.
// Attack() hands the fight to this book before its own pylon deployment, so the book
// deploys and dismisses its own Tesla pylons. Beams always stay bright. One concept per
// phase, developed through four movements of about fifteen seconds.
// Phase one: Pendulum Pylons. Tesla pylons hang on the visible top edge and swing their
//   beams like pendulums; the pilot rides the corridors between them under zigzag rice.
//   Movements: In Step, Breathing, Six Pendulums, Lance.
// Phase two: Squall Line. Six top-edge pylons lean their beams alternately right and left
//   like slanting rain, under forked lightning. Movements: Gusts, Gale, Crosswind, Squall.
// Hard final: Faraday Cage. Wall pylons fire short horizontal bars with doors that climb
//   and fall through the arena like ladder rungs. Movements: Ladder, Swapped Doors,
//   Three Rungs, Storm Cage.
public sealed partial class ThunderLanceBoss
{
    protected override IEnumerator Proposed()
    {
        // Phase three keeps the phase-two pylons alive; every phase brings its own.
        DismissDrones();
        StopBeams();
        Phrase(QuietShippingCues());
        while (true)
        {
            if (phaseThree)
                yield return FaradayCage();
            else if (phaseTwo)
                yield return SquallLine();
            else
                yield return PendulumPylons();
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

    // A card's pylons are still deployed until the book dismisses them.
    bool Deployed(ThunderLaserBot[] pylons) => pylons.Length > 0 && drones.Contains(pylons[0]);

    // Waits until these pylons have reached their stations.
    IEnumerator Arrive(ThunderLaserBot[] pylons)
    {
        foreach (var pylon in pylons)
            while (pylon != null && !pylon.Arrived)
                yield return null;
    }

    // Pendulum Pylons (phase one). Each movement recalls the pylons and deploys a new set
    // from the lance; the lull while they climb belongs to the card.
    IEnumerator PendulumPylons()
    {
        for (int movement = 0; ; movement = (movement + 1) % 4)
        {
            DismissDrones();
            DeployPendulums(movement);
            yield return Wait(1.2f);
            yield return PendulumVolleys(movement, 13.5f);
        }
    }

    //   0 In Step: four pylons swing together, fifteen degrees either side of straight down,
    //     so the corridors sway across the arena as one.
    //   1 Breathing: the right pair mirrors the left, so the middle corridor opens and closes.
    //   2 Six Pendulums: six pylons with a smaller, slower swing and narrower corridors.
    //   3 Lance: In Step again, while the boss drives its own attached lance down the middle.
    void DeployPendulums(int movement)
    {
        bool six = movement == 2;
        float[] stations = six ? new[] { -4.0f, -2.4f, -.8f, .8f, 2.4f, 4.0f } : new[] { -3.3f, -1.1f, 1.1f, 3.3f };
        float arc = six ? 24 : 30, rate = six ? 10 : 12;
        foreach (float x in stations)
        {
            bool mirrored = movement == 1 && x > 0;
            var aim = CombatVisuals.Rotate(Vector2.down, mirrored ? arc * .5f : -arc * .5f);
            drones.Add(ThunderLaserBot.Deploy(Muzzle(2), new Vector2(x, TopBank), aim, 22, .2f)
                .OwnedBy(this)
                .Swing(arc, mirrored ? -rate : rate)
                .ArmAt(1.8f));
        }
    }

    // The boss's fire under the pendulums: bright zigzag rice down the pilot's corridor,
    // dark arrows that stall and re-aim, shoulder sparks and a dark thunder roll. In the Lance
    // movement the lance fires its beam every eight beats, and the shoulders take over its bolts.
    IEnumerator PendulumVolleys(int movement, float seconds)
    {
        int n = 0;
        for (float t = 0; t < seconds; t += .6f, n++)
        {
            if (movement == 3 && n % 8 == 2)
                Phrase(LanceBurst());
            bool lance = beams[2] != null && beams[2].IsAttachedTo(Hull);
            UseTone(HostileTone.Bright);
            if (lance)
                ShoulderSparks();
            else
                Bolts(Muzzle(2), CombatVisuals.Aim(Muzzle(2)), Dense(9), 9, 3.3f);
            if (n % 2 == 1)
                StallingArrows();
            if (n % 3 == 2)
                ShoulderSparks();
            if (n % 4 == 3 && !lance)
                ThunderRoll();
            yield return Wait(Rest(.6f));
        }
    }

    // The lance charges, then holds one wide attached beam straight down; the boss stays put.
    IEnumerator LanceBurst()
    {
        yield return Charge(Hull, WeaponPort(2), .5f);
        LanceBeam(2, Vector2.down, 0, 0, 2.0f);
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

    // Squall Line (phase two): one set of pylons for the whole phase, worked through its
    // movements while the lance throws forked lightning.
    IEnumerator SquallLine()
    {
        DeploySquall();
        yield return Settle();
        Phrase(SquallBeams(drones.ToArray()));
        for (int n = 0; ; n++)
        {
            ForkedLightning(Dense(5));
            if (n % 3 == 2)
                CapacitorBeads();
            yield return Wait(Rest(1.0f));
        }
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

    //   0 Gusts: the two sets take turns, each gliding a little downwind while it fires.
    //   1 Gale: shorter gusts that glide further.
    //   2 Crosswind: both sets fire together as a diamond lattice, shut down, step half a
    //     spacing and fire again.
    //   3 Squall: each set crosses a third of the arena while it fires.
    IEnumerator SquallBeams(ThunderLaserBot[] pylons)
    {
        for (int movement = 0; Deployed(pylons); movement = (movement + 1) % 4)
        {
            if (movement == 2)
            {
                for (int step = 0; step < 4 && Deployed(pylons); step++)
                    yield return Crosswind(pylons, step % 2 == 0 ? .86f : 0);
                continue;
            }
            int gusts = movement == 1 ? 6 : movement == 3 ? 4 : 5;
            float gust = movement == 1 ? 1.7f : movement == 3 ? 2.8f : 2.4f;
            float drift = movement == 1 ? 1.2f : movement == 3 ? 1.6f : .8f;
            for (int turn = 0; turn < gusts && Deployed(pylons); turn++)
                yield return Gust(pylons, turn % 2, gust, drift);
        }
    }

    // One set holds still through its visible charge, glides downwind while firing, then
    // shuts down and glides home while the other set takes over.
    IEnumerator Gust(ThunderLaserBot[] pylons, int set, float gust, float drift)
    {
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
        PowerDown(pylons);
        yield return Wait(.3f);
    }

    // Both sets fire together from stations shifted by the given amount.
    IEnumerator Crosswind(ThunderLaserBot[] pylons, float shift)
    {
        PowerDown(pylons);
        for (int i = 0; i < pylons.Length; i++)
            if (pylons[i] != null)
                pylons[i].MoveTo(SquallStation(i, shift));
        yield return Arrive(pylons);
        foreach (var pylon in pylons)
            if (pylon != null)
                pylon.Powered = true;
        yield return Wait(.85f + 2.2f);
        PowerDown(pylons);
        yield return Wait(.3f);
    }

    static void PowerDown(ThunderLaserBot[] pylons)
    {
        foreach (var pylon in pylons)
            if (pylon != null)
            {
                pylon.Powered = false;
                pylon.FiresWhileMoving = false;
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

    // Faraday Cage (Hard final). Each movement recalls the pylons and deploys a new cage.
    IEnumerator FaradayCage()
    {
        for (int movement = 0; ; movement = (movement + 1) % 4)
        {
            DismissDrones();
            const float door = 1.7f;
            float span = Camera.RightLimit - Camera.LeftLimit - .7f;
            float longArm = (span - door) * .62f, shortArm = span - door - longArm;
            int bars = movement == 2 ? 3 : 2;
            for (int bar = 0; bar < bars; bar++)
            {
                // Doors alternate sides from bar to bar; Swapped Doors flips them all.
                bool doorRight = (bar % 2 == 0) != (movement == 1);
                AddCagePylon(bar, -1, doorRight ? longArm : shortArm, movement);
                AddCagePylon(bar, 1, doorRight ? shortArm : longArm, movement);
            }
            var pylons = drones.ToArray();
            yield return Arrive(pylons);
            foreach (var pylon in pylons)
                if (pylon != null)
                    pylon.FiresWhileMoving = true;
            Phrase(GlideCage(pylons, movement));
            yield return CageVolleys(movement, 13);
        }
    }

    //   0 Ladder: two bars 5.4 apart move together; the lower reaches the bottom of the arena,
    //     their ranges overlap, so no level stays clear and the bars never cross.
    //   1 Swapped Doors: the doors change sides and the ladder climbs faster.
    //   2 Three Rungs: three bars 3.6 apart with a shorter stroke, doors alternating.
    //   3 Storm Cage: the ladder again, with forked lightning thrown through the doors.
    static float CageHeight(int bar, float clock, int movement)
    {
        bool three = movement == 2;
        float stroke = three ? 2.4f : 3.4f, spacing = three ? 3.6f : 5.4f, rate = movement == 1 ? .9f : .7f;
        return Camera.DownLimit + .3f + stroke + bar * spacing + stroke * Mathf.Sin(clock * rate);
    }

    static Vector2 CageStation(int bar, int wall, float clock, int movement) =>
        new Vector2(wall < 0 ? Camera.LeftLimit + .35f : Camera.RightLimit - .35f, CageHeight(bar, clock, movement));

    void AddCagePylon(int bar, int wall, float reach, int movement)
    {
        drones.Add(ThunderLaserBot.Deploy(Muzzle(2), CageStation(bar, wall, 0, movement), wall < 0 ? Vector2.right : Vector2.left, reach, .18f)
            .OwnedBy(this)
            .ArmAt(1.6f));
    }

    // Pylons are added in pairs per bar, left wall first. Ends when the book recalls them.
    IEnumerator GlideCage(ThunderLaserBot[] pylons, int movement)
    {
        float clock = 0;
        while (Deployed(pylons))
        {
            if (!Holding)
                clock += Time.deltaTime * LevelRun.EnemyTempoScale;
            for (int i = 0; i < pylons.Length; i++)
                if (pylons[i] != null)
                    pylons[i].MoveTo(CageStation(i / 2, i % 2 == 0 ? -1 : 1, clock, movement));
            yield return null;
        }
    }

    // The boss's fire through the cage: dark zigzag arrows shed from the shoulders, and
    // bright rice triplets aimed through the doors (forked lightning in the Storm Cage).
    IEnumerator CageVolleys(int movement, float seconds)
    {
        for (float t = 0; t < seconds; t += .9f)
        {
            StaticLace();
            if (movement == 3)
                ForkedLightning(Dense(3));
            else
            {
                UseTone(HostileTone.Bright);
                Bolts(Muzzle(2), CombatVisuals.Aim(Muzzle(2)), 3, 6, 3.6f);
            }
            yield return Wait(Rest(.9f));
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
}
