using UnityEngine;
using UnityEngine.Rendering;

// The same independent animated flame mesh/material as the player, never baked into the hull.
public sealed class EnemyThrusters : MonoBehaviour
{
    public Vector3[] sockets;
    public Transform visualRoot;
    Transform[] flames;
    Vector3 previous;
    float phase;
    public void Extinguish()
    {
        if (flames != null)
            foreach (var flame in flames)
                if (flame != null)
                    flame.gameObject.SetActive(false);
        enabled = false;
    }

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        var source = player != null ? player.GetComponent<PSXShipEffects>() : null;
        if (source == null || source.flameMesh == null || source.exhaustMaterial == null)
            return;
        flames = new Transform[sockets.Length];
        previous = transform.position;
        phase = Random.value * 10;
        for (int i = 0; i < sockets.Length; i++)
        {
            var go = new GameObject("Animated engine exhaust " + i);
            go.transform.SetParent(visualRoot != null ? visualRoot : transform, false);
            go.transform.localPosition = sockets[i];
            go.transform.localRotation = Quaternion.Euler(0, 0, 180);
            go.AddComponent<MeshFilter>().sharedMesh = source.flameMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = source.exhaustMaterial;
            r.sortingLayerName = "Enemy";
            r.shadowCastingMode = ShadowCastingMode.Off;
            flames[i] = go.transform;
        }
    }

    void LateUpdate()
    {
        if (flames == null)
            return;
        float thrust = Time.deltaTime > 0 ? Vector3.Distance(previous, transform.position) / Time.deltaTime : 0;
        previous = transform.position;
        for (int i = 0; i < flames.Length; i++)
        {
            float flutter = .85f + .2f * Mathf.Sin(Mathf.Floor(Time.time * 24) * 2.3f + phase + i);
            flames[i].localScale = new Vector3(1.3f, (1.2f + Mathf.Min(thrust * .12f, 1.4f)) * flutter, 1);
        }
    }
}
