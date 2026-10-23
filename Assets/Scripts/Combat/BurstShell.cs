using UnityEngine;
public sealed class BurstShell : CombatRound
{
    LineRenderer fuse;
    protected override void Initialize(){visual.color=Color.white;if(fuse==null)fuse=CombatVisuals.Line(transform,Color.yellow,.035f);fuse.enabled=true;}
    protected override void Tick(float dt)
    {
        transform.position+=(Vector3)heading*speed*Mathf.Clamp01(1-age/1.25f)*dt;
        float charge=Mathf.Clamp01((age-1.25f)/.8f);
        visual.color=Color.white;
        CombatVisuals.Ring(fuse,transform.position,transform.localScale.x*(.8f-.3f*charge));
        if(age>=2.05f){Ring(12,3.6f,.14f,Vector2.SignedAngle(Vector2.down,heading));Release();}
    }
}
