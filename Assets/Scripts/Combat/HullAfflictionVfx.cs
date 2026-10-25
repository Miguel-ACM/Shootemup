using UnityEngine;

// Small local emissions cling to the hull, leaving the collision lanes readable.
public sealed class HullAfflictionVfx
{
    readonly ParticleSystem particles;
    readonly AEnemy enemy;
    readonly bool poison;
    float next;
    public HullAfflictionVfx(AEnemy target,bool toxic)
    {
        enemy=target;poison=toxic;
        particles=PSXImpactEffects.Create(target.transform,toxic ? "Poison bubbles" : "Burning hull flames",48,toxic);
        if(toxic){var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sortingLayerName="Bullet";renderer.sortingOrder=29;}
        var fade=particles.colorOverLifetime;fade.enabled=true;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.9f,0),new GradientAlphaKey(0,1)});fade.color=gradient;
    }
    public void Tick()
    {
        if(particles==null || enemy==null || Time.deltaTime<=0 || Time.time<next)return;
        next=Time.time+.08f;
        float radius=Mathf.Clamp(enemy.size*.65f,.2f,.85f);
        for(int i=0;i<3;i++)
        {
            var offset=Random.insideUnitCircle*radius;
            var e=new ParticleSystem.EmitParams();
            e.position=enemy.transform.position+new Vector3(offset.x,offset.y,-2);
            var toxic=PlayerShotPalette.Poison;toxic.a=.8f;
            e.startColor=poison ? toxic : Color.Lerp(new Color(.85f,.08f,.02f),new Color(1,.55f,.08f),Random.value);
            e.startSize3D=poison ? Vector3.one*Random.Range(.08f,.15f) : new Vector3(.08f,.23f,.08f);
            e.velocity=new Vector3(Random.Range(-.15f,.15f),poison ? .35f : .9f,0);e.startLifetime=poison ? .65f : .38f;
            particles.Emit(e,1);
        }
    }
    public void Dispose(){if(particles!=null)Object.Destroy(particles.gameObject);}
}
