using UnityEngine;

public class PulseLaser : CombatRound
{
    BeamRibbon ribbon;
    LaserSidePorts ports;
    LaserSound sound;
    Transform emitter;
    Vector3 muzzle;
    bool attached;
    bool startedSound;
    bool sustained;
    AEnemy owner;
    public float warning = .9f, activeTime = .65f, length = 22, width = .25f, sweep;
    public bool sideShots;
    // Siege beams unfold gradually. Damage uses exactly the same widening envelope.
    public float widthRise = .07f;
    public bool celestial;
    float nextSide, clockRate = 1;
    protected override float ClockRate => clockRate;
    const float Cooldown = .22f;
    public float Charge => Mathf.Clamp01(age / Mathf.Max(.01f, warning));
    public bool IsFiring => Active;
    public bool IsAttachedTo(Transform source) => attached && emitter == source && isActiveAndEnabled;
    public bool IsSustained => sustained;
    public float reversalArc;
    float Expansion => celestial ? Mathf.SmoothStep(0, 1, Mathf.Clamp01((age - warning) / Mathf.Max(.01f, widthRise))) : Mathf.Clamp01((age-warning)/.07f);
    public float Power => Active ? Mathf.Min(Expansion, sustained ? 1 : Mathf.Lerp(.4f, 1, Mathf.Clamp01((warning + activeTime - age) / (celestial ? .16f : .09f)))) :
        age >= warning + activeTime ? Mathf.Pow(Mathf.Clamp01(1 - (age - warning - activeTime) / Cooldown), 2) * .4f : 0;
    protected override void Initialize()
    {
        warning = .9f;
        activeTime = .65f;
        length = 22;
        width = .25f;
        sweep = 0;
        sideShots = false;
        widthRise = .07f;
        celestial = false;
        nextSide = 0;
        emitter = null;
        owner = null;
        attached = false;
        startedSound = false;
        sustained = false;
        clockRate = 1;
        reversalArc = 0;
        transform.localScale = Vector3.one;
        visual.enabled = false;
        collision.enabled = false;
        if (ribbon == null)
        {
            ribbon = gameObject.AddComponent<BeamRibbon>();
            ribbon.Build();
        }
        if (sound == null)
            sound = gameObject.AddComponent<LaserSound>();
        ribbon.Draw(transform.position, heading, length, width, 0, 0);
        lifetime = 12;
        if (ports != null)
            ports.Hide();
    }

    public PulseLaser Bind(Transform source, Vector3 localMuzzle)
    {
        emitter = source;
        muzzle = localMuzzle;
        attached = source != null;
        owner = source != null ? source.GetComponentInParent<AEnemy>() : null;
        if (attached)
        {
            transform.position = source.TransformPoint(localMuzzle);
            // The beam is part of its emitter's phrase, including charge, sweep and release.
            clockRate = Mathf.Max(.01f, LevelRun.EnemyTempoScale * (source.GetComponentInParent<FormationFlight>() != null ? PatternMover.FlightTempo * GameRules.enemySpeed : 1));
        }
        return this;
    }

    public PulseLaser Sustain()
    {
        sustained = true;
        lifetime = float.PositiveInfinity;
        return this;
    }

    public void Cease()
    {
        sustained = false;
        activeTime = Mathf.Max(0, age - warning);
        age = Mathf.Max(age, warning);
        lifetime = age + Cooldown + .1f;
        if (!startedSound)
        {
            startedSound = true;
            sound.Stop();
        }
        if (ports != null)
            ports.Hide();
        sideShots = false;
    }

    protected virtual bool Active => age >= warning && (sustained || age < warning + activeTime);
    protected override void Tick(float dt)
    {
        if (!startedSound)
        {
            startedSound = true;
            sound.Charge(warning / clockRate, false, celestial);
        }
        if (attached)
        {
            if (emitter == null || !emitter.gameObject.activeInHierarchy || (owner != null && owner.health <= 0))
            {
                attached = false;
                Cease();
            }
            else
                transform.position = emitter.TransformPoint(muzzle);
        }
        if (!sustained && age > warning + activeTime + Cooldown)
        {
            Release();
            return;
        }
        if (Active)
        {
            float turn = sweep * dt;
            if (reversalArc > 0)
            {
                float half = reversalArc * .5f, rate = Mathf.Abs(sweep) / half, t = Mathf.Max(0, age - warning);
                turn = Mathf.Sign(sweep) * half * (Mathf.Cos(Mathf.Max(0, t - dt) * rate) - Mathf.Cos(t * rate));
            }
            heading = CombatVisuals.Rotate(heading, turn);
        }
        float power = Power;
        if (owner is DistrictEnemy ship)
            ship.PointWeapon(transform.position, heading);
        sound.Drive(power, Active);
        ribbon.Siege = celestial;
        float visibleLength = CombatWalls.BeamLength(transform.position, heading, length, width * power * .5f);
        ribbon.Draw(transform.position, heading, visibleLength, width, Charge, power, Mathf.Max(0, age - warning - activeTime) / Cooldown);
        var player = CombatVisuals.Player;
        if (Active && power > .15f && player != null)
        {
            // Use the actual cockpit collider, including its offset and Shrinker, with no extra halo.
            float radius = width * power * .5f * ProjectilePixelContact.HitboxScale;
            if (BeamContact.Touches(player.GetComponent<Collider2D>(), transform.position,
                transform.position + (Vector3)heading * visibleLength, radius, out _))
                player.SendMessage("ReceiveHazardDamage", damage, SendMessageOptions.DontRequireReceiver);
        }
        if (sideShots)
        {
            if (ports == null)
            {
                ports = gameObject.AddComponent<LaserSidePorts>();
                ports.Build();
            }
            if (nextSide == 0)
                nextSide = warning;
            var layout = LaserSidePorts.VisibleLayout(transform.position, heading, visibleLength);
            bool cue = age >= nextSide - .32f && age < warning + activeTime;
            ports.Draw(transform.position, heading, width, layout, Mathf.InverseLerp(nextSide - .32f, nextSide, age), cue);
            if (Active && age >= nextSide && Live.Count + layout.Count * 2 <= Limit)
            {
                nextSide = age + .45f;
                var side = new Vector2(-heading.y, heading.x);
                for (int i = 1; i <= layout.Count; i++)
                    for (int sign = -1; sign <= 1; sign += 2)
                        Launch<LaserSideRound>(LaserSidePorts.Origin(transform.position, heading, width, layout, i, sign), side * sign, 2.4f, .12f);
            }
        }
    }

    public override void Vanish()
    {
        Release();
    }
}
