using System.Collections;
using UnityEngine;

// Snow icebreaker: an armoured bow that smashes through pack ice. One concept per phase
// (SnowIcebreakerBoss.Cards.cs); every round leaves the prow or one of the two shoulder
// ports. Phase one Bow Wave, phase two Closing Pack, Hard phase three Shattered Icicles.
[UnityEngine.Scripting.Preserve]
public sealed partial class SnowIcebreakerBoss : ArmoredHullBoss
{
    static readonly Vector2[] shape = { new(0,-1.53f),new(-.78f,-1.05f),new(-1.14f,-.55f),new(-1.02f,.65f),new(-.79f,1.08f),
        new(.79f,1.08f),new(1.02f,.65f),new(1.14f,-.55f),new(.78f,-1.05f) };
    protected override Vector2[] Outline => shape;
    protected override void Configure() { health = 2100; hullScale = 1.14f; attackInterval = 1.05f; }

    const float Cruise = 4.1f;
    float cruise = Cruise;
    int side = 1;

    protected override void MoveBoss(float dt)
    {
        transform.position = Vector3.MoveTowards(transform.position, station, dt * cruise);
    }

    protected override IEnumerator Cards()
    {
        cruise = Cruise;
        while (true)
        {
            if (phaseThree)
                yield return ShatteredIcicles();
            else if (!phaseTwo)
                yield return BowWave();
            else
                yield return ClosingPack();
        }
    }

    // ---- Helpers ----

    // Track the pilot along the top while every port charges.
    IEnumerator LineUp(float seconds)
    {
        cruise = 5.2f;
        station.y = Camera.UpLimit - 2.2f;
        for (float t = 0; t < seconds;)
        {
            if (!Holding)
                t += Time.deltaTime * LevelRun.EnemyTempoScale;
            station.x = Mathf.Clamp(PilotX, Lane(-1), Lane(1));
            for (int port = 0; port < 3; port++)
                Cue(Muzzle(port), seconds - t);
            yield return null;
        }
    }

    IEnumerator Ram(float depth)
    {
        station = new Vector3(transform.position.x, depth, 1);
        cruise = 13;
        while (transform.position.y > depth + .06f)
            yield return null;
        cruise = 4.6f;
    }

    float PilotX
    {
        get
        {
            var pilot = CombatVisuals.Player;
            return pilot != null ? SpectralDecoy.AimPosition(pilot.position).x : 0;
        }
    }

    IEnumerator Sail(Vector3 to, float speed)
    {
        station = to;
        cruise = speed;
        while (Vector2.Distance(transform.position, to) > .08f)
            yield return null;
    }

    IEnumerator PrimeAll(float seconds)
    {
        for (float t = 0; t < seconds; t += Holding ? 0 : Time.deltaTime * LevelRun.EnemyTempoScale)
        {
            for (int port = 0; port < 3; port++)
                Cue(Muzzle(port), seconds - t);
            yield return null;
        }
    }

    // Launch a round from a port so it brakes to a stop exactly on a point of the arena
    // (the braking ramp covers half its distance). A later resume speed is relative to
    // the launch speed, so the caller divides by the returned launch value.
    T Place<T>(Vector3 port, Vector2 point, float at, float radius, out float launch) where T : CombatRound
    {
        var path = point - (Vector2)port;
        launch = Mathf.Max(.1f, WorldSpeed(path.magnitude / (at - Mathf.Min(.35f, at) * .5f)));
        return Fire<T>(port, path.sqrMagnitude > .0001f ? path.normalized : Vector2.down, launch, radius);
    }
}
