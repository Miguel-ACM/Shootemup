using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class Crab : AItem
{
    void Awake() => SetData("Crab", "Sideways", "Walks along the bottom of the screen, firing straight up.", ItemType.partner);
    public override void OnBegin() { }

    public override void OnPickup()
    {
        RoamingCompanion.Attach<CrabPatrol>(s, nameof(Crab), .53f, new Vector2(-.73f, -.56f));
        base.OnPickup();
    }
}

public sealed class CrabPatrol : RoamingCompanion
{
    float pace = 1, reload;
    bool enteredLane;

    protected override void Step(float dt)
    {
        float bottom = Camera.DownLimit + .52f;
        float left = Camera.LeftLimit + .46f, right = Camera.RightLimit - .46f;
        if (!enteredLane)
        {
            pace = (Copy & 1) == 0 ? 1 : -1;
            Fly(new Vector2(Mathf.Clamp(Position.x, left, right), bottom), 10, dt);
            enteredLane = Mathf.Abs(Position.y - bottom) < .08f;
            return;
        }
        float x = Mathf.Clamp(Position.x + pace * 2.9f * dt, left, right);
        var next = new Vector2(x, bottom);
        if (CombatWalls.Sweep(Position, next, .2f, out _, out _))
            pace = -pace;
        else
        {
            Place(next);
            if (x <= left || x >= right) pace = -pace;
        }
        Model.localPosition = Vector3.up * (Mathf.Abs(Mathf.Sin(Age * 23)) * .047f / Mathf.Max(.01f, Owner.transform.lossyScale.x));
        Model.localRotation = Quaternion.Euler(-13, 0, Mathf.Sin(Age * 23) * 6);
        reload -= dt;
        if (!Shipcontroller.Firing || reload > 0) return;
        reload = .37f;
        var origin = transform.position + Vector3.up * .23f;
        var shot = PlayerRound.Spawn(origin, Vector2.up, GameRules.playerShootDamage * .45f,
            GameRules.playerShootTravelSpeed, PlayerDelivery.Pulse, 1);
        if (shot == null) return;
        shot.companion = true;
        shot.LockHeading();
        ItemSound.Cue("shotCompanion", volume: .035f, cooldown: .16f);
    }

    protected override void Park()
    {
        enteredLane = false;
        Model.localPosition = Vector3.zero;
    }
}
