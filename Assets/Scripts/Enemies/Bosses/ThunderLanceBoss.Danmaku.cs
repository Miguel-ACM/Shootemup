using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Thunder lance. Phase one: six indestructible drones lock into a hexagon at the
// centre of the arena and fire six beams that swing together through sixty degrees
// and back, a turning star the player rides around. Phase two: four drones patrol
// the top, beams down, sweeping right and back, while eight drones line the left
// wall with beams across the arena. Each drone shuts down in turn and points its
// emitter at the sky: that lane is open to cross. Throughout, the boss itself fires
// mirrored arrow lace, chevrons and layered arrow wavefronts.
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
        if (beat == 0 || beat == 6)
            Phrase(Lace(beat == 0 ? 1 : -1));
        else if (beat == 3 || beat == 9)
            Phrase(Chevrons());
        else if (beat == 11 || phaseTwo && beat == 5)
            Wavefront();
    }

    void DeployStar()
    {
        var hub = new Vector2(0, Middle + .8f);
        for (int i = 0; i < 6; i++)
        {
            float angle = 30 + i * 60;
            Vector2 radial = CombatVisuals.Rotate(Vector2.down, angle);
            drones.Add(ThunderLaserBot.Deploy(Muzzle(2), hub + radial * 1.0f, radial, 16, .2f)
                .Swing(60, Difficulty.Hard ? 24 : 18)
                .ArmAt(1.6f));
        }
    }

    void DeployGrid()
    {
        float off = Difficulty.Hard ? 1.7f : 2.3f;
        for (int i = 0; i < 4; i++)
        {
            var home = new Vector2(Camera.LeftLimit + .9f + i * 2.0f, Camera.UpLimit - 1.1f);
            drones.Add(ThunderLaserBot.Deploy(Muzzle(2), home, Vector2.down, 20, .19f)
                .Patrol(1.8f, 6)
                .ShutDown(6, off + .3f, 2.2f + i * 1.5f)
                .ArmAt(1.6f));
        }
        float top = Camera.UpLimit - 3.6f, bottom = Camera.DownLimit + 1.4f;
        for (int i = 0; i < 8; i++)
        {
            var home = new Vector2(Camera.LeftLimit + .35f, Mathf.Lerp(top, bottom, i / 7f));
            drones.Add(ThunderLaserBot.Deploy(Muzzle(2), home, Vector2.right, 10.5f, .18f)
                .ShutDown(5.6f, off, 1.6f + i * .7f)
                .ArmAt(1.6f));
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
