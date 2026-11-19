using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Thunder lance. Phase one: six indestructible drones lock into a hexagon at the
// centre of the arena and fire six beams that swing together through sixty degrees
// and back; the swing is centred on straight down, so the player can hold the
// middle and ride the gap. Phase two: an evenly spaced laser lattice, four drones
// along the top firing down and six along the wall firing across. Each bank fires
// a complete comb, withdraws, then moves while the perpendicular bank takes its turn.
// Hard phase three: both complete banks sweep together. Their shared release and
// recharge give the pilot time to cross between cells.
// The boss fires mirrored arrow lace and chevrons, and in phase two zigzag lightning.
// Tones (beams always stay bright): phase one's lace is dark against bright aimed
// chevrons, and its wavefront's three arcs alternate bright, dark, bright. From phase
// two the lightning is bright with a dark capacitor bead, the chevrons turn dark,
// and Superstorm's zigzag rings are dark.
public sealed partial class ThunderLanceBoss
{
    readonly List<ThunderLaserBot> drones = new List<ThunderLaserBot>();
    bool deployed, regrid;
    static float Middle => (Camera.UpLimit + Camera.DownLimit) * .5f;
    protected override void Attack()
    {
        if (!deployed)
        {
            deployed = true;
            if (phaseTwo)
                DeployGrid();
            else
                DeployStar();
        }
        int beat = (volley - 1) % 12;
        if (phaseThree)
        {
            // Hard final phase, Superstorm: the grid keeps marching, faster, under
            // lightning fans and full rings of zigzag bolts.
            if (regrid)
            {
                regrid = false;
                Phrase(MarchGrid());
            }
            if (beat % 2 == 0)
                Lightning(beat / 2);
            else if (beat % 4 == 1)
                StormRing(beat);
            return;
        }
        if (!phaseTwo)
        {
            if (beat == 0 || beat == 6)
                Phrase(Lace(beat == 0 ? 1 : -1));
            else if (beat == 3 || beat == 9)
                Phrase(Chevrons(HostileTone.Bright));
            else if (beat == 11)
                Wavefront();
        }
        else if (beat % 3 == 0)
            Lightning(beat / 3);
        else if (beat == 4 || beat == 10)
            Phrase(Chevrons(HostileTone.Dark));
    }

    void DeployStar()
    {
        var hub = new Vector2(0, Middle + .8f);
        for (int i = 0; i < 6; i++)
        {
            // Swings run from each start angle sixty degrees on, so the gap below the hub
            // sweeps from thirty degrees left of straight down to thirty degrees right.
            float angle = i * 60;
            Vector2 radial = CombatVisuals.Rotate(Vector2.down, angle);
            drones.Add(ThunderLaserBot.Deploy(Muzzle(2), hub + radial * 1.0f, radial, 16, .2f)
                .OwnedBy(this)
                .Swing(60, Difficulty.Hard ? 24 : 18)
                .ArmAt(1.6f));
        }
    }

    const int Columns = 4, Rows = 6;

    // An evenly spaced lattice: four drones along the top firing down and six down the
    // left wall firing across, every beam the same width apart, so the cells are a
    // regular grid the player can read at a glance.
    void DeployGrid()
    {
        for (int i = 0; i < Columns; i++)
            drones.Add(ThunderLaserBot.Deploy(Muzzle(2), GridColumn(i, 0), Vector2.down, 22, .19f).OwnedBy(this).ArmAt(1.6f));
        for (int j = 0; j < Rows; j++)
            drones.Add(ThunderLaserBot.Deploy(Muzzle(2), GridRow(j, 0), Vector2.right, 10.5f, .18f).OwnedBy(this).ArmAt(1.6f));
        foreach (var drone in drones) drone.Powered = false;
        if (!phaseThree) Phrase(MarchGrid());
    }

    static float ColumnSpacing => (Camera.RightLimit - Camera.LeftLimit) / Columns;
    static float RowTop => Camera.UpLimit - 1.8f;
    static float RowSpacing => (RowTop - (Camera.DownLimit + 1.2f)) / (Rows - .5f);
    // The top bank hangs on the visible top edge of the screen in phases two and three (Miguel).
    static float TopBank => CombatRound.Border.yMax - .45f;
    static Vector2 GridColumn(int i, float shift) =>
        new Vector2(Camera.LeftLimit + Mathf.Repeat((i + .5f) * ColumnSpacing + shift, ColumnSpacing * Columns), TopBank);
    static Vector2 GridRow(int j, float shift) =>
        new Vector2(Camera.LeftLimit + .35f, RowTop - Mathf.Repeat(j * RowSpacing + shift, RowSpacing * Rows));

