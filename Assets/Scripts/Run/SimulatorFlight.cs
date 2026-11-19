using UnityEngine;

// The approach modifier is scoped to ordinary waves. The first boss warning tears it down.
public sealed class SimulatorFlight : MonoBehaviour
{
    static SimulatorFlight current;
    Shipcontroller ship;
    float elapsed;
    int lastBeat = -1, pressedBeat = -1;
    LineRenderer first, second;
    SpriteRenderer pulse;
    const float BeatLength = .55f;
    static SimulatorApproach Rule => current != null ? FlightTrials.Approach : SimulatorApproach.Standard;
    public static bool FreeEnergy => Rule == SimulatorApproach.UnlimitedEnergy;
    public static bool WaterHandling => Rule == SimulatorApproach.WaterHandling;
    public static bool Slow => Rule == SimulatorApproach.Slow;
    public static bool DownbeatActive => Rule == SimulatorApproach.Downbeat;
    public static bool EnemyReleaseOpen => !DownbeatActive || !current.BeatLit;
    public static bool GunsLocked => Rule == SimulatorApproach.Silent || Rule == SimulatorApproach.UnlimitedEnergy;

    public static string Label(SimulatorApproach approach) => approach switch
    {
        SimulatorApproach.UnlimitedEnergy => "INFINITE ENERGY  /  APPROACH",
        SimulatorApproach.VerticalSteering => "VERTICAL STEERING  /  APPROACH",
        SimulatorApproach.WaterHandling => "UNDERWATER HANDLING",
        SimulatorApproach.Downbeat => "FIRE ON THE BEAT  /  APPROACH",
        SimulatorApproach.HotFloor => "HOT FLOOR  /  APPROACH",
        SimulatorApproach.Silent => "GUNS OFFLINE  /  APPROACH",
        SimulatorApproach.MovingWindow => "FIRING WINDOW  /  APPROACH",
        SimulatorApproach.Slow => "FOCUS SPEED  /  APPROACH",
        _ => ""
    };

    public void Begin(Shipcontroller player)
    {
        if (current == this || FlightTrials.Approach == SimulatorApproach.Standard) return;
        current = this;
        ship = player;
        elapsed = 0;
        lastBeat = pressedBeat = -1;
        if (GunsLocked)
            foreach (var gun in ship.GetComponentsInChildren<AWeapon>()) gun.StopFiring();
        switch (Rule)
        {
            case SimulatorApproach.Downbeat:
                pulse = ItemSpark.Sprite(transform, "Downbeat lamp", "standard", new Color(1f, .86f, .46f), .14f);
                break;
            case SimulatorApproach.MovingWindow:
                first = CombatVisuals.Line(transform, new Color(.5f, .85f, 1f, .55f), .04f);
                second = CombatVisuals.Line(transform, new Color(.5f, .85f, 1f, .55f), .04f);
                break;
            case SimulatorApproach.HotFloor:
                first = CombatVisuals.Line(transform, new Color(1f, .3f, .1f, .65f), .05f);
                second = CombatVisuals.Line(transform, new Color(.85f, .1f, .04f, .12f), 1f);
                break;
        }
    }

    // final: the tape is leaving this district. A rule that lasts the whole flight ignores the
    // miniboss and boss teardowns and ends only then.
    public void End(bool final = false)
    {
        if (current != this || !final && FlightTrials.ApproachLastsWholeFlight) return;
        // Guns held through a locked or rhythmic approach pick up again without a fresh press.
        bool resync = GunsLocked || DownbeatActive;
        current = null;
        if (resync && ship != null)
        {
            var input = GameInput.Snapshot;
            foreach (var gun in ship.GetComponentsInChildren<AWeapon>())
                gun.SyncHeldInput(input.Primary, input.Secondary);
        }
        if (first != null) Destroy(first.gameObject);
        if (second != null) Destroy(second.gameObject);
        if (pulse != null) Destroy(pulse.gameObject);
        first = second = null;
        pulse = null;
        ship = null;
    }

    void OnDestroy() => End();

    int Beat => Mathf.FloorToInt(elapsed / BeatLength);
    bool BeatLit => elapsed - Beat * BeatLength <= .16f;
    float WindowCenter => Mathf.Lerp(Camera.LeftLimit * .63f, Camera.RightLimit * .63f, .5f + .5f * Mathf.Sin(elapsed * .8f));
    const float WindowHalfWidth = 1.25f;

    public static bool AcceptGunPress()
    {
        if (GunsLocked) return false;
        if (Rule != SimulatorApproach.Downbeat) return true;
        int beat = current.Beat;
        if (!current.BeatLit || current.pressedBeat == beat) return false;
        current.pressedBeat = beat;
        return true;
    }

    public static bool CanFire(Vector3 from) => !GunsLocked &&
        (Rule != SimulatorApproach.MovingWindow || Mathf.Abs(from.x - current.WindowCenter) <= WindowHalfWidth);

    public static Vector2 Steering(Vector2 input) =>
        Rule == SimulatorApproach.VerticalSteering ? new Vector2(0, input.y) : input;

    static void Line(LineRenderer line, Vector2 a, Vector2 b)
    {
        line.positionCount = 2;
        line.SetPosition(0, new Vector3(a.x, a.y, -1));
        line.SetPosition(1, new Vector3(b.x, b.y, -1));
    }

    void Update()
    {
        if (current != this || !GameClock.ActivePlay || ship == null) return;
        elapsed += GameClock.ActivePlayDelta;
        switch (Rule)
        {
            case SimulatorApproach.Downbeat:
                pulse.enabled = BeatLit;
                pulse.transform.position = ship.transform.position + new Vector3(0, .65f, -1);
                if (Beat != lastBeat)
                {
                    lastBeat = Beat;
                    ItemSound.Cue("itemFocus", volume: .13f, pitch: 1.25f);
                }
                break;
            case SimulatorApproach.MovingWindow:
                float center = WindowCenter;
                Line(first, new Vector2(center - WindowHalfWidth, Camera.DownLimit), new Vector2(center - WindowHalfWidth, Camera.UpLimit));
                Line(second, new Vector2(center + WindowHalfWidth, Camera.DownLimit), new Vector2(center + WindowHalfWidth, Camera.UpLimit));
                break;
            case SimulatorApproach.HotFloor:
                float height = (Camera.UpLimit - Camera.DownLimit) * .2f;
                float edge = Camera.DownLimit + height;
                Line(first, new Vector2(Camera.LeftLimit, edge), new Vector2(Camera.RightLimit, edge));
                first.enabled = elapsed >= 2 || Mathf.Repeat(elapsed, .4f) < .23f;
                second.enabled = elapsed >= 2;
                second.startWidth = second.endWidth = height;
                Line(second, new Vector2(Camera.LeftLimit, edge - height * .5f), new Vector2(Camera.RightLimit, edge - height * .5f));
                if (elapsed >= 2 && ship.transform.position.y < edge)
                    ship.ReceiveHazardDamage(1);
                break;
        }
    }
}
