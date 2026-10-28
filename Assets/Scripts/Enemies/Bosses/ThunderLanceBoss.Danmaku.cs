using System.Collections.Generic;
using UnityEngine;

// Thunder lance: it releases indestructible ball-lightning drones that run a
// grid in the lower arena, leaving fading dotted walls of charge: a maze that
// keeps redrawing itself. Above it, charged downward lasers cut lanes through
// the maze and lightning needles zigzag down from the shoulders. Phase two
// fields more drones in a tighter box with a finer grid.
public sealed partial class ThunderLanceBoss
{
    readonly List<ThunderMazeSpark> sparks = new List<ThunderMazeSpark>();
    protected override void Attack()
    {
        int beat = (volley - 1) % 16;
        if (beat == 0)
            ReleaseSparks();
        else if (beat == 4 || beat == 11)
        {
            Beam(beat == 4 ? 0 : 1);
            if (phaseTwo)
                Beam(2);
        }
        else if (beat == 2 || beat == 7 || beat == 13 || phaseTwo && beat == 9)
            Lightning(beat == 7 ? -1 : 1);
    }

    void ReleaseSparks()
    {
        sparks.RemoveAll(s => s == null);
        float width = phaseTwo ? 5.4f : 6.6f, height = phaseTwo ? 5.5f : 6.6f, cell = phaseTwo ? .92f : 1.1f;
        var box = new Rect(-width * .5f, Camera.DownLimit + 1.6f, width, height);
        int count = Dense(phaseTwo ? 3 : 2);
        for (int i = 0; i < count; i++)
        {
            var spark = ThunderMazeSpark.Release(Muzzle(2), box, cell, phaseTwo ? 8 : 7, Difficulty.Hard ? 4.6f : 3.6f, .2f, volley + i * 2);
            sparks.Add(spark);
        }
        PSXImpactEffects.Muzzle(Muzzle(2), Vector2.down);
    }

    // Shoulder needles that jag left and right on their way down.
    void Lightning(int sign)
    {
        int bolts = Dense(phaseTwo ? 5 : 4);
        for (int side = 0; side < 2; side++)
        {
            var port = Hull.TransformPoint(BeamPorts[side]);
            var axis = CombatVisuals.Aim(port);
            for (int i = 0; i < bolts; i++)
                Fire<NeedleRound>(port, CombatVisuals.Rotate(axis, (i - (bolts - 1) * .5f) * 16 + sign * 5), 3.6f, .19f)
                    .Zigzag(.2f + i * .02f, 24);
        }
    }

    protected override void PrepareScore(float seconds)
    {
        int beat = volley % 16;
        if (beat == 0)
            Cue(Muzzle(2), seconds);
        else if (beat == 4 || beat == 11)
        {
            Cue(Muzzle(beat == 4 ? 0 : 1), seconds);
            if (phaseTwo)
                Cue(Muzzle(2), seconds);
        }
        else if (beat == 2 || beat == 7 || beat == 13 || phaseTwo && beat == 9)
        {
            Cue(Muzzle(0), seconds);
            Cue(Muzzle(1), seconds);
        }
    }

    protected override void OnPhaseTwo()
    {
        DismissSparks();
        volley = 0;
        nextAttack = age + 1.5f;
    }

    void DismissSparks()
    {
        foreach (var spark in sparks)
            if (spark != null)
                spark.Dismiss();
        sparks.Clear();
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
        DismissSparks();
        base.Die();
    }

    void OnDestroy()
    {
        StopBeams();
        foreach (var spark in sparks)
            if (spark != null)
                Destroy(spark.gameObject);
    }
}
