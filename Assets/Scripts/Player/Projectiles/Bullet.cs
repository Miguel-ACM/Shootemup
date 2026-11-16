using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : ABullet
{
    Rigidbody2D rigidBody;
    bool rubberReturned;
    bool hitResolved;
    Vector2 wallFrom;
    float flight, weaveSign, travelled, pumpCapacity;
    Vector3 restScale;
    // Shared by all bullets so the scene is searched once, not on every trigger contact.
    static Shipcontroller player;

    void Start()
    {
        ProjectileSpriteTint.Friendly(gameObject);
        transform.localScale *= ShotTraits.SizeScale;
        restScale = transform.localScale;
        pumpCapacity = ShotTraits.PumpCapacity;
        direction.z = transform.position.z;
        rigidBody = GetComponent<Rigidbody2D>();
        wallFrom = transform.position;
        weaveSign = ShotTraits.NextWeaveSign();
        Move();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.deltaTime <= 0 || WorldTimeStop.Frozen)
            return;
        flight += Time.deltaTime;
        if (CombatWalls.Sweep(wallFrom, transform.position, .04f, out _, out _))
        {
            Despawn();
            return;
        }
        wallFrom = transform.position;
        if (!rubberReturned && transform.up.y > 0 && RubberReturn.AtTop(transform.position))
        {
            rubberReturned = true;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, new Vector3(transform.up.x, -Mathf.Abs(transform.up.y), 0));
        }
        if (followness != 0 && !rubberReturned)
        {
            var marked = CombatTargets.MarkedInRange(transform.position, 18);
            var e = marked ?? CombatUpgrades.Nearest(transform.position, null, Mathf.Sqrt(Mathf.Abs(followness) * 10));
            if (e != null)
            {
                float sqrDistance = ((Vector2)(e.transform.position - transform.position)).sqrMagnitude;
                int factor = 1;
                if (followness < 0)
                    factor = -1;

                if (e == marked || sqrDistance < Mathf.Abs(followness) * 10)
                {
                    Vector3 d = (e.transform.position - transform.position).normalized;
                    //d.x = Mathf.Cos(d.x);
                    //d.y = Mathf.Sin(d.y);
                    //rigidBody.velocity = Vector3.MoveTowards(transform.position, e.transform.position, 100);//new Vector3(d.x * speed, d.x * speed, direction.z);
                    Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, Vector3.Scale(d, new Vector3(factor, 1, 1)));
                    targetRotation = Utils.ClampRotation(targetRotation, new Vector3(0.1f, 1f, 0));
                    Quaternion nextRotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * followness * 10 * factor);
                    transform.rotation = nextRotation;
                    //new Vector3(transform.up.x * speed, transform.forward.y * speed, direction.z);
                    //Quaternion.RotateTowards(transform.rotation, neededRotation, Time.deltaTime * 10f);

                }
            }

        }
        Vector2 forward = transform.up.normalized;
        float pace = speed * ShotTraits.SpeedScale(flight);
        if (pumpCapacity > 0)
        {
            float progress = ShotTraits.PumpProgress(flight);
            float scale = 1 + pumpCapacity * progress;
            transform.localScale = new Vector3(restScale.x * scale, restScale.y * scale, restScale.z);
            pace *= ShotTraits.PumpSpeed(progress);
        }
        travelled += pace * Time.deltaTime;
        rigidBody.linearVelocity = forward * pace + new Vector2(forward.y, -forward.x) * ShotTraits.WeaveRate(flight, weaveSign);
    }


    void Move()
    {
        rigidBody.linearVelocity = direction.normalized * speed;//new Vector3(direction.x * speed, direction.y * speed, direction.z);
        transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // A moving multipart hull can queue several contacts before Destroy runs.
        if (hitResolved) return;
        if (CombatWalls.Sweep(wallFrom, transform.position, .04f, out _, out _)) { Despawn(); return; }
        if (collision.CompareTag("Enemy"))
        {
            AEnemy en = collision.gameObject.GetComponent<AEnemy>();
            if (en == null || !en.CanReceiveDamage)
                return;
            hitResolved = true;
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player").GetComponent<Shipcontroller>();
            player.PlayHitSound("hit1");
            CombatHit.Apply(en, damage * ShotTraits.ReachDamage(travelled), collision.ClosestPoint(transform.position),
                rigidBody != null ? rigidBody.linearVelocity : (Vector2)transform.up, companion: companionShot);
            bool ball = CombatUpgrades.Current != null && CombatUpgrades.Current.Count(UpgradeKind.BouncingBall) > 0;
            int bounces = CombatUpgrades.Current == null ? 0 : ball ? 1 : CombatUpgrades.Current.TryRebound(transform.position);
            if (bounces > 0)
            {
                var next = CombatUpgrades.Nearest(transform.position, new HashSet<AEnemy> { en }, 12);
                if (next != null)
                {
                    float power = ball ? damage : damage * .7f;
                    for (int i = 0; i < bounces; i++)
                    {
                        var aim = CombatVisuals.Rotate((next.transform.position - transform.position).normalized, i * 30);
                        var bounce = PlayerRound.Spawn(transform.position, aim, power, speed, PlayerDelivery.Pulse, 1);
                        if (bounce != null)
                        {
                            bounce.companion = companionShot;
                            bounce.ExcludePreviousHit(en);
                        }
                    }
                }
            }
            Destroy(gameObject);
        }
    }

    void OnBecameInvisible()
    {
        Destroy(gameObject);
    }
}
