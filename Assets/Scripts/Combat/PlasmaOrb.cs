using UnityEngine;
public sealed class PlasmaOrb : CombatRound
{
    float health;
    LineRenderer outline;
    protected override void Initialize()
    {
        // The Bullet layer intentionally ignores itself. Destructible shells must accept player rounds.
        gameObject.layer=0;
        health=12;visual.sprite=CombatVisuals.Projectile(false,true,true);visual.color=Color.white;
        if(outline==null)outline=CombatVisuals.Line(transform,Color.white,.05f);
        CombatVisuals.Ring(outline,transform.position,transform.localScale.x*.6f);
    }
    protected override void Tick(float dt){base.Tick(dt);CombatVisuals.Ring(outline,transform.position,transform.localScale.x*.6f);}
    public void Hit(float amount){health-=amount;if(health<=0)Vanish();}
    void OnTriggerEnter2D(Collider2D other)
    {
        var shot=other.GetComponent<ABullet>();if(shot==null)return;
        Hit(shot.damage);Destroy(shot.gameObject);
    }
}
