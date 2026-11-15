using UnityEngine;

// ELITE HUNT target: four red brackets lock onto the marked elite and slowly turn. When the ship
// is destroyed the hunt counts a catch; if it leaves the arena alive, the hunt is lost.
public sealed class HuntMark : MonoBehaviour
{
    static readonly Color Red = new Color(1, .28f, .22f);
    WaveOrchestrer owner;
    AEnemy enemy;
    Transform brackets;
    float radius, spin;
    bool resolved;

    public static HuntMark Attach(DistrictEnemy target, WaveOrchestrer owner)
    {
        var mark = target.gameObject.AddComponent<HuntMark>();
        mark.owner = owner;
        mark.enemy = target;
        mark.radius = Mathf.Max(.55f, target.hullScale * 1.55f);
        return mark;
    }

    void Start()
    {
        brackets = new GameObject("Hunt brackets").transform;
        brackets.SetParent(transform, false);
        for (int i = 0; i < 4; i++)
        {
            var corner = CombatVisuals.Line(brackets, Red, .06f);
            corner.name = "Hunt bracket " + i;
            corner.useWorldSpace = false;
            corner.sortingLayerName = "Enemy";
            corner.sortingOrder = 3;
            corner.positionCount = 3;
            float a = i * 90 + 45;
            var tip = (Vector3)CombatVisuals.Rotate(Vector2.right, a) * radius;
            var along = (Vector3)CombatVisuals.Rotate(Vector2.right, a + 90) * radius * .38f;
            var inward = -tip.normalized * radius * .38f;
            corner.SetPosition(0, tip + inward + new Vector3(0, 0, -.2f));
            corner.SetPosition(1, tip + new Vector3(0, 0, -.2f));
            corner.SetPosition(2, tip + along + new Vector3(0, 0, -.2f));
        }
    }

    void LateUpdate()
    {
        if (brackets == null)
            return;
        // The brackets breathe in and out and turn steadily, whatever the hull does.
        spin += Time.deltaTime * 40;
        float breathe = 1 + .1f * Mathf.Sin(Time.time * 6);
        brackets.rotation = Quaternion.Euler(0, 0, spin);
        brackets.localScale = Vector3.one * breathe;
    }

    void OnDestroy()
    {
        // The hull may already be torn down: read its health as a plain field, not via Unity's null.
        if (resolved || owner == null || ReferenceEquals(enemy, null))
            return;
        resolved = true;
        owner.HuntResolved(this, enemy.health <= 0);
    }
}
