using UnityEngine;
using System.Collections.Generic;

public sealed class TrailDrone : ACompanion
{
    Shipcontroller owner;
    TrailDrone leader;
    int index, count;
    Vector3 worldPosition;
    bool laser;
    float clock = -.8f, nextDamage;
    BeamRibbon beam;
    LaserSound sound;
    PSXShipEffects effects;
    public bool Laser => laser;
    public static TrailDrone Attach(Shipcontroller ship, bool laser)
    {
        var siblings = ship.GetComponentsInChildren<TrailDrone>();
        var go = new GameObject(laser ? "Trailing laser drone" : "Trailing gun drone");
        go.transform.SetParent(ship.transform, false);
        go.transform.position = ship.transform.position;
        var drone = go.AddComponent<TrailDrone>();
        drone.owner = ship;
        drone.laser = laser;
        drone.index = siblings.Length;
        drone.leader = siblings.Length > 0 ? siblings[siblings.Length - 1] : null;
        drone.count = siblings.Length + 1;
        foreach (var sibling in siblings)
            sibling.count = drone.count;
        drone.worldPosition = ship.transform.position;
        drone.type = CompanionType.follower;
        // Scale models only; damage, rounds and spacing stay constant.
        float scale = .7f * Mathf.Max(.6f, Mathf.Pow(.9f, drone.index));
        go.transform.localScale = Vector3.one * (scale / ship.transform.lossyScale.x);
        go.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.ExpansionItem(laser ? "LaserDroneItem" : "BulletDroneItem");
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CombatMaterials.Fleet;
        renderer.sortingLayerName = "Player";
        var source = ship.GetComponent<PSXShipEffects>();
        if (source != null)
        {
            drone.effects = go.AddComponent<PSXShipEffects>();
            drone.effects.flameMesh = source.flameMesh;
            drone.effects.exhaustMaterial = source.exhaustMaterial;
            drone.effects.exhaustOffset = new Vector2(.22f, -.27f);
            drone.effects.exhaustScale = .5f;
        }
        if (laser)
        {
            var ribbon = new GameObject("Drone beam");
            ribbon.transform.SetParent(go.transform, false);
            drone.beam = ribbon.AddComponent<BeamRibbon>();
            drone.beam.Build(true);
            ribbon.SetActive(false);
            drone.sound = go.AddComponent<LaserSound>();
        }
        drone.clock = -.4f - drone.index * .18f;
        return drone;
    }

    Vector3 Muzzle => transform.TransformPoint(new Vector3(0, .44f, -.1f));
    void LateUpdate()
    {
        if (owner == null || owner.health <= 0)
        {
            Destroy(gameObject);
            return;
        }
        // Ownership stays with the ship, but positions have an independent world-space history.
        // This also prevents parent motion from dragging a frozen drone through stopped time.
        if (WorldTimeStop.Frozen || Time.deltaTime <= 0)
        {
            transform.position = worldPosition;
            return;
        }
        float stagger = index % 2 == 0 ? .19f : -.19f;
        var camera = UnityEngine.Camera.main;
        float bottom = camera != null ? camera.transform.position.y - camera.orthographicSize : Camera.DownLimit;
        float halfWidth = camera != null ? camera.orthographicSize * 405f / 720f : Camera.RightLimit;
        float center = camera != null ? camera.transform.position.x : 0;
        float spacing = Mathf.Min(.65f, Mathf.Max(0, owner.transform.position.y - bottom - .4f) / (count + 1));
        float inward = owner.transform.position.x > 0 ? -1 : 1;
        // Near the bottom the tail bends inward instead of disappearing below the visible arena.
        var offset = new Vector3(stagger * spacing / .65f + inward * (.65f - spacing) * .8f, -spacing, -.02f);
        Vector3 target = (leader != null ? leader.worldPosition : owner.transform.position) + offset;
        target.x = Mathf.Clamp(target.x, center - halfWidth + .35f, center + halfWidth - .35f);
        target.y = Mathf.Max(bottom + .4f, target.y);
        // Laser drones hold a rigid row straight behind the ship instead of trailing its path:
        // one sits centred, two sit a little left and right, more spread evenly.
        if (laser)
        {
            int slot = 0, lasers = 0;
            foreach (var drone in owner.GetComponentsInChildren<TrailDrone>())
                if (drone.laser)
                {
                    if (drone == this)
                        slot = lasers;
                    lasers++;
                }
            target = owner.transform.position + new Vector3((slot - (lasers - 1) * .5f) * .55f, -1.05f, -.02f);
            target.y = Mathf.Max(bottom + .4f, target.y);
            worldPosition = target;
        }
        else
            worldPosition = Vector3.Lerp(worldPosition, target, 1 - Mathf.Exp(-12 * Time.deltaTime));
        transform.position = worldPosition;
        if (GameRules.playerDisabled)
        {
            clock = -.4f;
            if (beam != null)
                beam.gameObject.SetActive(false);
            sound?.Stop();
            return;
        }
        if (laser)
            FireLaser();
        else
            FireBullets();
    }

