using UnityEngine;
using System.Collections.Generic;

public enum SecondaryPattern { Paperclip, Firefly, Salt, Mortar, Teeth, Ribbon, Sewing, Flash, Vents }

// Slotless option fire. Every pickup owns one removable battery; neither weapon
// slot, the primary gun's volley counter nor active energy is consumed.
public sealed class SecondaryShot : AWeapon
{
    static readonly List<SecondaryShot> batteries = new List<SecondaryShot>();
    static float nextSound;
    public SecondaryPattern Pattern { get; private set; }
    public int Releases { get; private set; }
    bool NoseMounted => Pattern == SecondaryPattern.Teeth || Pattern == SecondaryPattern.Sewing;
    float clock;
    int side = -1;
    Transform option;
    ShipLoadout loadout;
    void OnEnable() { batteries.Add(this); }
    void OnDisable()
    {
        batteries.Remove(this);
        ClearFlash();
    }
    public float ReleasesPerSecond => Pattern switch {
        SecondaryPattern.Paperclip => 1.65f,
        SecondaryPattern.Firefly => 1.2f,
        SecondaryPattern.Salt => 2.2f,
        SecondaryPattern.Mortar => .65f,
        SecondaryPattern.Teeth => 7f,
        SecondaryPattern.Ribbon => 5f,
        SecondaryPattern.Flash => 2.4f,
        SecondaryPattern.Vents => 2.6f,
        _ => 7f
    };

    public static SecondaryShot Attach(Shipcontroller ship, SecondaryPattern pattern, string item)
    {
        var root = new GameObject(item + " secondary fire");
        root.transform.SetParent(ship.transform, false);
        var scale = ship.transform.lossyScale;
        root.transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
        var battery = root.AddComponent<SecondaryShot>();
        battery.Pattern = pattern;
        battery.SetWeaponInput(WeaponInput.both);
        battery.loadout = ship.GetComponent<ShipLoadout>();
        battery.option = new GameObject(item + " option").transform;
        battery.option.SetParent(root.transform, false);
        battery.option.localScale = Vector3.one * .22f;
        battery.option.gameObject.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.ExpansionItem(item);
        var renderer = battery.option.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = EverydayItemMaterials.For(item);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (pattern == SecondaryPattern.Flash)
        {
            var pulse = new GameObject("Flash tube laser");
            pulse.transform.SetParent(root.transform, false);
            battery.flash = pulse.AddComponent<BeamRibbon>();
            battery.flash.Build(true);
            pulse.SetActive(false);
        }
        return battery;
    }

    void Update()
    {
        if (!GameClock.ActivePlay)
        {
            if (!Menu.GameplayPaused) ClearFlash();
            return;
        }
        if (Time.timeScale <= 0 && !WorldTimeStop.PlayerCanAct)
            return;
        if (flashRemaining > 0)
            UpdateFlash();
        var input = GameInput.Snapshot;
        if (!input.Primary && !input.Secondary)
            return;
        if (loadout != null && loadout.ConsumedInputThisFrame)
            return;
        clock -= GameClock.PlayerDelta * Mathf.Clamp(GameRules.playerShootSpeed / 10f, .1f, 3f) * MagnifyingLens.Rate;
        if (clock > 0) return;
        clock = Mathf.Max(0, clock + 1 / ReleasesPerSecond);
        Release(input.Focus);
    }

    void LateUpdate()
    {
        if (option == null) return;
        // Copies share one visual, but keep their own fire and trade ownership.
        int mask = 0, rank = 0;
        bool first = true;
        foreach (var battery in batteries)
        {
            if (battery.transform.parent != transform.parent) continue;
            if (battery == this) break;
            if (battery.Pattern == Pattern) first = false;
            else if (!battery.NoseMounted) mask |= 1 << (int)battery.Pattern;
        }
        for (int i = 0; i <= (int)SecondaryPattern.Vents; i++)
            if ((mask & 1 << i) != 0) rank++;
        option.gameObject.SetActive(first);
        bool focus = GameInput.Snapshot.Focus;
        int flank = rank % 2 == 0 ? -1 : 1;
        Vector3 offset = NoseMounted ? new Vector3(0, Pattern == SecondaryPattern.Sewing ? .02f : .30f, -1.2f) :
            new Vector3(flank * (focus ? .34f : .65f + rank / 2 * .12f),
            .1f - rank / 2 * .20f, -1.2f);
        option.localPosition = Vector3.Lerp(option.localPosition, offset, 1 - Mathf.Exp(-GameClock.PlayerDelta * 16));
        option.localRotation = Quaternion.Euler(-18, Time.time * 28, 8);
    }

