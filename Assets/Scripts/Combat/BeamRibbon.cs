using UnityEngine;

// Continuous energy with a visible capacitor, ignition expansion and cooling tail.
// The dangerous body stays straight; decorative motion never changes its collision path.
public sealed class BeamRibbon : MonoBehaviour
{
    static Material energy;
    LineRenderer body, sheath, tell, collar, intake;
    SpriteRenderer mouth;
    Color hot, cool;
    public void Build(bool friendly=false)
    {
        hot=friendly ? new Color(.25f,.79f,.76f) : new Color(1,.43f,.12f);
        cool=friendly ? new Color(.12f,.54f,.62f) : new Color(.9f,.22f,.07f);
        if(energy==null)energy=new Material(Shader.Find("Shootemup/Continuous Beam")){name="Continuous plasma"};
        sheath=CombatVisuals.Line(transform,cool,.5f);sheath.sharedMaterial=energy;sheath.sortingOrder=19;
        body=CombatVisuals.Line(transform,hot,.2f);body.sharedMaterial=energy;body.sortingOrder=21;
        tell=CombatVisuals.Line(transform,hot,.035f);tell.sortingOrder=20;
        collar=CombatVisuals.Line(transform,hot,.035f);collar.sortingOrder=22;
        intake=CombatVisuals.Line(transform,hot,.025f);intake.sortingOrder=22;
        var go=new GameObject("Emitter plasma");go.transform.SetParent(transform,false);
        mouth=go.AddComponent<SpriteRenderer>();mouth.sprite=CombatVisuals.Projectile(false,true,true);
        mouth.sharedMaterial=CombatVisuals.ProjectileMaterial;mouth.sortingLayerName="Bullet";mouth.sortingOrder=23;
    }
    public void Draw(Vector3 p,Vector2 direction,float length,float width,float charge,float power,float release=0)
    {
        p.z=-1.05f;
        var end=p+(Vector3)direction*length;
        float flutter=.96f+.04f*Mathf.Sin(Time.time*53);
        bool charging=charge<1 && release<=0;
        tell.enabled=charging;
        if(charging)
        {
            tell.SetPosition(0,p);tell.SetPosition(1,end);
            tell.startColor=tell.endColor=new Color(hot.r,hot.g,hot.b,.46f+charge*.30f);
            tell.startWidth=tell.endWidth=.060f+charge*.015f;
        }
        body.enabled=sheath.enabled=power>.003f;
        float extent=charging ? 0 : Mathf.Clamp01(power*8+release)*length;
        body.SetPosition(0,p);body.SetPosition(1,p+(Vector3)direction*extent);
        sheath.SetPosition(0,p);sheath.SetPosition(1,p+(Vector3)direction*extent);
        body.startWidth=body.endWidth=width*power*flutter;
        body.startColor=body.endColor=new Color(hot.r,hot.g,hot.b,Mathf.Min(1,power*1.8f));
        sheath.startWidth=sheath.endWidth=width*(1.6f+power*.8f);
        sheath.startColor=sheath.endColor=new Color(cool.r,cool.g,cool.b,power*.18f);
        float heat=charging ? charge*.65f : power;
        mouth.enabled=heat>.015f;
        mouth.transform.position=p;mouth.transform.localScale=Vector3.one*(.10f+width*heat*2.1f);
        mouth.color=new Color(hot.r,Mathf.Lerp(hot.g,.85f,heat),Mathf.Lerp(hot.b,.62f,heat),heat*.85f);
        collar.enabled=intake.enabled=heat>.01f;
        CombatVisuals.Ring(collar,p,.08f+width*.7f*heat);
        collar.startColor=collar.endColor=new Color(hot.r,hot.g,hot.b,heat*.7f);
        CombatVisuals.Ring(intake,p,charging ? Mathf.Lerp(.42f,.12f,charge) : .12f+width*.8f);
        intake.startColor=intake.endColor=new Color(hot.r,hot.g,hot.b,charging ? Mathf.Sin(charge*Mathf.PI)*.5f : power*.16f);
    }
}
