using System.Collections.Generic;
using UnityEngine;

// Doppelganger: a mirror image of the ship flies on the opposite side of the screen, matching
// every move and bank in reverse, and fires with the pilot. It cannot be hit.
[UnityEngine.Scripting.Preserve]
public sealed class Doppelganger : AItem
{
    void Awake()
    {
        SetData("Doppelganger", "Mirror, mirror", "A mirror image of your ship flies opposite you and fires with you.", ItemType.partner);
    }

    public override void OnBegin()
    {
    }

    public override void OnPickup()
    {
        MirrorTwin.Attach(s);
        base.OnPickup();
    }
}

public sealed class MirrorTwin : ACompanion
{
    const float Power = .6f, Spread = 6;
    static readonly Color Reflection = new Color(.72f, .66f, 1f);
    readonly List<(Transform source, Transform copy, MeshRenderer renderer)> parts = new List<(Transform, Transform, MeshRenderer)>();
    Shipcontroller owner;
    MaterialPropertyBlock tint;
    Transform reflection;
    float shotClock;
    int index;

    public static void Attach(Shipcontroller ship)
    {
        var go = new GameObject("Mirror twin");
        go.transform.SetParent(ship.transform, false);
        var twin = go.AddComponent<MirrorTwin>();
        twin.owner = ship;
        twin.type = CompanionType.sides;
        twin.index = ship.GetComponentsInChildren<MirrorTwin>().Length - 1;
        twin.tint = new MaterialPropertyBlock();
        twin.tint.SetColor("_Tint", Reflection);
        ItemSpark.Emit(twin.Mirrored(ship.transform.position), Reflection, 10, .12f);
    }

    // Reflected across the arena's centre line; extra twins sit a little lower each.
    Vector3 Mirrored(Vector3 p) => new Vector3(-p.x, p.y - index * .45f, p.z);

    // The reflected parts live under their own unscaled root, so world poses copy across exactly.
    void Build()
    {
        reflection = new GameObject("Mirror twin reflection").transform;
        var visual = owner.GetComponent<PSXShipVisual>();
        var source = visual != null ? visual.Model : owner.transform;
        foreach (var mesh in source.GetComponentsInChildren<MeshFilter>())
        {
            var renderer = mesh.GetComponent<MeshRenderer>();
            if (mesh.sharedMesh == null || renderer == null || !renderer.enabled)
                continue;
            var part = new GameObject(mesh.name + " reflection");
            part.transform.SetParent(reflection, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
            var copy = part.AddComponent<MeshRenderer>();
            copy.sharedMaterials = renderer.sharedMaterials;
            copy.sortingLayerID = renderer.sortingLayerID;
            copy.sortingOrder = renderer.sortingOrder - 1;
            copy.SetPropertyBlock(tint);
            parts.Add((mesh.transform, part.transform, copy));
        }
    }

    void LateUpdate()
    {
        if (owner == null || owner.health <= 0)
        {
            Destroy(gameObject);
            return;
        }
        if (reflection != null)
            reflection.gameObject.SetActive(owner.gameObject.activeInHierarchy);
        if (parts.Count == 0)
            Build();
        var ship = owner.transform.position;
        var root = Mirrored(ship);
        root.x = Mathf.Clamp(root.x, Camera.LeftLimit, Camera.RightLimit);
        transform.position = root;
        // Each part copies the ship's pose reflected across the vertical axis: x flips, and the
        // rotation's y and z components change sign. A negative x scale keeps the faces right.
        foreach (var (source, copy, _) in parts)
        {
            if (source == null)
                continue;
            var offset = source.position - ship;
            copy.position = root + new Vector3(-offset.x, offset.y, offset.z);
            var q = source.rotation;
            copy.rotation = new Quaternion(q.x, -q.y, -q.z, q.w);
            var scale = source.lossyScale;
            copy.localScale = new Vector3(-scale.x, scale.y, scale.z);
        }
        Fire();
    }

    void OnDestroy()
    {
        if (reflection != null)
            Destroy(reflection.gameObject);
    }

    void Fire()
    {
        float dt = GameClock.ActivePlay ? GameClock.PlayerDelta : 0;
        shotClock -= dt;
        if (dt <= 0 || shotClock > 0 || !Shipcontroller.Firing)
            return;
        shotClock = 1 / Mathf.Max(1, GameRules.playerShootSpeed);
        int count = Mathf.Max(1, GameRules.playerShootNumber);
        var muzzle = transform.position + Vector3.up * .35f;
        for (int i = 0; i < count; i++)
        {
            var heading = CombatVisuals.Rotate(Vector2.up, (i - (count - 1) * .5f) * Spread);
            var shot = PlayerRound.Spawn(muzzle, heading, GameRules.playerShootDamage * Power, GameRules.playerShootTravelSpeed, PlayerDelivery.Pulse);
            if (shot != null)
                shot.companion = true;
        }
        ItemSound.Cue("shotCompanion", volume: .035f, cooldown: .1f);
    }
}