    void Release(bool focus)
    {
        Releases++;
        side = -side;
        Vector3 origin = option != null ? option.position : transform.position;
        origin += Vector3.up * .14f;
        // Centre-mounted batteries share the ship's nose, independent of the flanking options.
        if (NoseMounted) origin = transform.position + Vector3.up * .42f;
        Vector2 forward = focus ? ((Vector2)(transform.position + Vector3.up * 5 - origin)).normalized : Vector2.up;
        Vector2 rear = focus ? ((Vector2)(transform.position + Vector3.down * 4 - origin)).normalized : Vector2.down;
        switch (Pattern)
        {
            case SecondaryPattern.Paperclip:
                Fire(origin, CombatVisuals.Rotate(forward, side * (focus ? 2 : 13)), .55f, 1.15f, PlayerDelivery.Needle);
                break;
            case SecondaryPattern.Firefly:
                Fire(origin, CombatVisuals.Rotate(Vector2.up, side * 18), .5f, .65f, PlayerDelivery.Missile);
                break;
            case SecondaryPattern.Salt:
                for (int i = -1; i <= 1; i++)
                    Fire(origin, CombatVisuals.Rotate(rear, i * (focus ? 6 : 25)), .18f, .85f, PlayerDelivery.Pulse);
                break;
            case SecondaryPattern.Mortar:
                Fire(origin, forward, 1.25f, 1, PlayerDelivery.Shell);
                break;
            case SecondaryPattern.Teeth:
                for (int i = -2; i <= 2; i++)
                    Fire(origin, CombatVisuals.Rotate(Vector2.up, i * (focus ? 3 : 7) + (focus ? 0 : side * 2)),
                        .34f, 1.35f, PlayerDelivery.Pulse);
                break;
            case SecondaryPattern.Ribbon:
                Fire(origin, CombatVisuals.Rotate(forward, Mathf.Sin(Releases * .8f) * (focus ? 4 : 24)), .22f, 1, PlayerDelivery.Pulse);
                break;
            case SecondaryPattern.Sewing:
                Fire(origin, CombatVisuals.Rotate(forward, side * (focus ? 1 : 7)), .16f, 1.3f, PlayerDelivery.Needle);
                break;
            case SecondaryPattern.Flash:
                StartFlash(origin);
                return;
            case SecondaryPattern.Vents:
                for (int flank = -1; flank <= 1; flank += 2)
                {
                    Vector3 port = transform.position + new Vector3(flank * .42f, .12f);
                    Fire(port, Vector2.right * flank, .48f, .9f, PlayerDelivery.Pulse, lockHeading: true);
                }
                break;
        }
        // Reuse the approved, quiet shot cue, shared throttle for many batteries.
        if (Time.unscaledTime >= nextSound)
        {
            nextSound = Time.unscaledTime + .15f;
            SoundManager.PlaySound("shotSecondary", .025f);
        }
    }

    const float FlashDuration = .14f, FlashReach = 6.2f;
    BeamRibbon flash;
    Vector3 flashOrigin;
    float flashRemaining, flashLength, flashWidth;

    void StartFlash(Vector3 origin)
    {
        flashOrigin = origin;
        flashWidth = .11f * Mathf.Sqrt(Mathf.Max(.01f, GameRules.playerShootSize));
        flashLength = CombatWalls.BeamLength(origin, Vector2.up, FlashReach, flashWidth * .5f);
        Vector2 end = (Vector2)origin + Vector2.up * flashLength;
        var plasma = PlasmaOrb.FirstOnBeam(origin, end, flashWidth * .5f, out var impact);
        flashLength = Mathf.Clamp(impact.y - origin.y, 0, flashLength);
        AEnemy hit = null;
        foreach (var enemy in AEnemy.Live)
        {
            if (enemy == null || !enemy.CanBeTargeted ||
                !BeamContact.Touches(enemy, origin, (Vector2)origin + Vector2.up * flashLength, flashWidth * .5f, out var point))
                continue;
            hit = enemy;
            impact = point;
            flashLength = Mathf.Max(0, point.y - origin.y);
        }
        float damage = .92f * GameRules.playerShootDamage * MagnifyingLens.Power;
        if (hit != null)
            CombatHit.Apply(hit, damage, impact, Vector2.up, generation: 1, energy: true);
        else if (plasma != null)
            plasma.Hit(damage);
        flashRemaining = FlashDuration;
        flash.gameObject.SetActive(true);
        flash.Draw(flashOrigin, Vector2.up, flashLength, flashWidth, 1, .81f);
        ItemSound.Cue("shotSecondary", volume: .023f, pitch: 1.7f, cooldown: .13f);
    }

    void UpdateFlash()
    {
        flashRemaining -= GameClock.PlayerDelta;
        if (flashRemaining <= 0)
        {
            ClearFlash();
            return;
        }
        float progress = flashRemaining / FlashDuration;
        flash.Draw(flashOrigin, Vector2.up, flashLength, flashWidth, 1, .81f * progress, 1 - progress);
    }

    void ClearFlash()
    {
        flashRemaining = 0;
        if (flash != null) flash.gameObject.SetActive(false);
    }

    static void Fire(Vector3 origin, Vector2 direction, float power, float speed, PlayerDelivery delivery, bool lockHeading = false)
    {
        // Leave ample native pool capacity for main weapons, companions and procs.
        if (PlayerRound.LiveCount >= PlayerRound.Limit - 50) return;
        var round = PlayerRound.Spawn(origin, direction, GameRules.playerShootDamage * power,
            GameRules.playerShootTravelSpeed * speed, delivery, 1);
        if (round == null) return;
        round.returns = false;
        if (lockHeading) round.LockHeading();
        round.LimitLifetime(delivery == PlayerDelivery.Shell ? 1.1f : 2.4f);
        MagnifyingLens.Enlarge(round);
    }
}
