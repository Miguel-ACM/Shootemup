using UnityEngine;

// Shared sprites at actual weapon lips. No per-round lights or allocations.
public sealed class EnemyFiringCue : MonoBehaviour
{
    sealed class Port
    {
        public Transform anchor;
        public Vector3 local;
        public SpriteRenderer sprite;
        public Transform lip;
        public Vector2 direction = Vector2.down, releaseAxis;
        public float heat, flash;
    }
    readonly System.Collections.Generic.List<Port> ports = new System.Collections.Generic.List<Port>(24);
    Transform hull;
    public int PortCount => ports.Count;
    public void Build(Transform model)
    {
        foreach (var port in ports) { if (port.sprite != null) Destroy(port.sprite.gameObject); if (port.lip != null) Destroy(port.lip.gameObject); }
        ports.Clear();
        hull = model;
        foreach (var p in EnemyWeaponLayout.Ports(GetComponent<DistrictEnemy>().hull))
            Register(model, p);
    }

    public void Register(Transform anchor, Vector3 local)
    {
        var go = new GameObject("Weapon charge and release");
        go.transform.SetParent(transform, false);
        var light = go.AddComponent<SpriteRenderer>();
        light.sprite = CombatVisuals.Disc;
        light.sharedMaterial = CombatVisuals.ProjectileMaterial;
        light.sortingLayerName = "Bullet";
        light.sortingOrder = 20;
        light.enabled = false;
        var lip = new GameObject("Articulated weapon lip");
        lip.transform.SetParent(anchor, false);
        lip.transform.localPosition = local;
        lip.AddComponent<MeshFilter>().sharedMesh = IndustrialMeshFactory.WeaponLip();
        var renderer = lip.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = anchor.GetComponent<MeshRenderer>()?.sharedMaterial ?? CombatMaterials.Fleet;
        renderer.sortingLayerName = "Enemy";
        ports.Add(new Port { anchor = anchor, local = local, sprite = light, lip = lip.transform });
    }

    static float Heat(float seconds) => seconds >= 0 && seconds < .48f ? 1 - seconds / .48f : 0;
    public void Warn(float seconds)
    {
        foreach (var port in ports) port.heat = Heat(seconds);
    }
    public void ClearWarning() { foreach (var port in ports) port.heat = 0; }
    public void WarnAt(Vector3 position, float seconds)
    {
        var port = Nearest(position);
        if (port != null) port.heat = Mathf.Max(port.heat, Heat(seconds));
    }
    public void PointAll(Vector2 direction) { foreach (var port in ports) port.direction = direction; }
    public void PointAt(Vector3 position, Vector2 direction) { var port = Nearest(position); if (port != null) port.direction = direction; }
    Port Nearest(Vector3 position)
    {
        Port nearest = null;
        float distance = float.PositiveInfinity;
        foreach (var port in ports)
        {
            if (port.anchor == null) continue;
            float d = (port.anchor.TransformPoint(port.local) - position).sqrMagnitude;
            if (d < distance) { distance = d; nearest = port; }
        }
        return nearest;
    }
    public Vector3 Primary => ports.Count > 0 && ports[0].anchor != null ? ports[0].anchor.TransformPoint(ports[0].local) : hull.position;
    public void Flash(Vector3 position, Vector2 direction)
    {
        var port = Nearest(position);
        if (port != null) { port.flash = .12f; port.releaseAxis += direction; }
    }

    void LateUpdate()
    {
        foreach (var port in ports)
        {
            if (port.lip != null && port.anchor != null)
            {
                if (port.releaseAxis.sqrMagnitude > .001f) port.direction = port.releaseAxis.normalized;
                port.releaseAxis = Vector2.zero;
                var target = Quaternion.FromToRotation(Vector3.down, port.direction);
                port.lip.rotation = Quaternion.RotateTowards(port.lip.rotation, target, Time.deltaTime * 540);
            }
            port.flash = Mathf.Max(0, port.flash - Time.deltaTime);
            bool show = !GameRules.playerDisabled && port.anchor != null && port.anchor.gameObject.activeInHierarchy && (port.heat > 0 || port.flash > 0);
            port.sprite.enabled = show;
            if (!show) continue;
            port.sprite.transform.position = port.anchor.TransformPoint(port.local) + Vector3.back * .08f;
            float release = port.flash / .12f;
            float size = release > 0 ? .08f + .20f * release : .055f + .15f * port.heat * port.heat;
            port.sprite.transform.localScale = Vector3.one * size;
            var color = Color.Lerp(LevelProjectilePalette.Accent(LevelRun.Current), Color.white, release * .22f);
            color.a = release > 0 ? release * .9f : port.heat * .75f;
            port.sprite.color = color;
        }
    }
}

public static partial class IndustrialMeshFactory
{
    static Mesh weaponLip;
    public static Mesh WeaponLip()
    {
        if (weaponLip != null) return weaponLip;
        var b = new Builder();
        b.Box(new Vector3(0,.105f,0),new Vector3(.105f,.21f,.11f),6,11);
        b.Box(new Vector3(0,.015f,-.015f),new Vector3(.135f,.035f,.13f),11,6);
        return weaponLip = b.Finish("Swiveling barrel lip");
    }
}
