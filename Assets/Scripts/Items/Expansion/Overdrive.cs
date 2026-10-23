using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class Overdrive : AActiveItem
{
    void Awake(){SetData("Overdrive","Active","For six seconds, double projectile damage. Costs 30 energy.",ItemType.active);cost=30;cd=10;}
    public override float OnPress(float mana)
    {
        float valid=CheckValidPress(mana);if(valid<0)return valid;
        var boost=s.GetComponent<OverdriveBoost>();if(boost==null)boost=s.gameObject.AddComponent<OverdriveBoost>();
        boost.until=Time.time+6;lastCD=0;return mana-cost;
    }
}
public sealed class OverdriveBoost : MonoBehaviour
{
    public float until;
    void Awake(){GameRules.playerShootDamage*=2;}
    void Update(){if(Time.time>until)Destroy(this);}
    void OnDestroy(){GameRules.playerShootDamage*=.5f;}
}
