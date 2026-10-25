using UnityEngine;
public sealed class BurningHull : MonoBehaviour
{
    float expires, nextTick, power;
    AEnemy enemy;
    HullAfflictionVfx effect;
    public void Ignite(float amount){enemy=GetComponent<AEnemy>();power=Mathf.Min(12,power+amount*.35f);expires=Time.time+2.5f;if(effect==null)effect=new HullAfflictionVfx(enemy,false);}
    void Update()
    {
        if(Time.time>expires){Destroy(this);return;}
        effect?.Tick();
        if(enemy!=null && enemy.health>0 && Time.time>=nextTick)
        {nextTick=Time.time+.4f;enemy.damage(power*.4f);}
    }
    void OnDestroy(){effect?.Dispose();}
}
