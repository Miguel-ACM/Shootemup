using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Thunder lance. Phase one: six indestructible drones lock into a hexagon at the
// centre of the arena and fire six beams that swing together through sixty degrees
// and back; the swing is centred on straight down, so the player can hold the
// middle and ride the gap. Phase two: four drones along the top (beams down) and
// eight down the left wall (beams across) start bunched in the top-left corner.
// The groups take turns to move: every beam shuts down and turns to the sky, one
// group steps along (the top row rightward, the left column downward), and every
// beam charges and fires again. Step by step the grid crosses the whole screen.
// The boss fires mirrored arrow lace and chevrons, and in phase two zigzag lightning.
public sealed partial class ThunderLanceBoss
{
    readonly List<ThunderLaserBot> drones = new List<ThunderLaserBot>();
    bool deployed;
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

    void DeployGrid()
    {
        var corner = new Vector2(Camera.LeftLimit + .4f, Camera.UpLimit - .9f);
        for (int i = 0; i < 4; i++)
            drones.Add(ThunderLaserBot.Deploy(Muzzle(2), corner + Vector2.right * i * .35f, Vector2.down, 20, .19f).ArmAt(1.6f));
        for (int i = 0; i < 8; i++)
            drones.Add(ThunderLaserBot.Deploy(Muzzle(2), corner + Vector2.down * (.7f + i * .3f), Vector2.right, 10.5f, .18f).ArmAt(1.6f));
        Phrase(MarchGrid());
    }

    // The two groups take turns to step across the screen, every beam dark while anyone moves.
    IEnumerator MarchGrid()
    {
        float topSpacing = 2.4f, leftSpacing = 1.8f;
        float width = Camera.RightLimit - Camera.LeftLimit - .8f, height = Camera.UpLimit - Camera.DownLimit - 2.6f;
        int topStep = 0, leftStep = 0;
        float hold = Difficulty.Hard ? 2.4f : 3.2f;
        for (int turn = 0; ; turn++)
        {
            foreach (var drone in drones)
                if (drone != null)
                    drone.Powered = false;
            yield return Wait(.35f);
            bool top = turn % 2 == 0;
            if (top)
                topStep++;
            else
                leftStep++;
            for (int i = 0; i < drones.Count; i++)
            {
                if (drones[i] == null)
                    continue;
                if (i < 4)
                {
                    // Spread out from the corner, then sweep right and back in steps.
                    float spacing = Mathf.Min(topSpacing, .35f + topStep * .7f);
                    float shift = Mathf.PingPong(Mathf.Max(0, topStep - 3) * .6f, width - topSpacing * 3);
                    drones[i].MoveTo(new Vector2(Camera.LeftLimit + .4f + shift + i * spacing, Camera.UpLimit - .9f));
                }
                else
                {
                    int j = i - 4;
                    float spacing = Mathf.Min(leftSpacing, .3f + leftStep * .5f);
                    float shift = Mathf.PingPong(Mathf.Max(0, leftStep - 3) * .6f, Mathf.Max(.1f, height - leftSpacing * 7));
                    drones[i].MoveTo(new Vector2(Camera.LeftLimit + .35f, Camera.UpLimit - 1.6f - shift - j * spacing));
                }
            }
            yield return Wait(.6f);
            foreach (var drone in drones)
                if (drone != null)
                    drone.Powered = true;
            yield return Wait(hold);
        }
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