    IEnumerator MarchGrid()
    {
        if (phaseThree)
        {
            yield return SweepGrid();
            yield break;
        }
        float shiftX = 0, shiftY = 0;
        for (int turn = 0; ; turn++)
        {
            yield return Settle();
            bool columns = turn % 2 == 0;
            int first = columns ? 0 : Columns, end = columns ? Columns : drones.Count;
            for (int i = first; i < end; i++) drones[i].Powered = true;
            // Each bank gets its full visible charge plus a live sweep before release.
            yield return Wait(Difficulty.Easy ? 2.05f : 2.4f);
            foreach (var drone in drones)
                if (drone != null)
                    drone.Powered = false;
            yield return Wait(.3f);
            if (columns)
                shiftX = shiftX == 0 ? ColumnSpacing * .45f : 0;
            else
                shiftY = shiftY == 0 ? RowSpacing * .45f : 0;
            for (int i = first; i < end; i++)
                drones[i].MoveTo(columns ? GridColumn(i, shiftX) : GridRow(i - Columns, shiftY));
        }
    }

    IEnumerator Settle()
    {
        while (true)
        {
            bool all = true;
            foreach (var drone in drones)
                if (drone != null && !drone.Arrived)
                    all = false;
            if (all)
                yield break;
            yield return null;
        }
    }

    // Superstorm's lattice: columns sweep half a cell either side of their places and
    // rows sweep up and down, with a shared release and visible recharge.
    IEnumerator SweepGrid()
    {
        for (int i = 0; i < drones.Count; i++)
            if (drones[i] != null)
                drones[i].MoveTo(i < Columns ? SweepColumn(i, -ColumnSpacing * .5f) : SweepRow(i - Columns, -RowSpacing * .5f));
        yield return Settle();
        foreach (var drone in drones)
            if (drone != null)
                drone.FiresWhileMoving = true;
        float clock = 0;
        while (true)
        {
            if (!Holding)
                clock += Time.deltaTime * LevelRun.EnemyTempoScale;
            float sx = Mathf.PingPong(clock * .7f, ColumnSpacing) - ColumnSpacing * .5f;
            float sy = Mathf.PingPong(clock * .45f, RowSpacing) - RowSpacing * .5f;
            bool firing = Mathf.Repeat(clock, 5.4f) < 4.7f;
            for (int i = 0; i < drones.Count; i++)
            {
                if (drones[i] == null)
                    continue;
                drones[i].MoveTo(i < Columns ? SweepColumn(i, sx) : SweepRow(i - Columns, sy));
                drones[i].Powered = firing;
            }
            yield return null;
        }
    }

    static Vector2 SweepColumn(int i, float shift) => new Vector2(Camera.LeftLimit + (i + .5f) * ColumnSpacing + shift, TopBank);
    static Vector2 SweepRow(int j, float shift) => new Vector2(Camera.LeftLimit + .35f, RowTop - .25f - j * RowSpacing - shift);

    // A full ring of zigzag bolts from the front emitter.
    void StormRing(int n)
    {
        UseTone(HostileTone.Dark);
        int bolts = Dense(24);
        for (int i = 0; i < bolts; i++)
            ScoredShot(i%3==0,Muzzle(2), CombatVisuals.Rotate(Vector2.down, (i + (n % 2) * .5f) * 360f / bolts), 3.2f).Zigzag(.22f, 22);
    }

    protected override void OnPhaseThree()
    {
        regrid = true;
        volley = 0;
    }

