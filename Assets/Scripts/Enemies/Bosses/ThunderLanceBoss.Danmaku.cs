using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Thunder lance. Phase one: six indestructible drones lock into a hexagon at the
// centre of the arena and fire six beams that swing together through sixty degrees
// and back; the swing is centred on straight down, so the player can hold the
// middle and ride the gap. Phase two: an evenly spaced laser lattice, four drones
// along the top firing down and six down the left wall firing across, with one open
// column and one open row whose drones point at the sky. The groups take turns to
// slide half a cell while every beam is dark, the open lanes move on, and every beam
// charges where it now stands before it fires again.
// The boss fires mirrored arrow lace and chevrons, and in phase two zigzag lightning.
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
                Phrase(Chevrons());
            else if (beat == 11)
                Wavefront();
        }
        else if (beat % 3 == 0)
            Lightning(beat / 3);
        else if (beat == 4 || beat == 10)
            Phrase(Chevrons());
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
            drones.Add(ThunderLaserBot.Deploy(Muzzle(2), GridColumn(i, 0), Vector2.down, 20, .19f).ArmAt(1.6f));
        for (int j = 0; j < Rows; j++)
            drones.Add(ThunderLaserBot.Deploy(Muzzle(2), GridRow(j, 0), Vector2.right, 10.5f, .18f).ArmAt(1.6f));
        Phrase(MarchGrid());
    }

    static float ColumnSpacing => (Camera.RightLimit - Camera.LeftLimit) / Columns;
    static float RowTop => Camera.UpLimit - 1.8f;
    static float RowSpacing => (RowTop - (Camera.DownLimit + 1.2f)) / (Rows - .5f);
    static Vector2 GridColumn(int i, float shift) =>
        new Vector2(Camera.LeftLimit + Mathf.Repeat((i + .5f) * ColumnSpacing + shift, ColumnSpacing * Columns), Camera.UpLimit - .9f);
    static Vector2 GridRow(int j, float shift) =>
        new Vector2(Camera.LeftLimit + .35f, RowTop - Mathf.Repeat(j * RowSpacing + shift, RowSpacing * Rows));

    // The lattice keeps one open column and one open row, whose drones point at the sky.
    // Every step all beams go dark together while one group slides half a cell (top row
    // rightward, left column downward, taking turns) and the open lane moves on; then
    // every beam charges visibly where it now stands before it fires again.
    IEnumerator MarchGrid()
    {
        float shiftX = 0, shiftY = 0;
        int openColumn = 1, openRow = 2;
        for (int turn = 0; ; turn++)
        {
            float hold = phaseThree ? 1.7f : Difficulty.Hard ? 2.6f : 3.3f;
            for (int i = 0; i < drones.Count; i++)
                if (drones[i] != null)
                    drones[i].Powered = i < Columns ? i != openColumn : i - Columns != openRow;
            yield return Wait(hold);
            foreach (var drone in drones)
                if (drone != null)
                    drone.Powered = false;
            yield return Wait(.3f);
            if (turn % 2 == 0)
            {
                shiftX += ColumnSpacing * .5f;
                openColumn = (openColumn + 1) % Columns;
            }
            else
            {
                shiftY += RowSpacing * .5f;
                openRow = (openRow + 2) % Rows;
            }
            for (int i = 0; i < drones.Count; i++)
                if (drones[i] != null)
                    drones[i].MoveTo(i < Columns ? GridColumn(i, shiftX) : GridRow(i - Columns, shiftY));
            yield return Wait(.55f);
        }
    }

    // A full ring of zigzag bolts from the front emitter.
    void StormRing(int n)
    {
        int bolts = Dense(24);
        for (int i = 0; i < bolts; i++)
            Fire<NeedleRound>(Muzzle(2), CombatVisuals.Rotate(Vector2.down, (i + (n % 2) * .5f) * 360f / bolts), 3.2f, .19f).Zigzag(.22f, 22);
    }

    protected override void OnPhaseThree()
    {
        regrid = true;
        volley = 0;
    }

    // Zigzag lightning from both shoulders and the front, in mirrored fans.
    void Lightning(int n)
    {
        int bolts = Dense(7);
        for (int port = 0; port < 3; port++)
        {
            var origin = Muzzle(port);
            var aim = port == 2 ? CombatVisuals.Aim(origin) : CombatVisuals.Rotate(Vector2.down, port == 0 ? 20 : -20);
            for (int i = 0; i < bolts; i++)
                Fire<NeedleRound>(origin, CombatVisuals.Rotate(aim, (i - (bolts - 1) * .5f) * 14 + (n % 2) * 7), 3.6f, .19f)
                    .Zigzag(.2f, 26);
        }
    }

    // Mirrored pendulum streams of arrows: a symmetric lace that opens and folds.
    IEnumerator Lace(int sign)
    {
        int steps = Dense(phaseTwo ? 30 : 26), arms = phaseTwo ? 3 : 2;
        for (int i = 0; i < steps; i++)
        {
            float swing = sign * 62 * Mathf.Sin(i / (float)steps * Mathf.PI * 2);
            for (int arm = 0; arm < arms; arm++)
            {
                float angle = swing + (arm - (arms - 1) * .5f) * 24;
                for (int mirror = -1; mirror <= 1; mirror += 2)
                    Fire<NeedleRound>(Muzzle(mirror < 0 ? 0 : 1), CombatVisuals.Rotate(Vector2.down, mirror * angle), 3.5f, .19f);
            }
            yield return Wait(Rest(.075f));
        }
    }

    // Five rows of arrow chevrons aimed at the player, each row a wider V.
    IEnumerator Chevrons()
    {
        var aim = CombatVisuals.Aim(Muzzle(2));
        int rows = Dense(phaseTwo ? 6 : 5);
        for (int row = 0; row < rows; row++)
        {
            int wing = 2 + row;
            Fire<NeedleRound>(Muzzle(2), aim, 4.4f, .19f);
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
                float lane = i - (lanes - 1) * .5f;
                Fire<NeedleRound>(Muzzle(2), CombatVisuals.Rotate(Vector2.down, lane * 150f / (lanes - 1) + (layer % 2) * 150f / (lanes - 1) * .5f),
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
        DismissDrones();
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
