using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class Overdrive : AActiveItem
{
    void Awake(){SetData("Overdrive","Active","Double damage for 6 seconds. 30 energy.",ItemType.active);cost=30;cd=10;}
    public override float OnPress(float mana)
    {
        float valid=CheckValidPress(mana);if(valid<0)return valid;
        var boost=s.GetComponent<OverdriveBoost>();if(boost==null)boost=s.gameObject.AddComponent<OverdriveBoost>();
        boost.until=Time.time+6;ItemSound.Play("overdrive",.22f);ItemSpark.Emit(s.transform.position,new Color(1,.55f,.2f),8,.15f,true);lastCD=0;return mana-cost;
    }
}
public sealed class OverdriveBoost : MonoBehaviour
{
    public float until;
    SpriteRenderer left,right;
    void Awake(){GameRules.playerShootDamage*=2;}
    void Start()
    {
        left=ItemSpark.Sprite(transform,"Overdrive port","needle",new Color(1,.55f,.2f),.28f);
        right=ItemSpark.Sprite(transform,"Overdrive starboard","needle",new Color(1,.55f,.2f),.28f);
        left.transform.localPosition=new Vector3(-.38f,-.3f,-2);right.transform.localPosition=new Vector3(.38f,-.3f,-2);
    }
    void Update()
    {
        if(Time.time>until){ItemSound.Play("powerdown",.17f);ItemSpark.Emit(transform.position,new Color(1,.55f,.2f),4,.1f);Destroy(this);return;}
        float a=Mathf.Min(1,until-Time.time)*(.7f+.2f*Mathf.Sin(Time.time*18));
        if(left!=null)left.color=right.color=new Color(1,1,1,a);
    }
    void OnDestroy(){GameRules.playerShootDamage*=.5f;if(left!=null)Destroy(left.gameObject);if(right!=null)Destroy(right.gameObject);}
}
