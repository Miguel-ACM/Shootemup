using UnityEngine;

[UnityEngine.Scripting.Preserve]
public sealed class Pufferfish : AItem
{
    void Awake() => SetData("Pufferfish", "Take a breath", "Puffs up and pushes nearby bullets away.", ItemType.partner);
    public override void OnBegin() { }

    public override void OnPickup()
    {
        RoamingCompanion.Attach<PufferfishGuard>(s, nameof(Pufferfish), .49f, new Vector2(.59f, -.48f));
        base.OnPickup();
    }
}

public sealed class PufferfishGuard : RoamingCompanion
{
    const float Radius = 1.8f;
    static readonly Color Foam = new Color(.55f, .85f, .89f);
    float breath = 1.15f, swell;

    protected override void Step(float dt)
    {
        Fly(Home + new Vector2(.055f * Mathf.Sin(Age * 4), .07f * Mathf.Sin(Age * 3)), 10, dt);
        breath -= dt;
        if (breath <= 0)
        {
            bool pushed = false;
            foreach (var round in CombatRound.Live)
            {
                if (!round.IsInterceptable) continue;
                Vector2 away = (Vector2)round.transform.position - Position;
                if (away.sqrMagnitude > Radius * Radius) continue;
                if (CombatWalls.Sweep(Position, round.transform.position, .05f, out _, out _)) continue;
                var direction = away.sqrMagnitude > .0001f ? away.normalized : Vector2.up;
                round.PuffAway(direction * Mathf.Lerp(8.3f, 4.7f, away.magnitude / Radius));
                pushed = true;
            }
            breath = pushed ? 4.2f : .16f;
            if (pushed)
            {
                swell = 1;
                RunInventory.Ping(this);
                PSXFireballs.Shockwave(transform.position, Radius, .3f, Foam, 1);
                ItemSound.Cue("fan", volume: .095f, pitch: 1.15f, cooldown: .18f);
            }
        }
        float inhale = .045f * Mathf.Sin(Age * 5.4f);
        float recoil = Mathf.Sin((1 - swell) * Mathf.PI * 2) * swell;
        Model.localScale = Vector3.Scale(RestScale, new Vector3(1 + inhale + swell * .62f,
            1 - inhale + swell * .30f - recoil * .18f, 1 + swell * .48f));
        Model.localRotation = Quaternion.Euler(-14 + recoil * 18, 8 * Mathf.Sin(Age * 4), 9 * Mathf.Sin(Age * 3));
        Model.localPosition = Vector3.down * (.12f * recoil);
        swell = Mathf.MoveTowards(swell, 0, dt * 3.5f);
    }

    protected override void Park()
    {
        swell = 0;
        Model.localPosition = Vector3.zero;
        Model.localScale = RestScale;
    }
}
