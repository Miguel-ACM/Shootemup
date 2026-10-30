using UnityEngine;
public sealed class SupportDrone : MonoBehaviour
{
    Transform model;
    float nextShot, phase;
    void Start()
    {
        phase = Random.value * Mathf.PI * 2;
        var go = new GameObject("Linked support drone");
        model = go.transform;
        model.SetParent(transform, false);
        model.localScale = Vector3.one * .22f;
        go.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.CombatHull(4);
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = CombatMaterials.Fleet;
        r.sortingLayerName = "Player";
        var exhaust = go.AddComponent<EnemyThrusters>();
        exhaust.sockets = IndustrialMeshFactory.CombatEngineSockets(4);
    }

    void Update()
    {
        if (model == null)
            return;
        model.localPosition = new Vector3(Mathf.Cos(Time.time * 1.3f + phase) * 1.25f, -.55f, -.1f);
        model.localRotation = Quaternion.Euler(0, 0, 180);
        if (GameRules.playerDisabled || !Shipcontroller.Firing || Time.time < nextShot)
            return;
        nextShot = Time.time + .55f;
        var target = CombatUpgrades.Nearest(model.position, null, 12);
        if (target == null)
            return;
        int count = Mathf.Min(3, GameRules.playerShootNumber - 1);
        for (int i = 0; i < count; i++)
        {
            var b = PlayerRound.Spawn(model.position, CombatVisuals.Rotate(((Vector2)(target.transform.position - model.position)).normalized, (i - (count - 1) * .5f) * 9), GameRules.playerShootDamage * .55f, 12);
            if (b != null)
                b.companion = true;
        }
    }

    void OnDestroy()
    {
        if (model != null)
            Destroy(model.gameObject);
    }
}
