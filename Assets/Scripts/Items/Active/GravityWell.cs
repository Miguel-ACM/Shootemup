using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class GravityWell : AActiveItem
{
    void Awake()
    {
        SetData("Gravity Well", "Active", "Freeze and erase nearby bullets. 25 energy.", ItemType.active);
        cost = 25;
        cd = 7;
    }

    public override float OnPress(float mana)
    {
        float valid = CheckValidPress(mana);
        if (valid < 0)
            return valid;
        var go = new GameObject("Gravity well");
        go.transform.position = s.transform.position + Vector3.up * 2;
        go.AddComponent<GravityField>();
        lastCD = 0;
        return mana - cost;
    }
}
public sealed class GravityField : MonoBehaviour
{
    float age;
    LineRenderer ring;
    void Start()
    {
        ring = CombatVisuals.Line(transform, new Color(.6f, .5f, 1, .65f), .08f);
    }

    void Update()
    {
        age += Time.deltaTime;
        CombatVisuals.Ring(ring, transform.position, 2.8f);
        foreach (var shot in new System.Collections.Generic.List<CombatRound>(CombatRound.Live))
        {
            if (shot == null || shot is PulseLaser || Vector2.Distance(shot.transform.position, transform.position) > 2.8f)
                continue;
            shot.transform.position = Vector3.MoveTowards(shot.transform.position, transform.position, Time.deltaTime * 6);
            if (Vector2.Distance(shot.transform.position, transform.position) < .5f || age > 2.4f)
            {
                CombatUpgrades.Current?.Block();
                shot.Vanish();
            }
        }
        if (age > 2.5f)
            Destroy(gameObject);
    }
}
