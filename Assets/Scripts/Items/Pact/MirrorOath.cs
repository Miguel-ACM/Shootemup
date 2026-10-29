// A recharging mirror turns one touching round back on the enemy.
[UnityEngine.Scripting.Preserve]
public sealed class MirrorOath : APactItem
{
    void Awake()
    {
        SetPact("Mirror Oath", "Every " + OathMirror.RechargeSeconds + "s, reflect the next bullet that hits you.", "-1 maximum hit.");
    }

    protected override void Seal()
    {
        s.ReduceMaxHits(1);
        if (s.GetComponent<OathMirror>() == null)
            s.gameObject.AddComponent<OathMirror>();
    }
}
