using UnityEngine;
using System.Collections.Generic;

// Store hit damage, never route a pop through DirectHit again (no recursive procs).
public sealed class StickyGum : MonoBehaviour
{
    sealed class Blob {public float damage,age;public Transform model;}
    readonly List<Blob> blobs=new List<Blob>();
    AEnemy enemy;
    public int Count=>blobs.Count;
    public static float Stick(AEnemy enemy,float damage)
    {
        if(damage<=0)return damage;
        var gum=enemy.GetComponent<StickyGum>() ?? enemy.gameObject.AddComponent<StickyGum>();
        gum.enemy=enemy;
        if(gum.blobs.Count>=12){gum.blobs[0].damage+=damage;return 0;}
        var go=new GameObject("Swelling gum");go.transform.SetParent(enemy.transform,false);
        float angle=gum.blobs.Count*2.4f;
        go.transform.localPosition=new Vector3(Mathf.Cos(angle)*enemy.size*.4f,Mathf.Sin(angle)*enemy.size*.4f,-.65f);
        var r=go.AddComponent<SpriteRenderer>();r.sprite=ProjectileSprites.Load("standard");
        ProjectileSpriteTint.Apply(go,new Color(.95f,.35f,.65f));r.sortingOrder=24;
        go.transform.localScale=Vector3.one*.13f;go.transform.rotation=Quaternion.identity;
        gum.blobs.Add(new Blob{damage=damage,model=go.transform});return 0;
    }
    void Update()
    {
        if(Time.deltaTime<=0 || WorldTimeStop.Frozen || GameRules.playerDisabled)return;
        for(int i=blobs.Count-1;i>=0;i--)
        {
            var blob=blobs[i];blob.age+=Time.deltaTime;
            blob.model.localScale=Vector3.one*Mathf.Lerp(.13f,.39f,blob.age/.65f);
            blob.model.rotation=Quaternion.identity;
            if(blob.age<.65f)continue;
            GumSpritePop.Spawn(blob.model.position);
            if(enemy!=null && enemy.CanBeTargeted){enemy.damage(blob.damage);Score.IncreaseScore(Mathf.RoundToInt(blob.damage));}
            Destroy(blob.model.gameObject);blobs.RemoveAt(i);
        }
        if(blobs.Count==0)Destroy(this);
    }
}
