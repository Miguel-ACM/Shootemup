using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives a model part from the shooter(s) it represents so firing reads correctly:
/// Aim turrets track the player (and snap to each shot's direction), Spin emitters turn while
/// spraying circular patterns, Fixed parts only flash. Every real shot throws muzzle sparks
/// from the part and kicks it back briefly. Enemy bullets report their spawn point through
/// NotifyShot; the visual whose shooter sits at that point reacts.
/// </summary>
public sealed class PSXShooterVisual : MonoBehaviour
{
    public enum Mode { Aim, Spin, Fixed }
    public Mode mode;
    /// <summary>Shooter transforms this part represents (bullets spawn at their positions).</summary>
    public Transform[] sources;
    /// <summary>World distance from the pivot to the muzzle along the shot direction (also scales the recoil).</summary>
    public float reach=.4f;
    static readonly List<PSXShooterVisual> active=new List<PSXShooterVisual>();
    Quaternion rest;
    Vector3 restPosition;
    float heading,spinSpeed,kick,lastShot=-10,sparkReady;
    Transform player;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){active.Clear();}

    void Awake(){rest=transform.localRotation;restPosition=transform.localPosition;}
    void OnEnable(){active.Add(this);}
    void OnDisable(){active.Remove(this);}

    public static void NotifyShot(Vector3 position,Vector3 direction)
    {
        PSXShooterVisual best=null;float bestDistance=.2f;
        foreach(var visual in active)
            if(visual.sources!=null)
                foreach(var source in visual.sources)
                {
                    if(source==null)continue;
                    float d=Vector2.Distance(source.position,position);
                    if(d<bestDistance){bestDistance=d;best=visual;}
                }
        if(best!=null)best.Shot(direction);
    }

    void Shot(Vector3 direction)
    {
        lastShot=Time.time;
        var dir=((Vector2)direction).normalized;if(dir.sqrMagnitude<.01f)dir=Vector2.down;
        if(mode==Mode.Aim){heading=Vector2.SignedAngle(Vector2.down,dir);kick=1;}
        if(mode==Mode.Fixed)kick=1;
        // Circular volleys fire many rounds at once; throttle sparks so each volley reads as a ring of flashes.
        if(mode==Mode.Spin && Time.time<sparkReady)return;
        if(mode==Mode.Spin)sparkReady=Time.time+.02f;
        PSXImpactEffects.Muzzle(transform.position+(Vector3)(dir*reach),dir);
    }

    void LateUpdate()
    {
        if(mode==Mode.Aim)
        {
            if(player==null){var go=GameObject.FindGameObjectWithTag("Player");if(go!=null)player=go.transform;}
            // Between shots, track the player at a limited, stepped turn rate.
            if(player!=null && Time.time-lastShot>.25f)
            {
                float target=Vector2.SignedAngle(Vector2.down,player.position-transform.position);
                heading=Mathf.MoveTowardsAngle(heading,target,180*Time.deltaTime);
            }
            transform.rotation=Quaternion.AngleAxis(Mathf.Round(heading/3)*3,Vector3.forward)*ParentRest();
        }
        else if(mode==Mode.Spin)
        {
            bool firing=Time.time-lastShot<.6f;
            spinSpeed=Mathf.MoveTowards(spinSpeed,firing ? 420 : 30,(firing ? 900 : 240)*Time.deltaTime);
            heading+=spinSpeed*Time.deltaTime;
            transform.rotation=Quaternion.AngleAxis(Mathf.Round(heading/5)*5,Vector3.forward)*ParentRest();
        }
        kick=Mathf.MoveTowards(kick,0,Time.deltaTime*10);
        // Recoil: push back opposite the barrel (world space) for a few frames.
        var back=Quaternion.AngleAxis(mode==Mode.Aim ? heading : 0,Vector3.forward)*Vector3.up*kick*reach*.25f;
        transform.localPosition=restPosition+(transform.parent!=null ? transform.parent.InverseTransformVector(back) : back);
    }

    Quaternion ParentRest()=>(transform.parent!=null ? transform.parent.rotation : Quaternion.identity)*rest;
}
