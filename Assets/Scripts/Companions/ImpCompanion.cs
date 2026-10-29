using UnityEngine;

// The Devil Imp pact's familiar: circles the ship and snaps needles at enemies that come close.
public sealed class ImpCompanion : MonoBehaviour
{
    const float Orbit = 1.15f, Reach = 5.5f, Interval = .32f;
    Transform model;
    float angle, nextShot;

    public static void Attach(Shipcontroller ship)
    {
        new GameObject("Devil imp").AddComponent<ImpCompanion>().transform.SetParent(ship.transform, false);
    }

    void Start()
    {
        var go = new GameObject("Imp model");
        model = go.transform;
        model.SetParent(transform, false);
        model.localScale = Vector3.one * .42f;
        go.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.ExpansionItem("DevilImp");
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = EverydayItemMaterials.For("DevilImp");
        r.sortingLayerName = "Player";
    }

    void Update()
    {
        if (model == null)
            return;
        angle += WorldTimeStop.PlayerDelta * 2.4f;
        // A slightly squashed orbit with a bob, so it reads as a creature rather than a satellite.
        model.localPosition = new Vector3(Mathf.Cos(angle) * Orbit, Mathf.Sin(angle) * Orbit * .8f + Mathf.Sin(angle * 3) * .08f, -.1f);
        model.localRotation = Quaternion.Euler(0, Mathf.Sin(angle * 2) * 25, 0);
        if (GameRules.playerDisabled || Time.time < nextShot)
            return;
        var target = CombatUpgrades.Nearest(model.position, null, Reach);
        if (target == null)
            return;
        nextShot = Time.time + Interval;
        var shot = PlayerRound.Spawn(model.position, ((Vector2)(target.transform.position - model.position)).normalized, GameRules.playerShootDamage * .9f, 14, PlayerDelivery.Needle);
        if (shot != null)
            shot.companion = true;
    }

    void OnDestroy()
    {
        if (model != null)
            Destroy(model.gameObject);
    }
}
