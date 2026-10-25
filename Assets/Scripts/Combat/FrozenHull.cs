using UnityEngine;

public sealed class FrozenHull : MonoBehaviour
{
    public int Frost {get;private set;}
    public bool Frozen=>Frost>=3 && Time.time<expires;
    public static bool Holds(GameObject go){var ice=go.GetComponent<FrozenHull>();return ice!=null && ice.Frozen && !(ice.enemy is DistrictBoss);}
    AEnemy enemy;
    readonly SpriteRenderer[] crystals=new SpriteRenderer[3];
    float expires,next;
    void Awake(){enemy=GetComponent<AEnemy>();}
    public float Hit(float damage)
    {
        if(Frozen)
        {
            Clear();next=Time.time+.6f;ItemSound.Play("shatter",.2f);
            ItemSpark.Emit(transform.position,new Color(.5f,.8f,.95f),7,.22f,false,"needle");
            for(int i=0;i<5;i++)
            {
                var shot=PlayerRound.Spawn(transform.position,CombatVisuals.Rotate(Vector2.up,i*72),damage*.25f,9,PlayerDelivery.Needle,1);
                if(shot==null)continue;shot.ExcludePreviousHit(enemy);shot.pierces=0;
                shot.GetComponent<SpriteRenderer>().sprite=ProjectileSprites.Load("needle");
                shot.transform.localScale*=.65f;shot.gameObject.AddComponent<ShortLivedRound>().duration=.55f;
            }
            return damage*1.5f;
        }
        if(Time.time<next)return damage;next=Time.time+.12f;
        Frost=Mathf.Min(3,Frost+1);expires=Time.time+(Frost==3 ? 1.2f : 3);
        for(int i=0;i<Frost;i++)
        {
            if(crystals[i]==null)
            {
                crystals[i]=ItemSpark.Sprite(transform,"Hull frost","needle",new Color(.45f,.8f,.95f),.32f);
                float angle=(i*120+30)*Mathf.Deg2Rad;
                crystals[i].transform.localPosition=new Vector3(Mathf.Cos(angle)*enemy.size*.6f,Mathf.Sin(angle)*enemy.size*.6f,-2);
                crystals[i].transform.localRotation=Quaternion.Euler(0,0,i*120-60);
            }
            crystals[i].enabled=true;
        }
        if(Frost==3)ItemSound.Play("frost",.16f);return damage;
    }
    void Update(){if(Time.deltaTime>0 && Time.time>=expires)Clear();}
    void Clear(){Frost=0;foreach(var crystal in crystals)if(crystal!=null)crystal.enabled=false;}
    void OnDestroy(){foreach(var crystal in crystals)if(crystal!=null)Destroy(crystal.gameObject);}
}
