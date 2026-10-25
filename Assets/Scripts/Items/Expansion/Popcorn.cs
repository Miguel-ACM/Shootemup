using UnityEngine;
[UnityEngine.Scripting.Preserve]
public sealed class Popcorn : AItem
{
    void Awake(){SetData("Popcorn","Freshly popped","Every fifth hit pops into three pellets. Burning enemies pop immediately.",ItemType.passive);}
    public override void OnBegin(){}
    public override void OnPickup(){if(s.GetComponent<PopcornHits>()==null)s.gameObject.AddComponent<PopcornHits>();base.OnPickup();}
}
public sealed class PopcornHits : MonoBehaviour
{
    int hits;
    float next;
    public void Hit(AEnemy enemy,float damage)
    {
        hits++;
        bool burning=enemy.GetComponent<BurningHull>()!=null;
        if(hits<5 && !burning)return;
        // A beam or shotgun must not multiply a single impact into an unbounded storm.
        if(Time.time<next)return;hits=0;next=Time.time+.12f;
        ItemSound.Play("popcorn",.16f,1,.1f);
        ItemSpark.Emit(enemy.transform.position,new Color(.95f,.77f,.4f),5,.13f);
        for(int i=0;i<3;i++)
        {
            var shot=PlayerRound.Spawn(enemy.transform.position,CombatVisuals.Rotate(Vector2.up,(i-1)*65),damage*.35f,8,PlayerDelivery.Pulse,1);
            if(shot==null)continue;shot.ExcludePreviousHit(enemy);
            var sr=shot.GetComponent<SpriteRenderer>();sr.sprite=ProjectileSprites.Load("standard");
            shot.transform.localScale=Vector3.one*.2f;
            shot.gameObject.AddComponent<ShortLivedRound>().duration=.48f;
        }
    }
}
