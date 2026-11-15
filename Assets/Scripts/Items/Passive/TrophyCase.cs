using System.Collections.Generic;
using UnityEngine;

// Trophy Case: destroy an elite and its last volley goes in the case. While the pilot fires,
// the ship replays its trophies in turn as friendly fire, turned to face up the screen: the
// elite's fan, ring or spread, now yours. The case holds the three latest trophies.
[UnityEngine.Scripting.Preserve]
public sealed class TrophyCase : AItem
{
    void Awake()
    {
        SetData("Trophy Case", "Mounted and polished", "Destroy an elite to take its volley as your own.", ItemType.passive);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        s.GetComponent<CombatUpgrades>().Add(UpgradeKind.TrophyCase);
        if (s.GetComponent<TrophyShelf>() == null)
            s.gameObject.AddComponent<TrophyShelf>();
        base.OnPickup();
    }
}

// Watches which rounds each living elite launches (rounds carry no owner, so a newly launched
// round is credited to the elite it appears beside), keeps each elite's latest volley, and
// fires the claimed volleys back from the ship.
public sealed class TrophyShelf : MonoBehaviour
{
    const int Shelves = 3, MaxShots = 12;
    const float Beside = 1.6f, VolleyGap = .3f, Interval = 1.4f, Power = .8f, ShotSpeed = 11;
    static readonly Color Gold = new Color(1, .82f, .3f);
    static TrophyShelf current;
    readonly Dictionary<AEnemy, (List<float> angles, float last)> volleys = new Dictionary<AEnemy, (List<float>, float)>();
    readonly List<float[]> trophies = new List<float[]>();
    readonly List<AEnemy> elites = new List<AEnemy>();
    readonly List<AEnemy> stale = new List<AEnemy>();
    int lastSerial;
    float clock;
    int next;
    bool primed;

    void Start()
    {
        current = this;
    }

    void OnDestroy()
    {
        if (current == this)
            current = null;
    }

    void LateUpdate()
    {
        Watch();
        Replay();
    }

    void Watch()
    {
        elites.Clear();
        foreach (var enemy in AEnemy.Live)
            if (enemy is DistrictEnemy ship && ship.Elite && enemy.CanBeTargeted)
                elites.Add(enemy);
        // Launch serials only grow: anything above the last one seen was launched since.
        int newest = lastSerial;
        foreach (var round in CombatRound.Live)
        {
            if (primed && round.Serial <= lastSerial)
                continue;
            newest = Mathf.Max(newest, round.Serial);
            // The first frame only learns where the launch count stands.
            if (primed && elites.Count > 0 && BulletReflection.CanReflect(round))
                Credit(round);
        }
        lastSerial = newest;
        primed = true;
        if (volleys.Count > 24)
        {
            stale.Clear();
            foreach (var pair in volleys)
                if (pair.Key == null)
                    stale.Add(pair.Key);
            foreach (var key in stale)
                volleys.Remove(key);
        }
    }

    void Credit(CombatRound round)
    {
        Vector2 at = round.transform.position;
        AEnemy owner = null;
        float nearest = Beside * Beside;
        foreach (var elite in elites)
        {
            float distance = ((Vector2)elite.transform.position - at).sqrMagnitude;
            if (distance < nearest)
            {
                nearest = distance;
                owner = elite;
            }
        }
        if (owner == null)
            return;
        // Rounds more than a moment apart start a new volley.
        if (!volleys.TryGetValue(owner, out var volley) || Time.time - volley.last > VolleyGap)
            volley = (new List<float>(), 0);
        volley.angles.Add(Mathf.Atan2(round.Heading.y, round.Heading.x) * Mathf.Rad2Deg);
        volley.last = Time.time;
        volleys[owner] = volley;
    }

    /// <summary>Called for every kill: an elite with a recorded volley becomes a trophy.</summary>
    public static void Claim(AEnemy enemy)
    {
        if (current == null || !(enemy is DistrictEnemy ship) || !ship.Elite || !current.volleys.TryGetValue(enemy, out var volley))
            return;
        current.volleys.Remove(enemy);
        var angles = volley.angles;
        if (angles.Count == 0)
            return;
        // Sample a big volley evenly, and reflect it so a downward fan now points up.
        int count = Mathf.Min(MaxShots, angles.Count);
        var trophy = new float[count];
        for (int i = 0; i < count; i++)
            trophy[i] = -angles[i * angles.Count / count];
        current.trophies.Add(trophy);
        if (current.trophies.Count > Shelves)
            current.trophies.RemoveAt(0);
        RunInventory.Ping(UpgradeKind.TrophyCase);
        ItemProcVfx.Emit(ItemProcShape.Convert, enemy.transform.position, Gold, .7f);
        ItemSpark.Emit(enemy.transform.position, Gold, 10, .12f);
        ItemSpark.Emit(current.transform.position, Gold, 6, .1f);
        ItemSound.Cue("ready", volume: .12f, pitch: 1.2f);
    }

    void Replay()
    {
        float dt = GameClock.ActivePlay ? GameClock.PlayerDelta : 0;
        clock -= dt;
        if (trophies.Count == 0 || dt <= 0 || clock > 0 || !Shipcontroller.Firing)
            return;
        clock = Interval;
        var trophy = trophies[next++ % trophies.Count];
        var muzzle = transform.position + Vector3.up * .35f;
        foreach (float angle in trophy)
        {
            var heading = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            PlayerRound.Spawn(muzzle, heading, GameRules.playerShootDamage * Power, ShotSpeed, PlayerDelivery.Pulse, 1);
        }
        ItemSpark.Emit(muzzle, Gold, 3, .08f);
        ItemSound.Cue("shotCompanion", volume: .05f, pitch: .85f, cooldown: .1f);
    }
}