    void FireBullets()
    {
        clock += Time.deltaTime;
        if (clock < 0 || !Shipcontroller.Firing)
            return;
        var target = CombatUpgrades.Nearest(Muzzle, null, 18);
        if (target == null)
        {
            clock = -.15f;
            return;
        }
        clock = -.55f;
        effects?.Shot();
        SoundManager.PlaySound("shot1", .045f);
        var direction = ((Vector2)(target.transform.position - Muzzle)).normalized;
        foreach (int side in new[] { -1, 1 })
        {
            var shot = PlayerRound.Spawn(Muzzle + Vector3.right * side * .09f, direction, GameRules.playerShootDamage * .4f, GameRules.playerShootTravelSpeed);
            if (shot != null)
                shot.companion = true;
        }
    }

    // A continuous beam straight up that ends on the first enemy in its path, with sparks where it lands.
    const float BeamReach = 18, BeamTick = .12f;
    static readonly Color BeamSpark = new Color(.45f, .85f, 1f);
    float beamPower, nextSpark;

    void FireLaser()
    {
        bool wanted = Shipcontroller.Firing && CombatUpgrades.Nearest(Muzzle, null, BeamReach) != null;
        beamPower = Mathf.MoveTowards(beamPower, wanted ? 1 : 0, Time.deltaTime * (wanted ? 3 : 5));
        if (beamPower <= 0)
        {
            if (beam.gameObject.activeSelf)
            {
                beam.gameObject.SetActive(false);
                sound.Stop();
            }
            return;
        }
        if (!beam.gameObject.activeSelf)
        {
            beam.gameObject.SetActive(true);
            sound.Charge(.25f, true);
        }
        var origin = Muzzle;
        float length = BeamReach;
        AEnemy struck = null;
        foreach (var enemy in AEnemy.Live)
        {
            if (enemy == null || !enemy.CanBeTargeted)
                continue;
            var collider = enemy.GetComponent<Collider2D>();
            if (collider == null)
                continue;
            var bounds = collider.bounds;
            if (Mathf.Abs(bounds.center.x - origin.x) > bounds.extents.x + .06f)
                continue;
            float along = bounds.min.y - origin.y;
            if (along < length && bounds.max.y > origin.y)
            {
                length = Mathf.Max(0, along);
                struck = enemy;
            }
        }
        beam.Draw(origin, Vector2.up, length, .12f, 1, beamPower, 0);
        sound.Drive(beamPower, true);
        if (struck == null || beamPower < .9f)
            return;
        var point = (Vector2)origin + Vector2.up * length;
        if (Time.time >= nextSpark)
        {
            // Irregular bursts: varied timing, spread, size and tint so the contact crackles instead of pulsing.
            nextSpark = Time.time + Random.Range(.025f, .09f);
            var jitter = new Vector2(Random.Range(-.08f, .08f), Random.Range(-.05f, .05f));
            var tint = Color.Lerp(BeamSpark, Color.white, Random.value * .6f);
            ItemSpark.Emit(point + jitter, tint, Random.Range(1, 5), Random.Range(.06f, .15f));
        }
        if (Time.time < nextDamage)
            return;
        nextDamage = Time.time + BeamTick;
        float damage = GameRules.playerShootDamage * .5f;
        if (CombatUpgrades.Current != null)
            damage = CombatUpgrades.Current.DirectHit(struck, damage, 0, true);
        struck.HitAt(point, Vector2.up);
        struck.damage(damage);
    }
    void OnDisable()
    {
        sound?.Stop();
    }
}
