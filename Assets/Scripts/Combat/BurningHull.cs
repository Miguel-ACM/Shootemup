using UnityEngine;
public sealed class BurningHull : MonoBehaviour
{
    float expires, nextTick, power;
    AEnemy enemy;
    public void Ignite(float amount){enemy=GetComponent<AEnemy>();power=Mathf.Min(12,power+amount*.35f);expires=Time.time+2.5f;}
    void Update()
    {
        if(Time.time>expires){Destroy(this);return;}
        if(enemy!=null && enemy.health>0 && Time.time>=nextTick)
        {nextTick=Time.time+.4f;enemy.damage(power*.4f);PSXImpactEffects.Vanish(transform.position+Vector3.up*.4f,new Color(1,.4f,.1f),.18f);}
    }
}