    // Zigzag lightning from both shoulders and the front, in mirrored fans.
    void Lightning(int n)
    {
        // A straight capacitor bead interrupts the stationary cells between zigzags.
        UseTone(HostileTone.Dark);
        Fire<SimpleRound>(Muzzle(2),CombatVisuals.Aim(Muzzle(2)),3.0f,.22f).Ramp(1.2f,1);
        UseTone(HostileTone.Bright);
        int bolts = Dense(7);
        for (int port = 0; port < 3; port++)
        {
            var origin = Muzzle(port);
            var aim = port == 2 ? CombatVisuals.Aim(origin) : CombatVisuals.Rotate(Vector2.down, port == 0 ? 20 : -20);
            for (int i = 0; i < bolts; i++)
                ScoredShot(i%3==0,origin, CombatVisuals.Rotate(aim, (i - (bolts - 1) * .5f) * 14 + (n % 2) * 7), 3.6f)
                    .Zigzag(.2f, 26);
        }
    }

    // Mirrored pendulum streams of arrows: a symmetric lace that opens and folds.
    IEnumerator Lace(int sign)
    {
        int steps = Dense(phaseTwo ? 30 : 26), arms = phaseTwo ? 3 : 2;
        for (int i = 0; i < steps; i++)
        {
            UseTone(HostileTone.Dark);
            float swing = sign * 62 * Mathf.Sin(i / (float)steps * Mathf.PI * 2);
            for (int arm = 0; arm < arms; arm++)
            {
                float angle = swing + (arm - (arms - 1) * .5f) * 24;
                for (int mirror = -1; mirror <= 1; mirror += 2)
                    ScoredShot(i%9<3,Muzzle(mirror < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, mirror * angle), 3.5f);
            }
            yield return Wait(Rest(.075f));
        }
    }

    // Five rows of arrow chevrons aimed at the player, each row a wider V.
    IEnumerator Chevrons(HostileTone chevrons)
    {
        var aim = CombatVisuals.Aim(Muzzle(2));
        int rows = Dense(phaseTwo ? 6 : 5);
        for (int row = 0; row < rows; row++)
        {
            int wing = 2 + row;
            UseTone(chevrons);
            Fire<SimpleRound>(Muzzle(2), aim, 4.4f, .21f);
            for (int k = 1; k <= wing; k++)
                for (int mirror = -1; mirror <= 1; mirror += 2)
                    Fire<NeedleRound>(Muzzle(2), CombatVisuals.Rotate(aim, mirror * k * 7), 4.4f - k * .12f, .19f);
            yield return Wait(Rest(.12f));
        }
    }

    // A symmetric arrow fan released at three speeds: three nested arcs that separate.
    void Wavefront()
    {
        int lanes = Dense(phaseTwo ? 25 : 21);
        for (int layer = 0; layer < 3; layer++)
            for (int i = 0; i < lanes; i++)
            {
                UseTone(layer == 1 ? HostileTone.Dark : HostileTone.Bright);
                float lane = i - (lanes - 1) * .5f;
                ScoredShot(layer==0,Muzzle(2), CombatVisuals.Rotate(Vector2.down, lane * 150f / (lanes - 1) + (layer % 2) * 150f / (lanes - 1) * .5f),
                    2.5f + layer * .7f, .19f);
            }
    }

    protected override void PrepareScore(float seconds)
    {
        int beat = volley % 12;
        if (beat == 0 || beat == 6)
        {
            Cue(Muzzle(0), seconds);
            Cue(Muzzle(1), seconds);
        }
        else if (beat == 3 || beat == 9 || beat == 11 || phaseTwo && beat == 5)
            Cue(Muzzle(2), seconds);
    }

    protected override void OnPhaseTwo()
    {
        DismissDrones();
        deployed = false;
        volley = 0;
        nextAttack = age + 1.5f;
    }

    void DismissDrones()
    {
        foreach (var drone in drones)
            if (drone != null)
                drone.Dismiss();
        drones.Clear();
    }

    void StopBeams()
    {
        foreach (var beam in beams)
            if (beam != null && beam.IsAttachedTo(Hull))
                beam.Cease();
    }

    protected override void Die()
    {
        StopBeams();
        // Every pylon explodes with its boss, in a quick ripple.
        for (int i = 0; i < drones.Count; i++)
            if (drones[i] != null)
                drones[i].Explode(.08f + i * .1f);
        drones.Clear();
        base.Die();
    }

    void OnDestroy()
    {
        StopBeams();
        foreach (var drone in drones)
            if (drone != null)
                Destroy(drone.gameObject);
    }
}
